using System;
using System.Collections.Generic;
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
    internal IEnumerable<object> Resources => resources.Values;

    internal readonly Components pools = new();
    internal readonly Dictionary<Key, View> views = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref Key GetEntityKey(uint id) => ref entityKeys[id];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Entity GetEntity(uint id) => entities[id];

    internal int IdCount => (int)nextId;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsValid(uint id) => id != Entity.NULL_ID && entities[id].Id == id;

    /// <summary>Gets entity validity.</summary> 
    public bool IsAlive(in Entity entity) => IsValid(entity.Id);

    /// <summary>Gets the count of alive entities.</summary>
    public int Count() => (int)count;

    /// <summary>Clears all entities, components and resources from the world.</summary>
    public void Clear() {
      entities = new Entity[INITIAL_CAPACITY];
      entityKeys = new Key[INITIAL_CAPACITY];
      destroyed = Entity.NULL_ID;
      (nextId, count) = (0, 0);

      pools.Clear();
      resources.Clear();
      views.Clear();
    }

    /// <summary>Creates a new entity and wraps it in a handle.</summary>
    public EntityHandle Handle() => Handle(Create());

    /// <summary>Wraps the provided entity in a handle.</summary>
    public EntityHandle Handle(in Entity entity) => new(this, entity);

    /// <summary>Creates a new empty Entity.</summary>
    public Entity Create() {
      count++;
      if (destroyed == Entity.NULL_ID) {
        if (nextId == Entity.NULL_ID) {
          throw new Exception("FATAL ERROR: Max number of entities exceeded!");
        }
        if (entities.Length <= nextId) {
          ResizeAndFill(ref entities, (int)nextId, Entity.NULL_ENTITY);
          Resize(ref entityKeys, (int)nextId);
        }
        entityKeys[nextId] = default;
        return entities[nextId] = new Entity(nextId++, 0);
      } else {
        var (oldId, version) = entities[destroyed];
        (var id, destroyed) = (destroyed, oldId);
        entityKeys[id] = default;
        return entities[id] = new Entity(id, version);
      }
    }

    /// <summary>Removes an existing entity.</summary>
    /// <returns>true if entity successfully removed, false otherwise</returns>
    public bool Remove(in Entity entity) {
      var (id, version) = entity;
      if (IsValid(id)) {
        foreach (var idx in entityKeys[id]) {
          pools.RawPool(idx).Remove(id);
        }
        entityKeys[id] = default;
        entities[id] = new Entity(destroyed, ++version);
        destroyed = id;
        count--;
        return true;
      }
      return false;
    }

    /// <summary>Creates N copies of an entity, with all the same components and tags.</summary>
    public void Clone(in Entity entity, int count) {
      for (int i = 0; i < count; i++) {
        Clone(entity);
      }
    }

    /// <summary>Creates a new copy of an entity, with all the same components and tags.</summary>
    public Entity Clone(in Entity entity) {
      var (originId, clone) = (entity.Id, Create());
      var cloneId = clone.Id;
      var cloneFlags = entityKeys[cloneId] = entityKeys[originId];
      foreach (var idx in cloneFlags) {
        var pool = pools.RawPool(idx);
        pool.Clone(originId, cloneId);
      }
      return clone;
    }

    /// <summary>Gets the count of components of type T.</summary>
    public int Count<T>() where T : struct, BaseComponent => pools.RawPool<T>().Count;

    /// <summary>Does entity have a component or tag of type T?</summary>
    public bool Has<T>(in Entity entity) where T : struct, BaseComponent => pools.RawPool<T>().Has(entity.Id);

    /// <summary>Gets a component reference of type T from an entity.</summary>
    public ref T Get<T>(in Entity entity) where T : struct, Component => ref Pool<T>()[entity];

    /// <summary>Gets a component of type T from an entity, if it exists.</summary>
    /// <returns>true if component found, false otherwise</returns>
    public bool TryGet<T>(in Entity entity, out T component) where T : struct, Component {
      if (Has<T>(entity)) {
        component = Get<T>(entity);
        return true;
      }
      component = default;
      return false;
    }

    /// <summary>Tags an entity with a tag of type T.</summary>
    public void Tag<T>(in Entity entity) where T : struct, Tag {
      var id = entity.Id;
      if (IsValid(id)) {
        var pool = (TagPool<T>)pools.RawPool<T>();
        ref var flags = ref GetEntityKey(id);
        if (!flags[pool.key]) {
          flags += pool.key;
          pool.Set(id);
        }
      }
    }

    /// <summary>Assign a component of type T to an entity, if it doesn't exist.</summary>
    public void Assign<T>(in Entity entity, T component) where T : struct, Component {
      var id = entity.Id;
      if (IsValid(id)) {
        var pool = Pool<T>();
        ref var flags = ref GetEntityKey(id);
        if (!flags[pool.key]) {
          flags += pool.key;
          pool.Set(id, component);
        }
      }
    }

    /// <summary>Assigns a boxed Component or Tag by its runtime type.</summary>
    /// <remarks>Used only for internal deserialization.</remarks>
    internal void AssignRaw(in Entity entity, Type type, object component) {
      var id = entity.Id;
      if (IsValid(id)) {
        var pool = pools.RawPool(type);
        ref var flags = ref GetEntityKey(id);
        if (!flags[pool.key]) {
          flags += pool.key;
          pool.SetRaw(id, component);
        }
      }
    }

    /// <summary>Assigns or replaces a component of type T to an entity.</summary>
    public void Patch<T>(in Entity entity, T component) where T : struct, Component {
      var id = entity.Id;
      if (IsValid(id)) {
        var pool = Pool<T>();
        entityKeys[id] += pool.key;
        pool.Set(id, component);
      }
    }

    /// <summary>Removes a component/tag of type T from an entity.</summary>
    public void Remove<T>(in Entity entity) where T : struct, BaseComponent {
      var id = entity.Id;
      if (IsValid(id)) {
        var pool = pools.RawPool<T>();
        entityKeys[id] -= pool.key;
        pool.Remove(id);
      }
    }

    /// <summary>Remove all components or tags of type T from all entities.</summary>
    public void Clear<T>() where T : struct, BaseComponent {
      var pool = pools.RawPool<T>();
      foreach (var id in pool.AsSpan()) {
        entityKeys[id] -= pool.key;
      }
      pool.Clear();
    }

    /// <summary>Gets the resource of type T.</summary> 
    public T GetResource<T>() where T : class => (T)resources[typeof(T)];

    /// <summary>Removes the resource of type T from the registry.</summary>
    public void ClearResource<T>() where T : class => resources.Remove(typeof(T));

    /// <summary>Assigns or replaces the resource of type T in registry.</summary>
    public void SetResource<T>(T resource) where T : class => resources[typeof(T)] = resource;

    /// <summary>Assigns a Resource by its runtime type.</summary>
    /// <remarks>Used only for internal deserialization.</remarks>
    internal void SetResource(Type type, object resource) => resources[type] = resource;

    internal bool ContainsView(Key key) => views.ContainsKey(key);

    /// <summary>Gets a cached view, creating one if it doesn't exist.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal View GetView(Key key) {
      if (!views.TryGetValue(key, out View view)) {
        // If any of the components haven't been registered yet, return an empty View
        foreach (var idx in key) {
          if (!pools.IsRegistered(idx)) return new View();
        }
        view = new View(this, key);
        views.Add(key, view);
      }
      view.Update(this);
      return view;
    }
  }
}
