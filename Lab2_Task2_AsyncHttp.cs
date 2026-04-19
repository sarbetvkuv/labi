using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lab2.Task2.Async;

public static class AsyncHttpRequestsProgram
{
    private static readonly string[] ServerUrls =
    {
        "https://jsonplaceholder.typicode.com/todos/1",
        "https://httpbin.org/json",
        "https://api.github.com/repos/dotnet/runtime"
    };

    public static async Task Main()
    {
        Console.WriteLine("=== Lab 2 / Task 2 (Async HTTP version) ===");

        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(20);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Lab2AsyncClient/1.0");

        Stopwatch sw = Stopwatch.StartNew();

        Task<ServerResult>[] tasks = ServerUrls
            .Select((url, index) => FetchAsync(client, url, index + 1))
            .ToArray();

        ServerResult[] results = await Task.WhenAll(tasks);

        ServerResult? failed = results.FirstOrDefault(r => !r.Success);
        if (failed is not null)
        {
            Console.WriteLine($"Request #{failed.RequestNumber} failed: {failed.ErrorMessage}");
            return;
        }

        foreach (ServerResult result in results.OrderBy(r => r.RequestNumber))
        {
            Console.WriteLine($"Response #{result.RequestNumber} ({result.Url}):");
            Console.WriteLine(FormatJsonOrRaw(result.Payload!));
            Console.WriteLine();
        }

        sw.Stop();
        Console.WriteLine($"Total execution time: {sw.ElapsedMilliseconds} ms");
    }

    private static async Task<ServerResult> FetchAsync(HttpClient client, string url, int requestNumber)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using HttpResponseMessage response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return new ServerResult(
                    requestNumber,
                    url,
                    false,
                    null,
                    $"{(int)response.StatusCode} {response.ReasonPhrase}");
            }

            string payload = await response.Content.ReadAsStringAsync();
            return new ServerResult(requestNumber, url, true, payload, null);
        }
        catch (Exception ex)
        {
            return new ServerResult(requestNumber, url, false, null, ex.Message);
        }
    }

    private static string FormatJsonOrRaw(string text)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(text);
            return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return text;
        }
    }

    private sealed record ServerResult(
        int RequestNumber,
        string Url,
        bool Success,
        string? Payload,
        string? ErrorMessage);
}
