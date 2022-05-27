using System;
using System.Collections.Generic;

namespace ManulECS {
  /* This part of the code is slightly arcane - basically we use generic static classes as kind of
   * a faster alternative to a dictionary, keyed by components' generic types. Static constructors
   * and field initializers are thread-safe and will run only once per generic type, making them
   * very much suitable for storing type metadata.
   */
  internal sealed partial class Components {
    private const int MAX_INDEX = Key.MAX_SIZE * 32;
    private static readonly Dictionary<Type, int> types = new();
    private static int nextIndex = -1;

    private static class Type<T> {
      internal readonly static int id = MAX_INDEX;
      internal readonly static Key key;
      static Type() {
        if (nextIndex == MAX_INDEX) {
          throw new Exception($"Maximum component limit ({MAX_INDEX}) exceeded!");
        }
        id = ++nextIndex;
        key = new(id);
        types.Add(typeof(T), id);
      }
    }

    // Aggregate keys for the component configurations used by Views.
    private static class Union<A, B> { internal readonly static Key key = Type<A>.key + Type<B>.key; }
    private static class Union<A, B, C> { internal readonly static Key key = Union<A, B>.key + Type<C>.key; }
    private static class Union<A, B, C, D> { internal readonly static Key key = Union<A, B, C>.key + Type<D>.key; }
    private static class Union<A, B, C, D, E> { internal readonly static Key key = Union<A, B, C, D>.key + Type<E>.key; }
    private static class Union<A, B, C, D, E, F> { internal readonly static Key key = Union<A, B, C, D, E>.key + Type<F>.key; }
    private static class Union<A, B, C, D, E, F, G> { internal readonly static Key key = Union<A, B, C, D, E, F>.key + Type<G>.key; }
    private static class Union<A, B, C, D, E, F, G, H> { internal readonly static Key key = Union<A, B, C, D, E, F, G>.key + Type<H>.key; }

    internal int GetId<T>() => Type<T>.id;
    internal int GetId(Type type) => types.TryGetValue(type, out var id) ? id : MAX_INDEX;

    // Key accessors for any Tag/Component configuration, up to 8 different components.
    internal Key GetKey<T>() => Type<T>.key;
    internal Key GetKey<A, B>() => Union<A, B>.key;
    internal Key GetKey<A, B, C>() => Union<A, B, C>.key;
    internal Key GetKey<A, B, C, D>() => Union<A, B, C, D>.key;
    internal Key GetKey<A, B, C, D, E>() => Union<A, B, C, D, E>.key;
    internal Key GetKey<A, B, C, D, E, F>() => Union<A, B, C, D, E, F>.key;
    internal Key GetKey<A, B, C, D, E, F, G>() => Union<A, B, C, D, E, F, G>.key;
    internal Key GetKey<A, B, C, D, E, F, G, H>() => Union<A, B, C, D, E, F, G, H>.key;
  }
}
