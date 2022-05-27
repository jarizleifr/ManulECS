using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
      var serializer = new JsonWorldSerializer();
      using var stream = new MemoryStream();
      serializer.Write(stream, world, profile);
      var buffer = stream.ToArray();
      return buffer;
    }

    private void Deserialize(byte[] buffer) {
      var serializer = new JsonWorldSerializer() { AssemblyName = "ManulECS.Tests" };
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
      Assert.Equal(0, world.Count());
    }

    [Fact]
    public void SerializesAndDeserializes_Entities() {
      CreateNormalEntities();
      CreateProfileEntities();
      var buffer = Serialize();
      world.Clear();

      Deserialize(buffer);
      Assert.Equal(2, world.Count());
      Assert.Equal(2, world.Pool<Component1>().Count);
      Assert.Equal(1, world.Pool<Component2>().Count);
    }

    [Fact]
    public void Omits_OnOmitAttribute() {
      CreateDiscardingEntities();
      var buffer = Serialize();
      world.Clear();

      Deserialize(buffer);
      Assert.Equal(1, world.Count());
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
      Assert.Equal(0, world.Count());
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
      Assert.Equal(2, world.Count());
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
      Assert.Equal(0, world.Count());
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
      Assert.Equal(4, world.Count());

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

    [Fact]
    void SerializesResources() {
      using var stream = new MemoryStream();
      var serializer = new JsonWorldSerializer();
      world.SetResource(new TestResource { TestData = "MyTestData" });
      serializer.Write(stream, world);
      var json = Encoding.UTF8.GetString(stream.ToArray());
      Assert.Contains("TestResource", json);
      Assert.Contains("MyTestData", json);
    }

    [Fact]
    void DeserializesResources() {
      byte[] buffer;
      var serializer = new JsonWorldSerializer() { AssemblyName = "ManulECS.Tests" };
      using (var stream = new MemoryStream()) {
        world.SetResource(new TestResource { TestData = "DeserializesCorrectly" });
        serializer.Write(stream, world);
        buffer = stream.ToArray();
        world.Clear();
      }

      using (var stream = new MemoryStream(buffer)) {
        serializer.Read(stream, world);
        var resource = world.GetResource<TestResource>();
        Assert.Equal("DeserializesCorrectly", resource.TestData);
      }
    }

    [Fact]
    void SerializesWithDefaultNamespace() {
      using var stream = new MemoryStream();
      var serializer = new JsonWorldSerializer();
      var e = world.Handle().Assign(new Component1 { });
      serializer.Write(stream, world);
      var json = Encoding.UTF8.GetString(stream.ToArray());
      Assert.Contains("ManulECS.Tests.Component1", json);
    }

    [Fact]
    void SerializesWithExplicitNamespace() {
      using var stream = new MemoryStream();
      var serializer = new JsonWorldSerializer() { Namespace = "ManulECS.Tests" };
      var e = world.Handle().Assign(new Component1 { });
      serializer.Write(stream, world);
      var json = Encoding.UTF8.GetString(stream.ToArray());
      Assert.DoesNotContain("ManulECS.Tests", json);
      Assert.Contains("Component1", json);
    }

    [Fact]
    void ComponentReader_ReadsEmpty() {
      var serializer = new JsonWorldSerializer();
      var reader = serializer.GetComponentReader(world);
      Assert.False(reader.Read());
      Assert.True(reader.IsFirst);
      Assert.True(reader.HasEntityChanged);
    }

    [Fact]
    void ComponentReader_ReadsEntities_WithOneComponent() {
      var e1 = world.Handle().Assign(new Component1 { });
      var e2 = world.Handle().Assign(new Component1 { });
      var serializer = new JsonWorldSerializer();
      var reader = serializer.GetComponentReader(world);

      Assert.True(reader.IsFirst);
      Assert.True(reader.HasEntityChanged);

      Assert.True(reader.Read());
      Assert.True(reader.IsFirst);
      Assert.True(reader.HasEntityChanged);
      Assert.Equal(e1, reader.Entity);

      Assert.True(reader.Read());
      Assert.False(reader.IsFirst);
      Assert.True(reader.HasEntityChanged);
      Assert.Equal(e2, reader.Entity);
    }

    [Fact]
    void ComponentReader_ReadsEntities_WithMultipleComponents() {
      var e1 = world.Handle().Assign(new Component1 { }).Assign(new Component2 { });
      var e2 = world.Handle().Assign(new Component2 { });
      var e3 = world.Handle().Assign(new Component2 { });
      var serializer = new JsonWorldSerializer();
      var reader = serializer.GetComponentReader(world);

      Assert.True(reader.IsFirst);
      Assert.True(reader.HasEntityChanged);

      Assert.True(reader.Read());
      Assert.True(reader.IsFirst);
      Assert.True(reader.HasEntityChanged);
      Assert.Equal(e1, reader.Entity);

      Assert.True(reader.Read());
      Assert.False(reader.IsFirst);
      Assert.False(reader.HasEntityChanged);
      Assert.Equal(e1, reader.Entity);

      Assert.True(reader.Read());
      Assert.False(reader.IsFirst);
      Assert.True(reader.HasEntityChanged);
      Assert.Equal(e2, reader.Entity);

      Assert.True(reader.Read());
      Assert.False(reader.IsFirst);
      Assert.True(reader.HasEntityChanged);
      Assert.Equal(e3, reader.Entity);

      Assert.False(reader.Read());
    }
  }
}
