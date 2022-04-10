using BenchmarkDotNet.Attributes;

namespace ManulECS.Benchmark {
  public class Tags {
    public struct TestTag1 : ITag { }
    public struct TestTag2 : ITag { }

    private World world;

    [Params(10000, 100000)]
    public int N;

    [IterationSetup]
    public void Setup() {
      world = new World();
      world.Declare<TestTag1>();
      world.Declare<TestTag2>();

      for (int i = 0; i < N; i++) {
        var e = world.Create();
        world.Assign<TestTag1>(e);
        world.Assign<TestTag2>(e);
      }

      foreach (var _ in world.View<TestTag1>()) { }
      foreach (var _ in world.View<TestTag1, TestTag2>()) { }
    }

    [IterationCleanup]
    public void Cleanup() => world = null;

    [Benchmark]
    public void Loop1TagView() {
      foreach (var _ in world.View<TestTag1>()) { }
    }

    [Benchmark]
    public void Loop2TagView() {
      foreach (var _ in world.View<TestTag1, TestTag2>()) { }
    }
  }
}
