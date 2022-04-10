using System;
using BenchmarkDotNet.Attributes;

namespace ManulECS.Benchmark {
  public class SparseComponentsCold {
    [Sparse]
    public struct SparsePos : IComponent { public int x, y; }
    [Sparse]
    public struct SparseMove : IComponent { public int mx, my; }

    private World world;

    [Params(10000, 100000)]
    public int N;

    [IterationSetup]
    public void Setup() {
      world = new World();
      world.Declare<SparsePos>();
      world.Declare<SparseMove>();

      var rng = new Random(0);

      for (int i = 0; i < N; i++) {
        var e = world.Create();
        world.Assign(e, new SparsePos { x = rng.Next(0, 100), y = rng.Next(0, 100) });
        world.Assign(e, new SparseMove { mx = rng.Next(-1, 2), my = rng.Next(-1, 2) });
      }
    }

    [IterationCleanup]
    public void Cleanup() {
      world = null;
    }

    [Benchmark]
    public void Update1Component() {
      var positions = world.Pools<SparsePos>();
      foreach (var e in world.View<SparsePos, SparseMove>()) {
        ref var pos = ref positions[e];
        pos.x += 1;
      }
    }

    [Benchmark]
    public void Update2Components() {
      var (positions, moves) = world.Pools<SparsePos, SparseMove>();
      foreach (var e in world.View<SparsePos, SparseMove>()) {
        ref var pos = ref positions[e];
        ref var mov = ref moves[e];
        pos.x += mov.mx;
        pos.y += mov.my;
      }
    }
  }
}
