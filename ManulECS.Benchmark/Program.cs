using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace ManulECS.Benchmark {
  public struct Comp1 : IComponent { }
  public struct Comp2 : IComponent { }

  [Sparse]
  public struct SparseTag1 : ITag { }
  [Sparse]
  public struct SparseTag2 : ITag { }

  [Dense]
  public struct DenseTag1 : ITag { }
  [Dense]
  public struct DenseTag2 : ITag { }

  [Dense]
  public struct DensePos : IComponent { public int x, y; }
  [Dense]
  public struct DenseMove : IComponent { public int mx, my; }

  [Sparse]
  public struct SparsePos : IComponent { public int x, y; }
  [Sparse]
  public struct SparseMove : IComponent { public int mx, my; }

  public abstract class BaseBenchmark {
    protected World world;

    [GlobalSetup]
    public void GlobalSetup() {
      world = new World();
      world.Declare<Comp1>();
      world.Declare<Comp2>();
      world.Declare<SparseTag1>();
      world.Declare<SparseTag2>();
      world.Declare<DenseTag1>();
      world.Declare<DenseTag2>();
      world.Declare<DensePos>();
      world.Declare<DenseMove>();
      world.Declare<SparsePos>();
      world.Declare<SparseMove>();
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
        //typeof(CreateEntity),
        //typeof(RemoveEntity),
        typeof(SparseComponents),
        typeof(DenseComponents),
        //typeof(SparseTags),
        //typeof(DenseTags),
      }).RunAll();

      /*var test = new SparseComponents();
      test.N = 1000;
      test.GlobalSetup();
      test.Setup();
      for (int i = 0; i < 1000; i++) {
        test.Update2Components();
      }
      test.Cleanup();
      test.GlobalCleanup();*/
    }
  }
}
