using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace ManulECS.Benchmark {
  [MemoryDiagnoser]
  [SimpleJob(RunStrategy.Throughput, invocationCount: 80)]
  public class CreateEntity {
    private World world;

    [Params(100000)]
    public int N;

    [GlobalSetup]
    public void GlobalSetup() => world = new World();

    [IterationCleanup]
    public void Cleanup() => world.Clear();

    [Benchmark]
    public void CreateEntities() {
      for (int i = 0; i < N; i++) {
        world.Create();
      }
    }

    [Benchmark]
    public void CreateEntitiesWith1Component() {
      for (int i = 0; i < N; i++) {
        var entity = world.Create();
        world.Assign<Pos>(entity, new());
      }
    }

    [Benchmark]
    public void CreateEntitiesWith2Components() {
      for (int i = 0; i < N; i++) {
        var entity = world.Create();
        world.Assign<Pos>(entity, new());
        world.Assign<Move>(entity, new());
      }
    }

    [Benchmark]
    public void CreateEntitiesWith1Tag() {
      for (int i = 0; i < N; i++) {
        var entity = world.Create();
        world.Tag<Tag1>(entity);
      }
    }

    [Benchmark]
    public void CreateEntitiesWith2Tags() {
      for (int i = 0; i < N; i++) {
        var entity = world.Create();
        world.Tag<Tag1>(entity);
        world.Tag<Tag2>(entity);
      }
    }
  }
}
