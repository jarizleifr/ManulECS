using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace ManulECS.Benchmark {
  [MemoryDiagnoser]
  [SimpleJob(RunStrategy.Throughput, invocationCount: 1000)]
  public class Components {
    private World world;
    private Key viewKey;

    [Params(100000)]
    public int N;

    [GlobalSetup]
    public void GlobalSetup() {
      world = new World();
      viewKey = world.Key<Pos>() + world.Key<Move>();
    }

    [IterationSetup]
    public void Setup() {
      for (int i = 0; i < N; i++) {
        world.Handle()
          .Assign(new Pos { x = 2, y = 5 })
          .Assign(new Move { mx = 7, my = 3 });
      }
      world.View<Pos>();
      world.View<Pos, Move>();
    }

    [IterationCleanup]
    public void Cleanup() => world.Clear();

    [Benchmark]
    public void Update1Component() {
      var positions = world.Pool<Pos>();
      foreach (var e in world.View<Pos>()) {
        ref var pos = ref positions[e];
        pos.x += 1;
      }
    }

    [Benchmark]
    public void Update2Components() {
      var (positions, moves) = world.Pools<Pos, Move>();
      foreach (var e in world.View<Pos, Move>()) {
        ref var pos = ref positions[e];
        ref var mov = ref moves[e];
        pos.x += mov.mx;
        pos.y += mov.my;
      }
    }

    [Benchmark]
    public void Update2Components_WorstCaseScenario() {
      var view = world.views[viewKey];
      view.SetToDirty();
      view.Update(world);
      var (positions, moves) = world.Pools<Pos, Move>();
      foreach (var e in view.AsSpan()) {
        ref var pos = ref positions[e];
        ref var mov = ref moves[e];
        pos.x += mov.mx;
        pos.y += mov.my;
      }
    }
  }
}
