using System;
using System.Runtime.CompilerServices;

namespace ManulECS {
  internal unsafe record struct Key : IEquatable<Key> {
    private const int MAX_SIZE = Constants.KEY_SIZE;
    private fixed uint u[MAX_SIZE];

    internal Key(uint index, uint bits) => u[index] = bits;

    internal bool this[Key key] {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get {
        for (int i = 0; i < MAX_SIZE; i++) {
          if ((u[i] & key.u[i]) != key.u[i]) return false;
        }
        return true;
      }
    }

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
      unchecked {
        int hash = 17;
        for (int i = 0; i < MAX_SIZE; i++) {
          hash = hash * 29 + u[i].GetHashCode();
        }
        return hash;
      }
     }

     public struct FlagEnumerator {
       private readonly Key key;
       private int i, j;

       internal FlagEnumerator(Key key) =>
         (this.key, i, j) = (key, 0, -1);

       public int Current => i * 32 + j;

       public bool MoveNext() {
         while (i < MAX_SIZE) {
           if (++j < 32) {
             if ((key.u[i] & 1u << j) != 0) {
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
     }

     public FlagEnumerator GetEnumerator() => new(this);
  }
}
