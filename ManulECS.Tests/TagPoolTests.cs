using Xunit;

namespace ManulECS.Tests {
  public class TagPoolTests {
    private struct SomeTag : ITag { }

    private readonly TagPool<SomeTag> pool;
    public TagPoolTests() => pool = new TagPool<SomeTag>(new Flag(0, 1u));

    [Fact]
    public void SettingNewTags_IncrementsCountProperly() {
      pool.Set(0);
      pool.Set(1);
      pool.Set(2);

      Assert.Equal(3, pool.Count);
    }

    [Fact]
    public void HasTag_WhenSet() {
      pool.Set(0);
      pool.Set(1);
      pool.Set(2);

      Assert.True(pool.Has(2));
    }

    [Fact]
    public void Setting_IncrementsVersion() {
      var oldVersion = pool.Version;
      pool.Set(0);

      Assert.Equal(oldVersion + 1, pool.Version);
    }

    [Fact]
    public void Unsetting_IncrementsVersion() {
      var oldVersion = pool.Version;
      pool.Remove(0);

      Assert.Equal(oldVersion + 1, pool.Version);
    }

    [Fact]
    public void CanSetMoreValuesThanInitialSize() {
      for (int i = 0; i < 256; i++) {
        pool.Set((uint)i);
      }

      Assert.Equal(256, pool.Count);
    }

    [Fact]
    public void LastIdPersistsCorrectly_WhenFirstItemIsUnset() {
      pool.Set(0);
      pool.Set(1);
      pool.Set(2);

      pool.Remove(0);

      Assert.True(pool.Has(2));
    }

    [Fact]
    public void GetCorrectCount_AfterRemovingLastValue() {
      pool.Set(0);
      pool.Set(1);
      pool.Set(2);

      pool.Remove(2);

      Assert.Equal(2, pool.Count);
    }

    [Fact]
    public void GetIndices_ReturnsCorrectValues() {
      pool.Set(3);
      pool.Set(2);
      pool.Set(1);

      var ids = pool.GetIndices().ToArray();
      Assert.Contains(3u, ids);
      Assert.Contains(2u, ids);
      Assert.Contains(1u, ids);
    }

    [Fact]
    public void GetIndices_HasCorrectLength() {
      pool.Set(1);
      pool.Set(2);
      pool.Set(3);
      pool.Set(4);
      pool.Set(5);

      var ids = pool.GetIndices().ToArray();
      Assert.Equal(5, ids.Length);
    }

    [Fact]
    public void GetIndices_OmitsRemovedIds() {
      pool.Set(1);
      pool.Set(5);
      pool.Set(3);
      pool.Set(2);
      pool.Set(4);

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
