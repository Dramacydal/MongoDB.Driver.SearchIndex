using Xunit;

namespace MongoDB.Driver.SearchIndex.Tests.Integration;

/// <summary>
/// Skips the test if MONGO_URI environment variable is not set.
/// </summary>
public sealed class RequiresMongoAttribute : FactAttribute
{
    public RequiresMongoAttribute()
    {
        if (MongoConnectionString.Value == null)
            Skip = "Set MONGO_URI env variable or create a .mongo_uri file to run integration tests";
    }
}
