using System;

namespace ManulECS {
  public struct FlagEnum : IEquatable<FlagEnum> {
    private uint u1, u2, u3, u4;

    public FlagEnum(params Flag[] flags) {
      (u1, u2, u3, u4) = (0, 0, 0, 0);
      foreach (var flag in flags) {
        this[flag] = true;
      }
    }

    public bool this[Flag flag] {
      get => flag.index switch {
        0 => (u1 & flag.bits) > 0,
        1 => (u2 & flag.bits) > 0,
        2 => (u3 & flag.bits) > 0,
        3 => (u4 & flag.bits) > 0,
        _ => throw new Exception("Limited to 128 components")
      };
      set {
        switch (flag.index) {
          case 0:
            u1 = value ? u1 |= flag.bits : u1 &= ~flag.bits;
            break;
          case 1:
            u2 = value ? u2 |= flag.bits : u2 &= ~flag.bits;
            break;
          case 2:
            u3 = value ? u3 |= flag.bits : u3 &= ~flag.bits;
            break;
          case 3:
            u4 = value ? u4 |= flag.bits : u4 &= ~flag.bits;
            break;
        }
      }
    }

    public bool Contains(FlagEnum filter) {
      if (filter.u1 != 0 && (u1 & filter.u1) != filter.u1) return false;
      if (filter.u2 != 0 && (u2 & filter.u2) != filter.u2) return false;
      if (filter.u3 != 0 && (u3 & filter.u3) != filter.u3) return false;
      if (filter.u4 != 0 && (u4 & filter.u4) != filter.u4) return false;
      return true;
    }

    public bool IsSubsetOf(FlagEnum filter) {
      if (filter.u1 != 0 && (u1 & filter.u1) != filter.u1) return false;
      if (filter.u2 != 0 && (u2 & filter.u2) != filter.u2) return false;
      if (filter.u3 != 0 && (u3 & filter.u3) != filter.u3) return false;
      if (filter.u4 != 0 && (u4 & filter.u4) != filter.u4) return false;
      return true;
    }

    public struct FlagEnumerator {
      private readonly FlagEnum flags;
      private int i, j;

      internal FlagEnumerator(FlagEnum flags) =>
        (this.flags, i, j) = (flags, 0, -1);

      public int Current => i * 32 + j;

      public bool MoveNext() {
        while (i < 4) {
          if (++j < 32) {
            if (i == 0 && (flags.u1 & (uint)1 << j) != 0) {
              return true;
            } else if (i == 1 && (flags.u2 & (uint)1 << j) != 0) {
              return true;
            } else if (i == 2 && (flags.u3 & (uint)1 << j) != 0) {
              return true;
            } else if (i == 3 && (flags.u4 & (uint)1 << j) != 0) {
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

    public bool Equals(FlagEnum other) => u1 == other.u1 && u2 == other.u2 && u3 == other.u3 && u4 == other.u4;

    public override int GetHashCode() => HashCode.Combine(u1, u2, u3, u4);
    public override bool Equals(object obj) => obj is FlagEnum flags && Equals(flags);
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

    public static bool operator ==(Flag left, Flag right) => left.Equals(right);
    public static bool operator !=(Flag left, Flag right) => !(left == right);

    public override int GetHashCode() => HashCode.Combine(index, bits);
    public override bool Equals(object obj) => obj is Flag flag && Equals(flag);
  }
}
