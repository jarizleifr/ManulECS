using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ManulECS {
  /// <summary>
  /// Sparse set backed with an array. Use for common components. 
  /// Uses more memory, but is faster.
  /// </summary>
  public sealed class SparsePool<T> : Pool<T> where T : struct {
    private uint[] mapping;

    /// <summary>Get a ref of component. This WILL throw exception if not found.</summary>
    public override ref T this[in Entity entity] {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => ref components[mapping[entity.Id]];
    }

    internal override bool Has(in Entity entity) =>
      entity.Id < mapping.Length &&
      mapping[entity.Id] != Entity.NULL_ID;

    internal override void Set(in Entity entity, T item) {
      var id = entity.Id;
      ArrayUtil.EnsureSize(id, ref mapping, Entity.NULL_ID);
      ref var key = ref mapping[id];
      if (key == Entity.NULL_ID) {
        key = (uint)Count;
        AddEntry(entity, item);
      } else {
        UpdateEntry(key, entity, item);
      }
      OnUpdate?.Invoke();
    }

    internal override void Remove(in Entity entity) {
      var id = entity.Id;
      if (id < mapping.Length) {
        ref var key = ref mapping[id];
        if (key != Entity.NULL_ID) {
          if (key == Count - 1) {
            Count--;
          } else {
            ReplaceEntryWithLast(key);
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

  /// <summary>
  /// Sparse set backed with a dictionary mapping. Use for rare components.
  /// Slower access speed, but is more memory efficient.
  /// </summary>
  public sealed class DensePool<T> : Pool<T> where T : struct {
    private readonly Dictionary<uint, uint> mapping = new();

    /// <summary>Get a ref of component. This WILL throw exception if not found.</summary>
    public override ref T this[in Entity entity] {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => ref components[mapping[entity.Id]];
    }

    internal override bool Has(in Entity entity) => mapping.ContainsKey(entity.Id);

    internal override void Set(in Entity entity, T item) {
      var id = entity.Id;
      if (mapping.TryGetValue(id, out var key)) {
        UpdateEntry(key, entity, item);
      } else {
        mapping.Add(id, (uint)Count);
        AddEntry(entity, item);
      }
      OnUpdate?.Invoke();
    }

    internal override void Remove(in Entity entity) {
      var id = entity.Id;
      if (mapping.TryGetValue(id, out var key)) {
        if (key == Count - 1) {
          Count--;
        } else {
          ReplaceEntryWithLast(key);
          mapping[entities[Count].Id] = key;
        }
        mapping.Remove(id);
        OnUpdate?.Invoke();
      }
    }

    internal override object Get(in Entity entity) => Has(entity)
      ? components[mapping[entity.Id]]
      : null;

    internal override void Clone(in Entity origin, in Entity target) =>
      Set(target, this[origin]);

    internal override void Clear() {
      mapping.Clear();
      Count = 0;
      OnUpdate?.Invoke();
    }

    internal override void Reset() {
      mapping.Clear();
      Count = 0;
      entities = new Entity[4];
      components = new T[4];
      OnUpdate?.Invoke();
    }
  }

  public abstract class Pool<T> : Pool where T : struct {
    protected T[] components;

    internal Span<T> Components => components.AsSpan(0, Count);

    public abstract ref T this[in Entity entity] {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get;
    }

    internal override void Set(in Entity entity) => Set(entity, default);
    internal abstract void Set(in Entity entity, T component);

    protected void AddEntry(in Entity entity, T component) {
      ArrayUtil.SetWithResize((uint)Count, ref entities, entity, ref components, component);
      Count++;
    }

    protected void UpdateEntry(uint index, in Entity entity, T component) {
      entities[index] = entity;
      components[index] = component;
    }

    protected void ReplaceEntryWithLast(uint index) {
      Count--;
      entities[index] = entities[Count];
      components[index] = components[Count];
    }
  }
}
