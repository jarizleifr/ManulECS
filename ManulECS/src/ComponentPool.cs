using System;
using System.Collections.Generic;

namespace ManulECS {
  public class SparseAttribute : Attribute { }
  public class DenseAttribute : Attribute { }

  public abstract class ComponentPool {
    internal int Version { get; set; } = 0;
    internal int Count { get; set; } = 0;
    internal Flag Flag { get; init; }
    internal abstract Span<uint> Indices { get; }
    internal abstract bool Has(in Entity entity);
    internal abstract object Get(in Entity entity);
    internal abstract void Remove(in Entity entity);
    internal abstract void Clone(in Entity origin, in Entity target);
    internal abstract void Clear();
    internal abstract void Reset();

    public ComponentPool() => Reset();
  }

  public abstract class ComponentPool<T> : ComponentPool where T : struct {
    protected uint[] ids;
    protected T[] components;

    internal override Span<uint> Indices => ids.AsSpan(0, Count);
    internal Span<T> Components => components.AsSpan(0, Count);

    internal override void Reset() {
      ids = new uint[4];
      components = new T[4];
      Version = 0;
      Count = 0;
    }

    protected void AddEntry(uint id, T component) {
      while (Count >= ids.Length) {
        Array.Resize(ref ids, ids.Length * 2);
        Array.Resize(ref components, components.Length * 2);
      }

      ids[Count] = id;
      components[Count] = component;
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

    internal abstract void Set(in Entity entity, T component);

    public abstract ref T GetRef(in Entity entity);
    public abstract ref T this[in Entity entity] { get; }
  }

  /// <summary>
  /// Sparse set backed with an array. Use for common components. Uses more memory, but is faster.
  /// </summary>
  public class SparseComponentPool<T> : ComponentPool<T> where T : struct {
    private uint[] mapping;

    public SparseComponentPool(Flag flag) => Flag = flag;

    internal override void Reset() {
      mapping = new uint[4];
      Array.Fill(mapping, Entity.NULL_ID);
      base.Reset();
    }

    /// <summary>
    /// Get a ref of component. This WILL throw exception if not found.
    /// </summary>
    public override ref T GetRef(in Entity entity) => ref components[mapping[entity.Id]];
    /// <summary>
    /// Get a ref of component. This WILL throw exception if not found.
    /// </summary>
    public override ref T this[in Entity entity] => ref components[mapping[entity.Id]];

    internal override bool Has(in Entity entity) =>
      entity.Id < mapping.Length &&
      mapping[entity.Id] != Entity.NULL_ID;

    internal override void Set(in Entity entity, T item) {
      Version++;
      var id = entity.Id;
      if (id >= mapping.Length) {
        Util.ResizeArray(id, ref mapping, Entity.NULL_ID);
      }

      var key = mapping[id];
      if (key != Entity.NULL_ID) {
        UpdateEntry(key, id, item);
        return;
      }

      mapping[id] = (uint)Count;
      AddEntry(id, item);
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

    internal override void Clone(in Entity origin, in Entity target) =>
      Set(target, GetRef(origin));

    internal override object Get(in Entity entity) => Has(entity)
      ? components[mapping[entity.Id]]
      : null;

    internal override void Clear() {
      Version++;
      Count = 0;
      Array.Fill(mapping, Entity.NULL_ID);
    }
  }

  /// <summary>
  /// Sparse set backed with a dictionary mapping. Use for rare components. Slower, but more memory efficient.
  /// </summary>
  public class DenseComponentPool<T> : ComponentPool<T> where T : struct {
    private readonly Dictionary<uint, uint> mapping = new();

    public DenseComponentPool(Flag flag) => Flag = flag;

    internal override void Reset() {
      mapping.Clear();
      base.Reset();
    }

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

    internal override void Clone(in Entity origin, in Entity target) =>
      Set(target, GetRef(origin));

    internal override object Get(in Entity entity) => Has(entity)
      ? components[mapping[entity.Id]]
      : null;

    internal override void Clear() {
      Version++;
      Count = 0;
      mapping.Clear();
    }
  }

  public class TagPool<T> : ComponentPool where T : struct {
    private static readonly T dummy = default; // Used for serialization
    private uint[] ids;

    public TagPool(Flag flag) => Flag = flag;

    internal override void Reset() {
      ids = new uint[4];
      Array.Fill(ids, Entity.NULL_ID);
      Count = 0;
      Version = 0;
    }

    internal override Span<uint> Indices => ids.AsSpan(0, Count);

    internal override bool Has(in Entity entity) => FindIndex(entity) != -1;

    internal override object Get(in Entity _) => dummy;

    internal void Set(in Entity entity) {
      if (!Has(entity)) {
        Version++;
        while (Count >= ids.Length) {
          Array.Resize(ref ids, ids.Length * 2);
        }
        ids[Count++] = entity.Id;
      }
    }

    internal override void Remove(in Entity entity) {
      Version++;
      var index = FindIndex(entity);
      if (index != -1) {
        if (index == Count - 1) {
          ids[index] = Entity.NULL_ID;
          Count--;
        } else {
          ids[index] = ids[--Count];
          ids[Count] = Entity.NULL_ID;
        }
      }
    }

    internal override void Clone(in Entity origin, in Entity target) {
      if (Has(origin)) {
        Set(target);
      } else {
        Remove(target);
      }
    }

    internal override void Clear() {
      Version++;
      Count = 0;
      Array.Fill(ids, Entity.NULL_ID);
    }

    private int FindIndex(Entity entity) => Array.FindIndex(ids, (i) =>
      i == entity.Id && i != Entity.NULL_ID
    );
  }
}
