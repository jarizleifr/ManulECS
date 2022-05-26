using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using static ManulECS.ArrayUtil;

namespace ManulECS {
  internal sealed class PoolCollection {
    private Pool[] pools = new Pool[Constants.INITIAL_CAPACITY];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Pool Pool<T>() where T : struct, IBaseComponent {
      var typeIndex = TypeIndex.Get<T>();
      if (typeIndex >= pools.Length || pools[typeIndex] == null) {
        typeIndex = Register<T>();
      }
      return pools[typeIndex];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Pool PoolByIndex(int index) => pools[index];

    /// <summary>Gets a Pool by its runtime type.</summary>
    internal Pool PoolByType(Type type) {
      var typeIndex = TypeIndex.Get(type);
      if (typeIndex >= pools.Length || pools[typeIndex] == null) {
        /* If the Pool for the current Type hasn't been registered and cached yet, we need to use
         * reflection to invoke the Register method, as we don't know the generic type in this case.
         *
         * At worst, this gets run once per component. Subsequent calls are much faster and consist
         * only of a dictionary lookup.
         */
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var method = GetType().GetMethod(nameof(this.Register), flags);
        typeIndex = (int)method.MakeGenericMethod(type).Invoke(this, null);
      }
      return pools[typeIndex];
    }

    internal void Clear() => Array.ForEach(pools, pool => pool?.Reset());

    private int Register<T>() where T : struct, IBaseComponent {
      var typeIndex = TypeIndex.Create<T>();
      if (pools.Length <= typeIndex) {
        Resize(ref pools, typeIndex);
      }
      var key = new Key(typeIndex);
      pools[typeIndex] = World.IsTag(typeof(T)) ? new TagPool<T>(key) : new Pool<T>(key);
      return typeIndex;
    }
  }
}
