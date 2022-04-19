using System.Linq;
using Xunit;

namespace ManulECS.Tests {
  [Collection("World")]
  public class ViewTests : TestContext {
    public ViewTests() {
      for (int i = 0; i < 10; i++) {
        world.Handle().Assign(new Component1 { });
        world.Handle().Assign(new Component1 { }).Assign(new Component2 { });
        world.Handle().Assign(new Component1 { }).Assign(new Component2 { }).Assign(new Component3 { });
      }
    }

    [Fact]
    public void CountsEntities_WhenViewHasOneComponent() =>
      Assert.Equal(30, world.View<Component1>().Count);

    [Fact]
    public void CountsEntities_WhenViewHasTwoComponents() =>
      Assert.Equal(20, world.View<Component1, Component2>().Count);

    [Fact]
    public void CountsEntities_WhenViewHasThreeComponents() =>
      Assert.Equal(10, world.View<Component1, Component2, Component3>().Count);

    [Fact]
    public void UpdatesView_WhenComponentAdded() {
      var view = world.View<Component1, Component2>();
      Assert.Equal(20, view.Count);

      world.Handle().Assign(new Component1 { }).Assign(new Component2 { });
      view = world.View<Component1, Component2>();
      Assert.Equal(21, view.Count);
    }

    [Fact]
    public void UpdatesView_WhenComponentRemoved() {
      var e1 = world.Handle().Assign(new Component1 { }).Assign(new Component2 { }).GetEntity();
      var view = world.View<Component1, Component2>();
      Assert.Equal(21, view.Count);

      world.Remove(e1);
      view = world.View<Component1, Component2>();
      Assert.Equal(20, view.Count);
    }

    [Fact]
    public void CachesView_WhenUsedFirstTime() {
      var matcher = world.Matcher<Component1>() + world.Matcher<Component2>();
      Assert.False(world.viewCache.Contains(matcher));

      world.View<Component1, Component2>();
      Assert.True(world.viewCache.Contains(matcher));
    }

    [Fact]
    public void PersistsChanges_WhenComponentChanged_OnIteration() {
      foreach (var e in world.View<Component1>()) {
        ref var c1 = ref world.GetRef<Component1>(e);
        c1.value = 100u;
      }
      foreach (var e in world.View<Component1>()) {
        Assert.Equal(100u, world.GetRef<Component1>(e).value);
      }
    }

    [Fact]
    public void RemovesEntity_WhenRemovingEntity_OnIteration() {
      foreach (var e in world.View<Component1, Component2>()) {
        world.Remove(e);
      }
      Assert.Equal(10, world.View<Component1>().Count);
      Assert.Equal(0, world.View<Component2>().Count);
    }

    [Fact]
    public void RemovesComponent_WhenRemovingComponent_OnIteration() {
      foreach (var e in world.View<Component1, Component2>()) {
        world.Remove<Component1>(e);
      }
      Assert.Equal(10, world.View<Component1>().Count);
      Assert.Equal(20, world.View<Component2>().Count);
    }
  }
}
