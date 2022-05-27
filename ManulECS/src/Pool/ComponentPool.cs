using System;
using System.Runtime.CompilerServices;
using static ManulECS.ArrayUtil;

namespace ManulECS {
  public sealed class Pool<T> : Pool {
    private T[] components;
    private uint[] mapping;

    internal Pool(in Key key) : base(key, ECSSerializeAttribute.GetAttribute(typeof(T))) { }

    /// <summary>Get a ref of component. This WILL throw exception if not found.</summary>
    public ref T this[in Entity entity] {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => ref components[mapping[entity.Id]];
    }

    internal override bool Has(uint id) => id < mapping.Length && mapping[id] != Entity.NULL_ID;

    internal void Set(uint id) => Set(id, default);

    internal void Set(uint id, T component) {
      if (mapping.Length <= id) {
        ResizeAndFill(ref mapping, (int)id, Entity.NULL_ID);
      }
      ref var key = ref mapping[id];
      if (key == Entity.NULL_ID) {
        key = (uint)nextIndex;
        if (ids.Length <= nextIndex) {
          ResizeAndFill(ref ids, nextIndex, Entity.NULL_ID);
          Resize(ref components, nextIndex);
        }
        (ids[nextIndex], components[nextIndex]) = (id, component);
        nextIndex++;
      } else {
        (ids[key], components[key]) = (id, component);
      }
      onUpdate?.Invoke();
    }

    internal override void SetRaw(uint id, object component) =>
      Set(id, (T)component);

    internal override void Remove(uint id) {
      if (id < mapping.Length) {
        ref var key = ref mapping[id];
        if (key != Entity.NULL_ID) {
          if (key < --nextIndex) {
            (ids[key], components[key]) = (ids[nextIndex], components[nextIndex]);
            mapping[ids[nextIndex]] = key;
          }
          key = Entity.NULL_ID;
          onUpdate?.Invoke();
        }
      }
    }

    internal override object Get(uint id) => Has(id) ? components[mapping[id]] : null;

    internal override void Clone(uint originId, uint targetId) =>
      Set(targetId, components[mapping[originId]]);

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
      components = new T[World.INITIAL_CAPACITY];
      onUpdate?.Invoke();
    }
  }
}
