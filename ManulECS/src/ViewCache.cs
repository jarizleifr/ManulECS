using System.Collections.Generic;

namespace ManulECS {
  internal struct ViewCache {
    private readonly Dictionary<Key, View> views = new();

    public ViewCache() { }

    internal View GetView(World world, Key key) {
      if (views.TryGetValue(key, out View existingView)) {
        existingView.Update(world);
        return existingView;
      } else {
        var view = new View(world, key);
        views.Add(key, view);
        return view;
      }
    }

    internal bool Contains(Key key) => views.ContainsKey(key);

    internal void Clear() {
      foreach (var view in views.Values) {
        view.Dispose();
      }
      views.Clear();
    }
  }
}
