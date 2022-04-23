using System;
using System.Collections.Generic;
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
      world.Handle()
        .Assign(new ProfileComponent1 { })
        .Assign(new Component1 { });

      world.Handle()
        .Assign(new ProfileComponent1 { })
        .Assign(new ProfileComponent2 { });
    }

    private void CreateDiscardingEntities() {
      var e1 = world.Create();
      world.Assign(e1, new Component1 { });
      world.Assign(e1, new DiscardComponent { });

      var e2 = world.Create();
      world.Assign(e2, new Component1 { });
      world.Assign(e2, new DiscardEntity { });
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
      Assert.Equal(1, world.Count<Component1>());
      Assert.Equal(1, world.Count<ProfileComponent2>());
    }

    [Fact]
    public void ThrowsException_OnConflictingProfiles() {
      world.Handle()
        .Assign(new ProfileComponent1 { })
        .Assign(new ProfileComponent3 { });
      Assert.Throws<Exception>(() => world.Serialize("test-profile"));
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
      var e1 = world.Handle().Assign(new Component1 { });
      var e2 = world.Handle().Assign(new ComponentWithReference1 { entity = e1 });
      world.Handle()
        .Assign(new ComponentWithReference1 { entity = e1 })
        .Assign(new ComponentWithReference2 { entity = e2 });

      var json = world.Serialize();
      world.Clear();
      world.Create();
      world.Deserialize(json);
      Assert.Equal(4, world.EntityCount);

      var list = new List<Entity>();
      foreach (var e in world.View<ComponentWithReference1>()) {
        list.Add(e);
      }
      foreach (var e in world.View<ComponentWithReference2>()) {
        list.Add(e);
      }
      Assert.Contains(new Entity(2, 0), list);
      Assert.Contains(new Entity(3, 0), list);
      var comp = world.Get<ComponentWithReference1>(new Entity(2, 0));
      var comp2 = world.Get<ComponentWithReference1>(new Entity(3, 0));
      var comp3 = world.Get<ComponentWithReference2>(new Entity(3, 0));
      Assert.Equal(new Entity(1, 0), comp.entity);
      Assert.Equal(new Entity(1, 0), comp2.entity);
      Assert.Equal(new Entity(2, 0), comp3.entity);
    }
  }
}
