using System.Collections.Generic;

namespace ManulECS {
  internal struct ViewCache {
    private readonly Dictionary<Matcher, View> views = new();

    public ViewCache() { }

    internal View GetView(World world, Matcher matcher) {
      if (views.TryGetValue(matcher, out View existingView)) {
        existingView.Update(world);
        return existingView;
      } else {
        var view = new View(world, matcher);
        views.Add(matcher, view);
        return view;
      }
    }

    internal bool Contains(Matcher matcher) => views.ContainsKey(matcher);

    internal void Clear() => views.Clear();
  }
}
