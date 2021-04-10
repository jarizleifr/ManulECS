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
    protected int count;
    protected T1[] items;
    protected T2[] items2;

    public PairList() => Initialize();

    protected void Initialize() {
      count = 0;
      items = new T1[4];
      items2 = new T2[4];
    }

    protected void AddEntry(in T1 item, in T2 item2) {
      if (count == items.Length) {
        Array.Resize(ref items, items.Length * 2);
        Array.Resize(ref items2, items2.Length * 2);
      }

      items[count] = item;
      items2[count] = item2;
      count++;
    }

    protected void UpdateEntry(uint index, in T1 item, in T2 item2) {
      items[index] = item;
      items2[index] = item2;
    }

    protected void ReplaceEntryWithLast(uint index) {
      items[index] = items[count - 1];
      items2[index] = items2[count - 1];
      count--;
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
    private int version = 0;
    private readonly Flag flag;
    private uint[] mapping;

    public SparseComponentPool(Flag flag) {
      this.flag = flag;
      mapping = new uint[4];
      for (int i = 0; i < mapping.Length; i++) {
        mapping[i] = Entity.NULL_ID;
      }
    }

    public int Version => version;
    public int Count => count;
    public Flag Flag => flag;
    public Span<uint> GetIndices() => new(items, 0, count);
    public Span<T> GetComponents() => new(items2, 0, count);

    /// <summary>
    /// Get reference of value. This WILL throw exception if not found.
    /// </summary>
    public ref T GetRef(Entity entity) => ref items2[mapping[entity.Id]];
    public ref T GetRef(uint key) => ref items2[mapping[key]];

    public ref T this[Entity entity] => ref items2[mapping[entity.Id]];
    public ref T this[uint key] => ref items2[mapping[key]];

    public bool Has(uint index) => index < mapping.Length && mapping[index] != Entity.NULL_ID;

    public void Set(uint index, in T item) {
      version++;

      if (index >= mapping.Length) {
        Util.ResizeArray(index, ref mapping, Entity.NULL_ID);
      }

      var key = mapping[index];
      if (key != Entity.NULL_ID) {
        UpdateEntry(key, index, item);
        return;
      }

      mapping[index] = (uint)count;
      AddEntry(index, item);
    }

    public void Remove(uint index) {
      version++;
      if (index >= mapping.Length) return;

      var key = mapping[index];
      if (key == Entity.NULL_ID) return;

      if (key == count - 1) {
        mapping[index] = Entity.NULL_ID;
        count--;
      } else {
        var i = items[count - 1];
        ReplaceEntryWithLast(key);
        mapping[i] = key;
        mapping[index] = Entity.NULL_ID;
      }
    }

    public void Clone(uint origin, uint target) => Set(target, GetRef(origin));

    public object Get(uint index) => Has(index) ? (object)items2[mapping[index]] : null;

    public void Clear() {
      version++;
      Initialize();
      for (int i = 0; i < mapping.Length; i++) {
        mapping[i] = Entity.NULL_ID;
      }
    }
  }

  /// <summary>
  /// Sparse set backed with a dictionary mapping. Use for rare components. Slower, but more memory efficient.
  /// </summary>
  public class DenseComponentPool<T> : PairList<uint, T>, IComponentPool<T> where T : struct {
    private int version = 0;
    private readonly Flag flag;
    private readonly Dictionary<uint, uint> mapping;

    public DenseComponentPool(Flag flag) {
      mapping = new Dictionary<uint, uint>();
      this.flag = flag;
    }

    public int Version => version;
    public int Count => count;
    public Flag Flag => flag;
    public Span<uint> GetIndices() => items.AsSpan(0, count);
    public Span<T> GetComponents() => items2.AsSpan(0, count);

    /// <summary>
    /// Get reference of value. This WILL throw exception if not found.
    /// </summary>
    public ref T GetRef(Entity entity) => ref items2[mapping[entity.Id]];
    public ref T GetRef(uint index) => ref items2[mapping[index]];
    public ref T this[Entity entity] => ref items2[mapping[entity.Id]];
    public ref T this[uint index] => ref items2[mapping[index]];

    public bool Has(uint index) => mapping.ContainsKey(index);

    public void Set(uint index, in T item) {
      version++;
      if (mapping.TryGetValue(index, out var key)) {
        UpdateEntry(key, index, item);
        return;
      }
      mapping.Add(index, (uint)count);
      AddEntry(index, item);
    }

    public void Remove(uint index) {
      version++;
      if (mapping.TryGetValue(index, out var key)) {
        if (key == count - 1) {
          mapping.Remove(index);
          count--;
        } else {
          var i = items[count - 1];
          ReplaceEntryWithLast(key);
          mapping[i] = key;
          mapping.Remove(index);
        }
      }
    }

    public void Clone(uint origin, uint target) => Set(target, GetRef(origin));

    public object Get(uint index) => Has(index) ? (object)items2[mapping[index]] : null;

    public void Clear() {
      version++;
      Initialize();
      mapping.Clear();
    }
  }

  public class TagPool<T> : IComponentPool where T : struct {
    // Use a dummy component for serialization
    private readonly T dummy = default;
    private int version = 0;
    private readonly Flag flag;

    private uint[] ids;
    private int count;

    public TagPool(Flag flag) {
      this.flag = flag;
      ids = new uint[4];
      count = 0;
    }

    public int Version => version;
    public int Count => count;
    public Flag Flag => flag;

    public bool Has(uint id) => FindIndex(id) != -1;

    public object Get(uint id) => dummy;

    public void Set(uint id) {
      version++;
      if (count == ids.Length) {
        Array.Resize(ref ids, ids.Length * 2);
      }
      ids[count++] = id;
    }

    public void Remove(uint id) {
      version++;
      var index = FindIndex(id);
      if (index != -1) {
        ids[index] = ids[--count];
      }
    }

    public void Clone(uint origin, uint target) {
      if (FindIndex(origin) != -1) {
        Set(target);
      } else {
        Remove(target);
      }
    }

    public Span<uint> GetIndices() => ids.AsSpan(0, count);

    public void Clear() {
      version++;
      count = 0;
    }

    private int FindIndex(uint id) {
      for (int i = 0; i < count; i++) {
        if (ids[i] == id) {
          return i;
        }
      }
      return -1;
    }
  }
}
