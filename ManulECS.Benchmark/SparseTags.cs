using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace ManulECS.Benchmark {
  [MemoryDiagnoser]
  [SimpleJob(RunStrategy.Throughput, invocationCount: 1000)]
  public class SparseTags : BaseBenchmark {
    [Params(100000)]
    public int N;

    [IterationSetup]
    public void Setup() {
      for (int i = 0; i < N; i++) {
        world.Handle().Tag<SparseTag1>().Tag<SparseTag2>();
      }
      world.View<SparseTag1>();
      world.View<SparseTag1, SparseTag2>();
    }

    [IterationCleanup]
    public void Cleanup() => world.Clear();

    [Benchmark]
    public void Loop1Tag() {
      Entity entity;
      foreach (var e in world.View<SparseTag1>()) {
        entity = e;
      }
    }
    [Benchmark]
    public void Loop2Tags() {
      Entity entity;
      foreach (var e in world.View<SparseTag1, SparseTag2>()) {
        entity = e;
      }
    }
  }
}
