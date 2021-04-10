using Xunit;

namespace ManulECS.Tests {
  public struct SomeComp : IComponent { public int value; }
  public class DenseComponentPoolTests : ComponentPoolFacts<DenseComponentPool<SomeComp>> {
    public override IComponentPool<SomeComp> GetPool(int initialSize) =>
        new DenseComponentPool<SomeComp>(new Flag(0, 1u));
  }
  public class SparseComponentPoolTests : ComponentPoolFacts<SparseComponentPool<SomeComp>> {
    public override IComponentPool<SomeComp> GetPool(int initialSize) =>
        new SparseComponentPool<SomeComp>(new Flag(0, 1u));
  }

  ///<summary>Unit tests for low-level ComponentPool operations.</summary>
  public abstract class ComponentPoolFacts<T> where T : IComponentPool<SomeComp> {
    public abstract IComponentPool<SomeComp> GetPool(int initialSize = 128);

    private readonly IComponentPool<SomeComp> pool;
    public ComponentPoolFacts() => pool = GetPool();

    private SomeComp Value(int value) => new() { value = value };

    [Fact]
    public void SettingNewValues_IncrementsCountProperly() {
      pool.Set(0, Value(100));
      pool.Set(1, Value(200));
      pool.Set(2, Value(300));

      Assert.Equal(3, pool.Count);
    }

    [Fact]
    public void UpdatingValue_ReturnsCorrectValue() {
      pool.Set(0, Value(100));
      pool.Set(1, Value(200));
      pool.Set(2, Value(123));

      pool.Set(2, Value(300));

      Assert.Equal(300, pool.GetRef(2).value);
    }

    [Fact]
    public void Setting_IncrementsVersion() {
      var oldVersion = pool.Version;
      pool.Set(0, Value(123));

      Assert.Equal(oldVersion + 1, pool.Version);
    }

    [Fact]
    public void Removing_IncrementsVersion() {
      var oldVersion = pool.Version;
      pool.Remove(0);

      Assert.Equal(oldVersion + 1, pool.Version);
    }

    [Fact]
    public void GettingByIndex_ReturnsCorrectValue() {
      pool.Set(0, Value(100));
      pool.Set(64000, Value(200));
      pool.Set(123, Value(300));

      Assert.Equal(100, pool.GetRef(0).value);
      Assert.Equal(200, pool.GetRef(64000).value);
      Assert.Equal(300, pool.GetRef(123).value);
    }

    [Fact]
    public void CanAddMoreValuesThanInitialSize() {
      for (int i = 0; i < 256; i++) {
        pool.Set((uint)i, Value(i * 100));
      }

      Assert.Equal(256, pool.Count);
    }

    [Fact]
    public void GetRefForLastItem_ReturnsCorrectValue_AfterRemovingFirstValue() {
      pool.Set(0, Value(100));
      pool.Set(1, Value(200));
      pool.Set(2, Value(300));

      pool.Remove(0);

      Assert.Equal(300, pool.GetRef(2).value);
    }

    [Fact]
    public void GetCorrectCount_AfterRemovingLastValue() {
      pool.Set(0, Value(100));
      pool.Set(1, Value(200));
      pool.Set(2, Value(300));

      pool.Remove(2);

      Assert.Equal(2, pool.Count);
    }

    [Fact]
    public void GetIndices_ReturnsCorrectValues() {
      pool.Set(3, Value(300));
      pool.Set(2, Value(200));
      pool.Set(1, Value(100));

      var ids = pool.GetIndices().ToArray();
      Assert.Contains(3u, ids);
      Assert.Contains(2u, ids);
      Assert.Contains(1u, ids);
    }

    [Fact]
    public void GetIndices_HasCorrectLength() {
      pool.Set(1, Value(100));
      pool.Set(2, Value(200));
      pool.Set(3, Value(300));
      pool.Set(4, Value(300));
      pool.Set(5, Value(300));

      var ids = pool.GetIndices().ToArray();
      Assert.Equal(5, ids.Length);
    }

    [Fact]
    public void GetIndices_OmitsRemovedIds() {
      pool.Set(1, Value(100));
      pool.Set(5, Value(300));
      pool.Set(3, Value(300));
      pool.Set(2, Value(200));
      pool.Set(4, Value(300));

      pool.Remove(1);
      pool.Remove(2);
      pool.Remove(3);

      var ids = pool.GetIndices().ToArray();
      Assert.Equal(2, ids.Length);
      Assert.Contains(4u, ids);
      Assert.Contains(5u, ids);
    }
  }
}
