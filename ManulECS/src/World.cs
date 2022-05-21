using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using static ManulECS.ArrayUtil;

namespace ManulECS {
  public partial class World {
    internal const int INITIAL_CAPACITY = 4;
    private uint destroyed = Entity.NULL_ID;
    private uint nextId = 0;
    private uint count = 0;

    private Entity[] entities = new Entity[INITIAL_CAPACITY];
    private Key[] entityKeys = new Key[INITIAL_CAPACITY];

    private readonly Dictionary<Type, object> resources = new();
    internal readonly PoolCollection pools = new();
    internal readonly ViewCache viewCache = new();

    internal ref Key EntityKey(in Entity entity) => ref entityKeys[entity.Id];
    internal Entity this[int index] => entities[index];

    internal IEnumerable<object> Resources => resources.Values;
    internal IEnumerable<Entity> Entities => nextId != 0
      ? entities.Where((entity, index) => entity.Id == index)
      : Enumerable.Empty<Entity>();

    internal uint Capacity => nextId;
    /// <summary>Gets the count of alive entities.</summary>
    public int Count() => (int)count;

    /// <summary>Creates a new empty Entity.</summary>
    public Entity Create() {
      Entity entity;
      if (destroyed == Entity.NULL_ID) {
        entity = new(nextId, 0);
        EnsureSize(ref entities, nextId, Entity.NULL_ENTITY);
        EnsureSize(ref entityKeys, nextId);
        (entities[nextId], entityKeys[nextId]) = (entity, default);
        nextId++;
      } else {
        var (id, version) = entities[destroyed];
        entity = new(destroyed, version);
        (entities[destroyed], entityKeys[destroyed]) = (entity, default);
        destroyed = id;
      }
      count++;
      return entity;
    }

    /// <summary>Creates a new entity and wraps it in a handle.</summary>
    public EntityHandle Handle() => Handle(Create());

    /// <summary>Wraps the provided entity in a handle.</summary>
    public EntityHandle Handle(in Entity entity) => new(this, entity);

    /// <summary>Removes an existing entity.</summary>
    /// <returns>true if entity successfully removed, false otherwise</returns>
    public bool Remove(in Entity entity) {
      if (IsAlive(entity)) {
        ref var key = ref EntityKey(entity);
        foreach (var idx in key) {
          pools.PoolByKeyIndex(idx).Remove(entity);
        }
        var (id, version) = entity;
        (entities[id], key) = (new(destroyed, ++version), default);
        destroyed = id;
        count--;
        return true;
      }
      return false;
    }

    /// <summary>Clears all entities, components and resources from the world.</summary>
    public void Clear() {
      entities = new Entity[INITIAL_CAPACITY];
      entityKeys = new Key[INITIAL_CAPACITY];
      destroyed = Entity.NULL_ID;
      nextId = 0;
      count = 0;

      pools.Clear();
      resources.Clear();
      viewCache.Clear();
    }

    /// <summary>Gets the count of components of type T.</summary>
    public int Count<T>() where T : struct, IBaseComponent =>
      UntypedPool<T>().Count;

    /// <summary>Does entity have a component or tag of type T?</summary>
    public bool Has<T>(in Entity entity) where T : struct, IBaseComponent =>
      UntypedPool<T>().Has(entity);

    /// <summary>Gets a component reference of type T from an entity.</summary>
    public ref T Get<T>(in Entity entity) where T : struct, IComponent =>
      ref Pool<T>()[entity];

    /// <summary>Gets a component of type T from an entity, if it exists.</summary>
    /// <returns>true if component found, false otherwise</returns>
    public bool TryGet<T>(in Entity entity, out T component) where T : struct, IComponent {
      if (Has<T>(entity)) {
        component = Get<T>(entity);
        return true;
      }
      component = default;
      return false;
    }

    /// <summary>Tags an entity with a tag of type T.</summary>
    public void Tag<T>(in Entity entity) where T : struct, ITag {
      if (IsAlive(entity)) {
        var pool = TagPool<T>();
        var key = pool.Key;
        ref var flags = ref EntityKey(entity);
        if (!flags[key]) {
          flags += key;
          pool.Set(entity);
        }
      }
    }

    /// <summary>Assign a component of type T to an entity, if it doesn't exist.</summary>
    public void Assign<T>(in Entity entity, T component) where T : struct, IComponent {
      if (IsAlive(entity)) {
        var pool = Pool<T>();
        var key = pool.Key;
        ref var flags = ref EntityKey(entity);
        if (!flags[key]) {
          flags += key;
          pool.Set(entity, component);
        }
      }
    }

    /// <summary>Assigns or replaces a component of type T to an entity.</summary>
    public void Patch<T>(in Entity entity, T component) where T : struct, IComponent {
      if (IsAlive(entity)) {
        var pool = Pool<T>();
        ref var flags = ref EntityKey(entity);
        flags += pool.Key;
        pool.Set(entity, component);
      }
    }

    /// <summary>Removes a component/tag of type T from an entity.</summary>
    public void Remove<T>(in Entity entity) where T : struct, IBaseComponent {
      if (IsAlive(entity)) {
        var pool = UntypedPool<T>();
        ref var flags = ref EntityKey(entity);
        flags -= pool.Key;
        pool.Remove(entity);
      }
    }

    /// <summary>Creates N copies of an entity, with all the same components and tags.</summary>
    public void Clone(in Entity entity, int count) {
      for (int i = 0; i < count; i++) {
        Clone(entity);
      }
    }

    /// <summary>Creates a new copy of an entity, with all the same components and tags.</summary>
    public Entity Clone(in Entity entity) {
      var clone = Create();
      ref var cloneFlags = ref EntityKey(clone);
      cloneFlags = EntityKey(entity);
      foreach (var idx in cloneFlags) {
        var pool = pools.PoolByKeyIndex(idx);
        pool.Clone(entity, clone);
      }
      return clone;
    }

    /// <summary>Remove all components or tags of type T from all entities.</summary>
    public void Clear<T>() where T : struct, IBaseComponent {
      var pool = UntypedPool<T>();
      var key = pool.Key;
      foreach (var entity in pool) {
        ref var flags = ref EntityKey(entity);
        flags -= key;
      }
      pool.Clear();
    }

    /// <summary>Gets entity validity.</summary> 
    public bool IsAlive(in Entity entity) => entity.Id != Entity.NULL_ID && entities[entity.Id] == entity;

    /// <summary>Gets the resource of type T.</summary> 
    public T GetResource<T>() => (T)resources[typeof(T)];

    /// <summary>Assigns or replaces the resource of type T in registry.</summary>
    public void SetResource<T>(T resource) => resources[typeof(T)] = resource;

    /// <summary>Removes the resource of type T from the registry.</summary>
    public void ClearResource<T>() => resources.Remove(typeof(T));

    internal static bool IsTag(Type type) => typeof(ITag).IsAssignableFrom(type);
    internal static bool IsComponent(Type type) => typeof(IComponent).IsAssignableFrom(type);

    /// <summary>Assigns a Resource by its runtime type.</summary>
    /// <remarks>Used only for internal deserialization.</remarks>
    internal void SetResource(Type type, object resource) => resources[type] = resource;

    /// <summary>Assigns a boxed Component or Tag by its runtime type.</summary>
    /// <remarks>Used only for internal deserialization.</remarks>
    internal void AssignObject(in Entity entity, Type type, object component) {
      if (IsAlive(entity)) {
        var pool = pools.PoolByType(type);
        var key = pool.Key;
        ref var flags = ref EntityKey(entity);
        if (!flags[key]) {
          flags += key;
          pool.SetObject(entity, component);
        }
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Key Key<T>() where T : struct, IBaseComponent =>
      pools.Pool<T>().Key;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Pool UntypedPool<T>() where T : struct, IBaseComponent =>
      pools.Pool<T>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal TagPool<T> TagPool<T>() where T : struct, ITag =>
      (TagPool<T>)pools.Pool<T>();
  }
}
