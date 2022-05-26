using System;
using System.Runtime.CompilerServices;
using static ManulECS.ArrayUtil;

namespace ManulECS {
  public sealed class TagPool<T> : Pool {
    private readonly static T dummy = default;
    private uint[] mapping;

    internal TagPool(in Key key) : base(key, ECSSerializeAttribute.GetAttribute(typeof(T))) { }

    internal override object Get(uint _) => dummy;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal override bool Has(uint id) =>
      id < mapping.Length && mapping[id] != Entity.NULL_ID;

    internal void Set(uint id) {
      if (mapping.Length <= id) {
        ResizeAndFill(ref mapping, (int)id, Entity.NULL_ID);
      }
      ref var key = ref mapping[id];
      if (key == Entity.NULL_ID) {
        key = (uint)nextIndex;
        if (ids.Length <= nextIndex) {
          Resize(ref ids, nextIndex);
        }
        ids[nextIndex++] = id;
        onUpdate?.Invoke();
      }
    }

    internal override void SetObject(uint id, object _) => Set(id);

    internal override void Remove(uint id) {
      if (id < mapping.Length) {
        ref var key = ref mapping[id];
        if (key != Entity.NULL_ID) {
          if (key < --nextIndex) {
            ids[key] = ids[nextIndex];
            mapping[ids[nextIndex]] = key;
          }
          key = Entity.NULL_ID;
          onUpdate?.Invoke();
        }
      }
    }

    internal override void Clone(uint originId, uint targetId) {
      if (Has(originId)) {
        Set(targetId);
      } else {
        Remove(targetId);
      }
    }

    internal override void Clear() {
      Array.Fill(mapping, Entity.NULL_ID);
      nextIndex = 0;
      onUpdate?.Invoke();
    }

    internal override void Reset() {
      mapping = new uint[World.INITIAL_CAPACITY];
      Array.Fill(mapping, Entity.NULL_ID);
      nextIndex = 0;
      ids = new uint[World.INITIAL_CAPACITY];
      onUpdate?.Invoke();
    }
  }
}
