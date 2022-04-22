using System;

namespace ManulECS {
  public abstract class Pool {
    protected Entity[] entities;
    protected int nextIndex = 0;

    internal Key Key { get; init; }
    internal Action OnUpdate { get; set; }
    internal int Count => nextIndex;
    internal int Capacity => entities.Length;

    internal abstract bool Has(in Entity entity);
    internal abstract object Get(in Entity entity);
    internal abstract void Remove(in Entity entity);
    internal abstract void Clone(in Entity origin, in Entity target);

    internal abstract void Reset();
    internal abstract void Clear();

    internal Pool() => Reset();

    public EntityEnumerator GetEnumerator() => new(entities, nextIndex);
  }
}
