using System;
using Xunit;

namespace ManulECS.Tests {
  public class WorldTests {
    private readonly World world;

    struct Comp1 : IComponent { public bool value; }
    struct Comp2 : IComponent { }
    struct Comp3 : IComponent { }
    struct CompOther : IComponent { public int v1; public int v2; }
    struct TagComp : ITag { }

    public WorldTests() {
      this.world = new World();
      world.Declare<Comp1>();
      world.Declare<Comp2>();
      world.Declare<Comp3>();
      world.Declare<CompOther>();

      for (int i = 0; i < 10; i++) {
        var e = world.Create();
        world.Assign(e, new Comp1 { });
      }
      for (int i = 0; i < 10; i++) {
        var e = world.Create();
        world.Assign(e, new Comp1 { });
        world.Assign(e, new Comp2 { });
      }
      for (int i = 0; i < 10; i++) {
        var e = world.Create();
        world.Assign(e, new Comp1 { });
        world.Assign(e, new Comp3 { });
      }
      for (int i = 0; i < 10; i++) {
        var e = world.Create();
        world.Assign(e, new Comp2 { });
        world.Assign(e, new Comp3 { });
      }
      for (int i = 0; i < 10; i++) {
        var e = world.Create();
        world.Assign(e, new Comp1 { });
        world.Assign(e, new Comp2 { });
        world.Assign(e, new Comp3 { });
      }
    }

    [Fact]
    public void RegisteringTag_ResultsInTagPool() {
      world.Declare<TagComp>();
      var tagPool = world.GetTagPool<TagComp>();
      Assert.IsType<TagPool<TagComp>>(tagPool);
    }

    [Fact]
    public void ReregisteringComponent_ThrowsException() {
      Assert.Throws<Exception>(() => world.Declare<Comp1>());
    }

    [Fact]
    public void ClearingWorld_RemovesEntities() {
      world.Create();
      world.Clear();
      Assert.Equal(0, world.EntityCount);
    }

    [Fact]
    public void AssignOrReplace_OverwritesComponent() {
      var e = world.Create();
      world.Assign(e, new CompOther { v1 = 100, v2 = 200 });
      world.AssignOrReplace(e, new CompOther { v1 = 10, v2 = 20 });

      var comp = world.GetRef<CompOther>(e);
      Assert.Equal(10, comp.v1);
      Assert.Equal(20, comp.v2);
    }

    [Fact]
    public void ComponentCountReturnsZero_AfterRemovingComponents() {
      world.Clear<Comp1>();
      Assert.Equal(0, world.GetPool<Comp1>().Count);
    }

    [Fact]
    public void CountReturnsZero_AfterRemovingAllEntities() {
      foreach (var e in world.Entities) {
        world.Remove(e);
      }
      Assert.Equal(0, world.EntityCount);
    }

    [Fact]
    public void SingleComponentView_CountsEntitiesCorrectly() {
      int count = 0;
      foreach (var _ in world.View<Comp1>()) {
        count++;
      }
      Assert.Equal(40, count);
    }

    [Fact]
    public void TwoComponentView_CountsEntitiesCorrectly() {
      int count = 0;
      foreach (var _ in world.View<Comp1, Comp2>()) {
        count++;
      }
      Assert.Equal(20, count);
    }

    [Fact]
    public void ThreeComponentView_CountsEntitiesCorrectly() {
      int count = 0;
      foreach (var _ in world.View<Comp1, Comp2, Comp3>()) {
        count++;
      }
      Assert.Equal(10, count);
    }

    [Fact]
    public void ModificationsToRefComponents_PersistAfterLooping() {
      foreach (var e in world.View<Comp1>()) {
        ref var c1 = ref world.GetRef<Comp1>(e);
        c1.value = true;
      }

      foreach (var e in world.View<Comp1>()) {
        ref var c1 = ref world.GetRef<Comp1>(e);
        Assert.True(c1.value);
      }
    }

    [Fact]
    public void RemovingEntitiesWhileLooping_WorksAsExpected() {
      int count = 0;
      foreach (var e in world.View<Comp1, Comp2>()) {
        world.Remove(e);
      }
      foreach (var _ in world.View<Comp1>()) {
        count++;
      }
      Assert.Equal(20, count);
    }

    [Fact]
    public void RemovingComponentsWhileLooping_WorksAsExpected() {
      int count = 0;
      foreach (var e in world.View<Comp1, Comp2>()) {
        world.Remove<Comp2>(e);
      }
      foreach (var _ in world.View<Comp2>()) {
        count++;
      }
      Assert.Equal(10, count);
    }
  }
}
