using BenchmarkDotNet.Running;

namespace YesSql.Samples.Performance
{
    public class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<Benchmarks>();
        }
    }
}
