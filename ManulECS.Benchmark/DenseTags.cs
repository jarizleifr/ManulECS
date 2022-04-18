using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace ManulECS.Benchmark {
  [MemoryDiagnoser]
  [SimpleJob(RunStrategy.Throughput, invocationCount: 10000)]
  public class DenseTags : BaseBenchmark {
    [Params(100000)]
    public int N;

    [IterationSetup]
    public void Setup() {
      for (int i = 0; i < N; i++) {
        world.Handle().Tag<DenseTag1>().Tag<DenseTag2>();
      }
      world.View<DenseTag1>();
      world.View<DenseTag1, DenseTag2>();
    }

    [IterationCleanup]
    public void Cleanup() => world.Clear();

    [Benchmark]
    public void Loop1Tag() {
      Entity entity;
      foreach (var e in world.View<DenseTag1>()) {
        entity = e;
      }
    }
    [Benchmark]
    public void Loop2Tags() {
      Entity entity;
      foreach (var e in world.View<DenseTag1, DenseTag2>()) {
        entity = e;
      }
    }
  }
}
