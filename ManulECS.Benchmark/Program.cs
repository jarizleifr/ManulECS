using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace ManulECS.Benchmark {
  public struct Comp1 : IComponent { }
  public struct Comp2 : IComponent { }

  public struct Tag1 : ITag { }
  public struct Tag2 : ITag { }
  public struct Pos : IComponent { public int x, y; }
  public struct Move : IComponent { public int mx, my; }

  public abstract class BaseBenchmark {
    protected World world;

    [GlobalSetup]
    public void GlobalSetup() {
      world = new World();
      world.Declare<Comp1>();
      world.Declare<Comp2>();
      world.Declare<Tag1>();
      world.Declare<Tag2>();
      world.Declare<Pos>();
      world.Declare<Move>();
    }

    [GlobalCleanup]
    public void GlobalCleanup() {
      world = null;
      TypeIndex.Reset();
    }
  }

  public class Program {
    public static void Main() {
      BenchmarkSwitcher.FromTypes(new[] {
        typeof(CreateEntity),
        typeof(RemoveEntity),
        typeof(SparseComponents),
        typeof(SparseTags),
      }).RunAll();
    }
  }
}
