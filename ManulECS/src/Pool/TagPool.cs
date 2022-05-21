using System;
using static ManulECS.ArrayUtil;

namespace ManulECS {
  public sealed class TagPool<T> : Pool {
    private readonly static T dummy = default;
    private uint[] mapping;

    internal TagPool(in Key key) : base(key, ECSSerializeAttribute.GetAttribute(typeof(T))) { }

    internal override object Get(in Entity _) => dummy;

    internal override bool Has(in Entity entity) =>
      entity.Id < mapping.Length && mapping[entity.Id] != Entity.NULL_ID;

    internal void Set(in Entity entity) {
      var id = entity.Id;
      EnsureSize(ref mapping, id, Entity.NULL_ID);
      ref var key = ref mapping[id];
      if (key == Entity.NULL_ID) {
        key = (uint)nextIndex;
        EnsureSize(ref entities, nextIndex);
        entities[nextIndex++] = entity;
        OnUpdate?.Invoke();
      }
    }

    internal override void SetObject(in Entity entity, object _) => Set(entity);

    internal override void Remove(in Entity entity) {
      var id = entity.Id;
      if (id < mapping.Length) {
        ref var key = ref mapping[id];
        if (key != Entity.NULL_ID) {
          if (key < --nextIndex) {
            entities[key] = entities[nextIndex];
            mapping[entities[nextIndex].Id] = key;
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
      nextIndex = 0;
      OnUpdate?.Invoke();
    }

    internal override void Reset() {
      mapping = new uint[World.INITIAL_CAPACITY];
      Array.Fill(mapping, Entity.NULL_ID);
      nextIndex = 0;
      entities = new Entity[World.INITIAL_CAPACITY];
      OnUpdate?.Invoke();
    }
  }
}
