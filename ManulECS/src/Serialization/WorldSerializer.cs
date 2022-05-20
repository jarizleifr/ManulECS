using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

  public record struct SerializedEntity(Entity Entity, List<object> Components);
  public record struct SerializedWorld(List<SerializedEntity> Entities, List<object> Resources);
  public record struct EntityPair(Entity Old, Entity Created);

  ///<summary>Base class for reading and writing World data, to and from streams.</summary>
  public abstract class WorldSerializer {
    private static readonly MethodInfo assignInfo = typeof(World).GetMethod(nameof(World.Assign));
    private static MethodInfo GetAssignMethod(object obj) => assignInfo.MakeGenericMethod(obj.GetType());
    private static readonly MethodInfo tagInfo = typeof(World).GetMethod(nameof(World.Tag));
    private static MethodInfo GetTagMethod(object obj) => tagInfo.MakeGenericMethod(obj.GetType());

    /// <summary>
    /// Current serialization profile. If set, Components and Resources not matching will not be serialized.
    /// </summary>
    public string Profile { get; init; }

    /// <summary>
    /// Reads from stream and assigns its contents to the current World. Override this to provide custom deserialization.
    /// </summary>
    public abstract void Read(Stream stream, World world);

    /// <summary>
    /// Writes content of the World to the stream. Override this to provide custom serialization.
    /// </summary>
    public abstract void Write(Stream stream, World world);

    /// <summary>Returns a list of Entities and their components as C# objects.</summary>
    protected List<SerializedEntity> GetEntities(World world) {
      return world.Entities.Aggregate(new List<SerializedEntity>(), (acc, entity) => {
        var components = new List<object>();
        string foundProfile = null;
        foreach (var idx in world.EntityKey(entity)) {
          var component = world.pools.PoolByKeyIndex(idx).Get(entity);
          if (!DiscardComponent(component)) {
            if (DiscardEntity(component)) {
              return acc;
            }

            var componentProfile = GetAttribute(component)?.Profile;
            if (foundProfile == null) {
              foundProfile = componentProfile;
            } else if (componentProfile != null && foundProfile != componentProfile) {
              throw new Exception("Entity has components belonging to different serialization profiles!");
            }
            components.Add(component);
          }
        }
        if (Profile == foundProfile && components.Any()) {
          acc.Add(new() { Entity = entity, Components = components });
        }
        return acc;
      });

      bool DiscardComponent(object obj) => GetAttribute(obj)?.Omit == Omit.Component;
      bool DiscardEntity(object obj) => GetAttribute(obj)?.Omit == Omit.Entity;
    }

    /// <summary>
    /// Creates a mapping for new identities from a list of previously serialized Entities.
    /// </summary>
    protected EntityPair[] GetRemap(World world, List<Entity> entities) => entities
      .Select(e => new EntityPair(e, world.Create()))
      .ToArray();

    /// <summary>Sets a newly deserialized component on an entity.</summary>
    protected void SetComponent(World world, Entity entity, object component) {
      (MethodInfo method, object[] args) = component is IComponent
        ? (GetAssignMethod(component), new object[] { entity, component })
        : (GetTagMethod(component), new object[] { entity });
      method.Invoke(world, args);
    }

    /// <summary>Gets a list of resources from the current World as C# objects.</summary>
    protected List<object> GetResources(World world) => world.Resources.Where(MatchesProfile).ToList();

    /// <summary>Assigns resources to the current world.</summary>
    protected void SetResources(World world, List<object> resources) {
      foreach (var resource in resources) {
        Console.WriteLine(resource.ToString());
        world.SetResource(resource.GetType(), resource);
      }
    }

    /// <summary>Checks if the object matches the current serialization profile.</summary>
    protected bool MatchesProfile(object obj) => GetAttribute(obj)?.Profile == Profile;

    private static ECSSerializeAttribute GetAttribute(object obj) =>
      (ECSSerializeAttribute)GetCustomAttribute(obj.GetType(), typeof(ECSSerializeAttribute));
  }
}

