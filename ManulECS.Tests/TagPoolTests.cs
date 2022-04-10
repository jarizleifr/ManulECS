using Xunit;

namespace ManulECS.Tests {
  public class TagPoolTests {
    private struct SomeTag : ITag { }

    private readonly TagPool<SomeTag> pool;
    public TagPoolTests() => pool = new TagPool<SomeTag>(new Flag(0, 1u));

    [Fact]
    public void SettingNewTags_IncrementsCountProperly() {
      pool.Set(new Entity(0));
      pool.Set(new Entity(1));
      pool.Set(new Entity(2));

      Assert.Equal(3, pool.Count);
    }

    [Fact]
    public void HasTag_WhenSet() {
      pool.Set(new Entity(0));
      pool.Set(new Entity(1));
      pool.Set(new Entity(2));

      Assert.True(pool.Has(new Entity(2)));
    }

    [Fact]
    public void Setting_IncrementsVersion() {
      var oldVersion = pool.Version;
      pool.Set(new Entity(0));

      Assert.Equal(oldVersion + 1, pool.Version);
    }

    [Fact]
    public void Unsetting_IncrementsVersion() {
      var oldVersion = pool.Version;
      pool.Remove(new Entity(0));

      Assert.Equal(oldVersion + 1, pool.Version);
    }

    [Fact]
    public void CanSetMoreValuesThanInitialSize() {
      for (uint i = 0; i < 256; i++) {
        pool.Set(new Entity(i));
      }

      Assert.Equal(256, pool.Count);
    }

    [Fact]
    public void LastIdPersistsCorrectly_WhenFirstItemIsUnset() {
      pool.Set(new Entity(0));
      pool.Set(new Entity(1));
      pool.Set(new Entity(2));

      pool.Remove(new Entity(0));

      Assert.True(pool.Has(new Entity(2)));
    }

    [Fact]
    public void GetCorrectCount_AfterRemovingLastValue() {
      pool.Set(new Entity(0));
      pool.Set(new Entity(1));
      pool.Set(new Entity(2));

      pool.Remove(new Entity(2));

      Assert.Equal(2, pool.Count);
    }

    [Fact]
    public void GetIndices_ReturnsCorrectValues() {
      pool.Set(new Entity(3));
      pool.Set(new Entity(2));
      pool.Set(new Entity(1));

      var ids = pool.Indices.ToArray();
      Assert.Contains(3u, ids);
      Assert.Contains(2u, ids);
      Assert.Contains(1u, ids);
    }

    [Fact]
    public void GetIndices_HasCorrectLength() {
      pool.Set(new Entity(1));
      pool.Set(new Entity(2));
      pool.Set(new Entity(3));
      pool.Set(new Entity(4));
      pool.Set(new Entity(5));

      var ids = pool.Indices.ToArray();
      Assert.Equal(5, ids.Length);
    }

    [Fact]
    public void GetIndices_OmitsRemovedIds() {
      pool.Set(new Entity(1));
      pool.Set(new Entity(5));
      pool.Set(new Entity(3));
      pool.Set(new Entity(2));
      pool.Set(new Entity(4));

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
      pool.Set(new Entity(1));
      pool.Set(new Entity(2));
      pool.Clear();
      Assert.Equal(0, pool.Count);
    }

    [Fact]
    public void Reset_SetsToInitialState() {
      pool.Set(new Entity(1));
      pool.Set(new Entity(2));
      pool.Reset();
      var ids = pool.Indices.ToArray();
      Assert.Empty(ids);
      Assert.Equal(0, pool.Count);
      Assert.Equal(0, pool.Version);
    }
  }
}
