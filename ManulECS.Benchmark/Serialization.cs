using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace ManulECS.Benchmark {
  [MemoryDiagnoser]
  [SimpleJob(RunStrategy.Throughput, invocationCount: 1)]
  public class Serialization {
    private World world;
    private string json;
    private byte[] bytes;

    private JsonWorldSerializer serializer = new() {
      Namespace = "ManulECS.Benchmark",
      AssemblyName = "ManulECS.Benchmark"
    };

    [Params(100000)]
    public int N;

    [GlobalSetup]
    public void GlobalSetup() => world = new World();

    [IterationSetup]
    public void Setup() {
      for (int i = 0; i < N; i++) {
        world.Handle().Tag<Tag1>().Assign(new Comp1 { }).Assign(new Comp2 { });
      }
      if (json == null) {
        using var stream = new MemoryStream();
        serializer.Write(stream, world);
        json = Encoding.UTF8.GetString(stream.ToArray());
        bytes = Encoding.UTF8.GetBytes(json);
      }
    }

    [IterationCleanup]
    public void Cleanup() => world.Clear();

    [Benchmark]
    public void Serialize() {
      using var stream = new MemoryStream();
      serializer.Write(stream, world);
    }

    [Benchmark]
    public void Deserialize() {
      using var stream = new MemoryStream(bytes);
      serializer.Read(stream, world);
    }
  }
}
