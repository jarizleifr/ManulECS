using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

    public static ECSSerializeAttribute GetAttribute(Type type) =>
      (ECSSerializeAttribute)GetCustomAttribute(type, typeof(ECSSerializeAttribute));
  }

  ///<summary>Base class for reading and writing World data, to and from streams.</summary>
  public abstract class WorldSerializer {
    /// <summary>
    /// Reads from the stream and assigns its contents to the current World.
    /// </summary>
    public abstract void Read(Stream stream, World world);

    /// <summary>
    /// Writes contents of the World to the stream, optionally only with the matching profile.
    /// </summary>
    public abstract void Write(Stream stream, World world, string profile = null);

    /// <summary>Gets a list of resources from the current World as objects.</summary>
    protected IEnumerable<object> GetResources(World world, string profile) => world.resources.Values
      .Where(r => ECSSerializeAttribute.GetAttribute(r.GetType())?.Profile == profile);

    /// <summary>
    /// Returns a reader which iterates through entities and their components. 
    /// </summary>
    protected internal ComponentReader GetComponentReader(World world, string profile = null) =>
      new ComponentReader(world, profile);

    protected internal struct ComponentReader {
      private World world;
      private string profile;
      private int current, previous, componentIndex;

      ///<summary>Gets whether none of the entities have been processed yet.</summary>
      public bool IsFirst => previous == -1;

      ///<summary>Gets if the current Entity has changed since last iteration.</summary>
      public bool HasEntityChanged => current != previous;

      ///<summary>Gets the current Entity</summary>
      public Entity Entity => world[(uint)current];

      ///<summary>Gets the current component from the current Entity.</summary>
      public object Component { get; private set; }

      public ComponentReader(World world, string profile) {
        this.world = world;
        this.profile = profile;
        (current, previous, componentIndex) = (0, -1, -1);
        Component = null;
      }

      ///<summary>Reads the next entity and/or component.</summary>
      ///<returns>true if next component was found, false if reader has reached the end.</returns>
      public bool Read() {
        while (current < world.Capacity) {
          var id = Entity.Id;
          if (!Discard(id)) {
            foreach (var idx in world.EntityKey(id)) {
              // Skip already checked components
              if (idx > componentIndex) {
                var pool = world.pools.PoolByKeyIndex(idx);
                if (pool.Omit != Omit.Component) {
                  // Update previous only if we have actually looped components 
                  if (componentIndex != -1) {
                    previous = current;
                  }
                  componentIndex = idx;
                  Component = pool.Get(id);
                  return true;
                }
              }
            }
          }
          // Update previous only if we have actually looped components 
          if (componentIndex != -1) {
            previous = current;
          }
          current++;
          componentIndex = -1;
        }
        return false;
      }

      private bool Discard(uint id) {
        if (componentIndex != -1) return false;
        if (!world.IsValid(id)) return true;
        string componentProfile = null;
        foreach (var idx in world.EntityKey(id)) {
          var pool = world.pools.PoolByKeyIndex(idx);
          if (pool.Omit == Omit.Entity) {
            return true;
          } else if (componentProfile == null) {
            componentProfile = pool.Profile;
          } else if (pool.Profile != null && componentProfile != pool.Profile) {
            throw new Exception("Entity has components belonging to different serialization profiles!");
          }
        }
        return profile != componentProfile;
      }
    }
  }
}

