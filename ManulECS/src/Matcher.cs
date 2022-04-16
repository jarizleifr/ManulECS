using System;
using System.Diagnostics;

namespace ManulECS {
  internal unsafe struct Matcher : IEquatable<Matcher> {
    internal const int MAX_SIZE = 4;
    private fixed uint u[MAX_SIZE];

    internal Matcher(in Flag f1) => this[f1] = true;
    internal Matcher(in Flag f1, in Flag f2) => this[f1] = this[f2] = true;
    internal Matcher(in Flag f1, in Flag f2, in Flag f3) => this[f1] = this[f2] = this[f3] = true;
    internal Matcher(in Flag f1, in Flag f2, in Flag f3, in Flag f4) =>
      this[f1] = this[f2] = this[f3] = this[f4] = true;
    internal Matcher(in Flag f1, in Flag f2, in Flag f3, in Flag f4, in Flag f5) =>
      this[f1] = this[f2] = this[f3] = this[f4] = this[f5] = true;
    internal Matcher(in Flag f1, in Flag f2, in Flag f3, in Flag f4, in Flag f5, in Flag f6) =>
      this[f1] = this[f2] = this[f3] = this[f4] = this[f5] = this[f6] = true;
    internal Matcher(in Flag f1, in Flag f2, in Flag f3, in Flag f4, in Flag f5, in Flag f6, in Flag f7) =>
      this[f1] = this[f2] = this[f3] = this[f4] = this[f5] = this[f6] = this[f7] = true;
    internal Matcher(
      in Flag f1, in Flag f2, in Flag f3, in Flag f4, in Flag f5, in Flag f6, in Flag f7, in Flag f8
    ) => this[f1] = this[f2] = this[f3] = this[f4] = this[f5] = this[f6] = this[f7] = this[f8] = true;

    internal bool this[in Flag flag] {
      get => (u[flag.index] & flag.bits) > 0;
      set {
        var val = u[flag.index];
        u[flag.index] = value ? val |= flag.bits : val &= ~flag.bits;
      }
    }

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
}
