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
      foreach (var e in world.View<SparsePos, SparseMove>()) {
        ref var pos = ref world.GetRef<SparsePos>(e);
        pos.x += 1;
      }
    }

    [Benchmark]
    public void Update2Components() {

      foreach (var e in world.View<SparsePos, SparseMove>()) {
        ref var pos = ref world.GetRef<SparsePos>(e);
        ref var mov = ref world.GetRef<SparseMove>(e);
        pos.x += mov.mx;
        pos.y += mov.my;
      }
    }
  }
}
