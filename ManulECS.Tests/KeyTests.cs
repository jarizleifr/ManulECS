using System.Collections.Generic;
using Xunit;

namespace ManulECS.Tests {
  public class KeyTests {
    [Fact]
    public void Matches() {
      var key = new Key(0);
      Assert.True(key[new Key(0)]);
      Assert.False(key[new Key(42)]);
    }

    [Fact]
    public void Matches_MultipleFlags() {
      var key = new Key(0) + new Key(2);
      Assert.True(key[new Key(0)]);
      Assert.True(key[new Key(2)]);
      Assert.True(key[key]);
    }

    [Fact]
    public void IsEqualOf() {
      var flag1 = new Key(0);
      var flag2 = new Key(6);
      var flag3 = new Key(9);

      var key1 = flag1 + flag2 + flag3;
      var key2 = flag1 + flag2 + flag3;
      var key3 = flag1;

      Assert.Equal(key1, key2);
      Assert.NotEqual(key1, key3);
    }

    [Fact]
    public void GetsSubset() {
      var flag1 = new Key(0);
      var flag2 = new Key(6);
      var flag3 = new Key(9);

      var key1 = flag1 + flag2 + flag3;
      var key2 = flag1 + flag3;

      Assert.True(key1[key2]);
      Assert.False(key2[key1]);
    }

    [Fact]
    public void Enumerates() {
      var f1 = new Key(0);
      var f2 = new Key(32);
      var f3 = new Key(64);
      var f4 = new Key(126);

      var key = f1 + f2 + f3 + f4;
      var flags = new List<int>();
      foreach (var idx in key) {
        flags.Add(idx);
      }
      Assert.Contains(0, flags);
      Assert.Contains(32, flags);
      Assert.Contains(64, flags);
      Assert.Contains(126, flags);
    }
  }
}
