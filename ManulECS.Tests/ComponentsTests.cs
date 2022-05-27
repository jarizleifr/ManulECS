using Xunit;

namespace ManulECS.Tests {
  [Collection("Components")]
  public class ComponentsTests {
    [Fact]
    public void RegistersTypeId() {
      var pools = new Components();
      var i1 = pools.GetId<Component1>();
      var i2 = pools.GetId<Tag1>();
      Assert.NotEqual(i1, i2);
    }

    [Fact]
    public void Registers_WithRuntimeType() {
      var pools = new Components();
      var key1 = pools.RawPool(typeof(Component1)).key;
      var key2 = pools.RawPool(typeof(Tag1)).key;
      Assert.Equal(key1, pools.GetKey<Component1>());
      Assert.Equal(key2, pools.GetKey<Tag1>());
    }

    [Fact]
    public void GetsExistingTypeId() {
      var pools = new Components();
      var i1 = pools.GetId<Component1>();
      var i2 = pools.GetId<Tag1>();
      Assert.Equal(i1, pools.GetId<Component1>());
      Assert.Equal(i2, pools.GetId<Tag1>());
    }
  }
}
