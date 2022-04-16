using System.Collections.Generic;
using Xunit;

namespace ManulECS.Tests {
  public class FlagTests {
    [Fact]
    public void Matches() {
      var matcher = new Matcher(new Flag(0, 1));
      Assert.True(matcher[new Flag(0, 1)]);
      Assert.False(matcher[new Flag(2, 32)]);
    }

    [Fact]
    public void Matches_MultipleFlags() {
      var matcher = new Matcher(new Flag(0, 1), new Flag(0, 4));
      Assert.True(matcher[new Flag(0, 1)]);
      Assert.True(matcher[new Flag(0, 4)]);
      Assert.True(matcher[new Flag(0, 5)]);
    }

    [Fact]
    public void IsEqualOf() {
      var flag1 = new Flag(0, 1);
      var flag2 = new Flag(0, 8);
      var flag3 = new Flag(1, 128);

      var matcher1 = new Matcher(flag1, flag2, flag3);
      var matcher2 = new Matcher(flag1, flag2, flag3);
      var matcher3 = new Matcher(flag1);

      Assert.Equal(matcher1, matcher2);
      Assert.NotEqual(matcher1, matcher3);
    }

    [Fact]
    public void IsSubsetOf() {
      var flag1 = new Flag(0, 1);
      var flag2 = new Flag(0, 8);
      var flag3 = new Flag(1, 128);

      var matcher1 = new Matcher(flag1, flag2, flag3);
      var matcher2 = new Matcher(flag1, flag3);

      Assert.True(matcher1.IsSubsetOf(matcher2));
      Assert.False(matcher2.IsSubsetOf(matcher1));
    }

    [Fact]
    public void Enumerates() {
      var f1 = new Flag(0, 1);
      var f2 = new Flag(1, 1);
      var f3 = new Flag(2, 1);
      var f4 = new Flag(3, 1 << 30);

      var matcher = new Matcher(f1, f2, f3, f4);
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
      var f1 = new Flag(0, 1);
      var f2 = new Flag(1, 2);
      var f3 = new Flag(1, 32);

      Assert.Equal(0u, BitUtil.Position(f1.index, f1.bits));
      Assert.Equal(33u, BitUtil.Position(f2.index, f2.bits));
      Assert.Equal(37u, BitUtil.Position(f3.index, f3.bits));
    }

    [Fact]
    public void CreatesNewFlagsSequentially() {
      var pools = new Pools { };
      Assert.Equal(new Flag(0, 1u), pools.GetNextFlag());
      Assert.Equal(new Flag(0, 2u), pools.GetNextFlag());
      Assert.Equal(new Flag(0, 4u), pools.GetNextFlag());
      Assert.Equal(new Flag(0, 8u), pools.GetNextFlag());
    }
  }
}
