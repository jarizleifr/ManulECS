using Xunit;

namespace ManulECS.Tests {
  public struct SomeComp : IComponent { public uint value; }
  public class DenseComponentPoolTests : ComponentPoolFacts<DenseComponentPool<SomeComp>> {
    public override ComponentPool<SomeComp> GetPool(int initialSize) =>
        new DenseComponentPool<SomeComp>(new Flag(0, 1u));
  }
  public class SparseComponentPoolTests : ComponentPoolFacts<SparseComponentPool<SomeComp>> {
    public override ComponentPool<SomeComp> GetPool(int initialSize) =>
        new SparseComponentPool<SomeComp>(new Flag(0, 1u));
  }

  ///<summary>Unit tests for low-level ComponentPool operations.</summary>
  public abstract class ComponentPoolFacts<T> where T : ComponentPool<SomeComp> {
    public abstract ComponentPool<SomeComp> GetPool(int initialSize = 128);

    private readonly ComponentPool<SomeComp> pool;
    public ComponentPoolFacts() => pool = GetPool();

    private static SomeComp Value(uint value) => new() { value = value };

    [Fact]
    public void SettingNewValues_IncrementsCountProperly() {
      pool.Set(new Entity(0), Value(100));
      pool.Set(new Entity(1), Value(200));
      pool.Set(new Entity(2), Value(300));

      Assert.Equal(3, pool.Count);
    }

    [Fact]
    public void UpdatingValue_ReturnsCorrectValue() {
      pool.Set(new Entity(0), Value(100));
      pool.Set(new Entity(1), Value(200));
      pool.Set(new Entity(2), Value(123));

      pool.Set(new Entity(2), Value(300));

      Assert.Equal(300u, pool.GetRef(new Entity(2)).value);
    }

    [Fact]
    public void Setting_IncrementsVersion() {
      var oldVersion = pool.Version;
      pool.Set(new Entity(0), Value(123));

      Assert.Equal(oldVersion + 1, pool.Version);
    }

    [Fact]
    public void Removing_IncrementsVersion() {
      var oldVersion = pool.Version;
      pool.Remove(new Entity(0));

      Assert.Equal(oldVersion + 1, pool.Version);
    }

    [Fact]
    public void GettingByIndex_ReturnsCorrectValue() {
      pool.Set(new Entity(0), Value(100));
      pool.Set(new Entity(64000), Value(200));
      pool.Set(new Entity(123), Value(300));

      Assert.Equal(100u, pool.GetRef(new Entity(0)).value);
      Assert.Equal(200u, pool.GetRef(new Entity(64000)).value);
      Assert.Equal(300u, pool.GetRef(new Entity(123)).value);
    }

    [Fact]
    public void CanAddMoreValuesThanInitialSize() {
      for (uint i = 0; i < 256; i++) {
        pool.Set(new Entity(i), Value(i * 100));
      }

      Assert.Equal(256, pool.Count);
    }

    [Fact]
    public void GetRefForLastItem_ReturnsCorrectValue_AfterRemovingFirstValue() {
      pool.Set(new Entity(0), Value(100));
      pool.Set(new Entity(1), Value(200));
      pool.Set(new Entity(2), Value(300));

      pool.Remove(new Entity(0));

      Assert.Equal(300u, pool.GetRef(new Entity(2)).value);
    }

    [Fact]
    public void GetCorrectCount_AfterRemovingLastValue() {
      pool.Set(new Entity(0), Value(100));
      pool.Set(new Entity(1), Value(200));
      pool.Set(new Entity(2), Value(300));

      pool.Remove(new Entity(2));

      Assert.Equal(2, pool.Count);
    }

    [Fact]
    public void GetIndices_ReturnsCorrectValues() {
      pool.Set(new Entity(3), Value(300));
      pool.Set(new Entity(2), Value(200));
      pool.Set(new Entity(1), Value(100));

      var ids = pool.Indices.ToArray();
      Assert.Contains(3u, ids);
      Assert.Contains(2u, ids);
      Assert.Contains(1u, ids);
    }

    [Fact]
    public void GetIndices_HasCorrectLength() {
      pool.Set(new Entity(1), Value(100));
      pool.Set(new Entity(2), Value(200));
      pool.Set(new Entity(3), Value(300));
      pool.Set(new Entity(4), Value(300));
      pool.Set(new Entity(5), Value(300));

      var ids = pool.Indices.ToArray();
      Assert.Equal(5, ids.Length);
    }

    [Fact]
    public void GetIndices_OmitsRemovedIds() {
      pool.Set(new Entity(1), Value(100));
      pool.Set(new Entity(5), Value(300));
      pool.Set(new Entity(3), Value(300));
      pool.Set(new Entity(2), Value(200));
      pool.Set(new Entity(4), Value(300));

      pool.Remove(new Entity(1));
      pool.Remove(new Entity(2));
      pool.Remove(new Entity(3));

      var ids = pool.Indices.ToArray();
      Assert.Equal(2, ids.Length);
      Assert.Contains(4u, ids);
      Assert.Contains(5u, ids);
    }

    [Fact]
    public void Clear_SetsCount_ToZero() {
      pool.Set(new Entity(1), Value(100));
      pool.Set(new Entity(2), Value(200));
      pool.Clear();
      Assert.Equal(0, pool.Count);
    }

    [Fact]
    public void Reset_SetsToInitialState() {
      pool.Set(new Entity(1), Value(100));
      pool.Set(new Entity(2), Value(200));
      pool.Reset();
      var ids = pool.Indices.ToArray();
      Assert.Empty(ids);
      Assert.Equal(0, pool.Count);
      Assert.Equal(0, pool.Version);
    }
  }
}
