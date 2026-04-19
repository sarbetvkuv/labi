using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Lab2.Task1_1;

public static class PrimeCounterLab
{
    private const int RangeStart = 1;
    private const int RangeEnd = 10_000;
    private const int ThreadCount = 4;

    private static readonly object ConsoleLock = new();

    public static void Main()
    {
        Console.WriteLine("=== Lab 2 / Task 1.1 (Prime counter) ===");
        Console.WriteLine($"Range: {RangeStart}..{RangeEnd}, Threads: {ThreadCount}");
        Console.WriteLine();

        RunVersion("Version 1 (Monitor)", () => new MonitorCounterSync());
        RunVersion("Version 2 (Mutex)", () => new MutexCounterSync());
        RunVersion("Version 3 (Semaphore)", () => new SemaphoreCounterSync());
    }

    private static void RunVersion(string title, Func<ICounterSync> syncFactory)
    {
        using ICounterSync counterSync = syncFactory();

        int totalPrimes = 0;
        (int Start, int End)[] ranges = SplitRange(RangeStart, RangeEnd, ThreadCount).ToArray();
        Thread[] threads = new Thread[ThreadCount];

        Stopwatch sw = Stopwatch.StartNew();

        for (int i = 0; i < ThreadCount; i++)
        {
            int workerNumber = i + 1;
            int start = ranges[i].Start;
            int end = ranges[i].End;

            threads[i] = new Thread(() =>
            {
                for (int n = start; n <= end; n++)
                {
                    PrintLine($"[{title}] Thread #{workerNumber} checks: {n}");

                    if (!IsPrime(n))
                    {
                        continue;
                    }

                    counterSync.Increment(ref totalPrimes);
                    PrintLine($"[{title}] Thread #{workerNumber} found prime: {n}");
                }
            });

            threads[i].Start();
        }

        foreach (Thread thread in threads)
        {
            thread.Join();
        }

        sw.Stop();
        Console.WriteLine($"{title} -> Total primes: {totalPrimes}, Elapsed: {sw.ElapsedMilliseconds} ms");
        Console.WriteLine();
    }

    private static IEnumerable<(int Start, int End)> SplitRange(int from, int to, int parts)
    {
        int count = to - from + 1;
        int baseSize = count / parts;
        int remainder = count % parts;

        int current = from;

        for (int i = 0; i < parts; i++)
        {
            int size = baseSize + (i < remainder ? 1 : 0);
            int start = current;
            int end = current + size - 1;
            current = end + 1;

            yield return (start, end);
        }
    }

    private static bool IsPrime(int n)
    {
        if (n < 2)
        {
            return false;
        }

        if (n == 2)
        {
            return true;
        }

        if (n % 2 == 0)
        {
            return false;
        }

        int limit = (int)Math.Sqrt(n);
        for (int d = 3; d <= limit; d += 2)
        {
            if (n % d == 0)
            {
                return false;
            }
        }

        return true;
    }

    private static void PrintLine(string line)
    {
        lock (ConsoleLock)
        {
            Console.WriteLine(line);
        }
    }

    private interface ICounterSync : IDisposable
    {
        void Increment(ref int counter);
    }

    private sealed class MonitorCounterSync : ICounterSync
    {
        private readonly object _lockObj = new();

        public void Increment(ref int counter)
        {
            lock (_lockObj)
            {
                counter++;
            }
        }

        public void Dispose()
        {
        }
    }

    private sealed class MutexCounterSync : ICounterSync
    {
        private readonly Mutex _mutex = new();

        public void Increment(ref int counter)
        {
            _mutex.WaitOne();
            try
            {
                counter++;
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }

        public void Dispose()
        {
            _mutex.Dispose();
        }
    }

    private sealed class SemaphoreCounterSync : ICounterSync
    {
        private readonly Semaphore _semaphore = new(1, 1);

        public void Increment(ref int counter)
        {
            _semaphore.WaitOne();
            try
            {
                counter++;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            _semaphore.Dispose();
        }
    }
}
