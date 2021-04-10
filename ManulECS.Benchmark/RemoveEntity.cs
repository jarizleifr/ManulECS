using BenchmarkDotNet.Attributes;

namespace ManulECS.Benchmark {
  [MemoryDiagnoser]
  public class RemoveEntity {
    public struct Comp1 : IComponent { }
    public struct Comp2 : IComponent { }
    public struct Tag1 : IComponent { }
    public struct Tag2 : IComponent { }

    [Params(10000, 100000)]
    public int N;

    private World world;
    private Entity testEntity;

    [IterationSetup]
    public void Setup() {
      world = new World();
      world.Declare<Comp1>();
      world.Declare<Comp2>();
      world.Declare<Tag1>();
      world.Declare<Tag2>();

      for (int i = 0; i < N; i++) {
        var e = world.Create();
        world.Assign(e, new Comp1 { });
        world.Assign(e, new Comp2 { });
        world.Assign(e, new Tag1 { });
        world.Assign(e, new Tag2 { });

        if (i == 0) {
          testEntity = e;
        }
      }
    }

    [IterationCleanup]
    public void Cleanup() => world = null;

    [Benchmark]
    public void Remove1ComponentFromEntity() {
      world.Remove<Comp1>(testEntity);
    }

    [Benchmark]
    public void Remove2ComponentsFromEntity() {
      world.Remove<Comp1>(testEntity);
      world.Remove<Comp2>(testEntity);
    }

    [Benchmark]
    public void Remove1TagFromEntity() {
      world.Remove<Tag1>(testEntity);
    }

    [Benchmark]
    public void Remove2TagFromEntity() {
      world.Remove<Tag1>(testEntity);
      world.Remove<Tag2>(testEntity);
    }
  }
}
