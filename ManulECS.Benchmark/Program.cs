using BenchmarkDotNet.Running;

namespace ManulECS.Benchmark {
  public struct Comp1 : Component { public int value; }
  public struct Comp2 : Component { public int value; }
  public struct Comp3 : Component { public int value; }
  public struct Tag1 : Tag { }
  public struct Tag2 : Tag { }

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
