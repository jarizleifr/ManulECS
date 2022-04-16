using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace ManulECS.Benchmark {
  [MemoryDiagnoser]
  [SimpleJob(RunStrategy.Throughput, invocationCount: 1)]
  public class CreateEntity : BaseBenchmark {
    [Params(10000000)]
    public int N;

    [IterationCleanup]
    public void Cleanup() => world.Clear();

    [Benchmark]
    public void CreateEntities() {
      for (int i = 0; i < N; i++) {
        world.Create();
      }
    }

    [Benchmark]
    public void CreateEntitiesWith1SparseComponent() => CreateWith1Component<SparsePos>();
    [Benchmark]
    public void CreateEntitiesWith2SparseComponents() => CreateWith2Components<SparsePos, SparseMove>();
    [Benchmark]
    public void CreateEntitiesWith1DenseComponent() => CreateWith1Component<DensePos>();
    [Benchmark]
    public void CreateEntitiesWith2DenseComponents() => CreateWith2Components<DensePos, DenseMove>();
    [Benchmark]
    public void CreateEntitiesWith1SparseTag() => CreateWith1Tag<SparseTag1>();
    [Benchmark]
    public void CreateEntitiesWith2SparseTags() => CreateWith2Tags<SparseTag1, SparseTag2>();
    [Benchmark]
    public void CreateEntitiesWith1DenseTag() => CreateWith1Tag<DenseTag1>();
    [Benchmark]
    public void CreateEntitiesWith2DenseTags() => CreateWith2Tags<DenseTag1, DenseTag2>();

    private void CreateWith1Component<T>() where T : struct, IComponent {
      for (int i = 0; i < N; i++) {
        world.Handle().Assign<T>(new());
      }
    }
    private void CreateWith2Components<T1, T2>()
      where T1 : struct, IComponent
      where T2 : struct, IComponent {
      for (int i = 0; i < N; i++) {
        world.Handle().Assign<T1>(new()).Assign<T2>(new());
      }
    }
    private void CreateWith1Tag<T>() where T : struct, ITag {
      for (int i = 0; i < N; i++) {
        world.Handle().Tag<T>();
      }
    }
    private void CreateWith2Tags<T1, T2>()
      where T1 : struct, ITag
      where T2 : struct, ITag {
      for (int i = 0; i < N; i++) {
        world.Handle().Tag<T1>().Tag<T2>();
      }
    }
  }
}
