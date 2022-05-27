namespace ManulECS {
  /// <summary>Marker interface for either tag or component constraints. Internal use only.</summary>
  public interface BaseComponent { }

  /// <summary>Marker interface for component constraints.</summary>
  public interface Component : BaseComponent { }

  /// <summary>Marker interface for tag constraints.</summary>
  public interface Tag : BaseComponent { }
}

