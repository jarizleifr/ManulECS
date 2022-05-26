using System.Collections.Generic;
using Xunit;

namespace ManulECS.Tests {
  public class WorldTests {
    private World world;

    public WorldTests() {
      world = new World();
      for (int i = 0; i < 10; i++) {
        world.Handle().Assign(new Component1 { });
        world.Handle().Assign(new Component1 { }).Assign(new Component2 { });
      }
    }

    [Fact]
    public void Creates_MultipleWorlds() {
      var otherWorld = new World();
      for (int i = 0; i < 10; i++) {
        otherWorld.Handle().Assign(new Component1 { });
        otherWorld.Handle().Assign(new Component1 { }).Assign(new Component2 { });
      }
      Assert.Equal(20, otherWorld.Count());
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
    public void Clears() {
      world.Clear();
      Assert.Equal(0, world.Count());
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
    public void UpdatesCount_WhenEntityRemoved() {
      var count = world.Count();
      world.Remove(world[0]);
      Assert.Equal(count - 1, world.Count());
    }

    [Fact]
    public void AssignsComponent() {
      var e = world.Create();
      world.Assign(e, new Component1 { value = 100u });
      var comp = world.Get<Component1>(e);
      Assert.Equal(100u, comp.value);
      Assert.True(world.EntityKey(e.Id)[world.Key<Component1>()]);
    }

    [Fact]
    public void PatchesComponent() {
      Entity e = world.Handle().Assign(new Component1 { value = 100u });
      world.Patch(e, new Component1 { value = 200u });

      var comp = world.Get<Component1>(e);
      Assert.Equal(200u, comp.value);
      Assert.True(world.EntityKey(e.Id)[world.Key<Component1>()]);
    }

    [Fact]
    public void RemovesComponent() {
      Entity e = world.Handle().Assign(new Component1 { value = 100u });
      Assert.True(world.Has<Component1>(e));
      Assert.True(world.EntityKey(e.Id)[world.Key<Component1>()]);

      world.Remove<Component1>(e);
      Assert.False(world.Has<Component1>(e));
      Assert.False(world.EntityKey(e.Id)[world.Key<Component1>()]);
    }

    [Fact]
    public void Tags() {
      Entity e = world.Handle().Tag<Tag>();
      Assert.True(world.Has<Tag>(e));
      Assert.True(world.EntityKey(e.Id)[world.Key<Tag>()]);
    }

    [Fact]
    public void Untags() {
      Entity e = world.Handle().Tag<Tag>();
      Assert.True(world.Has<Tag>(e));
      Assert.True(world.EntityKey(e.Id)[world.Key<Tag>()]);

      world.Remove<Tag>(e);
      Assert.False(world.Has<Tag>(e));
      Assert.False(world.EntityKey(e.Id)[world.Key<Tag>()]);
    }

    [Fact]
    public void AssignsObject() {
      var e = world.Create();
      world.AssignObject(e, typeof(Component1), new Component1 { value = 100u });
      world.AssignObject(e, typeof(Tag), null);
      var comp = world.Get<Component1>(e);
      Assert.Equal(100u, comp.value);
      Assert.True(world.Has<Tag>(e));
      Assert.True(world.EntityKey(e.Id)[world.Key<Tag>()]);
    }

    [Fact]
    public void CreatesNewKeyFlagsSequentially() {
      var key = world.Key<Component1>() + world.Key<Component2>() + world.Key<Component3>();
      var list = new List<int>();
      foreach (var idx in key) {
        list.Add(idx);
      }
      Assert.Contains(0, list);
      Assert.Contains(1, list);
      Assert.Contains(2, list);
    }
  }
}
