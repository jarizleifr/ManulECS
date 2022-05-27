using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace ManulECS.Benchmark {
  [MemoryDiagnoser]
  [SimpleJob(RunStrategy.Throughput, warmupCount: 24, invocationCount: 1000)]
  public class Components {
    private World world;

    [Params(100000)]
    public int N;

    [GlobalSetup]
    public void GlobalSetup() => world = new World();

    [IterationSetup]
    public void Setup() {
      for (int i = 0; i < N; i++) {
        world.Handle()
          .Assign(new Comp1 { value = 3 })
          .Assign(new Comp2 { value = 5 })
          .Assign(new Comp3 { value = 7 });
      }
      world.View<Comp1>();
      world.View<Comp1, Comp2>();
      world.View<Comp1, Comp2, Comp3>();
    }

    [IterationCleanup]
    public void Cleanup() => world.Clear();

    [Benchmark]
    public void Update1Component() {
      var comps1 = world.Pool<Comp1>();
      foreach (var e in world.View<Comp1>()) {
        ref var c1 = ref comps1[e];
        ++c1.value;
      }
    }

    [Benchmark]
    public void Update2Components() {
      var (comps1, comps2) = world.Pools<Comp1, Comp2>();
      foreach (var e in world.View<Comp1, Comp2>()) {
        ref var c1 = ref comps1[e];
        var c2 = comps2[e];
        c1.value += c2.value;
      }
    }

    [Benchmark]
    public void Update2Components_WorstCaseScenario() {
      var view = world.GetView(world.pools.GetKey<Comp1, Comp2>());
      view.SetToDirty();
      view.Update(world);
      var (comps1, comps2) = world.Pools<Comp1, Comp2>();
      foreach (var e in (ReadOnlySpan<Entity>)view) {
        ref var c1 = ref comps1[e];
        var c2 = comps2[e];
        c1.value += c2.value;
      }
    }

    [Benchmark]
    public void Update3Components() {
      var (comps1, comps2, comps3) = world.Pools<Comp1, Comp2, Comp3>();
      foreach (var e in world.View<Comp1, Comp2, Comp3>()) {
        ref var c1 = ref comps1[e];
        var c2 = comps2[e];
        var c3 = comps3[e];
        c1.value += c2.value + c3.value;
      }
    }
  }
}
