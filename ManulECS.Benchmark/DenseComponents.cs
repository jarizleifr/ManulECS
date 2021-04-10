using System;
using BenchmarkDotNet.Attributes;

namespace ManulECS.Benchmark {
  public class DenseComponents {
    [Dense]
    public struct DensePos : IComponent { public int x, y; }
    [Dense]
    public struct DenseMove : IComponent { public int mx, my; }

    private World world;

    [Params(10000, 100000)]
    public int N;

    [GlobalSetup]
    public void Setup() {
      world = new World();
      world.Declare<DensePos>();
      world.Declare<DenseMove>();

      var rng = new Random(0);

      for (int i = 0; i < N; i++) {
        var e = world.Create();
        world.Assign(e, new DensePos { x = rng.Next(0, 100), y = rng.Next(0, 100) });
        world.Assign(e, new DenseMove { mx = rng.Next(-1, 2), my = rng.Next(-1, 2) });
      }

      foreach (var _ in world.View<DensePos, DenseMove>()) { }
    }

    [GlobalCleanup]
    public void Cleanup() {
      world = null;
    }

    [Benchmark]
    public void Update1Component() {
      foreach (var e in world.View<DensePos>()) {
        ref var pos = ref world.GetRef<DensePos>(e);
        pos.x += 1;
      }
    }

    [Benchmark]
    public void Update2Components() {
      foreach (var e in world.View<DensePos, DenseMove>()) {
        ref var pos = ref world.GetRef<DensePos>(e);
        ref var mov = ref world.GetRef<DenseMove>(e);
        pos.x += mov.mx;
        pos.y += mov.my;
      }
    }
  }
}
