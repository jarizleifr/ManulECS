using System;
using System.Collections.Generic;
using System.Reflection;
using static ManulECS.ArrayUtil;

namespace ManulECS {
  internal sealed class PoolCollection {
    private const int MAX_COMPONENTS = Key.MAX_SIZE * 32;
    private const uint LAST_BIT = 0x80000000;

    private readonly Dictionary<Type, int> registered = new();
    private int[] keyToTypeIndex = new int[World.INITIAL_CAPACITY];
    private Pool[] indexedPools = new Pool[World.INITIAL_CAPACITY];

    private (int index, uint bits) next = (-1, LAST_BIT);
    private (int index, uint bits) Next => next = next.bits != LAST_BIT
      ? (next.index, next.bits << 1)
      : (next.index + 1, 1u);

    internal Pool Pool<T>() where T : struct, IBaseComponent {
      var typeIndex = TypeIndex.Get<T>();
      if (!registered.ContainsKey(typeof(T))) {
        typeIndex = Register<T>();
      }
      return indexedPools[typeIndex];
    }

    /// <summary>Gets a Pool by its runtime type.</summary>
    internal Pool PoolByType(Type type) {
      if (!registered.TryGetValue(type, out var typeIndex)) {
        /* If the Pool for the current Type hasn't been registered and cached yet, we need to use
         * reflection to invoke the Register method, as we don't know the generic type in this case.
         *
         * At worst, this gets run once per component on first deserialization. Subsequent calls are
         * much faster and consist only of a dictionary lookup.
         */
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var method = GetType().GetMethod(nameof(this.Register), flags);
        typeIndex = (int)method.MakeGenericMethod(type).Invoke(this, null);
      }
      return indexedPools[typeIndex];
    }

    internal Pool PoolByKeyIndex(int index) => indexedPools[keyToTypeIndex[index]];

    internal void Clear() => Array.ForEach(indexedPools, pool => pool?.Reset());

    private int Register<T>() where T : struct, IBaseComponent {
      var (index, bits) = Next;
      (var keyIndex, var typeIndex) = (BitUtil.Position(index, bits), TypeIndex.Create<T>());
      registered.Add(typeof(T), typeIndex);

      if (keyIndex == MAX_COMPONENTS) {
        throw new Exception($"{MAX_COMPONENTS} component maximum exceeded!");
      }

      EnsureSize(ref keyToTypeIndex, keyIndex);
      EnsureSize(ref indexedPools, typeIndex);

      var key = new Key(index, bits);
      Pool pool = World.IsTag(typeof(T)) ? new TagPool<T>(key) : new Pool<T>(key);
      (keyToTypeIndex[keyIndex], indexedPools[typeIndex]) = (typeIndex, pool);

      return typeIndex;
    }
  }
}
