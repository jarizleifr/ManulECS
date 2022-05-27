using System;

namespace ManulECS {
  /* Entity is an opaque wrapper around an unsigned integer, where the lowest 3 bytes represent the
   * Id and the highest byte represents the Version of the Entity.
   */
  /// <summary>An Entity, which can own Components and/or Tags.</summary>
  public readonly struct Entity : IEquatable<Entity> {
    public static readonly Entity NULL_ENTITY = new(NULL_ID, 0);
    public const uint NULL_ID = 0xFFFFFF;

    internal readonly uint uuid;

    /* The compiler seems to have some trouble inlining this property, I've omitted the MethodImpl
     * here, as it simply does nothing. We could have the Id and the Version as separate fields,
     * effectively using a 64-bit Entity instead, but the padding it causes in the Entity array makes
     * the final result slower, than just accepting the overhead from this shift.
     */
    internal uint Id => uuid >> 8;
    internal byte Version => (byte)uuid;

    internal Entity(uint id, byte version) => uuid = id << 8 | version;
    internal Entity(uint uuid) => this.uuid = uuid;

    internal void Deconstruct(out uint id, out byte version) => (id, version) = (Id, Version);

    public bool Equals(Entity entity) => uuid == entity.uuid;

    public static bool operator ==(Entity left, Entity right) => left.Equals(right);
    public static bool operator !=(Entity left, Entity right) => !left.Equals(right);

    public override string ToString() => $"{Id}:{Version}";
    public override bool Equals(object obj) => obj is Entity entity && Equals(entity);
    public override int GetHashCode() => (int)uuid;
  }

  public readonly struct EntityHandle {
    private readonly World world;
    private readonly Entity entity;

    internal EntityHandle(World world, Entity entity) =>
      (this.world, this.entity) = (world, entity);

    public EntityHandle Tag<T>() where T : struct, Tag {
      world.Tag<T>(entity);
      return this;
    }

    public EntityHandle Assign<T>(T component) where T : struct, Component {
      world.Assign(entity, component);
      return this;
    }

    public EntityHandle Patch<T>(T component) where T : struct, Component {
      world.Patch(entity, component);
      return this;
    }

    public EntityHandle Remove<T>() where T : struct, BaseComponent {
      world.Remove<T>(entity);
      return this;
    }

    public static implicit operator Entity(EntityHandle handle) => handle.entity;
  }
}
