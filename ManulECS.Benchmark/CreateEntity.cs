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
    public void CreateEntitiesWith1Component() => CreateWith1Component<Pos>();

    [Benchmark]
    public void CreateEntitiesWith2Components() => CreateWith2Components<Pos, Move>();

    [Benchmark]
    public void CreateEntitiesWith1Tag() => CreateWith1Tag<Tag1>();

    [Benchmark]
    public void CreateEntitiesWith2Tags() => CreateWith2Tags<Tag1, Tag2>();

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
