using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace ManulECS.Benchmark {
  [MemoryDiagnoser]
  [SimpleJob(RunStrategy.Throughput, invocationCount: 1000)]
  public class DenseComponents : BaseBenchmark {
    [Params(100000)]
    public int N;

    private readonly Random rng = new(0);

    [IterationSetup]
    public void Setup() {
      for (int i = 0; i < N; i++) {
        world.Handle()
          .Assign(new DensePos { x = rng.Next(0, 100), y = rng.Next(0, 100) })
          .Assign(new DenseMove { mx = rng.Next(-1, 2), my = rng.Next(-1, 2) });
      }
      world.View<DensePos>();
      world.View<DensePos, DenseMove>();
    }

    [IterationCleanup]
    public void Cleanup() => world.Clear();

    [Benchmark]
    public void Update1Component() {
      var positions = world.Pool<DensePos>();
      foreach (var e in world.View<DensePos, DenseMove>()) {
        ref var pos = ref positions[e];
        pos.x += 1;
      }
    }
    [Benchmark]
    public void Update2Components() {
      var (positions, moves) = world.Pools<DensePos, DenseMove>();
      foreach (var e in world.View<DensePos, DenseMove>()) {
        ref var pos = ref positions[e];
        ref var mov = ref moves[e];
        pos.x += mov.mx;
        pos.y += mov.my;
      }
    }
  }
}
