using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Running;

namespace FaceAISharpBenchmarks
{
    internal static class Program
    {
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static void Main(string[] args)
        {
            BenchmarkRunner.Run<Bench>();

            while (true)
            {
                Console.WriteLine("\nDone! Press X to exit.");
                
                if (Console.ReadKey().Key == ConsoleKey.X)
                {
                    break;
                }
            }
        }
    }
}