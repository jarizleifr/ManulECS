using System.Collections.Generic;
using Xunit;

namespace ManulECS.Tests {
  public class DenseTagPoolTests : TagPoolFacts<DenseTagPool<Tag>> {
    protected override TagPool<Tag> GetPool() => new DenseTagPool<Tag>() { Matcher = new Matcher(0, 1u) };
  }

  public class SparseTagPoolTests : TagPoolFacts<SparseTagPool<Tag>> {
    protected override TagPool<Tag> GetPool() => new SparseTagPool<Tag>() { Matcher = new Matcher(0, 1u) };
  }

  public abstract class TagPoolFacts<T> : ComponentPoolFacts where T : TagPool<Tag> {
    private readonly TagPool<Tag> pool;

    protected abstract TagPool<Tag> GetPool();

    public TagPoolFacts() {
      pool = GetPool();
      untypedPool = pool;
    }

    protected override List<Entity> CreateTestEntities(int count) {
      var entities = new List<Entity>();
      for (uint i = 0; i < count; i++) {
        var entity = new Entity(i);
        pool.Set(entity);
        entities.Add(entity);
      }
      return entities;
    }

    [Fact]
    public void UpdatesCount_OnSet() {
      CreateTestEntities(3);
      Assert.Equal(3, pool.Count);
    }

    [Fact]
    public void SetsTag() {
      var entities = CreateTestEntities(3);
      Assert.True(pool.Has(entities[2]));
    }

    [Fact]
    public void UpdatesVersion_OnSet() {
      var oldVersion = pool.Version;
      CreateTestEntities(1);
      Assert.Equal(oldVersion + 1, pool.Version);
    }
  }
}
