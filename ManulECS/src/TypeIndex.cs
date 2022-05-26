using System.Runtime.CompilerServices;
using System.Threading;

namespace ManulECS {
  /// <summary>Provides fast static type indexing for component types.</summary>
  internal static class TypeIndex {
    private const int MAX_INDEX = Constants.MAXIMUM_GLOBAL_COMPONENTS;

    private static class Index<T> where T : struct, IBaseComponent {
      internal static int value = MAX_INDEX;
    }

    private static int nextIndex = -1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Get<T>() where T : struct, IBaseComponent => Index<T>.value;

    internal static int Create<T>() where T : struct, IBaseComponent {
      /* Thread-safety isn't a huge concern here, especially if using only a single World object,
       * but testing frameworks like Xunit like to run tests in parallel, in which case multiple
       * threads might try to register a component with the same index.
       * 
       * This safeguard makes sure indices are created properly in a multithreaded context.
       */
      if (Interlocked.CompareExchange(ref Index<T>.value, nextIndex + 1, MAX_INDEX) == MAX_INDEX) {
        Interlocked.Increment(ref nextIndex);
      }
      return Index<T>.value;
    }
  }
}

