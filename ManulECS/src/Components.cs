using System;
using System.Collections.Generic;

namespace ManulECS {
  internal class Components {
    private readonly Dictionary<Type, IComponentPool> pools = new();
    private IComponentPool[] indexedPools = new IComponentPool[4];
    private Flag nextFlag = new(0, 1u);

    internal void Register<T>() where T : struct, IBaseComponent {
      var flag = GetNextFlag();
      var index = (uint)flag.BitPosition;
      if (index >= indexedPools.Length) {
        if (index == 128) {
          throw new Exception("128 component maximum exceeded!");
        }
        Util.ResizeArray(index, ref indexedPools, null);
      }

      IComponentPool pool;
      if (typeof(ITag).IsAssignableFrom(typeof(T))) {
        pool = new TagPool<T>(flag);
      } else {
        pool = CreatePool<T>(flag);
      }

      if (!pools.TryAdd(typeof(T), pool)) {
        throw new Exception($"Component/Tag {typeof(T)} already registered!");
      }
      indexedPools[flag.BitPosition] = pool;
    }

    internal void Clear() {
      foreach (var pool in indexedPools) {
        if (pool != null) {
          pool.Clear();
        }
      }
    }

    internal void RemoveComponents(Entity entity, in FlagEnum flags) {
      foreach (var index in flags) {
        indexedPools[index].Remove(entity.Id);
      }
    }

    internal Flag GetFlag<T>() where T : struct => pools[typeof(T)].Flag;

    internal IComponentPool GetUntypedPool<T>() where T : struct, IBaseComponent =>
      pools[typeof(T)];

    internal IComponentPool<T> GetPool<T>() where T : struct, IComponent =>
      (IComponentPool<T>)pools[typeof(T)];

    internal TagPool<T> GetTagPool<T>() where T : struct, ITag =>
      (TagPool<T>)pools[typeof(T)];

    internal IComponentPool GetIndexedPool(int index) => indexedPools[index];

    private Flag GetNextFlag() {
      var flag = nextFlag;
      nextFlag = nextFlag.bits != 0x8000_0000
        ? new Flag(nextFlag.index, nextFlag.bits << 1)
        : new Flag(nextFlag.index + 1, 1u);
      return flag;
    }

    private static IComponentPool CreatePool<T>(Flag flag) where T : struct {
      if (typeof(T).IsDefined(typeof(DenseAttribute), false)) {
        return new DenseComponentPool<T>(flag);
      } else {
        return new SparseComponentPool<T>(flag);
      }
    }
  }
}
