using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace ManulECS.Benchmark {
  [MemoryDiagnoser]
  [SimpleJob(RunStrategy.Throughput, invocationCount: 100)]
  public class RemoveEntity {
    private World world;
    private readonly List<Entity> entities = new();

    [Params(100000)]
    public int N;

    [GlobalSetup]
    public void GlobalSetup() => world = new World();

    [IterationSetup]
    public void Setup() {
      for (int i = 0; i < N * 100; i++) {
        entities.Add(
          world.Handle()
          .Assign(new Comp1 { }).Assign(new Comp2 { })
          .Tag<Tag1>().Tag<Tag2>()
        );
      }
    }

    [IterationCleanup]
    public void Cleanup() {
      entities.Clear();
      world.Clear();
    }

    [Benchmark]
    public void RemoveEntities() {
      for (int i = 0; i < N; i++) {
        world.Remove(entities[i]);
      }
    }

    [Benchmark]
    public void Remove1ComponentFromEntities() {
      for (int i = 0; i < N; i++) {
        world.Remove<Comp1>(entities[i]);
      }
    }

    [Benchmark]
    public void Remove2ComponentsFromEntities() {
      for (int i = 0; i < N; i++) {
        world.Remove<Comp1>(entities[i]);
        world.Remove<Comp2>(entities[i]);
      }
    }

    [Benchmark]
    public void Remove1TagFromEntities() {
      for (int i = 0; i < N; i++) {
        world.Remove<Tag1>(entities[i]);
      }
    }

    [Benchmark]
    public void Remove2TagFromEntities() {
      for (int i = 0; i < N; i++) {
        world.Remove<Tag1>(entities[i]);
        world.Remove<Tag2>(entities[i]);
      }
    }
  }
}
