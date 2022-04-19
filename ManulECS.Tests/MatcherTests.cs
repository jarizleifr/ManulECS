using System.Collections.Generic;
using Xunit;

namespace ManulECS.Tests {
  public class MatcherTests {
    [Fact]
    public void Matches() {
      var matcher = new Matcher(0, 1);
      Assert.True(matcher[new Matcher(0, 1)]);
      Assert.False(matcher[new Matcher(2, 32)]);
    }

    [Fact]
    public void Matches_MultipleFlags() {
      var matcher = new Matcher(0, 1) + new Matcher(0, 4);
      Assert.True(matcher[new Matcher(0, 1)]);
      Assert.True(matcher[new Matcher(0, 4)]);
      Assert.True(matcher[new Matcher(0, 5)]);
    }

    [Fact]
    public void IsEqualOf() {
      var flag1 = new Matcher(0, 1);
      var flag2 = new Matcher(0, 8);
      var flag3 = new Matcher(1, 128);

      var matcher1 = flag1 + flag2 + flag3;
      var matcher2 = flag1 + flag2 + flag3;
      var matcher3 = flag1;

      Assert.Equal(matcher1, matcher2);
      Assert.NotEqual(matcher1, matcher3);
    }

    [Fact]
    public void IsSubsetOf() {
      var flag1 = new Matcher(0, 1);
      var flag2 = new Matcher(0, 8);
      var flag3 = new Matcher(1, 128);

      var matcher1 = flag1 + flag2 + flag3;
      var matcher2 = flag1 + flag3;

      Assert.True(matcher1.IsSubsetOf(matcher2));
      Assert.False(matcher2.IsSubsetOf(matcher1));
    }

    [Fact]
    public void Enumerates() {
      var f1 = new Matcher(0, 1);
      var f2 = new Matcher(1, 1);
      var f3 = new Matcher(2, 1);
      var f4 = new Matcher(3, 1 << 30);

      var matcher = f1 + f2 + f3 + f4;
      var flags = new List<int>();
      foreach (var idx in matcher) {
        flags.Add(idx);
      }
      Assert.Contains(0, flags);
      Assert.Contains(32, flags);
      Assert.Contains(64, flags);
      Assert.Contains(126, flags);
    }

    [Fact]
    public void FindsBitPosition() {
      Assert.Equal(0u, BitUtil.Position(0, 1));
      Assert.Equal(33u, BitUtil.Position(1, 2));
      Assert.Equal(37u, BitUtil.Position(1, 32));
    }

    [Fact]
    public void CreatesNewFlagsSequentially() {
      var pools = new Pools { };
      Assert.Equal((0, 1u), pools.GetNextFlag());
      Assert.Equal((0, 2u), pools.GetNextFlag());
      Assert.Equal((0, 4u), pools.GetNextFlag());
      Assert.Equal((0, 8u), pools.GetNextFlag());
    }
  }
}
