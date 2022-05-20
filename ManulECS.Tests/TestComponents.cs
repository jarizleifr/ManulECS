namespace ManulECS.Tests {
  public struct Component1 : IComponent { public uint value; }
  public struct Component2 : IComponent { public int value; }
  public struct Component3 : IComponent { public int v1; public int v2; }
  public struct Component4 : IComponent { public int v1; public int v2; }
  public struct Tag : ITag { }

  [ECSSerialize("test-profile")]
  public struct ProfileComponent1 : IComponent { }

  [ECSSerialize("test-profile")]
  public struct ProfileComponent2 : IComponent { }

  [ECSSerialize("test-profile-2")]
  public struct ProfileComponent3 : IComponent { }

  [ECSSerialize(Omit.Component)]
  public struct DiscardComponent : IComponent { }

  [ECSSerialize(Omit.Entity)]
  public struct DiscardEntity : IComponent { }

  public struct ComponentWithReference1 : IComponent {
    public Entity entity;
  }
  public struct ComponentWithReference2 : IComponent {
    public Entity entity;
  }
}
