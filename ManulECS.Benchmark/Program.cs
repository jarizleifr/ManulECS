using BenchmarkDotNet.Running;

namespace ManulECS.Benchmark {
  public struct Comp1 : IComponent { }
  public struct Comp2 : IComponent { }
  public struct Tag1 : ITag { }
  public struct Tag2 : ITag { }
  public struct Pos : IComponent { public int x, y; }
  public struct Move : IComponent { public int mx, my; }

  public class Program {
    public static void Main() {
      BenchmarkSwitcher.FromTypes(new[] {
        typeof(CreateEntity),
        typeof(RemoveEntity),
        typeof(Components),
        typeof(Tags),
        typeof(Serialization),
      }).RunAll();
    }
  }
}
