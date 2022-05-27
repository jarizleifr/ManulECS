using Xunit;

namespace ManulECS.Tests {
  public class ViewTests {
    private World world;

    public ViewTests() {
      world = new World();
      for (int i = 0; i < 10; i++) {
        world.Handle().Assign(new Component1 { });
        world.Handle().Assign(new Component1 { }).Assign(new Component2 { });
        world.Handle().Assign(new Component1 { }).Assign(new Component2 { }).Assign(new Component3 { });
      }
    }

    [Fact]
    public void CountsEntities_WhenViewHasOneComponent() =>
      Assert.Equal(30, world.View<Component1>().Length);

    [Fact]
    public void CountsEntities_WhenViewHasTwoComponents() =>
      Assert.Equal(20, world.View<Component1, Component2>().Length);

    [Fact]
    public void CountsEntities_WhenViewHasThreeComponents() =>
      Assert.Equal(10, world.View<Component1, Component2, Component3>().Length);

    [Fact]
    public void UpdatesView_WhenComponentAdded() {
      var view = world.View<Component1, Component2>();
      Assert.Equal(20, view.Length);

      world.Handle().Assign(new Component1 { }).Assign(new Component2 { });
      view = world.View<Component1, Component2>();
      Assert.Equal(21, view.Length);
    }

    [Fact]
    public void UpdatesView_WhenComponentRemoved() {
      var e1 = world.Handle().Assign(new Component1 { }).Assign(new Component2 { });
      var view = world.View<Component1, Component2>();
      Assert.Equal(21, view.Length);

      world.Remove(e1);
      view = world.View<Component1, Component2>();
      Assert.Equal(20, view.Length);
    }

    [Fact]
    public void CachesView_WhenUsedFirstTime() {
      var key = world.pools.GetKey<Component1, Component2>();
      Assert.False(world.views.ContainsKey(key));

      world.View<Component1, Component2>();
      Assert.True(world.views.ContainsKey(key));
    }

    [Fact]
    public void PersistsChanges_WhenComponentChanged_OnIteration() {
      foreach (var e in world.View<Component1>()) {
        ref var c1 = ref world.Get<Component1>(e);
        c1.value = 100u;
      }
      foreach (var e in world.View<Component1>()) {
        Assert.Equal(100u, world.Get<Component1>(e).value);
      }
    }

    [Fact]
    public void RemovesEntity_WhenRemovingEntity_OnIteration() {
      foreach (var e in world.View<Component1, Component2>()) {
        world.Remove(e);
      }
      Assert.Equal(10, world.View<Component1>().Length);
      Assert.Equal(0, world.View<Component2>().Length);
    }

    [Fact]
    public void RemovesComponent_WhenRemovingComponent_OnIteration() {
      foreach (var e in world.View<Component1, Component2>()) {
        world.Remove<Component1>(e);
      }
      Assert.Equal(10, world.View<Component1>().Length);
      Assert.Equal(20, world.View<Component2>().Length);
    }
  }
}
