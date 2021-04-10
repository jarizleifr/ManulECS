#pragma warning disable CS0660

using System;

namespace ManulECS {
  public struct FlagEnum : IEquatable<FlagEnum> {
    private uint[] bitarray;

    public FlagEnum(Flag flag) {
      bitarray = null;
      this[flag] = true;
    }
    public FlagEnum(Flag f1, Flag f2) {
      bitarray = null;
      this[f1] = true;
      this[f2] = true;
    }
    public FlagEnum(Flag f1, Flag f2, Flag f3) {
      bitarray = null;
      this[f1] = true;
      this[f2] = true;
      this[f3] = true;
    }
    public FlagEnum(Flag f1, Flag f2, Flag f3, Flag f4) {
      bitarray = null;
      this[f1] = true;
      this[f2] = true;
      this[f3] = true;
      this[f4] = true;
    }

    // TODO: This is pretty heavy performance-wise, it gets run A LOT
    public bool this[Flag flag] {
      get => bitarray != null && flag.index < bitarray.Length && (bitarray[flag.index] & flag.bits) > 0;
      set {
        if (bitarray == null) {
          bitarray = new uint[flag.index + 1];
        } else if (bitarray.Length < flag.index + 1) {
          Array.Resize(ref bitarray, flag.index + 1);
        }
        bitarray[flag.index] = value ? bitarray[flag.index] |= flag.bits : bitarray[flag.index] &= ~flag.bits;
      }
    }

    public bool Contains(FlagEnum filter) {
      if (bitarray == null || filter.bitarray == null) return false;

      for (int i = 0; i < filter.bitarray.Length; i++) {
        uint b = filter.bitarray[i];
        // if checked bit is SOMETHING, but array is out of bounds or checked bitarray doesn't match
        if (b != 0u && (i >= bitarray.Length || (bitarray[i] & b) != b)) return false;
      }
      return true;
    }

    public struct FlagEnumerator {
      private readonly uint[] bitarray;
      private int i, j;

      internal FlagEnumerator(uint[] bitarray) =>
          (this.bitarray, i, j) = (bitarray, 0, -1);

      public int Current => i * 32 + j;

      public bool MoveNext() {
        if (bitarray == null) return false;

        while (i < bitarray.Length) {
          if (++j < 32) {
            if ((bitarray[i] & (uint)1 << j) != 0) {
              return true;
            }
          } else {
            j = -1;
            i++;
            continue;
          }
        }
        return false;
      }

      public void Reset() => (i, j) = (0, -1);
    }

    public FlagEnumerator GetEnumerator() => new(bitarray);

    public bool IsSubsetOf(FlagEnum filter) {
      if (bitarray == null || filter.bitarray == null) return false;

      for (int i = 0; i < filter.bitarray.Length; i++) {
        uint b = filter.bitarray[i];
        // if checked bit is SOMETHING, but array is out of bounds or checked bitarray doesn't match
        if (b != 0u && (i >= bitarray.Length || (bitarray[i] & b) != b)) return false;
      }
      return true;
    }

    public static bool operator ==(FlagEnum f1, FlagEnum f2) => f1.Equals(f2);
    public static bool operator !=(FlagEnum f1, FlagEnum f2) => !f1.Equals(f2);

    public bool Equals(FlagEnum other) {
      if (bitarray.Length != other.bitarray.Length) {
        return false;
      }

      for (int i = 0; i < bitarray.Length; i++) {
        if (bitarray[i] != other.bitarray[i]) {
          return false;
        }
      }
      return true;
    }

    public override int GetHashCode() {
      unchecked {
        int hashCode = 17;
        for (int i = 0; i < bitarray.Length; i++) {
          hashCode = (hashCode * 23) + bitarray[i].GetHashCode();
        }
        return hashCode;
      }
    }
  }

  public readonly struct Flag : IEquatable<Flag> {
    public readonly int index;
    public readonly uint bits;

    public Flag(int index, uint bits) {
      if (bits == 0u) throw new Exception("Bits cannot be 0");
      this.index = index;
      this.bits = bits;
    }

    private static readonly int[] positions = {
      0, 1, 28, 2, 29, 14, 24, 3, 30, 22, 20, 15, 25, 17, 4, 8,
      31, 27, 13, 23, 21, 19, 16, 7, 26, 12, 18, 6, 11, 5, 10, 9
    };

    public int BitPosition => index * 32 + positions[unchecked((uint)((int)bits & -(int)bits) * 0x077CB531U) >> 27];

    public bool Equals(Flag other) => index == other.index && bits == other.bits;

    public override int GetHashCode() => HashCode.Combine(index, bits);
  }
}
