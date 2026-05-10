using System.Net.Http.Json;
using Microsoft.Data.Sqlite;
using System.Text.Json;

public class ArticleProcessor
{
    private const string ApiUrl = "https://christ-coding-challenge.test.pub.k8s.christ.de/Article/GetArticles";
    private const string DbPath = "data/articles.db";

    private readonly HttpClient _httpClient = new HttpClient();

    public async Task RunPeriodicUpdatesAsync(CancellationToken token = default)
    {
        try
        {
            await InitializeDatabaseAsync();

            while (!token.IsCancellationRequested)
            {
                var articles = await FetchArticlesAsync();
                if (articles.Count == 0)
                {
                    Console.WriteLine("No data was returned from the API. Retrying in 300 seconds.");
                }
                else
                {
                    var storedCount = await GetStoredArticleCountAsync();
                    if (articles.Count != storedCount)
                    {
                        Console.WriteLine($"Fetched {articles.Count} items from the API (previously {storedCount}). Updating database.");
                        await StoreArticlesAsync(articles);
                        await ComputeAndStoreAggregatesAsync(articles);
                        //await DisplayAggregatesAsync();
                    }
                    else
                    {
                        bool hasChanged = await HasDataChangedAsync(articles);
                        if (hasChanged)
                        {
                            Console.WriteLine($"Fetched {articles.Count} items; attributes have changed. Updating database.");
                            await StoreArticlesAsync(articles);
                            await ComputeAndStoreAggregatesAsync(articles);
                            //await DisplayAggregatesAsync();
                        }
                        else
                        {
                            Console.WriteLine($"Fetched {articles.Count} items; no changes detected. Next update in 300 seconds.");
                        }
                    }
                }

                await Task.Delay(300000, token); // 300 seconds, cancellable
            }
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("Periodic updates stopped.");
        }
        catch (HttpRequestException httpEx)
        {
            Console.WriteLine($"HTTP error: {httpEx.Message}. Retrying in 300 seconds.");
            await Task.Delay(300000, token);
            if (!token.IsCancellationRequested)
                await RunPeriodicUpdatesAsync(token); // Restart on error
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}. Retrying in 300 seconds.");
            await Task.Delay(300000, token);
            if (!token.IsCancellationRequested)
                await RunPeriodicUpdatesAsync(token);
        }
    }

    private async Task InitializeDatabaseAsync()
    {
        using var connection = new SqliteConnection($"Data Source={DbPath}");
        await connection.OpenAsync();

        using var command = connection.CreateCommand();

        // Create tables
        string createArticles = @"
CREATE TABLE IF NOT EXISTS Articles (
    id TEXT PRIMARY KEY,
    articleId TEXT
);";
        string createAttributes = @"
CREATE TABLE IF NOT EXISTS Attributes (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    article_id TEXT,
    attribute_key TEXT,
    source TEXT,
    value TEXT,
    label TEXT,
    language TEXT,
    UNIQUE(article_id, attribute_key, language, source, value)
);";
        string createIndexes = @"
CREATE INDEX IF NOT EXISTS idx_attributes_article_id ON Attributes(article_id);
CREATE INDEX IF NOT EXISTS idx_attributes_key_language ON Attributes(attribute_key, language);
";
        string createAggregates = @"
CREATE TABLE IF NOT EXISTS ArticleAggregates (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    language TEXT,
    mat TEXT,
    mat2 TEXT,
    mat3 TEXT,
    mrk TEXT,
    leg TEXT,
    leg2 TEXT,
    leg3 TEXT,
    ziel TEXT,
    wrg_2 TEXT,
    whg_2 TEXT,
    koll TEXT,
    count INTEGER
);";

        command.CommandText = createArticles;
        await command.ExecuteNonQueryAsync();
        command.CommandText = createAttributes;
        await command.ExecuteNonQueryAsync();
        command.CommandText = createIndexes;
        await command.ExecuteNonQueryAsync();
        command.CommandText = createAggregates;
        await command.ExecuteNonQueryAsync();
    }

    private async Task<bool> IsDatabaseEmptyAsync()
    {
        using var connection = new SqliteConnection($"Data Source={DbPath}");
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Articles";
        var result = await command.ExecuteScalarAsync();
        var count = result as long? ?? 0;
        return count == 0;
    }

    private async Task<long> GetStoredArticleCountAsync()
    {
        using var connection = new SqliteConnection($"Data Source={DbPath}");
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Articles";
        var result = await command.ExecuteScalarAsync();
        return result as long? ?? 0;
    }

    private async Task<bool> HasDataChangedAsync(List<Article> articles)
    {
        using var connection = new SqliteConnection($"Data Source={DbPath}");
        await connection.OpenAsync();

        foreach (var article in articles)
        {
            var storedAttrs = await GetStoredAttributesAsync(connection, article.Id);
            var fetchedAttrs = article.Attributes
                .Where(a => !string.IsNullOrEmpty(a.Key) && !string.IsNullOrEmpty(a.Value))
                .Select(a => (
                    Key: a.Key!, 
                    Source: (string?)(a.Source ?? string.Empty), 
                    Value: a.Value!, 
                    Label: (string?)(a.Label ?? string.Empty), 
                    Language: (string?)(a.Language ?? string.Empty)))
                .Distinct()
                .ToList();
            if (!AreAttributesEqual(storedAttrs, fetchedAttrs))
            {
                Console.WriteLine($"Change detected for article ID {article.Id} (ArticleId: {article.ArticleId}).");
                return true;
            }
        }

        return false;
    }

    private async Task<List<(string Key, string? Source, string Value, string? Label, string? Language)>> GetStoredAttributesAsync(SqliteConnection connection, string articleId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT attribute_key, source, value, label, language FROM Attributes WHERE article_id = @id";
        command.Parameters.AddWithValue("@id", articleId);

        var list = new List<(string Key, string? Source, string Value, string? Label, string? Language)>();

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add((
                reader.GetString(0),
                reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                reader.IsDBNull(4) ? string.Empty : reader.GetString(4)
            ));
        }

        return list;
    }

    private static bool AreAttributesEqual(List<(string Key, string? Source, string Value, string? Label, string? Language)> stored, List<(string Key, string? Source, string Value, string? Label, string? Language)> fetched)
    {
        if (stored.Count != fetched.Count) {
            return false;
        }
        var sortedStored = stored.OrderBy(x => x.Key).ThenBy(x => x.Language ?? "").ThenBy(x => x.Source ?? "").ThenBy(x => x.Value).ToList();
        var sortedFetched = fetched.OrderBy(x => x.Key).ThenBy(x => x.Language ?? "").ThenBy(x => x.Source ?? "").ThenBy(x => x.Value).ToList();

        for (int i = 0; i < sortedStored.Count; i++)
        {
            if (sortedStored[i] != sortedFetched[i])
            {
               return false;
            }
        }

        return true;
    }

    private async Task<List<Article>> FetchArticlesAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<Article>>(ApiUrl) ?? new List<Article>();
    }

    private async Task StoreArticlesAsync(List<Article> articles)
    {
        using var connection = new SqliteConnection($"Data Source={DbPath}");
        await connection.OpenAsync();

        using var command = connection.CreateCommand();

        foreach (var article in articles)
        {
            // Insert article
            command.CommandText = "INSERT OR REPLACE INTO Articles (id, articleId) VALUES (@id, @articleId)";
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@id", article.Id);
            command.Parameters.AddWithValue("@articleId", article.ArticleId);
            await command.ExecuteNonQueryAsync();

            // Insert attributes
            foreach (var attr in article.Attributes)
            {
                if (string.IsNullOrEmpty(attr.Key) || string.IsNullOrEmpty(attr.Value))
                    continue;

                command.CommandText = "INSERT OR REPLACE INTO Attributes (article_id, attribute_key, source, value, label, language) VALUES (@article_id, @attribute_key, @source, @value, @label, @language)";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@article_id", article.Id);
                command.Parameters.AddWithValue("@attribute_key", attr.Key);
                command.Parameters.AddWithValue("@source", attr.Source ?? string.Empty);
                command.Parameters.AddWithValue("@value", attr.Value);
                command.Parameters.AddWithValue("@label", attr.Label ?? string.Empty);
                command.Parameters.AddWithValue("@language", attr.Language ?? string.Empty);
                await command.ExecuteNonQueryAsync();
            }
        }
    }

    private async Task ComputeAndStoreAggregatesAsync(List<Article> articles)
    {
        using var connection = new SqliteConnection($"Data Source={DbPath}");
        await connection.OpenAsync();

        using var command = connection.CreateCommand();

        // Clear old aggregates
        command.CommandText = "DELETE FROM ArticleAggregates";
        await command.ExecuteNonQueryAsync();

        // Get all unique languages
        var languages = articles
            .SelectMany(a => a.Attributes)
            .Where(attr => !string.IsNullOrEmpty(attr.Language))
            .Select(attr => attr.Language!)
            .Distinct()
            .ToList();

        // If no languages, add empty string as default
        if (!languages.Any())
            languages.Add(string.Empty);

        // Compute aggregates per language
        foreach (var language in languages)
        {
            var filteredArticles = articles.Select(article => (
                Article: article,
                Attributes: article.Attributes
            )).ToList();

            var aggregates = filteredArticles
                .Select(item => (
                    Mat: GetAttributeValue(item.Attributes, "MAT", language),
                    Mat2: GetAttributeValue(item.Attributes, "MAT2", language),
                    Mat3: GetAttributeValue(item.Attributes, "MAT3", language),
                    Mrk: GetAttributeValue(item.Attributes, "MRK", language),
                    Leg: GetAttributeValue(item.Attributes, "LEG", language),
                    Leg2: GetAttributeValue(item.Attributes, "LEG2", language),
                    Leg3: GetAttributeValue(item.Attributes, "LEG3", language),
                    Ziel: GetAttributeValue(item.Attributes, "ZIEL", language),
                    Wrg2: GetAttributeValue(item.Attributes, "WRG_2", language),
                    Whg2: GetAttributeValue(item.Attributes, "WHG_2", language),
                    Koll: GetAttributeValue(item.Attributes, "KOLL", language)
                ))
                .GroupBy(value => value)
                .Select(group => new { Key = group.Key, Count = group.Count() })
                .ToList();

            // Insert aggregates for this language
            foreach (var agg in aggregates)
            {
                command.CommandText = "INSERT INTO ArticleAggregates (language, mat, mat2, mat3, mrk, leg, leg2, leg3, ziel, wrg_2, whg_2, koll, count) VALUES (@language, @mat, @mat2, @mat3, @mrk, @leg, @leg2, @leg3, @ziel, @wrg2, @whg2, @koll, @count)";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@language", language);
                command.Parameters.AddWithValue("@mat", agg.Key.Mat ?? string.Empty);
                command.Parameters.AddWithValue("@mat2", agg.Key.Mat2 ?? string.Empty);
                command.Parameters.AddWithValue("@mat3", agg.Key.Mat3 ?? string.Empty);
                command.Parameters.AddWithValue("@mrk", agg.Key.Mrk ?? string.Empty);
                command.Parameters.AddWithValue("@leg", agg.Key.Leg ?? string.Empty);
                command.Parameters.AddWithValue("@leg2", agg.Key.Leg2 ?? string.Empty);
                command.Parameters.AddWithValue("@leg3", agg.Key.Leg3 ?? string.Empty);
                command.Parameters.AddWithValue("@ziel", agg.Key.Ziel ?? string.Empty);
                command.Parameters.AddWithValue("@wrg2", agg.Key.Wrg2 ?? string.Empty);
                command.Parameters.AddWithValue("@whg2", agg.Key.Whg2 ?? string.Empty);
                command.Parameters.AddWithValue("@koll", agg.Key.Koll ?? string.Empty);
                command.Parameters.AddWithValue("@count", agg.Count);
                await command.ExecuteNonQueryAsync();
            }
        }
        Console.WriteLine("Aggregates computed and stored in the database.");
    }

    private static string? GetAttributeValue(List<AttributeItem> attributes, string attributeKey, string language)
    {
        // First, try to find in the specific language
        var value = attributes
            .Where(a => string.Equals(a.Key, attributeKey, StringComparison.OrdinalIgnoreCase))
            .Where(a => !string.IsNullOrEmpty(a.Value))
            .FirstOrDefault(a => (a.Language ?? string.Empty) == language)?.Value;

        if (value != null)
            return value;

        // If not found, fall back to null language
        return attributes
            .Where(a => string.Equals(a.Key, attributeKey, StringComparison.OrdinalIgnoreCase))
            .Where(a => !string.IsNullOrEmpty(a.Value))
            .FirstOrDefault(a => string.IsNullOrEmpty(a.Language))?.Value;
    }

    private async Task DisplayAggregatesAsync()
    {
        using var connection = new SqliteConnection($"Data Source={DbPath}");
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT language, mat, mat2, mat3, mrk, leg, leg2, leg3, ziel, wrg_2, whg_2, koll, count FROM ArticleAggregates ORDER BY language, count DESC";

        var aggregates = new List<(string Language, string? Mat, string? Mat2, string? Mat3, string? Mrk, string? Leg, string? Leg2, string? Leg3, string? Ziel, string? Wrg2, string? Whg2, string? Koll, int Count)>();

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            aggregates.Add((
                reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                reader.IsDBNull(8) ? null : reader.GetString(8),
                reader.IsDBNull(9) ? null : reader.GetString(9),
                reader.IsDBNull(10) ? null : reader.GetString(10),
                reader.IsDBNull(11) ? null : reader.GetString(11),
                reader.GetInt32(12)
            ));
        }

        Console.WriteLine("Article aggregated:");
        foreach (var aggregate in aggregates)
        {
            Console.WriteLine($"Language: {aggregate.Language}, MAT: {aggregate.Mat}, MAT2: {aggregate.Mat2}, MAT3: {aggregate.Mat3}, MRK: {aggregate.Mrk}, LEG: {aggregate.Leg}, LEG2: {aggregate.Leg2}, LEG3: {aggregate.Leg3}, ZIEL: {aggregate.Ziel}, WRG_2: {aggregate.Wrg2}, WHG_2: {aggregate.Whg2}, KOLL: {aggregate.Koll} - Count: {aggregate.Count}");
        }
    }

    public async Task<List<AggregateData>> GetAggregatesAsync(string? language = null)
    {
        using var connection = new SqliteConnection($"Data Source={DbPath}");
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        string query = "SELECT language, mat, mat2, mat3, mrk, leg, leg2, leg3, ziel, wrg_2, whg_2, koll, count FROM ArticleAggregates";
        if (!string.IsNullOrEmpty(language))
        {
            query += " WHERE language = @language";
            command.Parameters.AddWithValue("@language", language);
        }
        query += " ORDER BY language, count DESC";
        command.CommandText = query;

        var aggregates = new List<AggregateData>();

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            aggregates.Add(new AggregateData(
                Language: reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                Mat: reader.IsDBNull(1) ? null : reader.GetString(1),
                Mat2: reader.IsDBNull(2) ? null : reader.GetString(2),
                Mat3: reader.IsDBNull(3) ? null : reader.GetString(3),
                Mrk: reader.IsDBNull(4) ? null : reader.GetString(4),
                Leg: reader.IsDBNull(5) ? null : reader.GetString(5),
                Leg2: reader.IsDBNull(6) ? null : reader.GetString(6),
                Leg3: reader.IsDBNull(7) ? null : reader.GetString(7),
                Ziel: reader.IsDBNull(8) ? null : reader.GetString(8),
                Wrg2: reader.IsDBNull(9) ? null : reader.GetString(9),
                Whg2: reader.IsDBNull(10) ? null : reader.GetString(10),
                Koll: reader.IsDBNull(11) ? null : reader.GetString(11),
                Count: reader.GetInt32(12)
            ));
        }

        return aggregates;
    }

    private static string? GetPreferredAttributeValue(List<AttributeItem> attributes, string attributeKey)
    {
        return attributes
            .Where(a => string.Equals(a.Key, attributeKey, StringComparison.OrdinalIgnoreCase))
            .Where(a => !string.IsNullOrEmpty(a.Value))
            .OrderBy(a => GetLanguagePriority(a.Language))
            .ThenBy(a => a.Language ?? string.Empty)
            .Select(a => a.Value)
            .FirstOrDefault();
    }

    private static int GetLanguagePriority(string? language) => language switch
    {
        "de" => 0,
        null => 1,
        "" => 1,
        _ => 2
    };
}

public sealed record Article(string Id, string ArticleId, List<AttributeItem> Attributes);
public sealed record AttributeItem(string? Key, string? Source, string? Value, string? Label, string? Language);
public sealed record AggregateData(string Language, string? Mat, string? Mat2, string? Mat3, string? Mrk, string? Leg, string? Leg2, string? Leg3, string? Ziel, string? Wrg2, string? Whg2, string? Koll, int Count);
