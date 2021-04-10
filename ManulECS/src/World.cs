using System;
using System.Collections.Generic;
using System.Linq;

namespace ManulECS {
  public partial class World : PairList<Entity, FlagEnum> {
    internal Components components = new();
    internal Dictionary<Type, object> resources = new();
    internal ViewCache viewCache = new();

    private uint destroyed = Entity.NULL_ID;
    private uint nextEntityId = 0;

    public IEnumerable<Entity> Each() => nextEntityId != 0
        ? items.Where((item, index) => item.Id == index)
        : Enumerable.Empty<Entity>();

    public int EntityCount => Each().Count();

    public bool IsAlive(Entity entity) {
      var entityInSlot = items[entity.Id];
      return entityInSlot.Id != Entity.NULL_ID && entityInSlot == entity;
    }

    internal Entity GetEntityByIndex(uint index) => items[index];

    internal ref FlagEnum GetEntityDataByIndex(uint index) => ref items2[index];

    internal Flag GetFlag<T>() where T : struct =>
        components.GetFlag<T>();

    public Entity Create() {
      if (destroyed == Entity.NULL_ID) {
        var entity = new Entity(nextEntityId, 0);
        AddEntry(entity, default);
        nextEntityId++;
        return entity;
      } else {
        var index = destroyed;
        var oldEntity = items[index];

        var entity = new Entity(destroyed, oldEntity.Version);
        UpdateEntry(index, entity, default);

        destroyed = oldEntity.Id;
        return entity;
      }
    }

    public bool Remove(Entity entity) {
      if (!IsAlive(entity)) return false;

      ref var data = ref GetEntityDataByIndex(entity.Id);
      components.RemoveComponents(entity, data);

      items[entity.Id] = new Entity(destroyed, (byte)(entity.Version + 1));
      data = default;

      destroyed = entity.Id;
      return true;
    }

    /// <summary>Clear all entities, components and resources from the world.</summary>
    public void Clear() {
      Initialize();
      destroyed = Entity.NULL_ID;
      nextEntityId = 0;

      resources.Clear();
      components.Clear();
      viewCache.Clear();
    }

    /// <summary>Declare a component/tag of type T for use.</summary>
    public void Declare<T>() where T : struct, IBaseComponent => components.Register<T>();

    public int Count<T>() where T : struct, IBaseComponent => components.GetUntypedPool<T>().Count;

    /// <summary>Check if entity has a component or tag of type T.</summary>
    public bool Has<T>(Entity entity) where T : struct, IBaseComponent =>
        components.GetUntypedPool<T>().Has(entity.Id);

    /// <summary>Get mutable component reference.</summary>
    public ref T GetRef<T>(Entity entity)
        where T : struct, IComponent =>
            ref components.GetPool<T>().GetRef(entity.Id);

    /// <summary>Immutable, but safe way to getting components, even if they don't exist.</summary>
    public bool TryGet<T>(Entity entity, out T component)
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
    public void Assign<T>(Entity entity) where T : struct, ITag {
      if (IsAlive(entity)) {
        var pool = components.GetTagPool<T>();
        var flag = pool.Flag;
        ref var entityData = ref GetEntityDataByIndex(entity.Id);

        if (!entityData[flag]) {
          entityData[flag] = true;
          pool.Set(entity.Id);
        }
      }
    }

    /// <summary>Assign a component of type T to an entity. Does nothing if component already exists.</summary>
    public void Assign<T>(Entity entity, in T component)
        where T : struct, IComponent {
      if (IsAlive(entity)) {
        var pool = components.GetPool<T>();
        var flag = pool.Flag;
        ref var entityData = ref GetEntityDataByIndex(entity.Id);

        if (!entityData[flag]) {
          entityData[flag] = true;
          pool.Set(entity.Id, component);
        }
      }
    }

    /// <summary>Assign or replace a component of type T to an entity.</summary>
    public void AssignOrReplace<T>(Entity entity, in T component)
    where T : struct, IComponent {
      if (IsAlive(entity)) {
        var pool = components.GetPool<T>();
        var flag = pool.Flag;
        ref var entityData = ref GetEntityDataByIndex(entity.Id);
        entityData[flag] = true;
        pool.Set(entity.Id, component);
      }
    }

    /// <summary>Remove a component/tag of type T from an entity.</summary>
    public void Remove<T>(Entity entity) where T : struct, IBaseComponent {
      if (IsAlive(entity)) {
        var pool = components.GetUntypedPool<T>();
        var flag = pool.Flag;
        ref var entityData = ref GetEntityDataByIndex(entity.Id);
        entityData[flag] = false;
        pool.Remove(entity.Id);
      }
    }

    /// <summary>Create a new copy of an entity, with all the same components and tags.</summary>
    public Entity Clone(Entity entity) {
      var clone = Create();

      var entityData = GetEntityDataByIndex(entity.Id);
      ref var cloneData = ref GetEntityDataByIndex(clone.Id);

      foreach (var idx in entityData) {
        var pool = components.GetIndexedPool(idx);
        pool.Clone(entity.Id, clone.Id);
        cloneData[pool.Flag] = true;
      }
      return clone;
    }

    /// <summary>Remove all components or tags of type T from all entities.</summary>
    public void Clear<T>() where T : struct, IBaseComponent {
      var pool = components.GetUntypedPool<T>();
      var flag = pool.Flag;
      if (pool.Count > 0) {
        foreach (var entity in Each()) {
          ref var entityData = ref GetEntityDataByIndex(entity.Id);
          entityData[flag] = false;
        }
        pool.Clear();
      }
    }

    public T GetResource<T>() => (T)resources[typeof(T)];

    /// <summary>Assigns or replaces the resource of type T in registry.</summary>
    public void SetResource<T>(T resource) => resources[typeof(T)] = resource;
    public void SetResource(Type type, object resource) => resources[type] = resource;
    public void ClearResource<T>() => resources.Remove(typeof(T));
    public void ClearResource(Type type) => resources.Remove(type);

    public string Serialize(string profile = null) => WorldSerializer.Create(this, profile);
    public void Deserialize(string json) => WorldSerializer.Apply(this, json);

    public IReadPool<T1> Pools<T1>()
        where T1 : struct, IComponent =>
            components.GetPool<T1>();

    public View View<T1>()
        where T1 : struct, IBaseComponent =>
            viewCache.GetView(this, new FlagEnum(GetFlag<T1>()));

    public (IReadPool<T1>, IReadPool<T2>) Pools<T1, T2>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent =>
            (Pools<T1>(), Pools<T2>());

    public View View<T1, T2>()
        where T1 : struct, IBaseComponent
        where T2 : struct, IBaseComponent =>
            viewCache.GetView(this, new FlagEnum(GetFlag<T1>(), GetFlag<T2>()));

    public (IReadPool<T1>, IReadPool<T2>, IReadPool<T3>) Pools<T1, T2, T3>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent =>
            (Pools<T1>(), Pools<T2>(), Pools<T3>());

    public View View<T1, T2, T3>()
        where T1 : struct, IBaseComponent
        where T2 : struct, IBaseComponent
        where T3 : struct, IBaseComponent =>
            viewCache.GetView(this, new FlagEnum(GetFlag<T1>(), GetFlag<T2>(), GetFlag<T3>()));

    public (IReadPool<T1>, IReadPool<T2>, IReadPool<T3>, IReadPool<T4>) Pools<T1, T2, T3, T4>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
        where T4 : struct, IComponent =>
            (Pools<T1>(), Pools<T2>(), Pools<T3>(), Pools<T4>());

    public View View<T1, T2, T3, T4>()
        where T1 : struct, IBaseComponent
        where T2 : struct, IBaseComponent
        where T3 : struct, IBaseComponent
        where T4 : struct, IBaseComponent =>
            viewCache.GetView(this, new FlagEnum(GetFlag<T1>(), GetFlag<T2>(), GetFlag<T3>(), GetFlag<T4>()));
  }
}
