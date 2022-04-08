using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ManulECS {
  public class SerializationProfileAttribute : Attribute {
    public readonly string name;
    public SerializationProfileAttribute(string name) => this.name = name;
  }

  public class NeverSerializeComponentAttribute : Attribute { }
  public class NeverSerializeEntityAttribute : Attribute { }

  internal static class WorldSerializer {
    public static bool ResourceProfileMatches(string profile, object res) {
      var resProfile = (SerializationProfileAttribute)Attribute.GetCustomAttribute(res.GetType(), typeof(SerializationProfileAttribute));
      if (resProfile == null) return profile == null;
      return profile != null && profile == resProfile.name;
    }

    public static string Create(World world, string profile) {
      var serializer = new JsonSerializer { TypeNameHandling = TypeNameHandling.Objects };
      var container = new JObject(
          new JProperty("resources",
            new JArray(
              from r in world.resources
              where ResourceProfileMatches(profile, r.Value)
              select JObject.FromObject(r.Value, serializer)
            )
          ),
          new JProperty("entities",
            new JArray(
              from obj in
                from e in world.Entities
                select EntityConverter.SerializeEntity(world, profile, e)
              where obj != null
              select obj
            )
          )
      );
      return container.ToString(Formatting.None);
    }

    public static void Apply(World world, string json) {
      var obj = JObject.Parse(json);
      var serializer = new JsonSerializer { TypeNameHandling = TypeNameHandling.Objects };

      foreach (JObject jsonRes in obj["resources"]) {
        var resource = jsonRes.ToObject<object>(serializer);
        world.SetResource(resource.GetType(), resource);
      }

      (Entity old, Entity created)[] entityRemap = obj["entities"]
          .Select(e => (e.ToObject<Entity>(), world.Create()))
          .ToArray();

      // Use custom Entity field converter to make sure entity references stay valid.
      var entitySerializer = JsonSerializer.Create(new JsonSerializerSettings() {
        TypeNameHandling = TypeNameHandling.Objects,
        Converters = { new EntityFieldConverter(entityRemap) }
      });

      int index = 0;
      foreach (JObject loadedEntity in obj["entities"]) {
        var newEntity = entityRemap[index].created;
        EntityConverter.DeserializeEntity(world, newEntity, loadedEntity["components"], entitySerializer);
        index++;
      }
    }
  }

  public class EntityFieldConverter : JsonConverter<Entity> {
    private readonly (Entity old, Entity created)[] entityRemap;
    public EntityFieldConverter((Entity, Entity)[] entityRemap) => this.entityRemap = entityRemap;

    public override bool CanWrite => false;
    public override void WriteJson(JsonWriter writer, Entity value, JsonSerializer serializer) { }
    public override Entity ReadJson(JsonReader reader, Type _t, Entity _e, bool _b, JsonSerializer _s) =>
      entityRemap.Where(e => e.old == JToken.Load(reader).ToObject<Entity>())
        .Select(e => e.created)
        .Single();
  }

  internal static class EntityConverter {
    private interface IComponentReader {
      void Set(World world, Entity entity, object component);
    }

    private class JsonComponentReader<T> : IComponentReader where T : struct, IComponent {
      public void Set(World world, Entity entity, object component) =>
          world.Assign(entity, (T)component);
    }

    private class JsonTagReader<T> : IComponentReader where T : struct, ITag {
      public void Set(World world, Entity entity, object _) =>
          world.Assign<T>(entity);
    }

    public static JObject SerializeEntity(World world, string profile, Entity entity) {
      bool componentProfilePresent = false;
      var componentArray = new JArray();
      var serializer = new JsonSerializer { TypeNameHandling = TypeNameHandling.Objects };
      foreach (var idx in world.GetEntityDataByIndex(entity.Id)) {
        var component = world.components.GetIndexedPool(idx).Get(entity.Id);
        var type = component.GetType();
        if (DiscardComponent(type)) continue;
        if (DiscardEntity(type)) return null;

        var attr = GetAttribute<SerializationProfileAttribute>(type);
        if (attr != null) {
          if (profile == null) return null;
          if (attr.name != profile) return null;

          componentProfilePresent = true;
        }
        componentArray.Add(JObject.FromObject(component, serializer));
      }
      if (!componentProfilePresent && profile != null) return null;
      if (componentArray.Count == 0) return null;

      var serialized = JObject.FromObject(entity);
      serialized.Add("components", componentArray);

      return serialized;

      bool DiscardComponent(Type type) =>
        GetAttribute<NeverSerializeComponentAttribute>(type) != null;

      bool DiscardEntity(Type type) =>
        GetAttribute<NeverSerializeEntityAttribute>(type) != null;

      T GetAttribute<T>(Type type) where T : Attribute =>
        (T)Attribute.GetCustomAttribute(type, typeof(T));
    }

    public static void DeserializeEntity(World world, Entity newEntity, JToken serializedComponents, JsonSerializer serializer) {
      foreach (JObject jsonComponent in serializedComponents) {
        object component;
        try {
          component = jsonComponent.ToObject<object>(serializer);
        } catch (Exception e) {
          // If errors, just skip the component
          Console.WriteLine(e);
          continue;
        }

        var generic = (component is IComponent)
            ? typeof(JsonComponentReader<>)
            : typeof(JsonTagReader<>);

        var type = generic.MakeGenericType(component.GetType());

        var componentReader = Activator.CreateInstance(type) as IComponentReader;
        componentReader.Set(world, newEntity, component);
      }
    }
  }
}
