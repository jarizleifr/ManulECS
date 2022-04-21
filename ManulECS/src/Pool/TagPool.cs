using System;
using System.Collections.Generic;

namespace ManulECS {
  public sealed class DenseTagPool<T> : TagPool<T> where T : struct {
    private readonly Dictionary<uint, uint> mapping = new();

    internal override bool Has(in Entity entity) => mapping.ContainsKey(entity.Id);

    internal override void Set(in Entity entity) {
      if (!Has(entity)) {
        mapping.Add(entity.Id, (uint)Count);
        ArrayUtil.SetWithResize((uint)Count, ref entities, entity);
        Count++;
        OnUpdate?.Invoke();
      }
    }

    internal override void Remove(in Entity entity) {
      var id = entity.Id;
      if (mapping.TryGetValue(id, out var key)) {
        if (key == Count - 1) {
          Count--;
        } else {
          entities[key] = entities[--Count];
          mapping[entities[Count].Id] = key;
        }
        mapping.Remove(id);
        OnUpdate?.Invoke();
      }
    }

    internal override void Clear() {
      mapping.Clear();
      Count = 0;
      OnUpdate?.Invoke();
    }

    internal override void Reset() {
      mapping.Clear();
      Count = 0;
      entities = new Entity[4];
      OnUpdate?.Invoke();
    }
  }

  public sealed class SparseTagPool<T> : TagPool<T> where T : struct {
    private uint[] mapping;

    internal override bool Has(in Entity entity) =>
      entity.Id < mapping.Length && mapping[entity.Id] != Entity.NULL_ID;

    internal override void Set(in Entity entity) {
      var id = entity.Id;
      ArrayUtil.EnsureSize(id, ref mapping, Entity.NULL_ID);
      ref var key = ref mapping[id];
      if (key == Entity.NULL_ID) {
        key = (uint)Count;
        ArrayUtil.SetWithResize((uint)Count, ref entities, entity);
        Count++;
        OnUpdate?.Invoke();
      }
    }

    internal override void Remove(in Entity entity) {
      var id = entity.Id;
      if (id < mapping.Length) {
        ref var key = ref mapping[id];
        if (key != Entity.NULL_ID) {
          if (key == Count - 1) {
            Count--;
          } else {
            entities[key] = entities[--Count];
            mapping[entities[Count].Id] = key;
          }
          key = Entity.NULL_ID;
          OnUpdate?.Invoke();
        }
      }
    }

    internal override object Get(in Entity _) => dummy;

    internal override void Clone(in Entity origin, in Entity target) {
      if (Has(origin)) {
        Set(target);
      } else {
        Remove(target);
      }
    }

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
      OnUpdate?.Invoke();
    }
  }

  public abstract class TagPool<T> : Pool {
    protected static readonly T dummy = default; // Used for serialization

    internal override object Get(in Entity _) => dummy;

    internal override void Clone(in Entity origin, in Entity target) {
      if (Has(origin)) {
        Set(target);
      } else {
        Remove(target);
      }
    }
  }
}
