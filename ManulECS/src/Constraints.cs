namespace ManulECS {
  /// <summary>Marker interface for either tag or component constraints. Internal use only.</summary>
  public interface IBaseComponent { }

  /// <summary>Marker interface for component constraints.</summary>
  public interface IComponent : IBaseComponent { }

  /// <summary>Marker interface for tag constraints.</summary>
  public interface ITag : IBaseComponent { }
}
