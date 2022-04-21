using System;
using System.Collections.Generic;
using System.Reflection;

namespace ManulECS {
  internal static class TypeIndex {
    internal const int MAX_INDEX = byte.MaxValue;

    private static class Index<T> where T : struct, IBaseComponent {
      internal static byte value = byte.MaxValue;
      internal static void Reset() => value = byte.MaxValue;
    }
    private static byte next = 0;
    private readonly static List<Type> types = new();

    internal static byte Get<T>() where T : struct, IBaseComponent =>
      Index<T>.value;

    internal static byte Create<T>() where T : struct, IBaseComponent {
      var type = typeof(T);
      if (!types.Contains(typeof(T))) {
        types.Add(type);
        Index<T>.value = next++;
      }
      return Index<T>.value;
    }

    internal static void Reset() {
      foreach (var type in types) {
        var generic = typeof(Index<>).MakeGenericType(type);
        var method = generic.GetMethod(
          "Reset", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod
        );
        method.Invoke(null, null);
      }
      next = 0;
      types.Clear();
    }
  }
}
