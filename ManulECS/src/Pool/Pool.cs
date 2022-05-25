using System;

namespace ManulECS {
  public abstract class Pool {
    protected Entity[] entities;
    protected int nextIndex = 0;

    internal Omit Omit { get; init; } = Omit.None;
    internal string Profile { get; init; } = null;

    internal Key Key { get; init; }

    protected Action onUpdate;
    internal Action OnUpdate {
      get => onUpdate;
      set => onUpdate = value;
    }

    internal int Count => nextIndex;
    internal int Capacity => entities.Length;

    internal Pool(in Key key, ECSSerializeAttribute attribute) {
      Key = key;
      if (attribute != null) {
        if (attribute.Profile != null) {
          Profile = attribute.Profile;
        } else if (attribute.Omit != Omit.None) {
          Omit = attribute.Omit;
        }
      }
      Reset();
    }

    public ReadOnlySpan<Entity> AsSpan() => entities.AsSpan(0, nextIndex);

    internal abstract bool Has(in Entity entity);
    internal abstract object Get(in Entity entity);
    internal abstract void Remove(in Entity entity);
    internal abstract void Clone(in Entity origin, in Entity target);
    internal abstract void Reset();
    internal abstract void Clear();

    /// <summary>Sets a component without knowing its type at compile-time.</summary>
    /// <remarks>Used only in deserialization.</remarks>
    internal abstract void SetObject(in Entity entity, object component);
  }
}
