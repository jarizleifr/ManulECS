using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace ManulECS.Benchmark {
  [MemoryDiagnoser]
  [SimpleJob(RunStrategy.Throughput, invocationCount: 10000)]
  public class Tags {
    private World world;

    [Params(100000)]
    public int N;

    [GlobalSetup]
    public void GlobalSetup() => world = new World();

    [IterationSetup]
    public void Setup() {
      for (int i = 0; i < N; i++) {
        world.Handle().Tag<Tag1>().Tag<Tag2>();
      }
      world.View<Tag1>();
      world.View<Tag1, Tag2>();
    }

    [IterationCleanup]
    public void Cleanup() => world.Clear();

    [Benchmark]
    public void Loop1Tag() {
      Entity entity;
      foreach (var e in world.View<Tag1>()) {
        entity = e;
      }
    }

    [Benchmark]
    public void Loop2Tags() {
      Entity entity;
      foreach (var e in world.View<Tag1, Tag2>()) {
        entity = e;
      }
    }
  }
}
