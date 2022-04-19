using System;
using System.Collections.Generic;

namespace ManulECS {
  internal class Pools {
    private readonly HashSet<Type> registered = new();
    internal Pool[] typed = new Pool[4];
    internal Pool[] flagged = new Pool[4];

    private (int index, uint bits) nextFlag = (0, 1u);
    internal (int index, uint bits) GetNextFlag() {
      var flag = nextFlag;
      nextFlag = nextFlag.bits != 0x8000_0000
        ? (nextFlag.index, nextFlag.bits << 1)
        : (nextFlag.index + 1, 1u);
      return flag;
    }

    internal void Register<T>() where T : struct, IBaseComponent {
      var type = typeof(T);

      if (registered.Contains(type)) {
        throw new Exception($"Component/Tag {typeof(T)} already registered!");
      }
      registered.Add(type);

      var flag = GetNextFlag();
      var (flagIndex, typeIndex) = (
        BitUtil.Position(flag.index, flag.bits),
        TypeIndex.Create<T>()
      );

      if (flagIndex == Matcher.MAX_SIZE * 32) {
        throw new Exception($"{Matcher.MAX_SIZE * 32} component maximum exceeded!");
      }

      var matcher = new Matcher(flag.index, flag.bits);
      Pool pool = type switch {
        var t when IsTag(t) && IsDense(t) => new DenseTagPool<T> { Matcher = matcher },
        var t when IsTag(t) && !IsDense(t) => new SparseTagPool<T> { Matcher = matcher },
        var t when !IsTag(t) && IsDense(t) => new DensePool<T> { Matcher = matcher },
        var t when !IsTag(t) && !IsDense(t) => new SparsePool<T> { Matcher = matcher },
        _ => throw new Exception($"Unsupported component pool type in component {type}!")
      };

      ArrayUtil.EnsureSize(typeIndex, ref typed, null);
      typed[typeIndex] = pool;
      ArrayUtil.EnsureSize(flagIndex, ref flagged, null);
      flagged[flagIndex] = pool;

      bool IsTag(Type type) => typeof(ITag).IsAssignableFrom(type);
      bool IsDense(Type type) => type.IsDefined(typeof(DenseAttribute), false);
    }

    internal void Clear() => Array.ForEach(flagged, pool => pool?.Reset());
  }
}
