using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace ManulECS.Benchmark {
  [MemoryDiagnoser]
  [SimpleJob(RunStrategy.Throughput, invocationCount: 100)]
  public class RemoveEntity : BaseBenchmark {
    [Params(100000)]
    public int N;

    private readonly List<Entity> entities = new();

    [IterationSetup]
    public void Setup() {
      for (int i = 0; i < N * 100; i++) {
        entities.Add(
          world.Handle()
            .Assign(new Pos { }).Assign(new Move { })
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
    public void Remove1ComponentFromEntities() => Remove1<Pos>();
    [Benchmark]
    public void Remove2ComponentsFromEntities() => Remove2<Pos, Move>();
    [Benchmark]
    public void Remove1TagFromEntities() => Remove1<Tag1>();
    [Benchmark]
    public void Remove2TagFromEntities() => Remove2<Tag1, Tag2>();

    private void Remove1<T>() where T : struct, IBaseComponent {
      for (int i = 0; i < N; i++) {
        world.Remove<T>(entities[i]);
      }
    }
    private void Remove2<T1, T2>()
      where T1 : struct, IBaseComponent
      where T2 : struct, IBaseComponent {
      for (int i = 0; i < N; i++) {
        world.Remove<T1>(entities[i]);
        world.Remove<T2>(entities[i]);
      }
    }
  }
}
