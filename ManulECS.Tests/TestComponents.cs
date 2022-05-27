namespace ManulECS.Tests {
  public struct Component1 : Component { public uint value; }
  public struct Component2 : Component { public int value; }
  public struct Component3 : Component { public int v1; public int v2; }
  public struct Component4 : Component { public int v1; public int v2; }
  public struct Tag1 : Tag { }

  [ECSSerialize("test-profile")]
  public struct ProfileComponent1 : Component { }

  [ECSSerialize("test-profile")]
  public struct ProfileComponent2 : Component { }

  [ECSSerialize("test-profile-2")]
  public struct ProfileComponent3 : Component { }

  [ECSSerialize(Omit.Component)]
  public struct DiscardComponent : Component { }

  [ECSSerialize(Omit.Entity)]
  public struct DiscardEntity : Component { }

  [ECSSerialize(Omit.Entity)]
  public struct OmitTag : Tag { }

  [ECSSerialize("test-profile")]
  public struct ProfileTag : Tag { }

  public struct ComponentWithReference1 : Component {
    public Entity entity;
  }

  public struct ComponentWithReference2 : Component {
    public Entity entity;
  }

  public class TestResource {
    public string TestData { get; set; } = "SomeValue";
  }
}
