using System;
using System.Collections.Generic;

namespace ManulECS {
  public class SparseAttribute : Attribute { }
  public class DenseAttribute : Attribute { }

  public interface IComponentPool {
    int Version { get; }
    int Count { get; }
    Flag Flag { get; }
    bool Has(uint id);
    object Get(uint id);
    void Remove(uint id);
    void Clone(uint origin, uint target);
    Span<uint> GetIndices();

    ///<summary>Clear all components in pool.<summary/>
    void Clear();
  }

  public interface IReadPool<T> {
    ref T GetRef(Entity entity);
    ref T this[Entity entity] { get; }
  }

  public interface IComponentPool<T> : IComponentPool, IReadPool<T> {
    ref T GetRef(uint index);
    ref T this[uint key] { get; }
    void Set(uint id, in T component);
    Span<T> GetComponents();
  }

  public abstract class PairList<T1, T2>
  where T1 : struct
  where T2 : struct {
    protected T1[] items;
    protected T2[] items2;

    public int Count { get; protected set; }

    public PairList() => Initialize();

    protected void Initialize() {
      Count = 0;
      items = new T1[4];
      items2 = new T2[4];
    }

    protected void AddEntry(in T1 item, in T2 item2) {
      if (Count == items.Length) {
        Array.Resize(ref items, items.Length * 2);
        Array.Resize(ref items2, items2.Length * 2);
      }

      items[Count] = item;
      items2[Count] = item2;
      Count++;
    }

    protected void UpdateEntry(uint index, in T1 item, in T2 item2) {
      items[index] = item;
      items2[index] = item2;
    }

    protected void ReplaceEntryWithLast(uint index) {
      Count--;
      items[index] = items[Count];
      items2[index] = items2[Count];
    }

    protected void Swap(uint index, uint index2) {
      (T1 tempItem, T2 tempItem2) = (items[index], items2[index]);
      items[index] = items[index2];
      items2[index] = items2[index2];

      items[index2] = tempItem;
      items2[index2] = tempItem2;
    }
  }

  /// <summary>
  /// Sparse set backed with an array. Use for common components. Uses more memory, but is faster.
  /// </summary>
  public class SparseComponentPool<T> : PairList<uint, T>, IComponentPool<T> where T : struct {
    private uint[] mapping = new uint[4];

    public Flag Flag { get; init; }
    public int Version { get; private set; } = 0;

    public SparseComponentPool(Flag flag) {
      Flag = flag;
      Array.Fill(mapping, Entity.NULL_ID);
    }

    public Span<uint> GetIndices() => new(items, 0, Count);
    public Span<T> GetComponents() => new(items2, 0, Count);

    /// <summary>
    /// Get reference of value. This WILL throw exception if not found.
    /// </summary>
    public ref T GetRef(Entity entity) => ref items2[mapping[entity.Id]];
    public ref T GetRef(uint key) => ref items2[mapping[key]];

    public ref T this[Entity entity] => ref items2[mapping[entity.Id]];
    public ref T this[uint key] => ref items2[mapping[key]];

    public bool Has(uint index) => index < mapping.Length && mapping[index] != Entity.NULL_ID;

    public void Set(uint index, in T item) {
      Version++;

      if (index >= mapping.Length) {
        Util.ResizeArray(index, ref mapping, Entity.NULL_ID);
      }

      var key = mapping[index];
      if (key != Entity.NULL_ID) {
        UpdateEntry(key, index, item);
        return;
      }

      mapping[index] = (uint)Count;
      AddEntry(index, item);
    }

    public void Remove(uint index) {
      Version++;
      if (index < mapping.Length) {
        ref var key = ref mapping[index];
        if (key != Entity.NULL_ID) {
          if (key == Count - 1) {
            key = Entity.NULL_ID;
            Count--;
          } else {
            ReplaceEntryWithLast(key);
            mapping[items[Count]] = key;
            key = Entity.NULL_ID;
          }
        }
      }
    }

    public void Clone(uint origin, uint target) => Set(target, GetRef(origin));

    public object Get(uint index) => Has(index) ? items2[mapping[index]] : null;

    public void Clear() {
      Version++;
      Initialize();
      Array.Fill(mapping, Entity.NULL_ID);
    }
  }

  /// <summary>
  /// Sparse set backed with a dictionary mapping. Use for rare components. Slower, but more memory efficient.
  /// </summary>
  public class DenseComponentPool<T> : PairList<uint, T>, IComponentPool<T> where T : struct {
    private readonly Dictionary<uint, uint> mapping = new();

    public int Version { get; private set; } = 0;
    public Flag Flag { get; init; }

    public DenseComponentPool(Flag flag) => Flag = flag;

    public Span<uint> GetIndices() => items.AsSpan(0, Count);
    public Span<T> GetComponents() => items2.AsSpan(0, Count);

    /// <summary>
    /// Get reference of value. This WILL throw exception if not found.
    /// </summary>
    public ref T GetRef(Entity entity) => ref items2[mapping[entity.Id]];
    public ref T GetRef(uint index) => ref items2[mapping[index]];
    public ref T this[Entity entity] => ref items2[mapping[entity.Id]];
    public ref T this[uint index] => ref items2[mapping[index]];

    public bool Has(uint index) => mapping.ContainsKey(index);

    public void Set(uint index, in T item) {
      Version++;
      if (mapping.TryGetValue(index, out var key)) {
        UpdateEntry(key, index, item);
      } else {
        mapping.Add(index, (uint)Count);
        AddEntry(index, item);
      }
    }

    public void Remove(uint index) {
      Version++;
      if (mapping.TryGetValue(index, out var key)) {
        if (key == Count - 1) {
          mapping.Remove(index);
          Count--;
        } else {
          ReplaceEntryWithLast(key);
          mapping[items[Count]] = key;
          mapping.Remove(index);
        }
      }
    }

    public void Clone(uint origin, uint target) => Set(target, GetRef(origin));

    public object Get(uint index) => Has(index) ? items2[mapping[index]] : null;

    public void Clear() {
      Version++;
      Initialize();
      mapping.Clear();
    }
  }

  public class TagPool<T> : IComponentPool where T : struct {
    // Use a dummy component for serialization
    private readonly T dummy = default;
    private uint[] ids = new uint[4];

    public Flag Flag { get; init; }
    public int Version { get; private set; } = 0;
    public int Count { get; private set; } = 0;

    public TagPool(Flag flag) {
      Flag = flag;
      Array.Fill(ids, Entity.NULL_ID);
    }

    public bool Has(uint id) => FindIndex(id) != -1;

    public object Get(uint id) => dummy;

    public void Set(uint id) {
      Version++;
      if (Count == ids.Length) {
        Array.Resize(ref ids, ids.Length * 2);
      }
      ids[Count++] = id;
    }

    public void Remove(uint id) {
      Version++;
      var index = FindIndex(id);
      if (index != -1) {
        ids[index] = ids[--Count];
      }
    }

    public void Clone(uint origin, uint target) {
      if (Has(origin)) {
        Set(target);
      } else {
        Remove(target);
      }
    }

    public Span<uint> GetIndices() => ids.AsSpan(0, Count);

    public void Clear() => (Version, Count) = (Version + 1, 0);

    private int FindIndex(uint id) => Array.FindIndex(ids, (i) => i == id);
  }
}
