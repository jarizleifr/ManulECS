using System;

namespace ManulECS {
  public record struct EntityHandle {
    public World World { private get; init; }
    public Entity Entity { private get; init; }

    public bool Has<T>() where T : struct, IBaseComponent =>
      World.Has<T>(Entity);

    public ref T GetRef<T>() where T : struct, IComponent {
      ref T component = ref World.GetRef<T>(Entity);
      return ref component;
    }

    public EntityHandle Tag<T>() where T : struct, ITag {
      World.Tag<T>(Entity);
      return this;
    }

    public EntityHandle Assign<T>(T component) where T : struct, IComponent {
      World.Assign(Entity, component);
      return this;
    }

    public EntityHandle Patch<T>(T component) where T : struct, IComponent {
      World.Patch(Entity, component);
      return this;
    }

    public EntityHandle Remove<T>() where T : struct, IBaseComponent {
      World.Remove<T>(Entity);
      return this;
    }

    public EntityHandle If(bool statement, Func<EntityHandle, EntityHandle> callback) =>
      statement ? callback.Invoke(this) : this;

    public Entity GetEntity() => Entity;
  }
}
