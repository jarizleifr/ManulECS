using BenchmarkDotNet.Running;

namespace ManulECS.Benchmark {
  public class Program {
    public static void Main() {
      BenchmarkRunner.Run<CreateEntity>();
      BenchmarkRunner.Run<RemoveEntity>();
      BenchmarkRunner.Run<Tags>();
      BenchmarkRunner.Run<SparseComponents>();
      BenchmarkRunner.Run<SparseComponentsCold>();
      BenchmarkRunner.Run<DenseComponents>();
    }
  }
}
