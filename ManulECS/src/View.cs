using System.Collections;
using System.Collections.Generic;

namespace ManulECS {
  /// <summary>A view of entities.</summary>
  public sealed class View : IEnumerable<Entity> {
    internal const int MAX_VIEW_COMPONENTS = 8;

    private readonly List<Entity> entities = new();
    private readonly int[] versions = new int[MAX_VIEW_COMPONENTS];

    internal View(World world, Matcher matcher) => Update(world, matcher);

    internal int Count => entities.Count;

    internal void Update(World world, Matcher matcher) {
      var (v, dirty) = (0, false);
      Pool smallest = null;
      foreach (var idx in matcher) {
        var pool = world.pools.flagged[idx];
        if (versions[v] != pool.Version) {
          dirty = true;
          versions[v++] = pool.Version;
        }
        if (smallest == null || smallest.Count > pool.Count) {
          smallest = pool;
        }
      }
      if (dirty) {
        entities.Clear();
        foreach (var id in smallest.Indices) {
          if (world.entityFlags[id].IsSubsetOf(matcher)) {
            var entity = world.entities[id];
            entities.Add(entity);
          }
        }
      }
    }

    /// <summary>Returns an enumerator that iterates through entities.</summary>
    public IEnumerator<Entity> GetEnumerator() =>
      entities.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
      GetEnumerator();
  }
}
