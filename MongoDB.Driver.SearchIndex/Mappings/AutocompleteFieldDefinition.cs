using MongoDB.Bson;

namespace MongoDB.Driver.SearchIndex.Mappings;

public enum AutocompleteTokenization
{
    EdgeGram,
    RightEdgeGram,
    NGram,
}

public class AutocompleteFieldDefinition : SearchFieldDefinition
{
    public AutocompleteTokenization Tokenization { get; init; } = AutocompleteTokenization.EdgeGram;
    public int MinGrams { get; init; } = 2;
    public int MaxGrams { get; init; } = 15;
    public bool FoldDiacritics { get; init; } = true;
    public string? Analyzer { get; init; }
    public SearchSimilarityType? Similarity { get; init; }
    public Dictionary<string, SearchFieldDefinition>? Multi { get; init; }

    public override BsonDocument ToBsonDocument()
    {
        var doc = new BsonDocument
        {
            { "type", "autocomplete" },
            { "tokenization", Tokenization switch {
                AutocompleteTokenization.EdgeGram      => "edgeGram",
                AutocompleteTokenization.RightEdgeGram => "rightEdgeGram",
                AutocompleteTokenization.NGram         => "nGram",
                _                                      => "edgeGram"
            }},
            { "minGrams", MinGrams },
            { "maxGrams", MaxGrams },
        };
        if (!FoldDiacritics) doc["foldDiacritics"] = false;
        if (Analyzer != null) doc["analyzer"] = Analyzer;
        var similarity = SerializeSimilarity(Similarity);
        if (similarity != null) doc["similarity"] = similarity;
        var multi = SerializeMulti(Multi);
        if (multi != null) doc["multi"] = multi;
        return doc;
    }
}
