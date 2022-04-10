using Xunit;

namespace ManulECS.Tests {
  public class ViewTests {
    private readonly World world;
    struct Comp1 : IComponent { }
    struct Comp2 : IComponent { }
    struct Comp3 : IComponent { }

    public ViewTests() {
      world = new World();
      world.Declare<Comp1>();
      world.Declare<Comp2>();
      world.Declare<Comp3>();

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
        world.Assign(e, new Comp2 { });
        world.Assign(e, new Comp3 { });
      }
    }

    [Fact]
    public void NewlyCreatedView_IsNotDirty() {
      var flags = new FlagEnum(world.GetFlag<Comp1>(), world.GetFlag<Comp2>());
      var view = world.View<Comp1, Comp2>();
      Assert.False(view.IsDirty(world, flags));
    }

    [Fact]
    public void AddingComponent_DirtiesView() {
      var flags = new FlagEnum(world.GetFlag<Comp1>(), world.GetFlag<Comp3>());
      var view = world.View<Comp1, Comp3>();
      var e = world.Create();
      world.Assign(e, new Comp1 { });

      Assert.True(view.IsDirty(world, flags));
    }

    [Fact]
    public void RemovingComponent_DirtiesView() {
      var flags = new FlagEnum(world.GetFlag<Comp1>(), world.GetFlag<Comp2>());
      var view = world.View<Comp1, Comp2>();
      var e = world.entities[0];
      world.Remove<Comp1>(e);

      Assert.True(view.IsDirty(world, flags));
    }

    [Fact]
    public void ClearingComponents_DirtiesView() {
      var flags = new FlagEnum(world.GetFlag<Comp1>(), world.GetFlag<Comp2>());
      var view = world.View<Comp1, Comp2>();
      world.GetPool<Comp1>().Clear();

      Assert.True(view.IsDirty(world, flags));
    }

    [Fact]
    public void ChangesToComponents_RecreatesViewOnLoop() {
      int count = 0;
      foreach (var _ in world.View<Comp1, Comp2>()) {
        count++;
      }
      Assert.Equal(20, count);

      var e1 = world.Create();
      world.Assign(e1, new Comp1 { });
      world.Assign(e1, new Comp2 { });

      count = 0;
      foreach (var _ in world.View<Comp1, Comp2>()) {
        count++;
      }
      Assert.Equal(21, count);

      world.Remove<Comp1>(e1);

      count = 0;
      foreach (var _ in world.View<Comp1, Comp2>()) {
        count++;
      }
      Assert.Equal(20, count);

      var e2 = world.Create();
      world.Assign(e2, new Comp1 { });
      world.Assign(e2, new Comp2 { });

      count = 0;
      foreach (var _ in world.View<Comp1, Comp2>()) {
        count++;
      }
      Assert.Equal(21, count);
    }

    [Fact]
    public void ViewIsCached_WhenLoopedFirstTime() {
      var matcher = new FlagEnum(world.GetFlag<Comp1>(), world.GetFlag<Comp2>());

      Assert.False(world.views.ContainsKey(matcher));

      world.View<Comp1, Comp2>();
      Assert.True(world.views.ContainsKey(matcher));
    }
  }
}
