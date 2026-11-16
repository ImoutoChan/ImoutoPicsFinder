#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace ImoutoPicsFinder.Danbooru;

/// <summary>
/// Reverse image search against danbooru's own iqdb instance: https://danbooru.donmai.us/iqdb_queries
/// It only knows about danbooru posts, but its index is fresher than the public iqdb.org one.
/// </summary>
public class DanbooruIqdbClient : IDanbooruIqdbClient
{
    private const string SearchUrl = "https://danbooru.donmai.us/iqdb_queries.json";

    // danbooru rescales raw iqdb scores so that everything above 50 counts as a potential match
    private const double MinimumScore = 50;

    private const int Limit = 10;

    // cloudflare blocks browser-like user agents on donmai.us, but lets descriptive bot ones through
    private const string UserAgent = "ImoutoPicsFinder/1.0 (+https://github.com/ImoutoChan/ImoutoPicsFinder)";

    private readonly string _login;
    private readonly string _apiKey;

    public DanbooruIqdbClient(string login, string apiKey)
    {
        _login = login;
        _apiKey = apiKey;
    }

    /// <summary>
    /// Returns null when the credentials aren't configured. They are mandatory: danbooru rejects
    /// anonymous file uploads with a csrf error, and only api key auth skips that check.
    /// </summary>
    public static DanbooruIqdbClient? CreateFromEnvironment()
    {
        var login = Environment.GetEnvironmentVariable("DanbooruLogin");
        var apiKey = Environment.GetEnvironmentVariable("DanbooruApiKey");

        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(apiKey))
            return null;

        return new DanbooruIqdbClient(login, apiKey);
    }

    public async Task<IReadOnlyCollection<DanbooruIqdbMatch>> SearchFileAsync(Stream file)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(120);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

        var fileContent = new StreamContent(file);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

        using var content = new MultipartFormDataContent();
        content.Add(fileContent, "search[file]", "image.jpg");
        // a bare limit in the query string is ignored on post, it has to go into the search params
        content.Add(new StringContent(Limit.ToString()), "search[limit]");

        // credentials, on the other hand, are only ever looked up in the query string
        var url = SearchUrl
                  + $"?login={Uri.EscapeDataString(_login)}"
                  + $"&api_key={Uri.EscapeDataString(_apiKey)}";

        using var response = await client.PostAsync(url, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"danbooru iqdb returned {(int)response.StatusCode}: {responseBody}");

        var matches = JsonSerializer.Deserialize<IReadOnlyList<DanbooruIqdbMatch>>(responseBody)
                      ?? new List<DanbooruIqdbMatch>();

        return matches
            .Where(x => x.Post != null && x.Score > MinimumScore)
            .OrderByDescending(x => x.Score)
            .ToList();
    }
}
