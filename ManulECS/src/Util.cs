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
        int oldSize = array.Length;
        int newSize = oldSize;
        while (newSize < minSize + 1) {
          newSize *= 2;
        }
        Array.Resize(ref array, newSize);
        Array.Fill(array, defaultValue, oldSize, array.Length - oldSize);
      }
    }
  }

  internal static class BitUtil {
    private static readonly int[] deBruijnSequence = {
      0, 1, 28, 2, 29, 14, 24, 3, 30, 22, 20, 15, 25, 17, 4, 8,
      31, 27, 13, 23, 21, 19, 16, 7, 26, 12, 18, 6, 11, 5, 10, 9
    };

    internal static uint Position(int index, uint bits) =>
      (uint)(index * 32 + deBruijnSequence[unchecked((uint)((int)bits & -(int)bits) * 0x077CB531U) >> 27]);
  }
}
