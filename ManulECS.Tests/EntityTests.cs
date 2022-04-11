using Xunit;

namespace ManulECS.Tests {
  public struct Component1 : IComponent { public int value; }
  public struct Component2 : IComponent { public int value; }

  public class EntityTests {
    private readonly World world;

    public EntityTests() {
      world = new World();
      world.Declare<Component1>();
      world.Declare<Component2>();
    }

    [Fact]
    public void CreatingEntities_IncrementsCountCorrectly() {
      world.Create();
      world.Create();
      world.Create();

      Assert.Equal(3, world.EntityCount);
    }

    [Fact]
    public void CreatedEntities_HaveCorrectIds() {
      var e1 = world.Create();
      var e2 = world.Create();
      var e3 = world.Create();

      Assert.Equal(0u, e1.Id);
      Assert.Equal(1u, e2.Id);
      Assert.Equal(2u, e3.Id);
    }

    [Fact]
    public void CanAddMoreEntitiesThanInitialSize() {
      for (int i = 0; i < 256; i++) {
        world.Create();
      }

      Assert.Equal(256, world.EntityCount);
    }

    [Fact]
    public void EntitiesProperty_OmitsRemovedEntity() {
      world.Create();
      var entity = world.Create();
      world.Create();

      world.Remove(entity);

      Assert.DoesNotContain(world.Entities, e => e.Id == 1);
    }

    [Fact]
    public void EntityId_GetsRecycled_WhenEntityRemoved() {
      world.Create();
      var entity = world.Create();
      world.Create();

      world.Remove(entity);

      var newEntity = world.Create();

      Assert.Equal(1u, newEntity.Id);
      Assert.Equal(1, newEntity.Version);
    }

    [Fact]
    public void MultipleEntityIds_GetRecycled_WhenEntitiesRemoved() {
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
    public void Version_GetsIncremented_WhenEntitiesRecycled() {
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
    public void NewEntityId_IsIncremented_WhenOutOfRecyclableIds() {
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
    public void Remove_ReturnsFalse_WhenRemovingAlreadyRemovedEntity() {
      world.Create();
      var e1 = world.Create();
      var e2 = world.Create();

      world.Remove(e1);

      Assert.False(world.Remove(e1));
      Assert.True(world.Remove(e2));
    }

    [Fact]
    public void ClonedEntity_HasSameComponents() {
      var origin = world.Create();
      world.Assign(origin, new Component1 { value = 42 });
      world.Assign(origin, new Component2 { value = 127 });

      var clone = world.Clone(origin);
      var c1 = world.GetRef<Component1>(clone);
      var c2 = world.GetRef<Component2>(clone);

      var d1 = world.GetEntityDataByIndex(origin.Id);
      var d2 = world.GetEntityDataByIndex(clone.Id);

      Assert.Equal(d1, d2);
      Assert.Equal(2, world.components.GetPool<Component1>().Count);
      Assert.Equal(2, world.components.GetPool<Component2>().Count);
      Assert.Equal(42, c1.value);
      Assert.Equal(127, c2.value);
    }
  }
}
