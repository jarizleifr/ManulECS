using System;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace ManulECS {
  [JsonObject(MemberSerialization.OptIn)]
  public readonly record struct Entity {
    public static readonly Entity NULL_ENTITY = new(NULL_ID, 0);
    public const uint NULL_ID = 0xFFFFFF;

    [JsonProperty("uuid")]
    private readonly uint value;

    public uint Id {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => value >> 8;
    }
    public byte Version => (byte)value;

    public Entity(uint id, byte version) {
      if (id > NULL_ID) throw new Exception("FATAL ERROR: Max number of entities exceeded!");
      value = id << 8 | version;
    }

    [JsonConstructor]
    public Entity(uint uuid) => value = uuid << 8;
  }
}
