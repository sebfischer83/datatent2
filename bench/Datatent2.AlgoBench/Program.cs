using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Datatent2.AlgoBench
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
