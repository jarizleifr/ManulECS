using System;

namespace ManulECS.Tests {
  public struct Component1 : IComponent { public uint value; }
  public struct Component2 : IComponent { public int value; }
  public struct Component3 : IComponent { public int v1; public int v2; }
  public struct Component4 : IComponent { public int v1; public int v2; }
  public struct Tag : ITag { }

  [Dense]
  public struct DenseTag : ITag { }

  [Dense]
  public struct Dense : IComponent { }

  [ECSSerialize("test-profile")]
  public struct ProfileComponent1 : IComponent { }

  [ECSSerialize("test-profile")]
  public struct ProfileComponent2 : IComponent { }

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

  public abstract class TestContext : IDisposable {
    protected World world;

    public TestContext() {
      world = new World()
        .Declare<Component1>()
        .Declare<Component2>()
        .Declare<Component3>()
        .Declare<Component4>()
        .Declare<Tag>()
        .Declare<DenseTag>()
        .Declare<Dense>()
        .Declare<ProfileComponent1>()
        .Declare<ProfileComponent2>()
        .Declare<DiscardComponent>()
        .Declare<DiscardEntity>()
        .Declare<ComponentWithReference1>()
        .Declare<ComponentWithReference2>();
    }

    public void Dispose() {
      world = null;
      TypeIndex.Reset();
      GC.SuppressFinalize(this);
    }
  }
}
