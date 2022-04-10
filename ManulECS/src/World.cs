using System;
using System.Collections.Generic;
using System.Linq;

namespace ManulECS {
  public partial class World {
    private uint destroyed = Entity.NULL_ID;
    private uint nextEntityId = 0;

    internal Entity[] entities = new Entity[4];
    internal FlagEnum[] entityFlags = new FlagEnum[4];

    internal readonly Dictionary<Type, object> resources = new();

    /// <summary>
    /// Creates a new empty Entity. 
    /// </summary>
    public Entity Create() {
      Entity entity;
      if (destroyed == Entity.NULL_ID) {
        entity = new Entity(nextEntityId, 0);
        if (nextEntityId == entities.Length) {
          Array.Resize(ref entities, entities.Length * 2);
          Array.Resize(ref entityFlags, entityFlags.Length * 2);
        }
        entities[nextEntityId] = entity;
        entityFlags[nextEntityId] = default;
        nextEntityId++;
      } else {
        var oldEntity = entities[destroyed];
        entity = new Entity(destroyed, oldEntity.Version);
        entities[destroyed] = entity;
        entityFlags[destroyed] = default;
        destroyed = oldEntity.Id;
      }
      return entity;
    }

    /// <summary>
    /// Creates a new empty Entity wrapped in an EntityHandle, which can be used
    /// to concisely build an Entity out of multiple components in sequence.
    /// </summary>
    public EntityHandle Handle() => Handle(Create());

    /// <summary>
    /// Wraps an Entity in an EntityHandle, for ease of access and more concise
    /// assignment of components and tags.
    /// </summary>
    public EntityHandle Handle(in Entity entity) => new() {
      World = this,
      Entity = entity,
    };

    /// <summary>
    /// Removes an existing Entity.
    /// </summary>
    /// <returns>
    /// true if Entity successfully removed, false if Entity did not exist.
    /// </returns>
    public bool Remove(in Entity entity) {
      if (IsAlive(entity)) {
        ref var data = ref entityFlags[entity.Id];
        foreach (var index in entityFlags[entity.Id]) {
          indexedPools[index].Remove(entity);
        }
        entities[entity.Id] = new Entity(destroyed, (byte)(entity.Version + 1));
        data = default;
        destroyed = entity.Id;
        return true;
      }
      return false;
    }

    /// <summary>Clear all entities, components and resources from the world.</summary>
    public void Clear() {
      entities = new Entity[4];
      entityFlags = new FlagEnum[4];
      destroyed = Entity.NULL_ID;
      nextEntityId = 0;

      Array.ForEach(indexedPools, pool => pool?.Reset());
      resources.Clear();
      views.Clear();
    }

    public int EntityCount => Entities.Count();

    /// <summary>Declare a component/tag of type T for use.</summary>
    public void Declare<T>() where T : struct, IBaseComponent =>
      Register<T>();

    public int Count<T>() where T : struct, IBaseComponent =>
      GetUntypedPool<T>().Count;

    /// <summary>Check if entity has a component or tag of type T.</summary>
    public bool Has<T>(in Entity entity) where T : struct, IBaseComponent =>
      GetUntypedPool<T>().Has(entity);

    /// <summary>Get mutable component reference.</summary>
    public ref T GetRef<T>(in Entity entity) where T : struct, IComponent =>
      ref GetPool<T>().GetRef(entity);

    /// <summary>Immutable, but safe way to getting components, even if they don't exist.</summary>
    public bool TryGet<T>(in Entity entity, out T component)
        where T : struct, IComponent {
      if (Has<T>(entity)) {
        component = GetRef<T>(entity);
        return true;
      }
      component = default;
      return false;
    }

    /// <summary>
    /// Assigns a tag on entity. 
    /// </summary>
    public void Assign<T>(in Entity entity) where T : struct, ITag {
      if (IsAlive(entity)) {
        var pool = GetTagPool<T>();
        var flag = pool.Flag;
        ref var flags = ref entityFlags[entity.Id];

        if (!flags[flag]) {
          flags[flag] = true;
          pool.Set(entity);
        }
      }
    }

    /// <summary>Assign a component of type T to an entity. Does nothing if component already exists.</summary>
    public void Assign<T>(in Entity entity, T component)
        where T : struct, IComponent {
      if (IsAlive(entity)) {
        var pool = GetPool<T>();
        var flag = pool.Flag;
        ref var flags = ref entityFlags[entity.Id];

        if (!flags[flag]) {
          flags[flag] = true;
          pool.Set(entity, component);
        }
      }
    }

    /// <summary>Assign or replace a component of type T to an entity.</summary>
    public void AssignOrReplace<T>(in Entity entity, T component)
    where T : struct, IComponent {
      if (IsAlive(entity)) {
        var pool = GetPool<T>();
        ref var flags = ref entityFlags[entity.Id];
        flags[pool.Flag] = true;
        pool.Set(entity, component);
      }
    }

    /// <summary>Remove a component/tag of type T from an entity.</summary>
    public void Remove<T>(in Entity entity) where T : struct, IBaseComponent {
      if (IsAlive(entity)) {
        var pool = GetUntypedPool<T>();
        ref var flags = ref entityFlags[entity.Id];
        flags[pool.Flag] = false;
        pool.Remove(entity);
      }
    }

    /// <summary>Create a new copy of an entity, with all the same components and tags.</summary>
    public Entity Clone(in Entity entity) {
      var clone = Create();
      ref var cloneFlags = ref entityFlags[clone.Id];
      cloneFlags = entityFlags[entity.Id];

      foreach (var idx in cloneFlags) {
        var pool = indexedPools[idx];
        pool.Clone(entity, clone);
      }
      return clone;
    }

    /// <summary>Remove all components or tags of type T from all entities.</summary>
    public void Clear<T>() where T : struct, IBaseComponent {
      var pool = GetUntypedPool<T>();
      var flag = pool.Flag;
      foreach (var idx in pool.Indices) {
        entityFlags[idx][flag] = false;
      }
      pool.Clear();
    }

    public bool IsAlive(Entity entity) {
      var entityInSlot = entities[entity.Id];
      return entityInSlot.Id != Entity.NULL_ID && entityInSlot == entity;
    }

    public T GetResource<T>() => (T)resources[typeof(T)];

    /// <summary>Assigns or replaces the resource of type T in registry.</summary>
    public void SetResource<T>(T resource) => resources[typeof(T)] = resource;
    public void SetResource(Type type, object resource) => resources[type] = resource;
    public void ClearResource<T>() => resources.Remove(typeof(T));
    public void ClearResource(Type type) => resources.Remove(type);

    public string Serialize(string profile = null) => WorldSerializer.Create(this, profile);
    public void Deserialize(string json) => WorldSerializer.Apply(this, json);

    internal IEnumerable<Entity> Entities => nextEntityId != 0
      ? entities.Where((entity, index) => entity.Id == index)
      : Enumerable.Empty<Entity>();
  }
}
