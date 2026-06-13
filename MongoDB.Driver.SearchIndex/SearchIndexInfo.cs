namespace MongoDB.Driver.SearchIndex;

public enum SearchIndexStatus
{
    Unknown,
    Pending,
    Building,
    Ready,
    Stale,
    Deleting,
    Failed,
}

public class SearchIndexInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public SearchIndexType Type { get; set; }
    public SearchIndexStatus Status { get; set; }
    public bool Queryable { get; set; }
    public int LatestVersion { get; set; }
    public SearchIndexDefinition? LatestDefinition { get; set; }
}
