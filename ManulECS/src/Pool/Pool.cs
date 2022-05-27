using System;
using System.Runtime.CompilerServices;

namespace ManulECS {
  public abstract class Pool {
    protected uint[] ids;
    protected int nextIndex = 0;

    internal readonly Key key;
    internal readonly Omit omit = Omit.None;
    internal readonly string profile = null;

    protected Action onUpdate;
    internal Action OnUpdate {
      get => onUpdate;
      set => onUpdate = value;
    }

    internal int Count => nextIndex;
    internal int Capacity => ids.Length;

    internal Pool(in Key key, ECSSerializeAttribute attribute) {
      this.key = key;
      if (attribute != null) {
        if (attribute.Profile != null) {
          profile = attribute.Profile;
        } else if (attribute.Omit != Omit.None) {
          omit = attribute.Omit;
        }
      }
      Reset();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<uint> AsSpan() => ids.AsSpan(0, nextIndex);

    internal abstract bool Has(uint id);
    internal abstract object Get(uint id);
    internal abstract void Remove(uint id);
    internal abstract void Clone(uint originId, uint targetId);
    internal abstract void Reset();
    internal abstract void Clear();

    /// <summary>Sets a component without knowing its type at compile-time.</summary>
    /// <remarks>Used only in deserialization.</remarks>
    internal abstract void SetRaw(uint id, object component);
  }
}
