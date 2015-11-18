using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArrayBench
{
    class Program
    {
        static void Main(string[] args)
        {
            // get data before hand.

            List<TestSubClass> list = (from i in Enumerable.Range(0, 10000)
                                       select new TestSubClass { A = i, B = i }).ToList();
            TestSubClass[] arr = list.ToArray();
            TestClass[] vararr = arr;

            // run benchmarks.

            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

            List<BenchmarkResult> results = new List<BenchmarkResult>();

            Benchmark(results, "list", list);
            Benchmark(results, "arr", arr);
            Benchmark(results, "vararr", vararr);

            BenchmarkResult.Write("results", results);
        }

        static void Benchmark<T>(List<BenchmarkResult> results, string name, IList<T> list) where T : TestClass
        {
            Benchmark(results, $"{name} SkipList(1)", 1, () => SkipList(list, 1).Sum(x => x.A));
            Benchmark(results, $"{name} SkipList(10)", 1, () => SkipList(list, 10).Sum(x => x.A));
            Benchmark(results, $"{name} SkipList(100)", 1, () => SkipList(list, 100).Sum(x => x.A));
            Benchmark(results, $"{name} SkipList(1000)", 1, () => SkipList(list, 1000).Sum(x => x.A));
            Benchmark(results, $"{name} SkipList(9000)", 1, () => SkipList(list, 9000).Sum(x => x.A));
            Benchmark(results, $"{name} SkipIterator(1)", 1, () => SkipIterator(list, 1).Sum(x => x.A));
            Benchmark(results, $"{name} SkipIterator(10)", 1, () => SkipIterator(list, 10).Sum(x => x.A));
            Benchmark(results, $"{name} SkipIterator(100)", 1, () => SkipIterator(list, 100).Sum(x => x.A));
            Benchmark(results, $"{name} SkipIterator(1000)", 1, () => SkipIterator(list, 1000).Sum(x => x.A));
            Benchmark(results, $"{name} SkipIterator(9000)", 1, () => SkipIterator(list, 9000).Sum(x => x.A));
        }

        class TestClass
        {
            public int A { get; set; }
        }

        class TestSubClass : TestClass
        {
            public int B { get; set; }
        }

        private static IEnumerable<TSource> SkipList<TSource>(IList<TSource> source, int count)
        {
            if (count < 0)
            {
                count = 0;
            }

            int sourceCount = source.Count;

            while (count < sourceCount)
            {
                yield return source[count++];
            }
        }

        private static IEnumerable<TSource> SkipIterator<TSource>(IEnumerable<TSource> source, int count)
        {
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                while (count > 0 && e.MoveNext()) count--;
                if (count <= 0)
                {
                    while (e.MoveNext()) yield return e.Current;
                }
            }
        }

        sealed class BenchmarkResult
        {
            public string Name { get; set; }
            public double SubLoopsPerSecond { get; set; }

            public static void Write(string filePath, IEnumerable<BenchmarkResult> results)
            {
                using (StreamWriter sw = new StreamWriter(filePath))
                {
                    Write(sw, results);
                }
            }

            public static void Write(TextWriter writer, IEnumerable<BenchmarkResult> results)
            {
                writer.WriteLine("Test\tSub-loops per second");

                foreach (var r in results)
                {
                    writer.WriteLine($"{r.Name}\t{r.SubLoopsPerSecond}");
                }
            }
        }

        static void Benchmark(List<BenchmarkResult> results, string name, int subLoopsPerCall, Action action)
        {
            // warm up and determine the loop count to use.

            Console.WriteLine($"{name} warming up...");

            int loops = 0;

            long endTicks, startTicks = Stopwatch.GetTimestamp();

            do
            {
                ++loops;
                action();
                endTicks = Stopwatch.GetTimestamp();
            }
            while ((endTicks - startTicks) < Stopwatch.Frequency);

            Console.WriteLine($"{name} finished warming up, using {loops:N0} loops.");
            Console.WriteLine($"{name} preliminary speed {LoopsPerSecond(endTicks - startTicks, loops, subLoopsPerCall):N2} sub-loops per second.");
            Console.Write($"{name} benchmarking...");

            // this repeatedly runs loops of action, timing each run of loops, until we get 10 runs with no better timing.

            long minTicks = long.MaxValue;
            int runs = 0;

            while (runs < 10)
            {
                startTicks = Stopwatch.GetTimestamp();
                for (int i = 0; i < loops; ++i)
                {
                    action();
                }
                endTicks = Stopwatch.GetTimestamp();

                long ticks = endTicks - startTicks;

                if (ticks < minTicks)
                {
                    minTicks = ticks;
                    Console.Write("+");
                    runs = 0;
                }
                else
                {
                    Console.Write(".");
                    ++runs;
                }
            }

            double timing = LoopsPerSecond(minTicks, loops, subLoopsPerCall);

            Console.WriteLine();
            Console.WriteLine($"{name} finished benchmarking.");
            Console.WriteLine($"{name} best speed: {timing:N2} sub-loops per second.");

            results.Add(new BenchmarkResult
            {
                Name = name,
                SubLoopsPerSecond = timing
            });
        }

        static double LoopsPerSecond(long ticks, int loops, int subLoops)
        {
            double seconds = ticks / (double)Stopwatch.Frequency;
            return (loops * subLoops) / seconds;
        }
    }
}
