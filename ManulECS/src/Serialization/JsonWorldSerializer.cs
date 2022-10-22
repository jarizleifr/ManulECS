using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using System.Text;
using System.Collections.Generic;

namespace ManulECS {
  public record struct EntityMapping(uint Old, Entity Created);

  public sealed class JsonWorldSerializer : WorldSerializer {
    public const string RESOURCES = "Resources";
    public const string ENTITIES = "Entities";

    /// <summary>Converts Entities to base ids, and optionally remaps Entity fields to new valid identities.</summary>
    internal sealed class EntityConverter : JsonConverter<Entity> {
      internal EntityMapping[] EntityMapping { get; set; }

      public override Entity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var id = reader.GetUInt32();
        return EntityMapping != null
          ? EntityMapping.SingleOrDefault(e => e.Old == id, new(0u, Entity.NULL_ENTITY)).Created
          : new Entity(id);
      }

      public override void Write(Utf8JsonWriter writer, Entity value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value.Id);
    }

    private readonly Dictionary<string, Type> typeCache = new();
    private readonly EntityConverter converter = new();
    private readonly JsonSerializerOptions options = new() {
      IncludeFields = true,
    };

    public string Namespace { get; init; } = null;
    public string AssemblyName { get; init; } = Assembly.GetEntryAssembly().GetName().Name;

    public JsonWorldSerializer() => options.Converters.Add(converter);

    public string Serialize(World world, string profile = null) {
      using var stream = new MemoryStream();
      Write(stream, world, profile);
      return Encoding.UTF8.GetString(stream.ToArray());
    }

    public void Deserialize(World world, string json) {
      using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
      Read(stream, world);
    }

    public override void Write(Stream stream, World world, string profile = null) {
      using var writer = new Utf8JsonWriter(stream);
      writer.WriteStartObject();

      // Write entities and their components to stream
      writer.WriteStartObject(ENTITIES);
      var reader = GetComponentReader(world, profile);
      while (reader.Read()) {
        if (reader.HasEntityChanged) {
          // Start new entity, close previous if exists
          if (!reader.IsFirst) writer.WriteEndObject();
          writer.WriteStartObject(reader.Entity.Id.ToString());
        }
        WriteSerializer(reader.Component);
      }
      if (!reader.IsFirst) writer.WriteEndObject();
      writer.WriteEndObject();

      // Write resources to stream
      writer.WriteStartObject(RESOURCES);
      foreach (var resource in GetResources(world, profile)) {
        WriteSerializer(resource);
      }
      writer.WriteEndObject();
      writer.WriteEndObject();

      void WriteSerializer(object @object) {
        var type = @object.GetType();
        writer.WritePropertyName(Namespace == null ? type.FullName : type.Name);
        JsonSerializer.Serialize(writer, @object, options);
      }
    }

    public override void Read(Stream stream, World world) {
      using var jsonDocument = JsonDocument.Parse(stream);
      var root = jsonDocument.RootElement;
      // Get entities, remap them with new identities and provide them to the serializer
      var entityEnumerator = root.GetProperty(ENTITIES).EnumerateObject();
      converter.EntityMapping = entityEnumerator
        .Select(e => new EntityMapping(uint.Parse(e.Name), world.Create()))
        .ToArray();

      // Loop through components and assign them to the new remapped Entity 
      var index = 0;
      foreach (var entity in entityEnumerator) {
        var created = converter.EntityMapping[index++].Created;
        foreach (var jsonComponent in entity.Value.EnumerateObject()) {
          try {
            var (type, component) = DeserializeProperty(jsonComponent);
            world.AssignRaw(created, type, component);
          } catch {
            // If errors, just skip the component with a warning.
            Console.WriteLine($"Component failed to deserialize: {jsonComponent}");
          }
        }
      }
      // Get resources and assign them to the world
      foreach (var jsonResource in root.GetProperty(RESOURCES).EnumerateObject()) {
        var (type, resource) = DeserializeProperty(jsonResource);
        world.SetResource(type, resource);
      }
    }

    private (Type, object) DeserializeProperty(JsonProperty property) {
      var name = property.Name;
      // Lock cache so we don't get problems in multithreaded contexts
      lock (typeCache) {
        if (!typeCache.TryGetValue(name, out var type)) {
          var typeName = Namespace == null ? $"{name}, {AssemblyName}" : $"{Namespace}.{name}, {AssemblyName}";
          type = Type.GetType(typeName);
          typeCache.Add(name, type);
        }
        return (type, property.Value.Deserialize(type, options));
      }
    }
  }
}
