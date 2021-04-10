using BenchmarkDotNet.Running;

namespace ManulECS.Benchmark {
  public class Program {
    public static void Main() {
      //BenchmarkRunner.Run<CreateEntity>();
      //BenchmarkRunner.Run<SparseComponents>();
      BenchmarkRunner.Run<Tags>();
      BenchmarkRunner.Run<RemoveEntity>();
      //BenchmarkRunner.Run<SparseComponentsCold>();
      //BenchmarkRunner.Run<DenseComponents>();
    }
  }
}
