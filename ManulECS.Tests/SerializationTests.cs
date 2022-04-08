using System;
using Xunit;

namespace ManulECS.Tests {
  public struct NormalComponent1 : IComponent { }
  public struct NormalComponent2 : IComponent { }

  [SerializationProfile("test-profile")]
  public struct ProfileComponent1 : IComponent { }

  [SerializationProfile("test-profile")]
  public struct ProfileComponent2 : IComponent { }

  [NeverSerializeComponent]
  public struct ShouldNeverAppear : IComponent { }

  [NeverSerializeEntity]
  public struct EntityShouldNeverAppear : IComponent { }

  public struct ComponentWithReference1 : IComponent {
    public Entity entity;
  }

  public struct ComponentWithReference2 : IComponent {
    public Entity entity;
  }

  public class SerializationTests {
    private readonly World world;

    public SerializationTests() {
      world = new World();
      world.Declare<NormalComponent1>();
      world.Declare<NormalComponent2>();

      world.Declare<ProfileComponent1>();
      world.Declare<ProfileComponent2>();

      world.Declare<ShouldNeverAppear>();
      world.Declare<EntityShouldNeverAppear>();

      world.Declare<ComponentWithReference1>();
      world.Declare<ComponentWithReference2>();
    }

    private void CreateNormalEntities() {
      var e1 = world.Create();
      world.Assign(e1, new NormalComponent1 { });

      var e2 = world.Create();
      world.Assign(e2, new NormalComponent1 { });
      world.Assign(e2, new NormalComponent2 { });
    }

    private void CreateProfileEntities() {
      var e1 = world.Create();
      world.Assign(e1, new ProfileComponent1 { });

      var e2 = world.Create();
      world.Assign(e2, new ProfileComponent1 { });
      world.Assign(e2, new ProfileComponent2 { });
    }

    private void CreateDiscardingEntities() {
      var e1 = world.Create();
      world.Assign(e1, new NormalComponent1 { });

      var e2 = world.Create();
      world.Assign(e2, new NormalComponent1 { });
      world.Assign(e2, new NormalComponent2 { });
    }

    private void CreateEntitiesWithReferencedComponents() {
      for (int i = 0; i < 10; i++) {
        var e = world.Create();

        if (i % 2 == 0) {
          world.Assign(e, new NormalComponent1 { });
          var item = world.Create();
          world.Assign(item, new ComponentWithReference1 { entity = e });
        } else {
          world.Assign(e, new NormalComponent2 { });
          var item = world.Create();
          world.Assign(item, new ComponentWithReference2 { entity = e });
        }
      }
    }

    [Fact]
    public void Entities_WithoutComponents_WillNotSerialize() {
      for (int i = 0; i < 10; i++) world.Create();
      var json = world.Serialize();
      world.Clear();
      world.Deserialize(json);
      Assert.Equal(0, world.Count);
    }

    [Fact]
    public void NormalEntities_SerializeAndDeserialize_Properly() {
      CreateNormalEntities();
      CreateProfileEntities();
      var json = world.Serialize();
      world.Clear();
      world.Deserialize(json);

      Assert.Equal(2, world.Count);
      Assert.Equal(2, world.components.GetPool<NormalComponent1>().Count);
      Assert.Equal(1, world.components.GetPool<NormalComponent2>().Count);
    }

    [Fact]
    public void NormalEntities_WillNotSerialize_WhenProfileProvided() {
      CreateNormalEntities();
      var json = world.Serialize("test-profile");
      world.Clear();
      world.Deserialize(json);

      Assert.Equal(0, world.Count);
      Assert.Equal(0, world.components.GetPool<NormalComponent1>().Count);
      Assert.Equal(0, world.components.GetPool<NormalComponent2>().Count);
    }
    [Fact]
    public void ProfileEntities_SerializeAndDeserialize_Properly_WhenProfileProvided() {
      CreateNormalEntities();
      CreateProfileEntities();
      var json = world.Serialize("test-profile");
      world.Clear();
      world.Deserialize(json);

      Assert.Equal(2, world.Count);
      Assert.Equal(2, world.components.GetPool<ProfileComponent1>().Count);
      Assert.Equal(1, world.components.GetPool<ProfileComponent2>().Count);
    }

    [Fact]
    public void ProfileEntities_WillNotSerialize_WithoutProfile() {
      CreateProfileEntities();
      var json = world.Serialize();
      world.Clear();
      world.Deserialize(json);

      Assert.Equal(0, world.Count);
      Assert.Equal(0, world.components.GetPool<ProfileComponent1>().Count);
      Assert.Equal(0, world.components.GetPool<ProfileComponent2>().Count);
    }

    [Fact]
    public void EntityReferences_WontBreak() {
      CreateEntitiesWithReferencedComponents();
      int index = 0;
      foreach (var e in world.Entities) {
        if (index % 2 != 0) {
          world.Remove(e);
        }
        index++;
      }
      index = 0;
      CreateEntitiesWithReferencedComponents();
      foreach (var e in world.Entities) {
        if (index % 2 == 0) {
          world.Remove(e);
        }
        index++;
      }

      var json = world.Serialize();
      world.Clear();

      world.Deserialize(json);

      foreach (var e in world.View<ComponentWithReference1>()) {
        ref var c = ref world.GetRef<ComponentWithReference1>(e);
        Assert.True(world.Has<NormalComponent1>(c.entity));
      }

      foreach (var e in world.View<ComponentWithReference2>()) {
        ref var c = ref world.GetRef<ComponentWithReference2>(e);
        Assert.True(world.Has<NormalComponent2>(c.entity));
      }
    }
  }
}
