using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ManulECS {
  public partial class World {
    private Flag nextFlag = new(0, 1u);
    private readonly Dictionary<Type, ComponentPool> pools = new();

    internal ComponentPool[] indexedPools = new ComponentPool[4];

    public ComponentPool<T1> Pools<T1>() where T1 : struct, IComponent => GetPool<T1>();

    public (ComponentPool<T1>, ComponentPool<T2>) Pools<T1, T2>()
      where T1 : struct, IComponent
      where T2 : struct, IComponent =>
        (GetPool<T1>(), GetPool<T2>());

    public (ComponentPool<T1>, ComponentPool<T2>, ComponentPool<T3>) Pools<T1, T2, T3>()
      where T1 : struct, IComponent
      where T2 : struct, IComponent
      where T3 : struct, IComponent =>
        (GetPool<T1>(), GetPool<T2>(), GetPool<T3>());

    public (ComponentPool<T1>, ComponentPool<T2>, ComponentPool<T3>, ComponentPool<T4>) Pools<T1, T2, T3, T4>()
      where T1 : struct, IComponent
      where T2 : struct, IComponent
      where T3 : struct, IComponent
      where T4 : struct, IComponent =>
        (GetPool<T1>(), GetPool<T2>(), GetPool<T3>(), GetPool<T4>());

    internal void Register<T>() where T : struct, IBaseComponent {
      var flag = GetNextFlag();
      var index = (uint)flag.BitPosition;
      if (index >= indexedPools.Length) {
        if (index == FlagEnum.MAX_SIZE * 32) {
          throw new Exception($"{FlagEnum.MAX_SIZE * 32} component maximum exceeded!");
        }
        Util.ResizeArray(index, ref indexedPools, null);
      }

      ComponentPool pool = typeof(T) switch {
        var type when typeof(ITag).IsAssignableFrom(type) => new TagPool<T>(flag),
        var type when type.IsDefined(typeof(DenseAttribute), false) => new DenseComponentPool<T>(flag),
        _ => new SparseComponentPool<T>(flag),
      };

      if (!pools.TryAdd(typeof(T), pool)) {
        throw new Exception($"Component/Tag {typeof(T)} already registered!");
      }
      indexedPools[index] = pool;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Flag GetFlag<T>() where T : struct => pools[typeof(T)].Flag;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ComponentPool GetUntypedPool<T>() where T : struct, IBaseComponent =>
      pools[typeof(T)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ComponentPool<T> GetPool<T>() where T : struct, IComponent =>
      pools[typeof(T)] as ComponentPool<T>;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal TagPool<T> GetTagPool<T>() where T : struct, ITag =>
      pools[typeof(T)] as TagPool<T>;

    private Flag GetNextFlag() {
      var flag = nextFlag;
      nextFlag = nextFlag.bits != 0x8000_0000
        ? new Flag(nextFlag.index, nextFlag.bits << 1)
        : new Flag(nextFlag.index + 1, 1u);
      return flag;
    }
  }
}
