using System;
using System.Diagnostics;

namespace ManulECS {
  public unsafe struct FlagEnum : IEquatable<FlagEnum> {
    public const int MAX_SIZE = 4;
    private fixed uint u[MAX_SIZE];

    public FlagEnum(in Flag f1) =>
      this[f1] = true;
    public FlagEnum(in Flag f1, in Flag f2) =>
      (this[f1], this[f2]) = (true, true);
    public FlagEnum(in Flag f1, in Flag f2, in Flag f3) =>
      (this[f1], this[f2], this[f3]) = (true, true, true);
    public FlagEnum(in Flag f1, in Flag f2, in Flag f3, in Flag f4) =>
      (this[f1], this[f2], this[f3], this[f4]) = (true, true, true, true);
    public FlagEnum(in Flag f1, in Flag f2, in Flag f3, in Flag f4, in Flag f5) =>
      (this[f1], this[f2], this[f3], this[f4], this[f5]) = (true, true, true, true, true);

    public bool this[in Flag flag] {
      get => (u[flag.index] & flag.bits) > 0;
      set {
        var val = u[flag.index];
        u[flag.index] = value ? val |= flag.bits : val &= ~flag.bits;
      }
    }

    public bool IsSubsetOf(FlagEnum filter) {
      for (int i = 0; i < MAX_SIZE; i++) {
        var (a, b) = (u[i], filter.u[i]);
        if (b != 0 && (a & b) != b) return false;
      }
      return true;
    }

    public struct FlagEnumerator {
      private readonly FlagEnum flags;
      private int i, j;

      internal FlagEnumerator(FlagEnum flags) =>
        (this.flags, i, j) = (flags, 0, -1);

      public int Current => i * 32 + j;

      public bool MoveNext() {
        while (i < MAX_SIZE) {
          if (++j < 32) {
            if ((flags.u[i] & 1u << j) != 0) {
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

    public FlagEnumerator GetEnumerator() => new(this);

    public static bool operator ==(FlagEnum left, FlagEnum right) => left.Equals(right);
    public static bool operator !=(FlagEnum left, FlagEnum right) => !(left == right);

    public bool Equals(FlagEnum other) {
      for (int i = 0; i < MAX_SIZE; i++) {
        if (u[i] != other.u[i]) return false;
      }
      return true;
    }

    public override int GetHashCode() {
      unchecked {
        int hash = 17;
        for (int i = 0; i < MAX_SIZE; i++) {
          hash = hash * 31 + u[i].GetHashCode();
        }
        return hash;
      }
    }

    public override bool Equals(object obj) => obj is FlagEnum flags && Equals(flags);
  }

  public readonly struct Flag : IEquatable<Flag> {
    public readonly int index;
    public readonly uint bits;

    public Flag(int index, uint bits) {
      Debug.Assert(bits != 0u, "Bits cannot be zero!");
      Debug.Assert(index < FlagEnum.MAX_SIZE, "Index cannot be larger than MAX_SIZE constant!");
      this.index = index;
      this.bits = bits;
    }

    private static readonly int[] positions = {
      0, 1, 28, 2, 29, 14, 24, 3, 30, 22, 20, 15, 25, 17, 4, 8,
      31, 27, 13, 23, 21, 19, 16, 7, 26, 12, 18, 6, 11, 5, 10, 9
    };

    public int BitPosition => index * 32 + positions[unchecked((uint)((int)bits & -(int)bits) * 0x077CB531U) >> 27];

    public bool Equals(Flag other) => index == other.index && bits == other.bits;

    public static bool operator ==(Flag left, Flag right) => left.Equals(right);
    public static bool operator !=(Flag left, Flag right) => !(left == right);

    public override int GetHashCode() => HashCode.Combine(index, bits);
    public override bool Equals(object obj) => obj is Flag flag && Equals(flag);
  }
}
