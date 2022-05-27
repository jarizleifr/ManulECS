using System.Collections.Generic;
using Xunit;

namespace ManulECS.Tests {
  public class TagPoolFacts : PoolFacts {
    private readonly TagPool<Tag1> pool;

    public TagPoolFacts() {
      pool = new(new Key(0));
      untypedPool = pool;
    }

    protected override List<Entity> CreateTestEntities(int count) {
      var entities = new List<Entity>();
      for (uint i = 0; i < count; i++) {
        var entity = new Entity(i, 0);
        pool.Set(entity.Id);
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
      Assert.True(pool.Has(entities[2].Id));
    }

    [Fact]
    public void SetsObject() {
      var e1 = new Entity(0, 0);
      var e2 = new Entity(1, 0);
      untypedPool.SetRaw(e1.Id, null);
      untypedPool.SetRaw(e2.Id, null);
      Assert.Equal(2, untypedPool.Count);
      Assert.True(pool.Has(e1.Id));
      Assert.True(pool.Has(e2.Id));
    }

    [Fact]
    public void InvokesOnUpdate_OnSet() {
      bool called = false;
      pool.OnUpdate += () => called = true;
      CreateTestEntities(1);
      Assert.True(called);
    }

    [Fact]
    public void SetsAttributes_OnConstruct() {
      var pool1 = new TagPool<OmitTag>(new Key(0));
      Assert.True(pool1.omit == Omit.Entity);
      Assert.Null(pool1.profile);

      var pool2 = new TagPool<ProfileTag>(new Key(1));
      Assert.True(pool2.omit == Omit.None);
      Assert.Equal("test-profile", pool2.profile);
    }
  }
}
