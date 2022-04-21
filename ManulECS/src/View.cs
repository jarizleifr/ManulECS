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
        Count = 0;
        ArrayUtil.EnsureSize((uint)smallest.Entities.Length, ref entities, Entity.NULL_ENTITY);
        foreach (var entity in smallest.Entities) {
          if (world.entityFlags[entity.Id].IsSubsetOf(key)) {
            entities[Count++] = entity;
          }
        }
        shouldUpdate = false;
      }
    }

    public ref struct ViewEnumerator {
      private readonly Span<Entity> entities;
      private int index;

      public ViewEnumerator(Entity[] entities, int length) {
        this.entities = entities.AsSpan(0, length);
        index = -1;
      }

      public Entity Current => entities[index];

      public bool MoveNext() => ++index < entities.Length;
    }

    /// <summary>Returns an enumerator that iterates through entities.</summary>
    public ViewEnumerator GetEnumerator() => new(entities, Count);
  }
}
