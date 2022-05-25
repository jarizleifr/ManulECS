using System;
using System.Runtime.CompilerServices;

namespace ManulECS {
  internal static class ArrayUtil {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Resize<T>(ref T[] array, int minSize) {
      var oldSize = array.Length;
      var newSize = oldSize;
      while (newSize <= minSize) newSize <<= 1;
      Array.Resize(ref array, newSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ResizeAndFill<T>(ref T[] array, int minSize, T defaultValue) {
      var oldSize = array.Length;
      var newSize = oldSize;
      while (newSize <= minSize) newSize <<= 1;
      Array.Resize(ref array, newSize);
      Array.Fill(array, defaultValue, oldSize, array.Length - oldSize);
    }
  }
}

