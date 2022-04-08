using System;
using System.Collections.Generic;

namespace ManulECS {
  public class View {
    private readonly List<Entity> entities;
    private readonly int[] versions = new int[8];

    public View(World world, in FlagEnum matcher) {
      entities = new List<Entity>();
      Update(world, matcher);
    }

    ///<summary>Checks if any of the assigned pools has changed.</summary>
    public bool IsDirty(World world, in FlagEnum matcher) {
      int v = 0;
      foreach (var idx in matcher) {
        var pool = world.components.GetIndexedPool(idx);
        if (versions[v++] != pool.Version) {
          return true;
        }
      }
      return false;
    }

    public void Update(World world, in FlagEnum matcher) {
      int v = 0;
      Span<uint> smallestSet = null;
      foreach (var idx in matcher) {
        var pool = world.components.GetIndexedPool(idx);
        if (smallestSet == null || smallestSet.Length > pool.Count) {
          versions[v++] = pool.Version;
          smallestSet = pool.GetIndices();
        }
      }

      entities.Clear();
      foreach (var id in smallestSet) {
        var entityData = world.GetEntityDataByIndex(id);
        if (entityData.IsSubsetOf(matcher)) {
          var entity = world.GetEntityByIndex(id);
          entities.Add(entity);
        }
      }
    }

    public struct ViewEnumerator {
      private readonly List<Entity> entities;
      private int index;

      internal ViewEnumerator(List<Entity> entities) =>
          (this.entities, index) = (entities, -1);

      public Entity Current => entities[index];

      public bool MoveNext() => ++index < entities.Count;

      public void Reset() => index = -1;
    }

    public ViewEnumerator GetEnumerator() => new(entities);
  }

  public class ViewCache {
    private readonly Dictionary<FlagEnum, View> views = new();

    public bool Contains(in FlagEnum matcher) => views.ContainsKey(matcher);

    public View GetView(World world, in FlagEnum matcher) {
      if (views.TryGetValue(matcher, out View existingView)) {
        if (existingView.IsDirty(world, matcher)) {
          existingView.Update(world, matcher);
        }
        return existingView;
      } else {
        var view = new View(world, matcher);
        views.Add(matcher, view);
        return view;
      }
    }

    public void Clear() => views.Clear();
  }
}
