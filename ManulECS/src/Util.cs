using System;
using System.Runtime.CompilerServices;

namespace ManulECS {
  internal static class ArrayUtil {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void EnsureSize<T>(uint minSize, ref T[] array, T defaultValue) {
      if (minSize >= array.Length) {
        int oldSize = array.Length;
        int newSize = oldSize;
        while (newSize < minSize + 1) {
          newSize *= 2;
        }
        Array.Resize(ref array, newSize);
        for (int i = oldSize; i < array.Length; i++) {
          array[i] = defaultValue;
        }
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetWithResize<T>(uint index, ref T[] array, T item) {
      if (index >= array.Length) {
        Array.Resize(ref array, array.Length * 2);
      }
      array[index] = item;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetWithResize<T1, T2>(uint index, ref T1[] array1, T1 item1, ref T2[] array2, T2 item2) {
      if (index >= array1.Length) {
        Array.Resize(ref array1, array1.Length * 2);
        Array.Resize(ref array2, array1.Length * 2);
      }
      array1[index] = item1;
      array2[index] = item2;
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
