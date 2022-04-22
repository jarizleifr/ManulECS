using System;
using System.Collections.Generic;
using static ManulECS.ArrayUtil;

namespace ManulECS {
  internal sealed class PoolCollection {
    private const int MAX_COMPONENTS = Key.MAX_SIZE * 32;
    private const uint LAST_BIT = 0x80000000;

    private readonly HashSet<Type> registered = new();
    private byte[] keyToTypeIndex = new byte[World.INITIAL_CAPACITY];
    private Pool[] indexedPools = new Pool[World.INITIAL_CAPACITY];

    private (int index, uint bits) next = (-1, LAST_BIT);
    private (int index, uint bits) Next => next = next.bits != LAST_BIT
      ? (next.index, next.bits << 1)
      : (next.index + 1, 1u);

    internal Pool Pool<T>() where T : struct, IBaseComponent =>
      indexedPools[TypeIndex.Get<T>()];
    internal Pool PoolByKeyIndex(int index) => indexedPools[keyToTypeIndex[index]];

    internal void Register<T>() where T : struct, IBaseComponent {
      var type = typeof(T);

      if (registered.Contains(type)) {
        throw new Exception($"Component/Tag {typeof(T)} already registered!");
      }
      registered.Add(type);

      var (index, bits) = Next;
      var (keyIndex, typeIndex) = (BitUtil.Position(index, bits), TypeIndex.Create<T>());

      if (keyIndex == MAX_COMPONENTS) {
        throw new Exception($"{MAX_COMPONENTS} component maximum exceeded!");
      }

      EnsureSize(ref keyToTypeIndex, keyIndex);
      EnsureSize(ref indexedPools, typeIndex);

      var key = new Key(index, bits);
      (keyToTypeIndex[keyIndex], indexedPools[typeIndex]) = (
        typeIndex,
        IsTag(type) ? new TagPool<T>(key) : new Pool<T>(key)
      );

      static bool IsTag(Type type) => typeof(ITag).IsAssignableFrom(type);
    }

    internal void Clear() => Array.ForEach(indexedPools, pool => pool?.Reset());
  }
}
