using System;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace ManulECS {
  [JsonObject(MemberSerialization.OptIn)]
  public readonly record struct Entity {
    public const uint NULL_ID = 0xFFFFFF;

    [JsonProperty("uuid")]
    private readonly uint value;

    public uint Id {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => value & NULL_ID;
    }
    public byte Version => (byte)((value & 0xFF000000) >> 24);

    public Entity(uint id, byte version) {
      if (id > NULL_ID) throw new Exception("FATAL ERROR: Max number of entities exceeded!");

      value = version;
      value <<= 24;
      value |= id & NULL_ID;
    }

    [JsonConstructor]
    public Entity(uint uuid) => value = uuid;
  }
}
