using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ManulECS {
  public sealed class JsonWorldSerializer : WorldSerializer {
    /// <summary>
    /// Guarantees that components with Entity fields stay valid after creating new identities to serialized entities.
    /// </summary>
    internal sealed class EntityFieldConverter : JsonConverter<Entity> {
      internal EntityPair[] EntityRemap { get; init; }
      public override bool CanWrite => false;
      public override void WriteJson(JsonWriter writer, Entity value, JsonSerializer serializer) { }
      public override Entity ReadJson(JsonReader reader, Type type, Entity existingValue, bool hasExistingValue, JsonSerializer serializer) {
        var entity = JToken.Load(reader).ToObject<Entity>();
        foreach (var (Old, Created) in EntityRemap) {
          if (Old == entity) {
            return Created;
          }
        }
        return entity;
      }
    }

    private const string resourcesName = nameof(SerializedWorld.Resources);
    private const string entitiesName = nameof(SerializedWorld.Entities);
    private const string entityName = nameof(SerializedEntity.Entity);
    private const string componentsName = nameof(SerializedEntity.Components);

    private JsonSerializer serializer = JsonSerializer.Create(new JsonSerializerSettings() {
      TypeNameHandling = TypeNameHandling.Objects,
    });

    ///<summary>Serializes a World as a Json string.</summary>
    public static string Serialize(World world, string profile = null) {
      var serializer = new JsonWorldSerializer { Profile = profile };
      using var stream = new MemoryStream();
      serializer.Write(stream, world);
      return Encoding.UTF8.GetString(stream.GetBuffer());
    }

    ///<summary>Deserializes a previously serialized World from a Json string.</summary>
    public static void Deserialize(World world, string json) {
      var serializer = new JsonWorldSerializer();
      using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
      serializer.Read(stream, world);
    }

    public override void Write(Stream stream, World world) {
      using var streamWriter = new StreamWriter(stream);
      using var jsonWriter = new JsonTextWriter(streamWriter);
      serializer.Serialize(jsonWriter, new SerializedWorld { Entities = GetEntities(world), Resources = GetResources(world) });
    }

    public override void Read(Stream stream, World world) {
      using var streamReader = new StreamReader(stream);
      using var jsonReader = new JsonTextReader(streamReader);
      var obj = JObject.Load(jsonReader);

      // Get entities, remap them with new identities and provide them to the serializer
      var entities = obj[entitiesName];
      var entityRemap = GetRemap(world, entities.Select(e => e[entityName].ToObject<Entity>()).ToList());
      var entitySerializer = JsonSerializer.Create(new JsonSerializerSettings() {
        TypeNameHandling = TypeNameHandling.Objects,
        Converters = { new EntityFieldConverter { EntityRemap = entityRemap } }
      });

      // Loop through components and assign them to the new remapped Entity 
      for (int i = 0; i < entityRemap.Length; i++) {
        foreach (var token in entities[i][componentsName]) {
          try {
            SetComponent(world, entityRemap[i].Created, token.ToObject<object>(entitySerializer));
          } catch {
            // If errors, just skip the component
            Console.WriteLine($"Component failed to deserialize: {token}");
          }
        }
      }

      // Get resources and assign them to the world
      var resources = obj[resourcesName];
      SetResources(world, resources.Select(token => token.ToObject<object>(serializer)).ToList());
    }
  }
}
