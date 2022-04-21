using System;

namespace ManulECS {
  [AttributeUsage(AttributeTargets.Struct)]
  public sealed class SparseAttribute : Attribute { }

  [AttributeUsage(AttributeTargets.Struct)]
  public sealed class DenseAttribute : Attribute { }

  public abstract class Pool {
    protected uint[] ids;

    internal int Count { get; set; }
    internal int Capacity => ids.Length;
    internal Key Key { get; init; }

    internal Span<uint> Indices => ids.AsSpan(0, Count);

    internal Action OnUpdate { get; set; }

    internal abstract bool Has(in Entity entity);
    internal abstract object Get(in Entity entity);
    internal abstract void Set(in Entity entity);
    internal abstract void Remove(in Entity entity);
    internal abstract void Clone(in Entity origin, in Entity target);

    internal abstract void Reset();
    internal abstract void Clear();

    public Pool() => Reset();
  }
}
