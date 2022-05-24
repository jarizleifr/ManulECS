using System;
using System.Runtime.CompilerServices;
using static ManulECS.ArrayUtil;

namespace ManulECS {
  public sealed class Pool<T> : Pool where T : struct {
    private T[] components;
    private uint[] mapping;

    internal Pool(in Key key) : base(key, ECSSerializeAttribute.GetAttribute(typeof(T))) { }

    /// <summary>Get a ref of component. This WILL throw exception if not found.</summary>
    public ref T this[in Entity entity] {
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      get => ref components[mapping[entity.Id]];
    }

    internal override bool Has(in Entity entity) =>
      entity.Id < mapping.Length && mapping[entity.Id] != Entity.NULL_ID;

    internal void Set(in Entity entity) => Set(entity, default);

    internal void Set(in Entity entity, T component) {
      var id = entity.Id;
      EnsureSize(ref mapping, id, Entity.NULL_ID);
      ref var key = ref mapping[id];
      if (key == Entity.NULL_ID) {
        key = (uint)nextIndex;
        EnsureSize(ref entities, nextIndex, Entity.NULL_ENTITY);
        EnsureSize(ref components, nextIndex);
        (entities[nextIndex], components[nextIndex]) = (entity, component);
        nextIndex++;
      } else {
        (entities[key], components[key]) = (entity, component);
      }
      OnUpdate?.Invoke();
    }

    internal override void SetObject(in Entity entity, object component) =>
      Set(entity, (T)component);

    internal override void Remove(in Entity entity) {
      var id = entity.Id;
      if (id < mapping.Length) {
        ref var key = ref mapping[id];
        if (key != Entity.NULL_ID) {
          if (key < --nextIndex) {
            (entities[key], components[key]) = (entities[nextIndex], components[nextIndex]);
            mapping[entities[nextIndex].Id] = key;
          }
          key = Entity.NULL_ID;
          OnUpdate?.Invoke();
        }
      }
    }

    internal override object Get(in Entity entity) => Has(entity) ? this[entity] : null;

    internal override void Clone(in Entity origin, in Entity target) =>
      Set(target, this[origin]);

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
      components = new T[World.INITIAL_CAPACITY];
      OnUpdate?.Invoke();
    }
  }
}
