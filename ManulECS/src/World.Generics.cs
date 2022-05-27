using System;
using System.Runtime.CompilerServices;

namespace ManulECS {
  /* Here we (ab)use the generic system to get us Pools and Views of components by their generic
   * type. Constraints get verbose very quickly, as there's no support for variadic type parameters
   * in C#, out of what you could do with code generation.
   *
   * Instead, I decided to just write this all out in a partial class, as introducing a source
   * generator would just trade some repetition for a completely new layer of complexity.
   */
  public partial class World {
    // Helpers for getting multiple pools at once, to use with Views.

    /// <summary>Gets a component Pool by its generic type.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pool<T> Pool<T>() where T : struct, Component => (Pool<T>)pools.RawPool<T>();

    /// <summary>Gets a tuple of pools of components of types A,B</summary>
    public (Pool<A>, Pool<B>) Pools<A, B>() where A : struct, Component where B : struct, Component =>
      (Pool<A>(), Pool<B>());

    /// <summary>Gets a tuple of pools of components of types A,B,C</summary>
    public (Pool<A>, Pool<B>, Pool<C>) Pools<A, B, C>()
      where A : struct, Component where B : struct, Component where C : struct, Component =>
      (Pool<A>(), Pool<B>(), Pool<C>());

    /// <summary>Gets a tuple of pools of components of types A,B,C,D</summary>
    public (Pool<A>, Pool<B>, Pool<C>, Pool<D>) Pools<A, B, C, D>()
      where A : struct, Component where B : struct, Component where C : struct, Component where D : struct, Component =>
      (Pool<A>(), Pool<B>(), Pool<C>(), Pool<D>());

    /// <summary>Gets a tuple of pools of components of types A,B,C,D,E</summary>
    public (Pool<A>, Pool<B>, Pool<C>, Pool<D>, Pool<E>) Pools<A, B, C, D, E>()
      where A : struct, Component where B : struct, Component where C : struct, Component where D : struct, Component
      where E : struct, Component =>
      (Pool<A>(), Pool<B>(), Pool<C>(), Pool<D>(), Pool<E>());

    /// <summary>Gets a tuple of pools of components of types A,B,C,D,E,F</summary>
    public (Pool<A>, Pool<B>, Pool<C>, Pool<D>, Pool<E>, Pool<F>) Pools<A, B, C, D, E, F>()
      where A : struct, Component where B : struct, Component where C : struct, Component where D : struct, Component
      where E : struct, Component where F : struct, Component =>
      (Pool<A>(), Pool<B>(), Pool<C>(), Pool<D>(), Pool<E>(), Pool<F>());

    /// <summary>Gets a tuple of pools of components of types A,B,C,D,E,F,G</summary>
    public (Pool<A>, Pool<B>, Pool<C>, Pool<D>, Pool<E>, Pool<F>, Pool<G>) Pools<A, B, C, D, E, F, G>()
      where A : struct, Component where B : struct, Component where C : struct, Component where D : struct, Component
      where E : struct, Component where F : struct, Component where G : struct, Component =>
      (Pool<A>(), Pool<B>(), Pool<C>(), Pool<D>(), Pool<E>(), Pool<F>(), Pool<G>());

    /// <summary>Gets a tuple of pools of components of types A,B,C,D,E,F,G,H</summary>
    public (Pool<A>, Pool<B>, Pool<C>, Pool<D>, Pool<E>, Pool<F>, Pool<G>, Pool<H>) Pools<A, B, C, D, E, F, G, H>()
      where A : struct, Component where B : struct, Component where C : struct, Component where D : struct, Component
      where E : struct, Component where F : struct, Component where G : struct, Component where H : struct, Component =>
      (Pool<A>(), Pool<B>(), Pool<C>(), Pool<D>(), Pool<E>(), Pool<F>(), Pool<G>(), Pool<H>());

    // Overloads for getting Views with a specific Component and/or Tag configuration.

    /// <summary>Get a view of entities with one component</summary>
    /// <returns>Span of entities possessing component of type A</returns>
    public ReadOnlySpan<Entity> View<A>() where A : struct, BaseComponent => GetView(pools.GetKey<A>());

    /// <summary>Get a view of entities with two components</summary>
    /// <returns>Span of entities possessing components of types A,B</returns>
    public ReadOnlySpan<Entity> View<A, B>() where A : struct, BaseComponent where B : struct, BaseComponent =>
      GetView(pools.GetKey<A, B>());

    /// <summary>Get a view of entities with three components</summary>
    /// <returns>Span of entities possessing components of types A,B,C</returns>
    public ReadOnlySpan<Entity> View<A, B, C>()
      where A : struct, BaseComponent where B : struct, BaseComponent where C : struct, BaseComponent =>
      GetView(pools.GetKey<A, B, C>());

    /// <summary>Get a view of entities with four components</summary>
    /// <returns>Span of entities possessing components of types A,B,C,D</returns>
    public ReadOnlySpan<Entity> View<A, B, C, D>()
      where A : struct, BaseComponent where B : struct, BaseComponent where C : struct, BaseComponent where D : struct, BaseComponent =>
      GetView(pools.GetKey<A, B, C, D>());

    /// <summary>Get a view of entities with five components</summary>
    /// <returns>Span of entities possessing components of types A,B,C,D,E</returns>
    public ReadOnlySpan<Entity> View<A, B, C, D, E>()
      where A : struct, BaseComponent where B : struct, BaseComponent where C : struct, BaseComponent where D : struct, BaseComponent
      where E : struct, BaseComponent =>
      GetView(pools.GetKey<A, B, C, D, E>());

    /// <summary>Get a view of entities with six components</summary>
    /// <returns>Span of entities possessing components of types A,B,C,D,E,F</returns>
    public ReadOnlySpan<Entity> View<A, B, C, D, E, F>()
      where A : struct, BaseComponent where B : struct, BaseComponent where C : struct, BaseComponent where D : struct, BaseComponent
      where E : struct, BaseComponent where F : struct, BaseComponent =>
      GetView(pools.GetKey<A, B, C, D, E, F>());

    /// <summary>Get a view of entities with seven components</summary>
    /// <returns>Span of entities possessing components of types A,B,C,D,E,F,G</returns>
    public ReadOnlySpan<Entity> View<A, B, C, D, E, F, G>()
      where A : struct, BaseComponent where B : struct, BaseComponent where C : struct, BaseComponent where D : struct, BaseComponent
      where E : struct, BaseComponent where F : struct, BaseComponent where G : struct, BaseComponent =>
      GetView(pools.GetKey<A, B, C, D, E, F, G>());

    /// <summary>Get a view of entities with eight components</summary>
    /// <returns>Span of entities possessing components of types A,B,C,D,E,F,G,H</returns>
    public ReadOnlySpan<Entity> View<A, B, C, D, E, F, G, H>()
      where A : struct, BaseComponent where B : struct, BaseComponent where C : struct, BaseComponent where D : struct, BaseComponent
      where E : struct, BaseComponent where F : struct, BaseComponent where G : struct, BaseComponent where H : struct, BaseComponent =>
      GetView(pools.GetKey<A, B, C, D, E, F, G, H>());
  }
}
