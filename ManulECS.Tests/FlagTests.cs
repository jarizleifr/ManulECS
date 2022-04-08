using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Reflection;
using Xunit;

namespace ManulECS.Tests {
  public class FlagTests {
    [Fact]
    public void FlagEnum_ReturnsFalse_WhenBitArrayNull() {
      var enum1 = new FlagEnum();

      Assert.False(enum1[new Flag(0, 1)]);
      Assert.False(enum1[new Flag(2, 32)]);
      Assert.False(enum1[new Flag(3, 64)]);
    }

    [Fact]
    public void FlagEnum_ReturnsFalse_WhenBitNotSet() {
      var enum1 = new FlagEnum(new Flag(0, 1), new Flag(0, 4));
      Assert.False(enum1[new Flag(0, 2)]);
    }

    [Fact]
    public void FlagEnum_ReturnsTrue_WhenCheckingMultipleBits() {
      var enum1 = new FlagEnum(new Flag(0, 1), new Flag(0, 4));
      Assert.True(enum1[new Flag(0, 5)]);
    }

    [Fact]
    public void IsEqualOf() {
      var flag1 = new Flag(0, 1);
      var flag2 = new Flag(0, 8);
      var flag3 = new Flag(1, 128);

      var enum1 = new FlagEnum(flag1, flag2, flag3);
      var enum2 = new FlagEnum(flag1, flag2, flag3);
      var enum3 = new FlagEnum(flag1);

      Assert.Equal(enum1, enum2);
      Assert.NotEqual(enum1, enum3);
    }

    [Fact]
    public void Contains() {
      var flag1 = new Flag(0, 1);
      var flag2 = new Flag(0, 8);
      var flag3 = new Flag(1, 128);

      var enum1 = new FlagEnum(flag1, flag2, flag3);
      var enum2 = new FlagEnum(flag1, flag3);

      Assert.True(enum1.IsSubsetOf(enum2));
      Assert.False(enum2.IsSubsetOf(enum1));
    }

    [Fact]
    public void FlagEnumEnumeratesCorrectly() {
      var f1 = new Flag(0, 1);
      var f2 = new Flag(1, 1);
      var f3 = new Flag(2, 1);
      var f4 = new Flag(3, 1 << 30);

      var flagEnum = new FlagEnum(f1, f2, f3, f4);
      var enums = new List<int>();
      foreach (var idx in flagEnum) {
        enums.Add(idx);
      }

      Assert.Contains(0, enums);
      Assert.Contains(32, enums);
      Assert.Contains(64, enums);
      Assert.Contains(126, enums);
    }

    [Fact]
    public void Flag_Returns_CorrectBitPosition() {
      var f1 = new Flag(0, 1);
      var f2 = new Flag(1, 2);
      var f3 = new Flag(1, 32);

      Assert.Equal(0, f1.BitPosition);
      Assert.Equal(33, f2.BitPosition);
      Assert.Equal(37, f3.BitPosition);
    }
  }
}
