using System.Collections.Generic;

namespace ManulECS {
  public class View {
    private readonly List<Entity> entities;
    private readonly int[] versions = new int[8];

    internal View(World world, FlagEnum matcher) {
      entities = new List<Entity>();
      Update(world, matcher);
    }

    ///<summary>Checks if any of the assigned pools has changed.</summary>
    internal bool IsDirty(World world, FlagEnum matcher) {
      int v = 0;
      foreach (var idx in matcher) {
        var pool = world.indexedPools[idx];
        if (versions[v++] != pool.Version) {
          return true;
        }
      }
      return false;
    }

    internal void Update(World world, FlagEnum matcher) {
      if (IsDirty(world, matcher)) {
        int v = 0;
        ComponentPool smallest = null;
        foreach (var idx in matcher) {
          var pool = world.indexedPools[idx];
          if (smallest == null || smallest.Count > pool.Count) {
            versions[v++] = pool.Version;
            smallest = pool;
          }
        }
        entities.Clear();
        foreach (var id in smallest.Indices) {
          if (world.entityFlags[id].IsSubsetOf(matcher)) {
            var entity = world.entities[id];
            entities.Add(entity);
          }
        }
      }
    }

    public struct ViewEnumerator {
      private readonly List<Entity> entities;
      private int index;

      internal ViewEnumerator(List<Entity> entities) =>
        (this.entities, index) = (entities, -1);

      public Entity Current => entities[index];
      public bool MoveNext() => ++index < entities.Count;
      public void Reset() => index = -1;
    }

    public ViewEnumerator GetEnumerator() => new(entities);
  }
}
