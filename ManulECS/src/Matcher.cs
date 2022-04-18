using System;
using System.Diagnostics;

namespace ManulECS {
  using F = Flag;
  internal record struct Flag {
    internal readonly int index;
    internal readonly uint bits;

    internal Flag(int index, uint bits) {
      Debug.Assert(bits != 0u, "Cannot have flags with all bits unset!");
      Debug.Assert(index < Matcher.MAX_SIZE, "Index cannot be larger than MAX_SIZE constant!");
      this.index = index;
      this.bits = bits;
    }
  }

  internal unsafe struct Matcher : IEquatable<Matcher> {
    internal const int MAX_SIZE = 4;
    private fixed uint u[MAX_SIZE];

    internal Matcher(in F f) => Set(f);
    internal Matcher(in F f1, in F f2) { Set(f1); Set(f2); }
    internal Matcher(in F f1, in F f2, in F f3) { Set(f1); Set(f2); Set(f3); }
    internal Matcher(in F f1, in F f2, in F f3, in F f4) {
      Set(f1); Set(f2); Set(f3); Set(f4);
    }
    internal Matcher(in F f1, in F f2, in F f3, in F f4, in F f5) {
      Set(f1); Set(f2); Set(f3); Set(f4); Set(f5);
    }
    internal Matcher(in F f1, in F f2, in F f3, in F f4, in F f5, in F f6) {
      Set(f1); Set(f2); Set(f3); Set(f4); Set(f5); Set(f6);
    }
    internal Matcher(in F f1, in F f2, in F f3, in F f4, in F f5, in F f6, in F f7) {
      Set(f1); Set(f2); Set(f3); Set(f4); Set(f5); Set(f6); Set(f7);
    }
    internal Matcher(in F f1, in F f2, in F f3, in F f4, in F f5, in F f6, in F f7, in F f8) {
      Set(f1); Set(f2); Set(f3); Set(f4); Set(f5); Set(f6); Set(f7); Set(f8);
    }

    internal void Set(in F flag) => u[flag.index] |= flag.bits;
    internal void Unset(in F flag) => u[flag.index] &= ~flag.bits;
    internal bool this[in F flag] => (u[flag.index] & flag.bits) > 0;

    internal bool IsSubsetOf(Matcher filter) {
      for (int i = 0; i < MAX_SIZE; i++) {
        var (a, b) = (u[i], filter.u[i]);
        if (b != 0 && (a & b) != b) return false;
      }
      return true;
    }

    public struct FlagEnumerator {
      private readonly Matcher flags;
      private int i, j;

      internal FlagEnumerator(Matcher flags) =>
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

    public static bool operator ==(Matcher left, Matcher right) => left.Equals(right);
    public static bool operator !=(Matcher left, Matcher right) => !(left == right);

    public bool Equals(Matcher other) {
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

    public override bool Equals(object obj) => obj is Matcher flags && Equals(flags);
  }
}
