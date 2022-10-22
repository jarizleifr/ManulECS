using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ManulECS;

/// <summary>A view of entities, possessing the components specified in the key.</summary>
public sealed class View {
  internal readonly static View Empty = new View(null, new Key());

  private readonly Key key;
  private List<Entity> entities = new(World.INITIAL_CAPACITY);

  private bool dirty = true;
  internal void SetToDirty() => dirty = true;

  internal View(World world, Key key) {
    this.key = key;
    foreach (var idx in key) {
      var pool = world.pools.RawPool(idx);
      pool.OnUpdate += SetToDirty;
    }
  }

  internal void Clear() {
    entities.Clear();
    dirty = true;
  }

  internal void Update(World world) {
    if (dirty) {
      Pool smallest = null;
      foreach (var idx in key) {
        var pool = world.pools.RawPool(idx);
        if (smallest == null || smallest.Count > pool.Count) {
          smallest = pool;
        }
      }
      entities.Clear();
      foreach (var id in smallest.AsSpan()) {
        if (world.GetEntityKey(id)[key]) {
          entities.Add(world.GetEntity(id));
        }
      }
      dirty = false;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static implicit operator ReadOnlySpan<Entity>(View view) =>
    CollectionsMarshal.AsSpan(view.entities);
}
