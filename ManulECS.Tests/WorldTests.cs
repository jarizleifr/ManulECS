using System;
using System.Linq;
using Xunit;

namespace ManulECS.Tests {
  [Collection("World")]
  public class WorldTests : TestContext {
    public WorldTests() {
      for (int i = 0; i < 10; i++) {
        world.Handle().Assign(new Component1 { });
        world.Handle().Assign(new Component1 { }).Assign(new Component2 { });
      }
    }

    [Fact]
    public void Creates_MultipleWorlds() {
      var otherWorld = new World();
      otherWorld.Declare<Component1>().Declare<Component2>();

      for (int i = 0; i < 10; i++) {
        otherWorld.Handle().Assign(new Component1 { });
        otherWorld.Handle().Assign(new Component1 { }).Assign(new Component2 { });
      }
      Assert.Equal(20, otherWorld.EntityCount);
      Assert.Equal(20, otherWorld.Count<Component1>());
      Assert.Equal(10, otherWorld.Count<Component2>());
    }

    [Fact]
    public void CreatesTagPool() {
      var pool = world.TagPool<Tag>();
      Assert.IsType<TagPool<Tag>>(pool);
    }

    [Fact]
    public void CreatesComponentPool() {
      var pool = world.Pool<Component1>();
      Assert.IsType<Pool<Component1>>(pool);
    }

    [Fact]
    public void ThrowsException_OnDeclare_WhenAlreadyDeclaredComponent() {
      Assert.Throws<Exception>(() => world.Declare<Component1>());
    }

    [Fact]
    public void Clears() {
      world.Clear();
      Assert.Equal(0, world.EntityCount);
      Assert.Equal(0, world.Count<Component1>());
      Assert.Equal(0, world.Count<Component2>());
    }

    [Fact]
    public void ClearsSpecificComponents() {
      world.Clear<Component1>();
      Assert.Equal(0, world.Pool<Component1>().Count);
      Assert.NotEqual(0, world.Pool<Component2>().Count);
    }

    [Fact]
    public void UpdatesEntityCount_WhenEntityRemoved() {
      var entities = world.Entities.ToList();
      world.Remove(entities[0]);
      Assert.Equal(entities.Count - 1, world.EntityCount);
    }

    [Fact]
    public void AssignsComponent() {
      var e = world.Create();
      world.Assign(e, new Component1 { value = 100u });
      var comp = world.GetRef<Component1>(e);
      Assert.Equal(100u, comp.value);
    }

    [Fact]
    public void ReplacesComponent() {
      var e = world.Create();
      world.Assign(e, new Component1 { value = 100u });
      world.Patch(e, new Component1 { value = 200u });

      var comp = world.GetRef<Component1>(e);
      Assert.Equal(200u, comp.value);
    }
  }
}
