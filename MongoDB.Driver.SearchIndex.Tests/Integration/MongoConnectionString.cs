namespace MongoDB.Driver.SearchIndex.Tests.Integration;

internal static class MongoConnectionString
{
    private static readonly string? _value = Load();

    public static string? Value => _value;

    private static string? Load()
    {
        var uri = Environment.GetEnvironmentVariable("MONGO_URI");
        if (!string.IsNullOrWhiteSpace(uri)) return uri;

        var file = Path.Combine(Directory.GetCurrentDirectory(), ".mongo_uri");
        if (!File.Exists(file)) return null;

        var content = File.ReadAllText(file).Trim();
        return string.IsNullOrEmpty(content) ? null : content;
    }
}
