using System.Collections.Generic;
using Xunit;

namespace ManulECS.Tests {
  public class DenseComponentPoolTests : ComponentPoolFacts<DensePool<Component1>> {
    protected override Pool<Component1> GetPool() =>
      new DensePool<Component1>() { Key = new Key(0, 1u) };
  }

  public class SparseComponentPoolTests : ComponentPoolFacts<SparsePool<Component1>> {
    protected override Pool<Component1> GetPool() =>
      new SparsePool<Component1>() { Key = new Key(0, 1u) };
  }

  public abstract class ComponentPoolFacts<T> : ComponentPoolFacts where T : Pool<Component1> {
    private static Component1 Value(uint value) => new() { value = value };
    private readonly Pool<Component1> pool;

    protected abstract Pool<Component1> GetPool();

    protected override List<Entity> CreateTestEntities(int count) {
      var entities = new List<Entity>();
      for (uint i = 0; i < count; i++) {
        var entity = new Entity(i);
        pool.Set(entity, Value(i));
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
      pool.Set(entity, Value(300));
      Assert.Equal(300u, pool.GetRef(entity).value);
    }

    [Fact]
    public void UpdatesVersion_OnSet() {
      var oldVersion = pool.Version;
      CreateTestEntities(1);
      Assert.Equal(oldVersion + 1, pool.Version);
    }

    [Fact]
    public void GetsValue() {
      var entities = CreateTestEntities(100);
      Assert.Equal(1u, pool.GetRef(entities[1]).value);
      Assert.Equal(5u, pool.GetRef(entities[5]).value);
      Assert.Equal(80u, pool.GetRef(entities[80]).value);
    }

    [Fact]
    public void GetsCorrectValue_AfterReplacingIntermediateWithLast() {
      var entities = CreateTestEntities(3);
      pool.Remove(entities[0]);
      Assert.Equal(2u, pool.GetRef(entities[2]).value);
    }
  }

  public abstract class ComponentPoolFacts {
    protected Pool untypedPool;
    protected abstract List<Entity> CreateTestEntities(int count);

    [Fact]
    public void UpdatesVersion_OnRemove() {
      var entity = CreateTestEntities(3)[1];
      var oldVersion = untypedPool.Version;
      untypedPool.Remove(entity);
      Assert.Equal(oldVersion + 1, untypedPool.Version);
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
      untypedPool.Remove(entities[2]);
      Assert.Equal(2, untypedPool.Count);
    }

    [Fact]
    public void Gets_Indices() {
      CreateTestEntities(3);
      var ids = untypedPool.Indices.ToArray();
      Assert.Contains(0u, ids);
      Assert.Contains(1u, ids);
      Assert.Contains(2u, ids);
      Assert.Equal(3, ids.Length);
    }

    [Fact]
    public void Gets_OnlyAliveIndices() {
      var entities = CreateTestEntities(5);
      untypedPool.Remove(entities[0]);
      untypedPool.Remove(entities[1]);
      untypedPool.Remove(entities[3]);
      var ids = untypedPool.Indices.ToArray();
      Assert.Equal(2, ids.Length);
      Assert.Contains(2u, ids);
      Assert.Contains(4u, ids);
    }

    [Fact]
    public void ClearsCount_OnClear() {
      CreateTestEntities(5);
      untypedPool.Clear();
      Assert.Equal(0, untypedPool.Count);
    }

    [Fact]
    public void Resets() {
      CreateTestEntities(5);
      untypedPool.Reset();
      var ids = untypedPool.Indices.ToArray();
      Assert.Empty(ids);
      Assert.Equal(0, untypedPool.Count);
      Assert.Equal(0, untypedPool.Version);
    }
  }
}
