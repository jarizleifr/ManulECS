using System;
using System.Collections.Generic;

namespace ManulECS {
  /// <summary>A view of entities.</summary>
  public sealed class View : IDisposable {
    private readonly Key key;
    private readonly List<Pool> subscribed;

    private readonly List<Entity> entities = new();

    private bool shouldUpdate = true;
    private void SetToUpdate() => shouldUpdate = true;

    internal int Count => entities.Count;

    internal View(World world, Key key) {
      this.key = key;

      foreach (var idx in key) {
        var pool = world.pools.typed[idx];
        pool.OnUpdate += SetToUpdate;
      }
      Update(world);
    }

    public void Dispose() {
      foreach (var pool in subscribed) {
        pool.OnUpdate -= SetToUpdate;
      }
      GC.SuppressFinalize(this);
    }

    internal void Update(World world) {
      if (shouldUpdate) {
        Pool smallest = null;
        foreach (var idx in key) {
          var pool = world.pools.typed[idx];
          if (smallest == null || smallest.Count > pool.Count) {
            smallest = pool;
          }
        }
        entities.Clear();
        foreach (var id in smallest.Indices) {
          if (world.entityFlags[id].IsSubsetOf(key)) {
            entities.Add(world.entities[id]);
          }
        }
        shouldUpdate = false;
      }
    }

    /// <summary>Returns an enumerator that iterates through entities.</summary>
    public List<Entity>.Enumerator GetEnumerator() =>
      entities.GetEnumerator();
  }
}
