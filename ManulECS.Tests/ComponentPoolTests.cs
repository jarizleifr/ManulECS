using System.Collections.Generic;
using Xunit;

namespace ManulECS.Tests {
  public class ComponentPoolFacts : PoolFacts {
    private static Component1 Value(uint value) => new() { value = value };
    private readonly Pool<Component1> pool;

    private static Pool<Component1> GetPool() => new(new Key(0, 1u));

    protected override List<Entity> CreateTestEntities(int count) {
      var entities = new List<Entity>();
      for (uint i = 0; i < count; i++) {
        var entity = new Entity(i, 0);
        pool.Set(entity.Id, Value(i));
        entities.Add(entity);
      }
      return entities;
    }

    public ComponentPoolFacts() {
      pool = GetPool();
      untypedPool = pool;
    }

    [Fact]
    public void UpdatesCount_OnSet() {
      CreateTestEntities(3);
      Assert.Equal(3, pool.Count);
    }

    [Fact]
    public void UpdatesValue_OnSet() {
      var entity = CreateTestEntities(3)[2];
      pool.Set(entity.Id, Value(300));
      Assert.Equal(300u, pool[entity].value);
    }

    [Fact]
    public void InvokesOnUpdate_OnSet() {
      bool called = false;
      pool.OnUpdate += () => called = true;
      CreateTestEntities(1);
      Assert.True(called);
    }

    [Fact]
    public void GetsValue() {
      var entities = CreateTestEntities(100);
      Assert.Equal(1u, pool[entities[1]].value);
      Assert.Equal(5u, pool[entities[5]].value);
      Assert.Equal(80u, pool[entities[80]].value);
    }

    [Fact]
    public void SetsUntypedComponent() {
      var e1 = new Entity(0, 0);
      var e2 = new Entity(1, 0);
      untypedPool.SetObject(e1.Id, new Component1 { value = 1 });
      untypedPool.SetObject(e2.Id, new Component1 { value = 2 });
      Assert.Equal(2, untypedPool.Count);
      Assert.Equal(1u, ((Component1)untypedPool.Get(e1.Id)).value);
      Assert.Equal(2u, ((Component1)untypedPool.Get(e2.Id)).value);
    }

    [Fact]
    public void GetsCorrectValue_AfterReplacingIntermediateWithLast() {
      var entities = CreateTestEntities(3);
      pool.Remove(entities[0].Id);
      Assert.Equal(2u, pool[entities[2]].value);
    }

    [Fact]
    public void SetsAttributes_OnConstruct() {
      var pool1 = new Pool<DiscardEntity>(new Key(0, 1u));
      Assert.True(pool1.Omit == Omit.Entity);
      Assert.Null(pool1.Profile);

      var pool2 = new Pool<ProfileComponent1>(new Key(0, 1u));
      Assert.True(pool2.Omit == Omit.None);
      Assert.Equal("test-profile", pool2.Profile);
    }
  }

  public abstract class PoolFacts {
    protected Pool untypedPool;
    protected abstract List<Entity> CreateTestEntities(int count);

    [Fact]
    public void InvokesOnUpdate_OnRemove() {
      var entity = CreateTestEntities(3)[1];
      bool called = false;
      untypedPool.OnUpdate += () => called = true;
      untypedPool.Remove(entity.Id);
      Assert.True(called);
    }

    [Fact]
    public void ResizesAutomatically() {
      Assert.Equal(4, untypedPool.Capacity);
      CreateTestEntities(5);
      Assert.True(untypedPool.Capacity > 4);
    }

    [Fact]
    public void UpdatesCount_OnRemovingLastValue() {
      var entities = CreateTestEntities(3);
      untypedPool.Remove(entities[2].Id);
      Assert.Equal(2, untypedPool.Count);
    }

    [Fact]
    public void Gets_Indices() {
      CreateTestEntities(3);
      var list = new List<uint>();
      foreach (var id in untypedPool.AsSpan()) {
        list.Add(id);
      }
      Assert.Contains(0u, list);
      Assert.Contains(1u, list);
      Assert.Contains(2u, list);
      Assert.Equal(3, list.Count);
    }

    [Fact]
    public void Gets_OnlyAliveIndices() {
      var entities = CreateTestEntities(5);
      untypedPool.Remove(entities[0].Id);
      untypedPool.Remove(entities[1].Id);
      untypedPool.Remove(entities[3].Id);
      var list = new List<uint>();
      foreach (var id in untypedPool.AsSpan()) {
        list.Add(id);
      }
      Assert.Equal(2, list.Count);
      Assert.Contains(2u, list);
      Assert.Contains(4u, list);
    }

    [Fact]
    public void ClearsCount_OnClear() {
      CreateTestEntities(5);
      untypedPool.Clear();
      Assert.Equal(0, untypedPool.Count);
    }

    [Fact]
    public void InvokesOnUpdate_OnClear() {
      CreateTestEntities(5);
      bool called = false;
      untypedPool.OnUpdate += () => called = true;
      untypedPool.Clear();
      Assert.True(called);
    }

    [Fact]
    public void InvokesOnUpdate_OnReset() {
      CreateTestEntities(5);
      bool called = false;
      untypedPool.OnUpdate += () => called = true;
      untypedPool.Reset();
      Assert.True(called);
    }

    [Fact]
    public void Resets() {
      CreateTestEntities(5);
      untypedPool.Reset();
      var list = new List<uint>();
      foreach (var id in untypedPool.AsSpan()) {
        list.Add(id);
      }
      Assert.Empty(list);
      Assert.Equal(0, untypedPool.Count);
    }
  }
}
