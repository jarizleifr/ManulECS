using System.Collections.Generic;
using Xunit;

namespace ManulECS.Tests {
  public class TagPoolFacts : PoolFacts {
    private readonly TagPool<Tag> pool;

    protected static TagPool<Tag> GetPool() => new(new Key(0, 1u));

    public TagPoolFacts() {
      pool = GetPool();
      untypedPool = pool;
    }

    protected override List<Entity> CreateTestEntities(int count) {
      var entities = new List<Entity>();
      for (uint i = 0; i < count; i++) {
        var entity = new Entity(i, 0);
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
    public void InvokesOnUpdate_OnSet() {
      bool called = false;
      pool.OnUpdate += () => called = true;
      CreateTestEntities(1);
      Assert.True(called);
    }
  }
}
