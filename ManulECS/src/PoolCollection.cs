using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using static ManulECS.ArrayUtil;

namespace ManulECS {
  internal readonly record struct KeyFlag(uint Index, uint Bits) {
    internal KeyFlag Next => Bits != 0x8000_0000 ? new(Index, Bits << 1) : new(Index + 1, 1u);

    public static implicit operator Key(KeyFlag flag) => new(flag.Index, flag.Bits);
    public static implicit operator int(KeyFlag flag) {
      (int i, uint pos) = (1, 0);
      while ((i & flag.Bits) == 0) {
        i = i << 1; ++pos;
      }
      return (int)(flag.Index * 32 + pos);
    }
  };

  internal sealed class PoolCollection {
    private const int MAX_GLOBAL_TYPES = Constants.MAXIMUM_GLOBAL_COMPONENTS;
    private const int MAX_LOCAL_TYPES = Constants.MAXIMUM_LOCAL_COMPONENTS;

    private readonly Dictionary<Type, int> types = new();
    private readonly BitArray registered = new(MAX_GLOBAL_TYPES);

    private int[] keyToTypeIndex = new int[Constants.INITIAL_CAPACITY];
    private Pool[] indexedPools = new Pool[Constants.INITIAL_CAPACITY];
    private KeyFlag nextFlag = new(0, 1u);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Pool Pool<T>() where T : struct, IBaseComponent {
      var typeIndex = TypeIndex.Get<T>();
      if (typeIndex == Constants.MAXIMUM_GLOBAL_COMPONENTS || !registered[typeIndex]) {
        typeIndex = Register<T>();
      }
      return indexedPools[typeIndex];
    }

    /// <summary>Gets a Pool by its runtime type.</summary>
    internal Pool PoolByType(Type type) {
      if (!types.TryGetValue(type, out var typeIndex)) {
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Pool PoolByKeyIndex(int index) => indexedPools[keyToTypeIndex[index]];

    internal void Clear() => Array.ForEach(indexedPools, pool => pool?.Reset());

    private int Register<T>() where T : struct, IBaseComponent {
      var typeIndex = TypeIndex.Create<T>();

      if (nextFlag == MAX_LOCAL_TYPES) {
        throw new Exception($"World local maximum component limit ({MAX_LOCAL_TYPES}) exceeded!");
      }
      if (typeIndex == MAX_GLOBAL_TYPES) {
        throw new Exception($"Global maximum component limit ({MAX_GLOBAL_TYPES}) exceeded!");
      }

      types.Add(typeof(T), typeIndex);
      registered[typeIndex] = true;

      if (keyToTypeIndex.Length <= nextFlag) {
        Resize(ref keyToTypeIndex, nextFlag);
      }
      if (indexedPools.Length <= typeIndex) {
        Resize(ref indexedPools, typeIndex);
      }

      Pool pool = World.IsTag(typeof(T)) ? new TagPool<T>(nextFlag) : new Pool<T>(nextFlag);
      (keyToTypeIndex[nextFlag], indexedPools[typeIndex]) = (typeIndex, pool);

      nextFlag = nextFlag.Next;
      return typeIndex;
    }
  }
}
