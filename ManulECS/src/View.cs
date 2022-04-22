using System;
using System.Collections.Generic;
using static ManulECS.ArrayUtil;

namespace ManulECS {
  /// <summary>A view of entities.</summary>
  public sealed class View : IDisposable {
    private readonly Key key;
    private readonly List<Pool> subscribed = new();

    private Entity[] entities = new Entity[World.INITIAL_CAPACITY];
    private int count = 0;
    public int Count => count;

    private bool shouldUpdate = true;
    internal void SetToUpdate() => shouldUpdate = true;

    internal View(World world, Key key) {
      this.key = key;

      foreach (var idx in key) {
        var pool = world.pools.PoolByKeyIndex(idx);
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
          var pool = world.pools.PoolByKeyIndex(idx);
          if (smallest == null || smallest.Count > pool.Count) {
            smallest = pool;
          }
        }
        count = 0;
        EnsureSize(ref entities, smallest.Count, Entity.NULL_ENTITY);
        foreach (var entity in smallest) {
          if (world.EntityKey(entity)[key]) {
            entities[count++] = entity;
          }
        }
        shouldUpdate = false;
      }
    }

    /// <summary>Returns an enumerator that iterates through entities.</summary>
    public EntityEnumerator GetEnumerator() => new(entities, count);
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
