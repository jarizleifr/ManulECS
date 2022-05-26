using Xunit;

namespace ManulECS.Tests {
  public class EntityTests {
    private World world;
    public EntityTests() => world = new World();

    [Fact]
    public void CreatesCorrectValues() {
      var entity = new Entity(666, 42);
      Assert.Equal(666u, entity.Id);
      Assert.Equal(42, entity.Version);
    }
    [Fact]
    public void UpdatesCount_OnCreate() {
      world.Create();
      world.Create();
      world.Create();

      Assert.Equal(3, world.Count());
    }

    [Fact]
    public void CreatesEntityIdsSequentially() {
      var e1 = world.Create();
      var e2 = world.Create();
      var e3 = world.Create();

      Assert.Equal(0u, e1.Id);
      Assert.Equal(1u, e2.Id);
      Assert.Equal(2u, e3.Id);
    }

    [Fact]
    public void ResizesAutomatically() {
      for (int i = 0; i < 256; i++) {
        world.Create();
      }
      Assert.Equal(256, world.Count());
    }

    [Fact]
    public void Recycles_Entities() {
      world.Create();
      var entity = world.Create();
      world.Create();

      world.Remove(entity);

      var newEntity = world.Create();

      Assert.Equal(1u, newEntity.Id);
      Assert.Equal(1, newEntity.Version);
    }

    [Fact]
    public void Recycles_MultipleEntities() {
      world.Create();
      var e1 = world.Create();
      var e2 = world.Create();

      world.Remove(e1);
      world.Remove(e2);

      e2 = world.Create();
      e1 = world.Create();

      Assert.Equal(2u, e2.Id);
      Assert.Equal(1u, e1.Id);
    }

    [Fact]
    public void UpdatesVersion_OnRecycling() {
      world.Create();
      var e1 = world.Create();
      var e2 = world.Create();

      world.Remove(e1);
      world.Remove(e2);

      e1 = world.Create();
      e2 = world.Create();

      world.Remove(e2);

      e2 = world.Create();

      Assert.Equal(1, e1.Version);
      Assert.Equal(2, e2.Version);
    }

    [Fact]
    public void UpdatesIdSequentially_WhenOutOfRecycledIds() {
      world.Create();
      var e1 = world.Create();
      var e2 = world.Create();

      world.Remove(e1);
      world.Remove(e2);

      world.Create();
      world.Create();
      world.Create();

      var newEntity = world.Create();

      Assert.Equal(4u, newEntity.Id);
    }

    [Fact]
    public void WontRemove_WhenAlreadyRemoved() {
      world.Create();
      var e1 = world.Create();
      var e2 = world.Create();

      world.Remove(e1);

      Assert.False(world.Remove(e1));
      Assert.True(world.Remove(e2));
    }

    [Fact]
    public void ClonesEntity() {
      var origin = world.Create();
      world.Assign(origin, new Component1 { value = 42 });
      world.Assign(origin, new Component2 { value = 127 });

      var clone = world.Clone(origin);
      var c1 = world.Get<Component1>(clone);
      var c2 = world.Get<Component2>(clone);

      var d1 = world.EntityKey(origin.Id);
      var d2 = world.EntityKey(clone.Id);

      Assert.Equal(d1, d2);
      Assert.Equal(2, world.Pool<Component1>().Count);
      Assert.Equal(2, world.Pool<Component2>().Count);
      Assert.Equal(42u, c1.value);
      Assert.Equal(127, c2.value);
    }
  }
}
