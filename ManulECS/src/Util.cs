using System;
using System.Runtime.CompilerServices;

namespace ManulECS {
  internal static class ArrayUtil {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void EnsureSize<T>(ref T[] array, int minSize, T defaultValue = default) =>
      EnsureSize(ref array, (uint)minSize, defaultValue);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void EnsureSize<T>(ref T[] array, uint minSize, T defaultValue = default) {
      if (minSize >= array.Length) {
        var oldSize = array.Length;
        var newSize = oldSize;
        while (newSize < minSize + 1) {
          newSize *= 2;
        }
        Array.Resize(ref array, newSize);
        Array.Fill(array, defaultValue, oldSize, array.Length - oldSize);
      }
    }
  }
}

