using System;

namespace ManulECS {
  public abstract class Pool {
    protected uint[] ids;
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
    internal int Capacity => ids.Length;

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

    public ReadOnlySpan<uint> AsSpan() => ids.AsSpan(0, nextIndex);

    internal abstract bool Has(uint id);
    internal abstract object Get(uint id);
    internal abstract void Remove(uint id);
    internal abstract void Clone(uint originId, uint targetId);
    internal abstract void Reset();
    internal abstract void Clear();

    /// <summary>Sets a component without knowing its type at compile-time.</summary>
    /// <remarks>Used only in deserialization.</remarks>
    internal abstract void SetObject(uint id, object component);
  }
}
