using System;
using System.Collections.Generic;

namespace ManulECS {
  /// <summary>
  /// Sparse set backed with an array. Use for common components. 
  /// Uses more memory, but is faster.
  /// </summary>
  public sealed class SparsePool<T> : Pool<T> where T : struct {
    private uint[] mapping;

    /// <summary>Get a ref of component. This WILL throw exception if not found.</summary>
    public override ref T GetRef(in Entity entity) => ref components[mapping[entity.Id]];
    /// <summary>Get a ref of component. This WILL throw exception if not found.</summary>
    public override ref T this[in Entity entity] => ref components[mapping[entity.Id]];

    internal override bool Has(in Entity entity) =>
      entity.Id < mapping.Length &&
      mapping[entity.Id] != Entity.NULL_ID;

    internal override void Set(in Entity entity, T item) {
      Version++;
      var id = entity.Id;
      ArrayUtil.EnsureSize(id, ref mapping, Entity.NULL_ID);
      ref var key = ref mapping[id];
      if (key == Entity.NULL_ID) {
        key = (uint)Count;
        AddEntry(id, item);
      } else {
        UpdateEntry(key, id, item);
      }
    }

    internal override void Remove(in Entity entity) {
      Version++;
      var id = entity.Id;
      if (id < mapping.Length) {
        ref var key = ref mapping[id];
        if (key != Entity.NULL_ID) {
          if (key == Count - 1) {
            key = Entity.NULL_ID;
            Count--;
          } else {
            ReplaceEntryWithLast(key);
            mapping[ids[Count]] = key;
            key = Entity.NULL_ID;
          }
        }
      }
    }

    internal override object Get(in Entity entity) => Has(entity)
      ? components[mapping[entity.Id]]
      : null;

    internal override void Clone(in Entity origin, in Entity target) =>
      Set(target, GetRef(origin));

    internal override void Clear() {
      Version++;
      Count = 0;
      Array.Fill(mapping, Entity.NULL_ID);
    }

    internal override void Reset() {
      mapping = new uint[4];
      Array.Fill(mapping, Entity.NULL_ID);
      base.Reset();
    }
  }

  /// <summary>
  /// Sparse set backed with a dictionary mapping. Use for rare components.
  /// Slower access speed, but is more memory efficient.
  /// </summary>
  public sealed class DensePool<T> : Pool<T> where T : struct {
    private readonly Dictionary<uint, uint> mapping = new();

    /// <summary>
    /// Get reference of value. This WILL throw exception if not found.
    /// </summary>
    public override ref T GetRef(in Entity entity) => ref components[mapping[entity.Id]];
    public override ref T this[in Entity entity] => ref components[mapping[entity.Id]];

    internal override bool Has(in Entity entity) => mapping.ContainsKey(entity.Id);

    internal override void Set(in Entity entity, T item) {
      Version++;
      var id = entity.Id;
      if (mapping.TryGetValue(id, out var key)) {
        UpdateEntry(key, id, item);
      } else {
        mapping.Add(id, (uint)Count);
        AddEntry(id, item);
      }
    }

    internal override void Remove(in Entity entity) {
      Version++;
      var id = entity.Id;
      if (mapping.TryGetValue(id, out var key)) {
        if (key == Count - 1) {
          mapping.Remove(id);
          Count--;
        } else {
          ReplaceEntryWithLast(key);
          mapping[ids[Count]] = key;
          mapping.Remove(id);
        }
      }
    }

    internal override object Get(in Entity entity) => Has(entity)
      ? components[mapping[entity.Id]]
      : null;

    internal override void Clone(in Entity origin, in Entity target) =>
      Set(target, GetRef(origin));

    internal override void Clear() {
      Version++;
      Count = 0;
      mapping.Clear();
    }

    internal override void Reset() {
      mapping.Clear();
      base.Reset();
    }
  }

  public abstract class Pool<T> : Pool where T : struct {
    protected T[] components;

    internal Span<T> Components => components.AsSpan(0, Count);

    public abstract ref T GetRef(in Entity entity);
    public abstract ref T this[in Entity entity] { get; }

    internal override void Set(in Entity entity) => Set(entity, default);
    internal abstract void Set(in Entity entity, T component);

    internal override void Reset() {
      ids = new uint[4];
      components = new T[4];
      Version = 0;
      Count = 0;
    }

    protected void AddEntry(uint id, T component) {
      ArrayUtil.SetWithResize((uint)Count, ref ids, id, ref components, component);
      Count++;
    }

    protected void UpdateEntry(uint index, uint id, T component) {
      ids[index] = id;
      components[index] = component;
    }

    protected void ReplaceEntryWithLast(uint index) {
      Count--;
      ids[index] = ids[Count];
      components[index] = components[Count];
    }

    protected void Swap(uint index, uint index2) {
      (uint tempItem, T tempItem2) = (ids[index], components[index]);
      ids[index] = ids[index2];
      components[index] = components[index2];

      ids[index2] = tempItem;
      components[index2] = tempItem2;
    }
  }
}
