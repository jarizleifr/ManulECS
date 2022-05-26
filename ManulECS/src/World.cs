using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static ManulECS.ArrayUtil;

namespace ManulECS {
  public partial class World {
    private uint destroyed = Entity.NULL_ID;
    private uint nextId = 0, count = 0;

    private Entity[] entities = new Entity[Constants.INITIAL_CAPACITY];
    private Key[] entityKeys = new Key[Constants.INITIAL_CAPACITY];

    private readonly PoolCollection pools = new();
    internal readonly Dictionary<Type, object> resources = new();
    internal readonly Dictionary<Key, View> views = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref Key EntityKey(uint id) => ref entityKeys[id];
    internal Entity this[uint index] => entities[index];

    internal uint Capacity => nextId;
    /// <summary>Gets the count of alive entities.</summary>
    public int Count() => (int)count;

    /// <summary>Creates a new empty Entity.</summary>
    public Entity Create() {
      Entity entity;
      if (destroyed == Entity.NULL_ID) {
        if (nextId == Entity.NULL_ID) {
          throw new Exception("FATAL ERROR: Max number of entities exceeded!");
        }
        if (entities.Length <= nextId) {
          ResizeAndFill(ref entities, (int)nextId, Entity.NULL_ENTITY);
          Resize(ref entityKeys, (int)nextId);
        }
        (entities[nextId], entityKeys[nextId]) = (entity = new(nextId, 0), default);
        nextId++;
      } else {
        var (id, version) = entities[destroyed];
        (entities[destroyed], entityKeys[destroyed]) = (entity = new(destroyed, version), default);
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
      var id = entity.Id;
      if (IsValid(id)) {
        ref var key = ref EntityKey(id);
        foreach (var idx in key) {
          PoolByKeyIndex(idx).Remove(id);
        }
        var (destroyedId, version) = entity;
        (entities[id], key) = (new(destroyed, ++version), default);
        destroyed = destroyedId;
        count--;
        return true;
      }
      return false;
    }

    /// <summary>Clears all entities, components and resources from the world.</summary>
    public void Clear() {
      entities = new Entity[Constants.INITIAL_CAPACITY];
      entityKeys = new Key[Constants.INITIAL_CAPACITY];
      destroyed = Entity.NULL_ID;
      (nextId, count) = (0, 0);

      pools.Clear();
      resources.Clear();
      foreach (var view in views.Values) {
        view.Clear();
      }
    }

    /// <summary>Gets the count of components of type T.</summary>
    public int Count<T>() where T : struct, IBaseComponent => UntypedPool<T>().Count;

    /// <summary>Does entity have a component or tag of type T?</summary>
    public bool Has<T>(in Entity entity) where T : struct, IBaseComponent => UntypedPool<T>().Has(entity.Id);

    /// <summary>Gets a component reference of type T from an entity.</summary>
    public ref T Get<T>(in Entity entity) where T : struct, IComponent => ref Pool<T>()[entity];

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
      var id = entity.Id;
      if (IsValid(id)) {
        var pool = TagPool<T>();
        var key = pool.key;
        ref var flags = ref EntityKey(id);
        if (!flags[key]) {
          flags += key;
          pool.Set(id);
        }
      }
    }

    /// <summary>Assign a component of type T to an entity, if it doesn't exist.</summary>
    public void Assign<T>(in Entity entity, T component) where T : struct, IComponent {
      var id = entity.Id;
      if (IsValid(id)) {
        var pool = Pool<T>();
        var key = pool.key;
        ref var flags = ref EntityKey(id);
        if (!flags[key]) {
          flags += key;
          pool.Set(id, component);
        }
      }
    }

    /// <summary>Assigns or replaces a component of type T to an entity.</summary>
    public void Patch<T>(in Entity entity, T component) where T : struct, IComponent {
      var id = entity.Id;
      if (IsValid(id)) {
        var pool = Pool<T>();
        ref var flags = ref EntityKey(id);
        flags += pool.key;
        pool.Set(id, component);
      }
    }

    /// <summary>Removes a component/tag of type T from an entity.</summary>
    public void Remove<T>(in Entity entity) where T : struct, IBaseComponent {
      var id = entity.Id;
      if (IsValid(id)) {
        var pool = UntypedPool<T>();
        ref var flags = ref EntityKey(id);
        flags -= pool.key;
        pool.Remove(id);
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
      var (id, clone) = (entity.Id, Create());
      var cloneId = clone.Id;
      ref var cloneFlags = ref EntityKey(cloneId);
      cloneFlags = EntityKey(id);
      foreach (var idx in cloneFlags) {
        var pool = PoolByKeyIndex(idx);
        pool.Clone(id, cloneId);
      }
      return clone;
    }

    /// <summary>Remove all components or tags of type T from all entities.</summary>
    public void Clear<T>() where T : struct, IBaseComponent {
      var pool = UntypedPool<T>();
      var key = pool.key;
      foreach (var id in pool.AsSpan()) {
        ref var flags = ref EntityKey(id);
        flags -= key;
      }
      pool.Clear();
    }

    /// <summary>Gets entity validity.</summary> 
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsAlive(in Entity entity) => IsValid(entity.Id);

    /// <summary>Gets the resource of type T.</summary> 
    public T GetResource<T>() => (T)resources[typeof(T)];

    /// <summary>Assigns or replaces the resource of type T in registry.</summary>
    public void SetResource<T>(T resource) => resources[typeof(T)] = resource;

    /// <summary>Removes the resource of type T from the registry.</summary>
    public void ClearResource<T>() => resources.Remove(typeof(T));

    /// <summary>Gets entity validity.</summary> 
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsValid(uint id) => id != Entity.NULL_ID && entities[id].Id == id;

    internal static bool IsTag(Type type) => typeof(ITag).IsAssignableFrom(type);

    internal static bool IsComponent(Type type) => typeof(IComponent).IsAssignableFrom(type);

    /// <summary>Assigns a Resource by its runtime type.</summary>
    /// <remarks>Used only for internal deserialization.</remarks>
    internal void SetResource(Type type, object resource) => resources[type] = resource;

    /// <summary>Assigns a boxed Component or Tag by its runtime type.</summary>
    /// <remarks>Used only for internal deserialization.</remarks>
    internal void AssignObject(in Entity entity, Type type, object component) {
      var id = entity.Id;
      if (IsValid(id)) {
        var pool = pools.PoolByType(type);
        var key = pool.key;
        ref var flags = ref EntityKey(id);
        if (!flags[key]) {
          flags += key;
          pool.SetObject(id, component);
        }
      }
    }

    /// <summary>Gets a cached view, and creates one if it doesn't exist.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal View GetView(World world, Key key) {
      if (!views.TryGetValue(key, out View view)) {
        view = new View(world, key);
        views.Add(key, view);
      }
      view.Update(world);
      return view;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Key Key<T>() where T : struct, IBaseComponent => pools.Pool<T>().key;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Pool UntypedPool<T>() where T : struct, IBaseComponent => pools.Pool<T>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal TagPool<T> TagPool<T>() where T : struct, ITag => (TagPool<T>)pools.Pool<T>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Pool PoolByKeyIndex(int index) => pools.PoolByIndex(index);
  }
}
