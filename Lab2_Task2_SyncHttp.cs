using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

namespace Lab2.Task2.Sync;

public static class SyncHttpRequestsProgram
{
    private static readonly string[] ServerUrls =
    {
        "https://jsonplaceholder.typicode.com/todos/1",
        "https://httpbin.org/json",
        "https://api.github.com/repos/dotnet/runtime"
    };

    public static void Main()
    {
        Console.WriteLine("=== Lab 2 / Task 2 (Sync HTTP version) ===");

        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(20);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Lab2SyncClient/1.0");

        Stopwatch sw = Stopwatch.StartNew();

        for (int i = 0; i < ServerUrls.Length; i++)
        {
            string url = ServerUrls[i];

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using HttpResponseMessage response = client.Send(request);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Request #{i + 1} failed: {(int)response.StatusCode} {response.ReasonPhrase}. URL: {url}");
                    return;
                }

                string responseText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Console.WriteLine($"Response #{i + 1} ({url}):");
                Console.WriteLine(FormatJsonOrRaw(responseText));
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Request #{i + 1} failed with exception: {ex.Message}. URL: {url}");
                return;
            }
        }

        sw.Stop();
        Console.WriteLine($"Total execution time: {sw.ElapsedMilliseconds} ms");
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
}
