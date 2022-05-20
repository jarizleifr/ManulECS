using Xunit;

namespace ManulECS.Tests {
  public class TypeIndexTests {
    [Fact]
    public void ReusesExistingIndex() {
      var i1 = TypeIndex.Create<Component1>();
      var i2 = TypeIndex.Create<Component2>();
      var i3 = TypeIndex.Create<Component3>();

      Assert.Equal(i1, TypeIndex.Create<Component1>());
      Assert.Equal(i2, TypeIndex.Create<Component2>());
      Assert.Equal(i3, TypeIndex.Create<Component3>());
    }
  }
}
