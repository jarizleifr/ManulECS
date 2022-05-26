using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ManulECS {
  /// <summary>Provides fast static type indexing for component types.</summary>
  internal static class TypeIndex {
    private const int MAX_INDEX = Constants.MAX_COMPONENTS;

    private static class Index<T> where T : struct, IBaseComponent {
      internal static int value = MAX_INDEX;
    }

    private static readonly Dictionary<Type, int> types = new();
    private static int nextIndex = -1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Get<T>() where T : struct, IBaseComponent => Index<T>.value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Get(Type type) => types.TryGetValue(type, out var typeIndex)
      ? typeIndex
      : MAX_INDEX;

    internal static int Create<T>() where T : struct, IBaseComponent {
      /* Thread-safety isn't a huge concern here, especially if using only a single World object,
        * but testing frameworks like Xunit like to run tests in parallel, in which case multiple
        * threads might try to register a component with the same index.
        * 
        * This lock makes sure indices are created properly in a multithreaded context.
        */
      lock (types) {
        ref var typeIndex = ref Index<T>.value;
        if (typeIndex == MAX_INDEX) {
          if (nextIndex == MAX_INDEX) {
            throw new Exception($"Maximum component limit ({MAX_INDEX}) exceeded!");
          }
          typeIndex = ++nextIndex;
          types.Add(typeof(T), typeIndex);
        }
        return typeIndex;
      }
    }
  }
}

