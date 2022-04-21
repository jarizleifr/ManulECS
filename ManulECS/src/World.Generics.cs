using System.Runtime.CompilerServices;

// Welcome to the incredible world of C# generics! Have fun!

// I've extracted World methods for getting Pools and Views to this separate file as this is a "bit" convoluted.

// In all honesty, all of this could be trivially generated with a source generator, but as the main goal
// of ManulECS is simplicity, I'd rather not include auto-generated code in the sources. 

namespace ManulECS {
  // Aliases to reduce this insanity a bit.
  using Cp = IComponent;
  using Ba = IBaseComponent;

  public partial class World {
    /// <summary>Get a pool of components</summary>
    /// <returns>Pool of components of type T</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pool<T> Pool<T>() where T : struct, Cp => indexedPools[TypeIndex.Get<T>()] as Pool<T>;

    /// <summary>Get two pools of components</summary>
    /// <returns>Tuple of pools of components of types T1, T2</returns>
    public (Pool<T1>, Pool<T2>) Pools<T1, T2>() where T1 : struct, Cp where T2 : struct, Cp =>
      (Pool<T1>(), Pool<T2>());

    /// <summary>Get three pools of components</summary>
    /// <returns>Tuple of pools of components of types T1, T2, T3</returns>
    public (Pool<T1>, Pool<T2>, Pool<T3>) Pools<T1, T2, T3>()
      where T1 : struct, Cp where T2 : struct, Cp where T3 : struct, Cp => (Pool<T1>(), Pool<T2>(), Pool<T3>());

    /// <summary>Get four pools of components</summary>
    /// <returns>Tuple of pools of components of types T1, T2, T3, T4</returns>
    public (Pool<T1>, Pool<T2>, Pool<T3>, Pool<T4>) Pools<T1, T2, T3, T4>()
      where T1 : struct, Cp where T2 : struct, Cp where T3 : struct, Cp where T4 : struct, Cp =>
        (Pool<T1>(), Pool<T2>(), Pool<T3>(), Pool<T4>());

    /// <summary>Get five pools of components</summary>
    /// <returns>Tuple of pools of components of types T1,T2,T3,T4,T5</returns>
    public (Pool<T1>, Pool<T2>, Pool<T3>, Pool<T4>, Pool<T5>) Pools<T1, T2, T3, T4, T5>()
      where T1 : struct, Cp where T2 : struct, Cp where T3 : struct, Cp where T4 : struct, Cp
      where T5 : struct, Cp => (Pool<T1>(), Pool<T2>(), Pool<T3>(), Pool<T4>(), Pool<T5>());

    /// <summary>Get six pools of components</summary>
    /// <returns>Tuple of pools of components of types T1,T2,T3,T4,T5,T6</returns>
    public (Pool<T1>, Pool<T2>, Pool<T3>, Pool<T4>, Pool<T5>, Pool<T6>) Pools<T1, T2, T3, T4, T5, T6>()
        where T1 : struct, Cp where T2 : struct, Cp where T3 : struct, Cp where T4 : struct, Cp
        where T5 : struct, Cp where T6 : struct, Cp =>
          (Pool<T1>(), Pool<T2>(), Pool<T3>(), Pool<T4>(), Pool<T5>(), Pool<T6>());

    /// <summary>Get seven pools of components</summary>
    /// <returns>Tuple of pools of components of types T1,T2,T3,T4,T5,T6,T7</returns>
    public (Pool<T1>, Pool<T2>, Pool<T3>, Pool<T4>, Pool<T5>, Pool<T6>, Pool<T7>)
      Pools<T1, T2, T3, T4, T5, T6, T7>()
        where T1 : struct, Cp where T2 : struct, Cp where T3 : struct, Cp where T4 : struct, Cp
        where T5 : struct, Cp where T6 : struct, Cp where T7 : struct, Cp =>
          (Pool<T1>(), Pool<T2>(), Pool<T3>(), Pool<T4>(), Pool<T5>(), Pool<T6>(), Pool<T7>());

    /// <summary>Get eight pools of components</summary>
    /// <returns>Tuple of pools of components of types T1,T2,T3,T4,T5,T6,T7,T8</returns>
    public (Pool<T1>, Pool<T2>, Pool<T3>, Pool<T4>, Pool<T5>, Pool<T6>, Pool<T7>, Pool<T8>)
      Pools<T1, T2, T3, T4, T5, T6, T7, T8>()
        where T1 : struct, Cp where T2 : struct, Cp where T3 : struct, Cp where T4 : struct, Cp
        where T5 : struct, Cp where T6 : struct, Cp where T7 : struct, Cp where T8 : struct, Cp =>
          (Pool<T1>(), Pool<T2>(), Pool<T3>(), Pool<T4>(), Pool<T5>(), Pool<T6>(), Pool<T7>(), Pool<T8>());

    /// <summary>Get a view of entities with one component</summary>
    /// <returns>View of entities possessing component of type T1</returns>
    public View View<T1>() where T1 : struct, Ba => viewCache.GetView(this, Key<T1>());

    /// <summary>Get a view of entities with two components</summary>
    /// <returns>View of entities possessing components of types T1,T2</returns>
    public View View<T1, T2>() where T1 : struct, Ba where T2 : struct, Ba =>
        viewCache.GetView(this, Key<T1>() + Key<T2>());

    /// <summary>Get a view of entities with three components</summary>
    /// <returns>View of entities possessing components of types T1,T2,T3</returns>
    public View View<T1, T2, T3>() where T1 : struct, Ba where T2 : struct, Ba where T3 : struct, Ba =>
        viewCache.GetView(this, Key<T1>() + Key<T2>() + Key<T3>());

    /// <summary>Get a view of entities with four components</summary>
    /// <returns>View of entities possessing components of types T1,T2,T3,T4</returns>
    public View View<T1, T2, T3, T4>()
      where T1 : struct, Ba where T2 : struct, Ba where T3 : struct, Ba where T4 : struct, Ba =>
        viewCache.GetView(this, Key<T1>() + Key<T2>() + Key<T3>() + Key<T4>());

    /// <summary>Get a view of entities with five components</summary>
    /// <returns>View of entities possessing components of types T1,T2,T3,T4,T5</returns>
    public View View<T1, T2, T3, T4, T5>()
      where T1 : struct, Ba where T2 : struct, Ba where T3 : struct, Ba where T4 : struct, Ba where T5 : struct, Ba =>
        viewCache.GetView(this, Key<T1>() + Key<T2>() + Key<T3>() + Key<T4>() + Key<T5>());

    /// <summary>Get a view of entities with six components</summary>
    /// <returns>View of entities possessing components of types T1,T2,T3,T4,T5,T6</returns>
    public View View<T1, T2, T3, T4, T5, T6>()
      where T1 : struct, Ba where T2 : struct, Ba where T3 : struct, Ba where T4 : struct, Ba
      where T5 : struct, Ba where T6 : struct, Ba =>
        viewCache.GetView(this, Key<T1>() + Key<T2>() + Key<T3>() + Key<T4>() + Key<T5>() + Key<T6>());

    /// <summary>Get a view of entities with seven components</summary>
    /// <returns>View of entities possessing components of types T1,T2,T3,T4,T5,T6,T7</returns>
    public View View<T1, T2, T3, T4, T5, T6, T7>()
      where T1 : struct, Ba where T2 : struct, Ba where T3 : struct, Ba where T4 : struct, Ba
      where T5 : struct, Ba where T6 : struct, Ba where T7 : struct, Ba =>
        viewCache.GetView(this,
          Key<T1>() + Key<T2>() + Key<T3>() + Key<T4>() + Key<T5>() + Key<T6>() + Key<T7>()
        );

    /// <summary>Get a view of entities with eight components</summary>
    /// <returns>View of entities possessing components of types T1,T2,T3,T4,T5,T6,T7,T8</returns>
    public View View<T1, T2, T3, T4, T5, T6, T7, T8>()
      where T1 : struct, Ba where T2 : struct, Ba where T3 : struct, Ba where T4 : struct, Ba
      where T5 : struct, Ba where T6 : struct, Ba where T7 : struct, Ba where T8 : struct, Ba =>
        viewCache.GetView(this,
          Key<T1>() + Key<T2>() + Key<T3>() + Key<T4>() + Key<T5>() + Key<T6>() + Key<T7>() + Key<T8>()
        );
  }
}
