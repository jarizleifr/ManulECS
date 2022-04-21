using Xunit;

namespace ManulECS.Tests {
  [Collection("World")]
  public class TypeIndexTests : TestContext {
    [Fact]
    public void CreatesIndicesSequentially() {
      TypeIndex.Reset();
      Assert.Equal(0u, TypeIndex.Create<Component1>());
      Assert.Equal(1u, TypeIndex.Create<Component2>());
      Assert.Equal(2u, TypeIndex.Create<Component3>());
    }

    [Fact]
    public void ReusesExistingIndex() {
      Assert.Equal(2u, TypeIndex.Create<Component3>());
      Assert.Equal(1u, TypeIndex.Create<Component2>());
      Assert.Equal(0u, TypeIndex.Create<Component1>());
    }

    [Fact]
    public void ResetsIndices() {
      TypeIndex.Reset();
      Assert.Equal(TypeIndex.MAX_INDEX, TypeIndex.Get<Component1>());
      Assert.Equal(TypeIndex.MAX_INDEX, TypeIndex.Get<Component2>());
      Assert.Equal(TypeIndex.MAX_INDEX, TypeIndex.Get<Component3>());
    }
  }
}
