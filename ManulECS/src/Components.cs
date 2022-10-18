using System;
using System.Linq;
using System.Runtime.CompilerServices;
using static System.Reflection.BindingFlags;
using static ManulECS.ArrayUtil;

namespace ManulECS;

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
    if (pools.Length <= id) {
      Resize(ref pools, id);
    }
    ref var pool = ref pools[id];
    if (pool == null) {
      var key = GetKey<T>();
      pool = typeof(Tag).IsAssignableFrom(typeof(T)) ? new TagPool<T>(key) : new Pool<T>(key);
    }
    return pool;
  }

  /// <summary>Gets a raw Pool by its runtime type.</summary>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  internal Pool RawPool(Type type) => (types.TryGetValue(type, out var id) && IsRegistered(id))
    ? pools[id]
    /* If the Pool for the current Type hasn't been registered and cached yet, we need to use
     * reflection to invoke the RawPool method, as we don't know the generic type in this case.
     *
     * At worst, this gets run once per component. Subsequent calls are much faster and consist
     * only of a dictionary lookup.
     */
    : (Pool)GetType()
      .GetMethods(NonPublic | Instance)
      .Single(m => m.Name == nameof(this.RawPool) && m.IsGenericMethod)
      .MakeGenericMethod(type)
      .Invoke(this, null);

  internal void Clear() => Array.ForEach(pools, pool => pool?.Reset());
}


