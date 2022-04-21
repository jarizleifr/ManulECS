using System;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace ManulECS {
  [JsonObject(MemberSerialization.OptIn)]
  public readonly record struct Entity {
    internal readonly static Entity NULL_ENTITY = new(NULL_ID, 0);
    internal const uint NULL_ID = 0xFFFFFF;

    [JsonProperty("uuid")]
    private readonly uint value;

    internal uint Id {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => value & NULL_ID;
    }
    internal byte Version => (byte)((value & 0xFF000000) >> 24);

    internal Entity(uint id, byte version) {
      if (id > NULL_ID) throw new Exception("FATAL ERROR: Max number of entities exceeded!");

      value = version;
      value <<= 24;
      value |= id & NULL_ID;
    }

    [JsonConstructor]
    internal Entity(uint uuid) => value = uuid;
  }
}
