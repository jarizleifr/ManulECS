using System;
using System.Collections;
using System.Collections.Generic;
using static System.Reflection.BindingFlags;
using static ManulECS.ArrayUtil;

namespace ManulECS;

internal sealed class ResourceStore : IEnumerable<object> {
  private static int nextId = 0;
  private static Dictionary<Type, int> types = new();
  private static class ResourceType<T> {
    internal readonly static int id = int.MaxValue;

    static ResourceType() {
      id = nextId++;
      types.Add(typeof(T), id);
    }
  }

  private object[] resources = new object[World.INITIAL_CAPACITY];

  public void Set<T>(T resource) {
    var id = GetId<T>();
    if (id >= resources.Length) {
      Resize(ref resources, id);
    }
    resources[id] = resource;
  }

  public void SetRaw(object resource, Type type) {
    if (types.TryGetValue(type, out var id) && id < resources.Length) {
      resources[id] = resource;
    } else {
      GetType()
        .GetMethod(nameof(this.Set), Public | Instance)
        .MakeGenericMethod(type)
        .Invoke(this, new[] { resource });
    }
  }

  public void Remove<T>() {
    var id = GetId<T>();
    if (id < resources.Length) {
      resources[id] = null;
    }
  }

  public void Clear() => resources = new object[World.INITIAL_CAPACITY];

  ///<exception cref="IndexOutOfRangeException">
  ///Thrown when resource of type T hasn't been set.
  ///</exception>
  public T Get<T>() => (T)resources[GetId<T>()];

  public IEnumerator<object> GetEnumerator() {
    foreach (var resource in resources) {
      if (resource != null) {
        yield return resource;
      }
    }
  }

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  internal int GetId<T>() => ResourceType<T>.id;
}
