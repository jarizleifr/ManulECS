using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static System.Attribute;

namespace ManulECS {
  public enum Omit : byte {
    None,
    Component,
    Entity,
  }

  [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
  public class ECSSerializeAttribute : Attribute {
    public string Profile { get; init; } = null;
    public Omit Omit { get; init; } = Omit.None;
    public ECSSerializeAttribute(Omit omit) => Omit = omit;
    public ECSSerializeAttribute(string profile) => Profile = profile;
  }

  public record struct EntityPair(Entity Old, Entity Created);

  internal sealed class EntityFieldConverter : JsonConverter<Entity> {
    internal EntityPair[] EntityRemap { get; init; }
    public override bool CanWrite => false;
    public override void WriteJson(JsonWriter _w, Entity _v, JsonSerializer _s) { }
    public override Entity ReadJson(JsonReader reader, Type _t, Entity _e, bool _b, JsonSerializer _s) {
      var entity = JToken.Load(reader).ToObject<Entity>();
      foreach (var (Old, Created) in EntityRemap) {
        if (Old == entity) {
          return Created;
        }
      }
      return entity;
    }
  }

  internal sealed class WorldSerializer {
    private const string resourcesName = "resources";
    private const string entitiesName = "entities";
    private const string componentsName = "components";

    private static readonly MethodInfo assignInfo = typeof(World).GetMethod(nameof(World.Assign));
    private static MethodInfo GetAssignMethod(object obj) => assignInfo.MakeGenericMethod(obj.GetType());
    private static readonly MethodInfo tagInfo = typeof(World).GetMethod(nameof(World.Tag));
    private static MethodInfo GetTagMethod(object obj) => tagInfo.MakeGenericMethod(obj.GetType());

    private readonly World world;
    private readonly string profile;
    private readonly JsonSerializer serializer = new() {
      TypeNameHandling = TypeNameHandling.Objects
    };

    internal WorldSerializer(World world, string profile = null) =>
      (this.world, this.profile) = (world, profile);

    internal string Create() {
      var resources = new JArray(
        world.Resources.Where(MatchesProfile).Select(SerializeResource)
      );
      var entities = world.Entities.Aggregate(new JArray(), (acc, cur) => {
        if (TrySerializeEntity(cur, out var serialized)) {
          acc.Add(serialized);
        }
        return acc;
      });

      return new JObject(
        new JProperty(resourcesName, resources),
        new JProperty(entitiesName, entities)
      ).ToString(Formatting.None);

      JObject SerializeResource(object obj) => JObject.FromObject(obj, serializer);

      bool TrySerializeEntity(Entity entity, out JObject serialized) {
        serialized = default;
        var componentArray = new JArray();
        string foundProfile = null;
        foreach (var idx in world.EntityKey(entity)) {
          var component = world.pools.PoolByKeyIndex(idx).Get(entity);
          if (!DiscardComponent(component)) {
            if (DiscardEntity(component)) {
              return false;
            }
            var componentProfile = GetProfile(component);
            if (foundProfile == null) {
              foundProfile = componentProfile;
            } else if (componentProfile != null && foundProfile != componentProfile) {
              throw new Exception("Entity has components belonging to different serialization profiles!");
            }
            componentArray.Add(JObject.FromObject(component, serializer));
          }
        }
        if (profile == foundProfile && componentArray.Any()) {
          serialized = JObject.FromObject(entity);
          serialized.Add(componentsName, componentArray);
          return true;
        }
        return false;

        string GetProfile(object obj) => GetAttribute(obj)?.Profile;
        bool DiscardComponent(object obj) => GetAttribute(obj)?.Omit == Omit.Component;
        bool DiscardEntity(object obj) => GetAttribute(obj)?.Omit == Omit.Entity;
      }
    }

    internal void Apply(string json) {
      var obj = JObject.Parse(json);
      foreach (object resource in obj[resourcesName].Select(DeserializeResource)) {
        world.SetResource(resource.GetType(), resource);
      }
      DeserializeEntities(obj[entitiesName]);

      object DeserializeResource(JToken token) => token.ToObject<object>(serializer);

      void DeserializeEntities(JToken entities) {
        var entityRemap = entities
          .Select(e => new EntityPair(e.ToObject<Entity>(), world.Create()))
          .ToArray();

        var serializer = JsonSerializer.Create(new JsonSerializerSettings() {
          TypeNameHandling = TypeNameHandling.Objects,
          Converters = { new EntityFieldConverter { EntityRemap = entityRemap } }
        });

        for (int i = 0; i < entities.Count(); i++) {
          foreach (var token in entities[i][componentsName]) {
            object component;
            try {
              component = token.ToObject<object>(serializer);
            } catch {
              // If errors, just skip the component
              Console.WriteLine($"Component failed to deserialize: {token}");
              continue;
            }

            var entity = entityRemap[i].Created;
            (MethodInfo method, object[] args) = component is IComponent
              ? (GetAssignMethod(component), new object[] { entity, component })
              : (GetTagMethod(component), new object[] { entity });
            method.Invoke(world, args);
          }
        }
      }
    }

    private bool MatchesProfile(object obj) => GetAttribute(obj)?.Profile == profile;

    private static ECSSerializeAttribute GetAttribute(object obj) =>
      (ECSSerializeAttribute)GetCustomAttribute(obj.GetType(), typeof(ECSSerializeAttribute));
  }
}
