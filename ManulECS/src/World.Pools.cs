using System;
using System.Collections.Generic;

namespace ManulECS {
  public partial class World {
    private const int MAX_COMPONENTS = ManulECS.Key.MAX_SIZE * 32;
    private readonly HashSet<Type> registered = new();
    private byte[] keyToIndex = new byte[4];
    internal Pool[] indexedPools = new Pool[4];

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

      var (index, bits) = GetNextFlag();
      var (keyIndex, typeIndex) = (
        BitUtil.Position(index, bits),
        TypeIndex.Create<T>()
      );

      if (keyIndex == MAX_COMPONENTS) {
        throw new Exception($"{MAX_COMPONENTS} component maximum exceeded!");
      }

      ArrayUtil.EnsureSize<byte>(keyIndex, ref keyToIndex, 0);
      keyToIndex[keyIndex] = typeIndex;

      var key = new Key(index, bits);
      Pool pool = IsTag(type)
        ? new TagPool<T> { Key = key }
        : new Pool<T> { Key = key };

      ArrayUtil.EnsureSize(typeIndex, ref indexedPools, null);
      indexedPools[typeIndex] = pool;

      static bool IsTag(Type type) => typeof(ITag).IsAssignableFrom(type);
    }

    internal Pool PoolByKeyIndex(int index) => indexedPools[keyToIndex[index]];
  }
}
