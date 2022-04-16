using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace ManulECS.Benchmark {
  [MemoryDiagnoser]
  [SimpleJob(RunStrategy.Throughput, invocationCount: 1)]
  public class RemoveEntity : BaseBenchmark {
    [Params(10000000)]
    public int N;

    [IterationSetup]
    public void Setup() {
      for (int i = 0; i < N; i++) {
        world.Handle()
          .Assign(new SparsePos { }).Assign(new SparseMove { })
          .Assign(new DensePos { }).Assign(new DenseMove { })
          .Tag<SparseTag1>().Tag<SparseTag2>()
          .Tag<DenseTag1>().Tag<DenseTag2>();
      }
    }
    [IterationCleanup]
    public void Cleanup() => world.Clear();

    [Benchmark]
    public void Remove1SparseComponentFromEntities() => Remove1<SparsePos>();
    [Benchmark]
    public void Remove2SparseComponentsFromEntities() => Remove2<SparsePos, SparseMove>();
    [Benchmark]
    public void Remove1DenseComponentFromEntities() => Remove1<DensePos>();
    [Benchmark]
    public void Remove2DenseComponentsFromEntities() => Remove2<DensePos, DenseMove>();
    [Benchmark]
    public void Remove1SparseTagFromEntities() => Remove1<SparseTag1>();
    [Benchmark]
    public void Remove2SparseTagFromEntities() => Remove2<SparseTag1, SparseTag2>();
    [Benchmark]
    public void Remove1DenseTagFromEntities() => Remove1<DenseTag1>();
    [Benchmark]
    public void Remove2DenseTagFromEntities() => Remove2<DenseTag1, DenseTag2>();

    private void Remove1<T>() where T : struct, IBaseComponent {
      for (int i = 0; i < N; i++) {
        world.Remove<T>(world.entities[i]);
      }
    }
    private void Remove2<T1, T2>()
      where T1 : struct, IBaseComponent
      where T2 : struct, IBaseComponent {
      for (int i = 0; i < N; i++) {
        world.Remove<T1>(world.entities[i]);
        world.Remove<T2>(world.entities[i]);
      }
    }
  }
}
