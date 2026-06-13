using MongoDB.Bson;

namespace MongoDB.Driver.SearchIndex.Mappings;

public enum SearchSimilarityType
{
    Bm25,
    Boolean,
    StableTfl,
}

public abstract class SearchFieldDefinition
{
    public abstract BsonDocument ToBsonDocument();

    internal static SearchFieldDefinition? FromBsonDocument(BsonDocument doc)
    {
        var type = doc.GetValue("type", "").AsString;
        return type switch
        {
            "string" => new StringFieldDefinition
            {
                Analyzer       = doc.Contains("analyzer")       ? doc["analyzer"].AsString                                                          : null,
                IndexOptions   = doc.Contains("indexOptions")   ? Enum.Parse<SearchIndexOptions>(doc["indexOptions"].AsString, ignoreCase: true)    : null,
                SearchAnalyzer = doc.Contains("searchAnalyzer") ? doc["searchAnalyzer"].AsString                                                    : null,
                Store          = doc.Contains("store")          ? doc["store"].AsBoolean                                                            : null,
                Norms          = doc.Contains("norms")          ? Enum.Parse<SearchFieldNorms>(doc["norms"].AsString, ignoreCase: true)             : null,
                IgnoreAbove    = doc.Contains("ignoreAbove")    ? doc["ignoreAbove"].AsInt32                                                        : null,
                Similarity     = doc.Contains("similarity")     ? ParseSimilarity(doc["similarity"].AsBsonDocument)                                 : null,
                Multi          = doc.Contains("multi")          ? ParseMulti(doc["multi"].AsBsonDocument)                                           : null,
            },
            "autocomplete" => new AutocompleteFieldDefinition
            {
                Tokenization   = Enum.TryParse<AutocompleteTokenization>(doc.GetValue("tokenization", "edgeGram").AsString, ignoreCase: true, out var tok)
                    ? tok : AutocompleteTokenization.EdgeGram,
                MinGrams       = doc.GetValue("minGrams", 2).AsInt32,
                MaxGrams       = doc.GetValue("maxGrams", 15).AsInt32,
                FoldDiacritics = doc.GetValue("foldDiacritics", true).AsBoolean,
                Analyzer       = doc.Contains("analyzer")   ? doc["analyzer"].AsString                                                             : null,
                Similarity     = doc.Contains("similarity") ? ParseSimilarity(doc["similarity"].AsBsonDocument)                                    : null,
                Multi          = doc.Contains("multi")      ? ParseMulti(doc["multi"].AsBsonDocument)                                              : null,
            },
            "boolean"  => new BooleanFieldDefinition(),
            "date"     => new DateFieldDefinition(),
            "dateFacet"=> new DateFacetFieldDefinition(),
            "embeddedDocuments" => new EmbeddedDocumentsFieldDefinition
            {
                Dynamic = doc.GetValue("dynamic", false).AsBoolean,
                Fields  = doc.Contains("fields") ? ParseFields(doc["fields"].AsBsonDocument) : null,
            },
            "objectId" => new ObjectIdFieldDefinition(),
            "vector"   => new VectorFieldDefinition
            {
                NumDimensions  = doc.GetValue("numDimensions", 0).AsInt32,
                Similarity     = doc.GetValue("similarity", "euclidean").AsString switch
                {
                    "cosine"     => KnnVectorSimilarity.Cosine,
                    "dotProduct" => KnnVectorSimilarity.DotProduct,
                    _            => KnnVectorSimilarity.Euclidean,
                },
                Quantization   = doc.GetValue("quantization", "none").AsString switch
                {
                    "scalar" => VectorQuantization.Scalar,
                    "binary" => VectorQuantization.Binary,
                    _        => VectorQuantization.None,
                },
                IndexingMethod = doc.GetValue("indexingMethod", "hnsw").AsString == "flat"
                    ? VectorIndexingMethod.Flat
                    : VectorIndexingMethod.Hnsw,
                HnswOptions    = doc.Contains("hnswOptions")
                    ? HnswOptions.FromBsonDocument(doc["hnswOptions"].AsBsonDocument)
                    : null,
            },
            "token"       => new TokenFieldDefinition
            {
                Normalizer = doc.GetValue("normalizer", "none").AsString == "lowercase"
                    ? TokenNormalizer.Lowercase
                    : TokenNormalizer.None,
            },
            "stringFacet" => new StringFacetFieldDefinition(),
            "uuid"        => new UuidFieldDefinition(),
            "numberFacet" => new NumberFacetFieldDefinition
            {
                Representation = doc.GetValue("representation", "double").AsString == "int64"
                    ? NumberRepresentation.Int64
                    : NumberRepresentation.Double,
                IndexIntegers  = doc.GetValue("indexIntegers", true).AsBoolean,
                IndexDoubles   = doc.GetValue("indexDoubles", true).AsBoolean,
            },
            "number"    => new NumberFieldDefinition
            {
                Representation = doc.GetValue("representation", "double").AsString == "int64"
                    ? NumberRepresentation.Int64
                    : NumberRepresentation.Double,
                IndexIntegers  = doc.GetValue("indexIntegers", true).AsBoolean,
                IndexDoubles   = doc.GetValue("indexDoubles", true).AsBoolean,
            },
            "knnVector" => new KnnVectorFieldDefinition
            {
                Dimensions = doc.GetValue("dimensions", 0).AsInt32,
                Similarity = doc.GetValue("similarity", "euclidean").AsString switch
                {
                    "cosine"     => KnnVectorSimilarity.Cosine,
                    "dotProduct" => KnnVectorSimilarity.DotProduct,
                    _            => KnnVectorSimilarity.Euclidean,
                },
            },
            "geo"      => new GeoFieldDefinition
            {
                IndexShapes = doc.GetValue("indexShapes", false).AsBoolean,
            },
            "document" => new DocumentFieldDefinition
            {
                Dynamic = doc.GetValue("dynamic", false).AsBoolean,
                Fields  = doc.Contains("fields") ? ParseFields(doc["fields"].AsBsonDocument) : null,
            },
            _ => null
        };
    }

    private static SearchSimilarityType? ParseSimilarity(BsonDocument doc)
    {
        return doc.GetValue("type", "").AsString switch
        {
            "bm25"      => SearchSimilarityType.Bm25,
            "boolean"   => SearchSimilarityType.Boolean,
            "stableTfl" => SearchSimilarityType.StableTfl,
            _           => null
        };
    }

    protected static BsonDocument? SerializeSimilarity(SearchSimilarityType? similarity)
    {
        if (similarity == null) return null;
        var value = similarity.Value switch
        {
            SearchSimilarityType.Bm25      => "bm25",
            SearchSimilarityType.Boolean   => "boolean",
            SearchSimilarityType.StableTfl => "stableTfl",
            _                              => "bm25"
        };
        return new BsonDocument("type", value);
    }

    private static Dictionary<string, SearchFieldDefinition> ParseMulti(BsonDocument multiDoc)
    {
        var result = new Dictionary<string, SearchFieldDefinition>();
        foreach (var entry in multiDoc)
        {
            if (entry.Value is BsonDocument entryDoc)
            {
                var parsed = FromBsonDocument(entryDoc);
                if (parsed != null)
                    result[entry.Name] = parsed;
            }
        }
        return result;
    }

    internal static Dictionary<string, List<SearchFieldDefinition>> ParseFields(BsonDocument fieldsDoc)
    {
        var result = new Dictionary<string, List<SearchFieldDefinition>>();
        foreach (var field in fieldsDoc)
        {
            var rawDefs = field.Value switch
            {
                BsonArray arr  => arr.OfType<BsonDocument>(),
                BsonDocument d => [d],
                _              => []
            };
            var parsed = rawDefs
                .Select(FromBsonDocument)
                .OfType<SearchFieldDefinition>()
                .ToList();
            if (parsed.Count > 0)
                result[field.Name] = parsed;
        }
        return result;
    }

    protected static BsonDocument? SerializeMulti(Dictionary<string, SearchFieldDefinition>? multi)
    {
        if (multi == null || multi.Count == 0) return null;
        var doc = new BsonDocument();
        foreach (var (name, def) in multi)
            doc[name] = def.ToBsonDocument();
        return doc;
    }
}
