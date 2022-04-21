using System;
using System.Runtime.CompilerServices;

namespace ManulECS {
  public sealed class Pool<T> : Pool where T : struct {
    private T[] components;
    private uint[] mapping;

    /// <summary>Get a ref of component. This WILL throw exception if not found.</summary>
    public ref T this[in Entity entity] {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => ref components[mapping[entity.Id]];
    }

    internal override bool Has(in Entity entity) =>
      entity.Id < mapping.Length &&
      mapping[entity.Id] != Entity.NULL_ID;

    internal override void Set(in Entity entity) => Set(entity, default);

    internal void Set(in Entity entity, T component) {
      var id = entity.Id;
      ArrayUtil.EnsureSize(id, ref mapping, Entity.NULL_ID);
      ref var key = ref mapping[id];
      if (key == Entity.NULL_ID) {
        key = (uint)Count;
        ArrayUtil.SetWithResize((uint)Count, ref entities, entity, ref components, component);
        Count++;
      } else {
        (entities[key], components[key]) = (entity, component);
      }
      OnUpdate?.Invoke();
    }

    internal override void Remove(in Entity entity) {
      var id = entity.Id;
      if (id < mapping.Length) {
        ref var key = ref mapping[id];
        if (key != Entity.NULL_ID) {
          if (key < --Count) {
            (entities[key], components[key]) = (entities[Count], components[Count]);
            mapping[entities[Count].Id] = key;
          }
          key = Entity.NULL_ID;
          OnUpdate?.Invoke();
        }
      }
    }

    internal override object Get(in Entity entity) => Has(entity)
      ? components[mapping[entity.Id]]
      : null;

    internal override void Clone(in Entity origin, in Entity target) =>
      Set(target, this[origin]);

    internal override void Clear() {
      Array.Fill(mapping, Entity.NULL_ID);
      Count = 0;
      OnUpdate?.Invoke();
    }

    internal override void Reset() {
      mapping = new uint[4];
      Array.Fill(mapping, Entity.NULL_ID);
      Count = 0;
      entities = new Entity[4];
      components = new T[4];
      OnUpdate?.Invoke();
    }
  }
}
