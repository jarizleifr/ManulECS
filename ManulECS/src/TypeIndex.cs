using System;
using System.Collections.Generic;
using System.Reflection;

namespace ManulECS {
  internal static class TypeIndex {
    private static class Index<T> where T : struct, IBaseComponent {
      internal static uint value = uint.MaxValue;
      internal static void Reset() => value = uint.MaxValue;
    }
    private static uint next = 0;
    private readonly static List<Type> types = new();

    internal static uint Get<T>() where T : struct, IBaseComponent =>
      Index<T>.value;

    internal static uint Create<T>() where T : struct, IBaseComponent {
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
