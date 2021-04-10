using BenchmarkDotNet.Attributes;

namespace ManulECS.Benchmark {
  [MemoryDiagnoser]
  public class CreateEntity {
    public struct Comp1 : IComponent { }
    public struct Comp2 : IComponent { }
    public struct Tag1 : ITag { }
    public struct Tag2 : ITag { }

    [Params(10000, 100000)]
    public int N;

    private World world;

    [IterationSetup]
    public void Setup() {
      world = new World();
      world.Declare<Comp1>();
      world.Declare<Comp2>();
      world.Declare<Tag1>();
      world.Declare<Tag2>();
    }

    [IterationCleanup]
    public void Cleanup() => world = null;

    [Benchmark]
    public void CreateEntities() {
      for (int i = 0; i < N; i++) {
        world.Create();
      }
    }

    [Benchmark]
    public void CreateEntitiesWith1Component() {
      for (int i = 0; i < N; i++) {
        var e = world.Create();
        world.Assign(e, new Comp1 { });
      }
    }
    [Benchmark]
    public void CreateEntitiesWith2Components() {
      for (int i = 0; i < N; i++) {
        var e = world.Create();
        world.Assign(e, new Comp1 { });
        world.Assign(e, new Comp2 { });
      }
    }

    [Benchmark]
    public void CreateEntitiesWith1Tag() {
      for (int i = 0; i < N; i++) {
        var e = world.Create();
        world.Assign<Tag1>(e);
      }
    }

    [Benchmark]
    public void CreateEntitiesWith2Tags() {
      for (int i = 0; i < N; i++) {
        var e = world.Create();
        world.Assign<Tag1>(e);
        world.Assign<Tag2>(e);
      }
    }
  }
}
