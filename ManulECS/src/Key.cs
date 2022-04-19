using System;

namespace ManulECS {
  internal unsafe struct Key : IEquatable<Key> {
    internal const int MAX_SIZE = 4;
    private fixed uint u[MAX_SIZE];

    internal Key(int index, uint bits) => u[index] = bits;

    internal bool this[Key flag] => IsSubsetOf(flag);

    internal bool IsSubsetOf(Key filter) {
      for (int i = 0; i < MAX_SIZE; i++) {
        if ((u[i] & filter.u[i]) != filter.u[i]) return false;
      }
      return true;
    }

    public static bool operator ==(Key left, Key right) => left.Equals(right);
    public static bool operator !=(Key left, Key right) => !(left == right);

    public static Key operator +(Key left, Key right) {
      Key key;
      for (int i = 0; i < MAX_SIZE; i++) {
        key.u[i] = left.u[i] | right.u[i];
      }
      return key;
    }
    public static Key operator -(Key left, Key right) {
      Key key;
      for (int i = 0; i < MAX_SIZE; i++) {
        key.u[i] = left.u[i] & ~right.u[i];
      }
      return key;
    }

    public bool Equals(Key other) {
      for (int i = 0; i < MAX_SIZE; i++) {
        if (u[i] != other.u[i]) return false;
      }
      return true;
    }

    public override int GetHashCode() {
      var hash = new HashCode();
      for (int i = 0; i < MAX_SIZE; i++) {
        hash.Add(u[i]);
      }
      return hash.ToHashCode();
    }

    public override bool Equals(object obj) => obj is Key flags && Equals(flags);

    public struct FlagEnumerator {
      private readonly Key flags;
      private int i, j;

      internal FlagEnumerator(Key flags) =>
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
  }
}
