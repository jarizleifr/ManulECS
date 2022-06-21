using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using static ManulECS.ArrayUtil;

namespace ManulECS {
  internal sealed partial class Components {
    private Pool[] pools = new Pool[World.INITIAL_CAPACITY];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsRegistered(int id) => id < pools.Length && pools[id] != null;

    /// <summary>Gets a raw Pool by its id.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Pool RawPool(int id) => pools[id];

    /// <summary>Gets a raw Pool by its generic type.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Pool RawPool<T>() {
      var id = GetId<T>();
      if (id >= pools.Length || pools[id] == null) {
        id = Register<T>();
      }
      return pools[id];
    }

    /// <summary>Gets a raw Pool by its runtime type.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Pool RawPool(Type type) {
      var id = GetId(type);
      if (id >= pools.Length || pools[id] == null) {
        /* If the Pool for the current Type hasn't been registered and cached yet, we need to use
         * reflection to invoke the Register method, as we don't know the generic type in this case.
         *
         * At worst, this gets run once per component. Subsequent calls are much faster and consist
         * only of a dictionary lookup.
         */
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var method = GetType().GetMethod(nameof(this.Register), flags);
        id = (int)method.MakeGenericMethod(type).Invoke(this, null);
      }
      return pools[id];
    }

    internal void Clear() => Array.ForEach(pools, pool => pool?.Reset());

    private int Register<T>() {
      var (id, key) = (GetId<T>(), GetKey<T>());
      if (pools.Length <= id) {
        Resize(ref pools, id);
      }
      pools[id] = typeof(Tag).IsAssignableFrom(typeof(T)) ? new TagPool<T>(key) : new Pool<T>(key);
      return id;
    }
  }
}

