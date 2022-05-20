using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ManulECS.Tests {
  public class SerializationTests {
    private World world;

    public SerializationTests() => world = new World();

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

    private byte[] Serialize(string profile = null) {
      var serializer = new JsonWorldSerializer() { Profile = profile };
      using var stream = new MemoryStream();
      serializer.Write(stream, world);
      return stream.GetBuffer();
    }

    private void Deserialize(byte[] buffer) {
      var serializer = new JsonWorldSerializer();
      using var stream = new MemoryStream(buffer);
      serializer.Read(stream, world);
    }

    [Fact]
    public void WontSerialize_EmptyEntities() {
      for (int i = 0; i < 10; i++) {
        world.Create();
      }
      var buffer = Serialize();
      world.Clear();

      Deserialize(buffer);
      Assert.Equal(0, world.EntityCount);
    }

    [Fact]
    public void SerializesAndDeserializes_Entities() {
      CreateNormalEntities();
      CreateProfileEntities();
      var buffer = Serialize();
      world.Clear();

      Deserialize(buffer);
      Assert.Equal(2, world.EntityCount);
      Assert.Equal(2, world.Pool<Component1>().Count);
      Assert.Equal(1, world.Pool<Component2>().Count);
    }

    [Fact]
    public void Omits_OnOmitAttribute() {
      CreateDiscardingEntities();
      var buffer = Serialize();
      world.Clear();

      Deserialize(buffer);
      Assert.Equal(1, world.EntityCount);
      Assert.Equal(1, world.Count<Component1>());
      Assert.Equal(0, world.Count<DiscardEntity>());
      Assert.Equal(0, world.Count<DiscardComponent>());
    }

    [Fact]
    public void WontSerialize_OnClashingProfile() {
      CreateNormalEntities();
      var buffer = Serialize("test-profile");
      world.Clear();

      Deserialize(buffer);
      Assert.Equal(0, world.EntityCount);
      Assert.Equal(0, world.Count<Component1>());
      Assert.Equal(0, world.Count<Component2>());
    }

    [Fact]
    public void SerializesAndDeserializes_OnMatchingProfile() {
      CreateNormalEntities();
      CreateProfileEntities();
      var buffer = Serialize("test-profile");
      world.Clear();

      Deserialize(buffer);
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
      Assert.Throws<Exception>(() => Serialize("test-profile"));
    }

    [Fact]
    public void WontSerialize_OnMissingProfile() {
      CreateProfileEntities();
      var buffer = Serialize();
      world.Clear();

      Deserialize(buffer);
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

      var buffer = Serialize();
      world.Clear();
      world.Create();

      Deserialize(buffer);
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
