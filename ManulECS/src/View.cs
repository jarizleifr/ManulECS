using System;
using System.Runtime.CompilerServices;
using static ManulECS.ArrayUtil;

namespace ManulECS {
  /// <summary>A view of entities, possessing the components specified in the key.</summary>
  public sealed class View {
    private readonly Key key;
    private Entity[] entities = new Entity[World.INITIAL_CAPACITY];

    private int count = 0;
    private bool dirty = true;
    internal void SetToDirty() => dirty = true;

    internal View(World world, Key key) {
      this.key = key;
      foreach (var idx in key) {
        var pool = world.pools.PoolByKeyIndex(idx);
        pool.OnUpdate += SetToDirty;
      }
    }

    internal void Clear() =>
      (entities, count, dirty) = (new Entity[World.INITIAL_CAPACITY], 0, true);

    internal void Update(World world) {
      if (dirty) {
        Pool smallest = null;
        foreach (var idx in key) {
          var pool = world.pools.PoolByKeyIndex(idx);
          if (smallest == null || smallest.Count > pool.Count) {
            smallest = pool;
          }
        }
        count = 0;
        EnsureSize(ref entities, smallest.Count, Entity.NULL_ENTITY);
        foreach (var entity in smallest.AsSpan()) {
          if (world.EntityKey(entity)[key]) {
            entities[count++] = entity;
          }
        }
        dirty = false;
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlySpan<Entity> AsSpan() => entities.AsSpan(0, count);
  }
}
