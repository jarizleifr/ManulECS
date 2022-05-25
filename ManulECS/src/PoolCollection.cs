using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using static ManulECS.ArrayUtil;

namespace ManulECS {
  internal readonly record struct KeyFlag(uint Index, uint Bits) {
    internal KeyFlag Next => Bits != 0x8000_0000 ? new(Index, Bits << 1) : new(Index + 1, 1u);

    public static implicit operator Key(KeyFlag flag) => new(flag.Index, flag.Bits);
    public static implicit operator uint(KeyFlag flag) {
      (int i, uint pos) = (1, 0);
      while ((i & flag.Bits) == 0) {
        i = i << 1; ++pos;
      }
      return flag.Index * 32 + pos;
    }
  };

  internal sealed class PoolCollection {
    private const int MAX_COMPONENTS = Key.MAX_SIZE * 32;

    private readonly Dictionary<Type, int> types = new();
    private readonly HashSet<int> registered = new();
    private int[] keyToTypeIndex = new int[World.INITIAL_CAPACITY];
    private Pool[] indexedPools = new Pool[World.INITIAL_CAPACITY];
    private KeyFlag nextFlag = new(0, 1u);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Pool Pool<T>() where T : struct, IBaseComponent {
      var typeIndex = TypeIndex.Get<T>();
      if (!registered.Contains(typeIndex) ){
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
      if (nextFlag == MAX_COMPONENTS) {
        throw new Exception($"{MAX_COMPONENTS} component maximum exceeded!");
      }

      var typeIndex = TypeIndex.Create<T>();
      types.Add(typeof(T), typeIndex);
      registered.Add(typeIndex);

      EnsureSize(ref keyToTypeIndex, nextFlag);
      EnsureSize(ref indexedPools, typeIndex);

      Pool pool = World.IsTag(typeof(T)) ? new TagPool<T>(nextFlag) : new Pool<T>(nextFlag);
      (keyToTypeIndex[nextFlag], indexedPools[typeIndex]) = (typeIndex, pool);

      nextFlag = nextFlag.Next;
      return typeIndex;
    }
  }
}
