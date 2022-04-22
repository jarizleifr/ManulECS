using Xunit;

namespace ManulECS.Tests {
  [Collection("World")]
  public class SerializationTests : TestContext {
    private void CreateNormalEntities() {
      var e1 = world.Create();
      world.Assign(e1, new Component1 { });

      var e2 = world.Create();
      world.Assign(e2, new Component1 { });
      world.Assign(e2, new Component2 { });
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
      world.Assign(e1, new Component1 { });
      world.Assign(e1, new DiscardComponent { });

      var e2 = world.Create();
      world.Assign(e2, new Component1 { });
      world.Assign(e2, new DiscardEntity { });
    }

    private void CreateEntitiesWithReferencedComponents() {
      for (int i = 0; i < 10; i++) {
        var e = world.Create();

        if (i % 2 == 0) {
          world.Assign(e, new Component1 { });
          var item = world.Create();
          world.Assign(item, new ComponentWithReference1 { entity = e });
        } else {
          world.Assign(e, new Component2 { });
          var item = world.Create();
          world.Assign(item, new ComponentWithReference2 { entity = e });
        }
      }
    }

    [Fact]
    public void WontSerialize_EmptyEntities() {
      for (int i = 0; i < 10; i++) world.Create();
      var json = world.Serialize();
      world.Clear();
      world.Deserialize(json);
      Assert.Equal(0, world.EntityCount);
    }

    [Fact]
    public void SerializesAndDeserializes_Entities() {
      CreateNormalEntities();
      CreateProfileEntities();
      var json = world.Serialize();
      world.Clear();
      world.Deserialize(json);

      Assert.Equal(2, world.EntityCount);
      Assert.Equal(2, world.Pool<Component1>().Count);
      Assert.Equal(1, world.Pool<Component2>().Count);
    }

    [Fact]
    public void Omits_OnOmitAttribute() {
      CreateDiscardingEntities();
      var json = world.Serialize();
      world.Clear();
      world.Deserialize(json);

      Assert.Equal(1, world.EntityCount);
      Assert.Equal(1, world.Count<Component1>());
      Assert.Equal(0, world.Count<DiscardEntity>());
      Assert.Equal(0, world.Count<DiscardComponent>());
    }

    [Fact]
    public void WontSerialize_OnClashingProfile() {
      CreateNormalEntities();
      var json = world.Serialize("test-profile");
      world.Clear();
      world.Deserialize(json);

      Assert.Equal(0, world.EntityCount);
      Assert.Equal(0, world.Count<Component1>());
      Assert.Equal(0, world.Count<Component2>());
    }
    [Fact]
    public void SerializesAndDeserializes_OnMatchingProfile() {
      CreateNormalEntities();
      CreateProfileEntities();
      var json = world.Serialize("test-profile");
      world.Clear();
      world.Deserialize(json);

      Assert.Equal(2, world.EntityCount);
      Assert.Equal(2, world.Count<ProfileComponent1>());
      Assert.Equal(1, world.Count<ProfileComponent2>());
    }

    [Fact]
    public void WontSerialize_OnMissingProfile() {
      CreateProfileEntities();
      var json = world.Serialize();
      world.Clear();
      world.Deserialize(json);

      Assert.Equal(0, world.EntityCount);
      Assert.Equal(0, world.Count<ProfileComponent1>());
      Assert.Equal(0, world.Count<ProfileComponent2>());
    }

    [Fact]
    public void KeepsEntityReferences() {
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
        ref var c = ref world.Get<ComponentWithReference1>(e);
        Assert.True(world.Has<Component1>(c.entity));
      }

      foreach (var e in world.View<ComponentWithReference2>()) {
        ref var c = ref world.Get<ComponentWithReference2>(e);
        Assert.True(world.Has<Component2>(c.entity));
      }
    }
  }
}
