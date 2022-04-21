using System;
using System.Collections.Generic;

namespace ManulECS {
  /// <summary>A view of entities.</summary>
  public sealed class View : IDisposable {
    private readonly Key key;
    private readonly List<Pool> subscribed = new();

    private bool shouldUpdate = true;
    internal void SetToUpdate() => shouldUpdate = true;

    private Entity[] entities = new Entity[4];
    internal int Count { get; private set; }

    internal View(World world, Key key) {
      this.key = key;

      foreach (var idx in key) {
        var pool = world.indexedPools[idx];
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
          var pool = world.indexedPools[idx];
          if (smallest == null || smallest.Count > pool.Count) {
            smallest = pool;
          }
        }
        Count = 0;
        ArrayUtil.EnsureSize((uint)smallest.Count, ref entities, Entity.NULL_ENTITY);
        foreach (var entity in smallest) {
          if (world.entityKeys[entity.Id][key]) {
            entities[Count++] = entity;
          }
        }
        shouldUpdate = false;
      }
    }

    /// <summary>Returns an enumerator that iterates through entities.</summary>
    public EntityEnumerator GetEnumerator() => new(entities, Count);
  }

  public struct EntityEnumerator {
    private readonly Entity[] entities;
    private readonly int length;
    private int index;

    public EntityEnumerator(Entity[] entities, int length) {
      this.entities = entities;
      this.length = length;
      index = -1;
    }

    public Entity Current => entities[index];

    public bool MoveNext() => ++index < length;
  }
}
