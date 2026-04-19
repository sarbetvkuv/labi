using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Lab2.Task1_2;

public static class DatasetProcessingLab
{
    private const int DatasetCount = 15;
    private const int NumbersPerDataset = 100;
    private const int MinNumber = 1;
    private const int MaxNumber = 100;
    private const int DefaultMaxParallelWorkers = 4;
    private const string DatasetFileName = "Lab2_Task1_2_Datasets.txt";

    public static void Main(string[] args)
    {
        int maxParallelWorkers = DefaultMaxParallelWorkers;
        if (args.Length > 0 && int.TryParse(args[0], out int parsed) && parsed > 0)
        {
            maxParallelWorkers = parsed;
        }

        string datasetFilePath = Path.Combine(Environment.CurrentDirectory, DatasetFileName);

        EnsureDatasetFile(datasetFilePath);
        List<int[]> datasets = LoadDatasets(datasetFilePath);

        Console.WriteLine("=== Lab 2 / Task 1.2 (Datasets processing) ===");
        Console.WriteLine($"Data file: {datasetFilePath}");
        Console.WriteLine($"Datasets: {datasets.Count}, Numbers per set: {NumbersPerDataset}, Max parallel workers: {maxParallelWorkers}");
        Console.WriteLine();

        var results = new List<ResultRecord>();
        object resultLock = new();
        using var totalMutex = new Mutex();
        using var semaphore = new Semaphore(maxParallelWorkers, maxParallelWorkers);

        int grandTotal = 0;
        Thread[] threads = new Thread[datasets.Count];

        Stopwatch sw = Stopwatch.StartNew();

        for (int i = 0; i < datasets.Count; i++)
        {
            int datasetIndex = i;
            threads[i] = new Thread(() =>
            {
                semaphore.WaitOne();
                try
                {
                    int sum = datasets[datasetIndex].Sum();
                    string threadName = Thread.CurrentThread.Name ?? $"Worker-{datasetIndex + 1}";

                    lock (resultLock)
                    {
                        results.Add(new ResultRecord(datasetIndex + 1, sum, threadName));
                    }

                    totalMutex.WaitOne();
                    try
                    {
                        grandTotal += sum;
                    }
                    finally
                    {
                        totalMutex.ReleaseMutex();
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            })
            {
                Name = $"Worker-{i + 1}"
            };

            threads[i].Start();
        }

        foreach (Thread thread in threads)
        {
            thread.Join();
        }

        sw.Stop();

        Console.WriteLine("Per-dataset results:");
        foreach (ResultRecord result in results.OrderBy(r => r.DatasetNumber))
        {
            Console.WriteLine($"Dataset #{result.DatasetNumber}: sum = {result.Sum}, thread = {result.ThreadName}");
        }

        Console.WriteLine();
        Console.WriteLine($"Grand total: {grandTotal}");
        Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds} ms");
    }

    private static void EnsureDatasetFile(string path)
    {
        if (File.Exists(path))
        {
            return;
        }

        var random = new Random();
        using var writer = new StreamWriter(path);

        for (int i = 0; i < DatasetCount; i++)
        {
            int[] numbers = new int[NumbersPerDataset];
            for (int j = 0; j < NumbersPerDataset; j++)
            {
                numbers[j] = random.Next(MinNumber, MaxNumber + 1);
            }

            writer.WriteLine(string.Join(' ', numbers));
        }
    }

    private static List<int[]> LoadDatasets(string path)
    {
        string[] lines = File.ReadAllLines(path);
        if (lines.Length != DatasetCount)
        {
            throw new InvalidOperationException(
                $"Dataset file must contain exactly {DatasetCount} lines, but got {lines.Length}.");
        }

        var result = new List<int[]>(DatasetCount);

        for (int lineNumber = 0; lineNumber < lines.Length; lineNumber++)
        {
            string[] parts = lines[lineNumber]
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length != NumbersPerDataset)
            {
                throw new InvalidOperationException(
                    $"Line {lineNumber + 1} must contain {NumbersPerDataset} numbers, but got {parts.Length}.");
            }

            int[] numbers = parts.Select(int.Parse).ToArray();
            result.Add(numbers);
        }

        return result;
    }

    private sealed record ResultRecord(int DatasetNumber, int Sum, string ThreadName);
}
