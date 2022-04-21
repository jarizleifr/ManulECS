using System;

namespace ManulECS {
  public sealed class TagPool<T> : Pool {
    private readonly static T dummy = default;
    private uint[] mapping;

    internal override object Get(in Entity _) => dummy;

    internal override bool Has(in Entity entity) =>
      entity.Id < mapping.Length && mapping[entity.Id] != Entity.NULL_ID;

    internal override void Set(in Entity entity) {
      var id = entity.Id;
      ArrayUtil.EnsureSize(id, ref mapping, Entity.NULL_ID);
      ref var key = ref mapping[id];
      if (key == Entity.NULL_ID) {
        key = (uint)Count;
        ArrayUtil.SetWithResize((uint)Count, ref entities, entity);
        Count++;
        OnUpdate?.Invoke();
      }
    }

    internal override void Remove(in Entity entity) {
      var id = entity.Id;
      if (id < mapping.Length) {
        ref var key = ref mapping[id];
        if (key != Entity.NULL_ID) {
          if (key < --Count) {
            entities[key] = entities[Count];
            mapping[entities[Count].Id] = key;
          }
          key = Entity.NULL_ID;
          OnUpdate?.Invoke();
        }
      }
    }

    internal override void Clone(in Entity origin, in Entity target) {
      if (Has(origin)) {
        Set(target);
      } else {
        Remove(target);
      }
    }

    internal override void Clear() {
      Array.Fill(mapping, Entity.NULL_ID);
      Count = 0;
      OnUpdate?.Invoke();
    }

    internal override void Reset() {
      mapping = new uint[4];
      Array.Fill(mapping, Entity.NULL_ID);
      Count = 0;
      entities = new Entity[4];
      OnUpdate?.Invoke();
    }
  }
}
