using System;
using Newtonsoft.Json;

namespace ManulECS {
  [JsonObject(MemberSerialization.OptIn)]
  public readonly struct Entity : IEquatable<Entity> {
    public const uint NULL_ID = 16777215;

    [JsonProperty("uuid")]
    private readonly uint value;

    public uint Id => value & 0xFFFFFF;
    public byte Version => (byte)((value & 0xFF000000) >> 24);

    public Entity(uint id, byte version) {
      if (id > NULL_ID) throw new Exception("FATAL ERROR: Max number of entities exceeded!");

      value = version;
      value <<= 24;
      value |= id & 0xFFFFFF;
    }

    [JsonConstructor]
    public Entity(uint uuid) {
      if ((uuid & 0xffffff) > NULL_ID) throw new Exception();
      value = uuid;
    }

    public static bool operator ==(Entity e1, Entity e2) => e1.value == e2.value;
    public static bool operator !=(Entity e1, Entity e2) => e1.value != e2.value;

    public bool Equals(Entity other) => value == other.value;
    public override int GetHashCode() => value.GetHashCode();

    public override bool Equals(object o) => o is Entity entity && Equals(entity);
  }
}
