using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace ManulECS.Benchmark {
  [MemoryDiagnoser]
  [SimpleJob(RunStrategy.Throughput, invocationCount: 1)]
  public class Serialization {
    private World world;
    private string json;

    [Params(100000)]
    public int N;

    [GlobalSetup]
    public void GlobalSetup() => world = new World();

    [IterationSetup]
    public void Setup() {
      for (int i = 0; i < N; i++) {
        world.Handle().Tag<Tag1>().Assign(new Comp1 { }).Assign(new Comp2 { });
      }
      if (json == null) {
        json = JsonWorldSerializer.Serialize(world);
      }
    }

    [IterationCleanup]
    public void Cleanup() => world.Clear();

    [Benchmark]
    public void Deserialize() {
      JsonWorldSerializer.Deserialize(world, json);
    }

    [Benchmark]
    public void Serialize() {
      JsonWorldSerializer.Serialize(world);
    }
  }
}
