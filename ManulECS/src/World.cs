using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ManulECS {
  public partial class World {
    private uint destroyed = Entity.NULL_ID;
    private uint nextEntityId = 0;

    internal Entity[] entities = new Entity[4];
    internal Matcher[] entityFlags = new Matcher[4];

    internal readonly Pools pools = new();
    internal readonly Dictionary<Type, object> resources = new();
    internal readonly ViewCache viewCache = new();

    /// <summary>Creates a new empty Entity.</summary>
    public Entity Create() {
      Entity entity;
      if (destroyed == Entity.NULL_ID) {
        entity = new Entity(nextEntityId, 0);
        ArrayUtil.SetWithResize(nextEntityId, ref entities, entity, ref entityFlags, default);
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
    /// Wraps an Entity in an EntityHandle, for ease of access and more concise assignment of components and tags.
    /// </summary>
    public EntityHandle Handle(in Entity entity) => new() {
      World = this,
      Entity = entity,
    };

    /// <summary>Removes an existing entity.</summary>
    /// <returns>true if entity successfully removed, false if entity did not exist.</returns>
    public bool Remove(in Entity entity) {
      if (IsAlive(entity)) {
        ref var data = ref entityFlags[entity.Id];
        foreach (var index in entityFlags[entity.Id]) {
          pools.flagged[index].Remove(entity);
        }
        entities[entity.Id] = new Entity(destroyed, (byte)(entity.Version + 1));
        data = default;
        destroyed = entity.Id;
        return true;
      }
      return false;
    }

    /// <summary>Clears all entities, components and resources from the world.</summary>
    public void Clear() {
      entities = new Entity[4];
      entityFlags = new Matcher[4];
      destroyed = Entity.NULL_ID;
      nextEntityId = 0;

      pools.Clear();
      resources.Clear();
      viewCache.Clear();
    }

    /// <summary>Gets the count of alive entities.</summary>
    public int EntityCount => Entities.Count();

    /// <summary>Declare a component/tag of type T for use.</summary>
    /// <returns>The calling World object, for chaining.</returns>
    public World Declare<T>() where T : struct, IBaseComponent {
      pools.Register<T>();
      return this;
    }

    /// <summary>Gets the count of components of type T.</summary>
    public int Count<T>() where T : struct, IBaseComponent =>
      UntypedPool<T>().Count;

    /// <summary>Does entity have a component or tag of type T?</summary>
    public bool Has<T>(in Entity entity) where T : struct, IBaseComponent =>
      UntypedPool<T>().Has(entity);

    /// <summary>Gets a component reference of type T from an entity.</summary>
    /// <exception cref="System.IndexOutOfRangeException">Thrown when entity has no such component</exception>
    public ref T GetRef<T>(in Entity entity) where T : struct, IComponent =>
      ref Pool<T>().GetRef(entity);

    /// <summary>Gets a component of type T from an entity, if it exists.</summary>
    /// <returns>true if component found, false otherwise</returns>
    public bool TryGet<T>(in Entity entity, out T component)
        where T : struct, IComponent {
      if (Has<T>(entity)) {
        component = GetRef<T>(entity);
        return true;
      }
      component = default;
      return false;
    }

    /// <summary>Tags an entity with a tag of type T.</summary>
    public void Tag<T>(in Entity entity) where T : struct, ITag {
      if (IsAlive(entity)) {
        var pool = TagPool<T>();
        var flag = pool.Flag;
        ref var flags = ref entityFlags[entity.Id];

        if (!flags[flag]) {
          flags[flag] = true;
          pool.Set(entity);
        }
      }
    }

    /// <summary>Assign a component of type T to an entity. Does nothing if component already exists.</summary>
    public void Assign<T>(in Entity entity, T component) where T : struct, IComponent {
      if (IsAlive(entity)) {
        var pool = Pool<T>();
        var flag = pool.Flag;
        ref var flags = ref entityFlags[entity.Id];

        if (!flags[flag]) {
          flags[flag] = true;
          pool.Set(entity, component);
        }
      }
    }

    /// <summary>Assigns or replaces a component of type T to an entity.</summary>
    public void Patch<T>(in Entity entity, T component) where T : struct, IComponent {
      if (IsAlive(entity)) {
        var pool = Pool<T>();
        ref var flags = ref entityFlags[entity.Id];
        flags[pool.Flag] = true;
        pool.Set(entity, component);
      }
    }

    /// <summary>Removes a component/tag of type T from an entity.</summary>
    public void Remove<T>(in Entity entity) where T : struct, IBaseComponent {
      if (IsAlive(entity)) {
        var pool = UntypedPool<T>();
        ref var flags = ref entityFlags[entity.Id];
        flags[pool.Flag] = false;
        pool.Remove(entity);
      }
    }

    /// <summary>Creates a new copy of an entity, with all the same components and tags.</summary>
    public Entity Clone(in Entity entity) {
      var clone = Create();
      ref var cloneFlags = ref entityFlags[clone.Id];
      cloneFlags = entityFlags[entity.Id];

      foreach (var idx in cloneFlags) {
        var pool = pools.flagged[idx];
        pool.Clone(entity, clone);
      }
      return clone;
    }

    /// <summary>Remove all components or tags of type T from all entities.</summary>
    public void Clear<T>() where T : struct, IBaseComponent {
      var pool = UntypedPool<T>();
      var flag = pool.Flag;
      foreach (var idx in pool.Indices) {
        entityFlags[idx][flag] = false;
      }
      pool.Clear();
    }

    /// <summary>Gets entity validity.</summary> 
    public bool IsAlive(Entity entity) {
      var entityInSlot = entities[entity.Id];
      return entityInSlot.Id != Entity.NULL_ID && entityInSlot == entity;
    }

    /// <summary>Gets the resource of type T.</summary> 
    public T GetResource<T>() => (T)resources[typeof(T)];

    /// <summary>Assigns or replaces the resource of type T in registry.</summary>
    public void SetResource<T>(T resource) => resources[typeof(T)] = resource;
    /// <summary>Removes the resource of type T from the registry.</summary>
    public void ClearResource<T>() => resources.Remove(typeof(T));

    /// <summary>Creates a json string from the registry, optionally with a profile</summary>
    public string Serialize(string profile = null) => new WorldSerializer(this, profile).Create();
    /// <summary>Applies the provided json on top of the existing registry</summary>
    public void Deserialize(string json) => new WorldSerializer(this).Apply(json);

    internal void SetResource(Type type, object resource) => resources[type] = resource;
    internal void ClearResource(Type type) => resources.Remove(type);

    internal IEnumerable<Entity> Entities => nextEntityId != 0
      ? entities.Where((entity, index) => entity.Id == index)
      : Enumerable.Empty<Entity>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Flag Flag<T>() where T : struct, IBaseComponent =>
      pools.typed[TypeIndex.Get<T>()].Flag;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Pool UntypedPool<T>() where T : struct, IBaseComponent =>
      pools.typed[TypeIndex.Get<T>()];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Pool TagPool<T>() where T : struct, ITag =>
      pools.typed[TypeIndex.Get<T>()];
  }
}
