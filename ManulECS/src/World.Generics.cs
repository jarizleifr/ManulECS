using System;
using System.Runtime.CompilerServices;

namespace ManulECS {
  public partial class World {
    /// <summary>Gets a pool of components of type T</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pool<T> Pool<T>() where T : struct, IComponent => (Pool<T>)pools.Pool<T>();

    /// <summary>Gets a tuple of pools of components of types T1,T2</summary>
    public (Pool<T1>, Pool<T2>) Pools<T1, T2>() where T1 : struct, IComponent where T2 : struct, IComponent =>
      (Pool<T1>(), Pool<T2>());

    /// <summary>Gets a tuple of pools of components of types T1,T2,T3</summary>
    public (Pool<T1>, Pool<T2>, Pool<T3>) Pools<T1, T2, T3>() where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent =>
      (Pool<T1>(), Pool<T2>(), Pool<T3>());

    /// <summary>Gets a tuple of pools of components of types T1,T2,T3,T4</summary>
    public (Pool<T1>, Pool<T2>, Pool<T3>, Pool<T4>) Pools<T1, T2, T3, T4>() where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent =>
      (Pool<T1>(), Pool<T2>(), Pool<T3>(), Pool<T4>());

    /// <summary>Gets a tuple of pools of components of types T1,T2,T3,T4,T5</summary>
    public (Pool<T1>, Pool<T2>, Pool<T3>, Pool<T4>, Pool<T5>) Pools<T1, T2, T3, T4, T5>() where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent where T5 : struct, IComponent =>
      (Pool<T1>(), Pool<T2>(), Pool<T3>(), Pool<T4>(), Pool<T5>());

    /// <summary>Gets a tuple of pools of components of types T1,T2,T3,T4,T5,T6</summary>
    public (Pool<T1>, Pool<T2>, Pool<T3>, Pool<T4>, Pool<T5>, Pool<T6>) Pools<T1, T2, T3, T4, T5, T6>() where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent where T5 : struct, IComponent where T6 : struct, IComponent =>
      (Pool<T1>(), Pool<T2>(), Pool<T3>(), Pool<T4>(), Pool<T5>(), Pool<T6>());

    /// <summary>Gets a tuple of pools of components of types T1,T2,T3,T4,T5,T6,T7</summary>
    public (Pool<T1>, Pool<T2>, Pool<T3>, Pool<T4>, Pool<T5>, Pool<T6>, Pool<T7>) Pools<T1, T2, T3, T4, T5, T6, T7>() where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent where T5 : struct, IComponent where T6 : struct, IComponent where T7 : struct, IComponent =>
      (Pool<T1>(), Pool<T2>(), Pool<T3>(), Pool<T4>(), Pool<T5>(), Pool<T6>(), Pool<T7>());

    /// <summary>Gets a tuple of pools of components of types T1,T2,T3,T4,T5,T6,T7,T8</summary>
    public (Pool<T1>, Pool<T2>, Pool<T3>, Pool<T4>, Pool<T5>, Pool<T6>, Pool<T7>, Pool<T8>) Pools<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent where T5 : struct, IComponent where T6 : struct, IComponent where T7 : struct, IComponent where T8 : struct, IComponent =>
      (Pool<T1>(), Pool<T2>(), Pool<T3>(), Pool<T4>(), Pool<T5>(), Pool<T6>(), Pool<T7>(), Pool<T8>());

    /// <summary>Get a view of entities with one component</summary>
    /// <returns>View of entities possessing component of type T1</returns>
    public ReadOnlySpan<Entity> View<T1>() where T1 : struct, IBaseComponent => GetView(this, Key<T1>()).AsSpan();

    /// <summary>Get a view of entities with two components</summary>
    /// <returns>View of entities possessing components of types T1,T2</returns>
    public ReadOnlySpan<Entity> View<T1, T2>() where T1 : struct, IBaseComponent where T2 : struct, IBaseComponent =>
      GetView(this, Key<T1>() + Key<T2>()).AsSpan();

    /// <summary>Get a view of entities with three components</summary>
    /// <returns>View of entities possessing components of types T1,T2,T3</returns>
    public ReadOnlySpan<Entity> View<T1, T2, T3>() where T1 : struct, IBaseComponent where T2 : struct, IBaseComponent where T3 : struct, IBaseComponent =>
      GetView(this, Key<T1>() + Key<T2>() + Key<T3>()).AsSpan();

    /// <summary>Get a view of entities with four components</summary>
    /// <returns>View of entities possessing components of types T1,T2,T3,T4</returns>
    public ReadOnlySpan<Entity> View<T1, T2, T3, T4>()
      where T1 : struct, IBaseComponent where T2 : struct, IBaseComponent where T3 : struct, IBaseComponent where T4 : struct, IBaseComponent =>
        GetView(this, Key<T1>() + Key<T2>() + Key<T3>() + Key<T4>()).AsSpan();

    /// <summary>Get a view of entities with five components</summary>
    /// <returns>View of entities possessing components of types T1,T2,T3,T4,T5</returns>
    public ReadOnlySpan<Entity> View<T1, T2, T3, T4, T5>()
      where T1 : struct, IBaseComponent where T2 : struct, IBaseComponent where T3 : struct, IBaseComponent where T4 : struct, IBaseComponent where T5 : struct, IBaseComponent =>
        GetView(this, Key<T1>() + Key<T2>() + Key<T3>() + Key<T4>() + Key<T5>()).AsSpan();

    /// <summary>Get a view of entities with six components</summary>
    /// <returns>View of entities possessing components of types T1,T2,T3,T4,T5,T6</returns>
    public ReadOnlySpan<Entity> View<T1, T2, T3, T4, T5, T6>()
      where T1 : struct, IBaseComponent where T2 : struct, IBaseComponent where T3 : struct, IBaseComponent where T4 : struct, IBaseComponent where T5 : struct, IBaseComponent where T6 : struct, IBaseComponent =>
        GetView(this, Key<T1>() + Key<T2>() + Key<T3>() + Key<T4>() + Key<T5>() + Key<T6>()).AsSpan();

    /// <summary>Get a view of entities with seven components</summary>
    /// <returns>View of entities possessing components of types T1,T2,T3,T4,T5,T6,T7</returns>
    public ReadOnlySpan<Entity> View<T1, T2, T3, T4, T5, T6, T7>()
      where T1 : struct, IBaseComponent where T2 : struct, IBaseComponent where T3 : struct, IBaseComponent where T4 : struct, IBaseComponent where T5 : struct, IBaseComponent where T6 : struct, IBaseComponent where T7 : struct, IBaseComponent =>
        GetView(this, Key<T1>() + Key<T2>() + Key<T3>() + Key<T4>() + Key<T5>() + Key<T6>() + Key<T7>()).AsSpan();

    /// <summary>Get a view of entities with eight components</summary>
    /// <returns>View of entities possessing components of types T1,T2,T3,T4,T5,T6,T7,T8</returns>
    public ReadOnlySpan<Entity> View<T1, T2, T3, T4, T5, T6, T7, T8>()
      where T1 : struct, IBaseComponent where T2 : struct, IBaseComponent where T3 : struct, IBaseComponent where T4 : struct, IBaseComponent where T5 : struct, IBaseComponent where T6 : struct, IBaseComponent where T7 : struct, IBaseComponent where T8 : struct, IBaseComponent =>
        GetView(this, Key<T1>() + Key<T2>() + Key<T3>() + Key<T4>() + Key<T5>() + Key<T6>() + Key<T7>() + Key<T8>()).AsSpan();
  }
}
