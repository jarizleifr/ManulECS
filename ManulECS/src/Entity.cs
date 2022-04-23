using System;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace ManulECS {
  [JsonObject(MemberSerialization.OptIn)]
  public readonly struct Entity : IEquatable<Entity> {
    public static readonly Entity NULL_ENTITY = new(NULL_ID, 0);
    public const uint NULL_ID = 0xFFFFFF;

    [JsonProperty("uuid")]
    private readonly uint value = NULL_ID;

    internal uint Id {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => value >> 8;
    }
    internal byte Version => (byte)value;

    internal Entity(uint id, byte version) {
      if (id > NULL_ID) throw new Exception("FATAL ERROR: Max number of entities exceeded!");
      value = id << 8 | version;
    }

    [JsonConstructor]
    internal Entity(uint uuid) => value = uuid;

    internal void Deconstruct(out uint id, out byte version) {
      id = Id; version = Version;
    }

    public bool Equals(Entity entity) => value == entity.value;

    public static bool operator ==(Entity left, Entity right) => left.Equals(right);
    public static bool operator !=(Entity left, Entity right) => !left.Equals(right);

    public override string ToString() => $"{Id}:{Version}";

    public override bool Equals(object obj) =>
      obj is Entity entity && Equals(entity);

    public override int GetHashCode() => value.GetHashCode();
  }

  public readonly struct EntityHandle {
    private readonly World world;
    private readonly Entity entity;

    internal EntityHandle(World world, Entity entity) =>
      (this.world, this.entity) = (world, entity);

    public bool Has<T>() where T : struct, IBaseComponent =>
      world.Has<T>(entity);

    public EntityHandle Tag<T>() where T : struct, ITag {
      world.Tag<T>(entity);
      return this;
    }

    public EntityHandle Assign<T>(T component) where T : struct, IComponent {
      world.Assign(entity, component);
      return this;
    }

    public EntityHandle Patch<T>(T component) where T : struct, IComponent {
      world.Patch(entity, component);
      return this;
    }

    public EntityHandle Remove<T>() where T : struct, IBaseComponent {
      world.Remove<T>(entity);
      return this;
    }

    public void Clone(int count = 1) => world.Clone(entity, count);

    public EntityHandle If(bool statement, Func<EntityHandle, EntityHandle> callback) =>
      statement ? callback.Invoke(this) : this;

    public static implicit operator Entity(EntityHandle handle) => handle.entity;
  }
}
