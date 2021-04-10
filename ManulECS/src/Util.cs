using System;

namespace ManulECS {
  internal static class Util {
    public static void ResizeArray<T>(uint minSize, ref T[] array, T defaultValue) {
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
}
