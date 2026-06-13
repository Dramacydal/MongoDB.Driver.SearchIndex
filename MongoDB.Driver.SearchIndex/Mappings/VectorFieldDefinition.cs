using MongoDB.Bson;

namespace MongoDB.Driver.SearchIndex.Mappings;

public enum KnnVectorSimilarity
{
    Euclidean,
    Cosine,
    DotProduct,
}

public class HnswOptions
{
    public int? MaxEdges { get; init; }
    public int? NumEdgeCandidates { get; init; }

    internal BsonDocument ToBsonDocument()
    {
        var doc = new BsonDocument();
        if (MaxEdges != null)           doc["maxEdges"]           = MaxEdges.Value;
        if (NumEdgeCandidates != null)  doc["numEdgeCandidates"]  = NumEdgeCandidates.Value;
        return doc;
    }

    internal static HnswOptions FromBsonDocument(BsonDocument doc) => new()
    {
        MaxEdges          = doc.Contains("maxEdges")          ? doc["maxEdges"].AsInt32          : null,
        NumEdgeCandidates = doc.Contains("numEdgeCandidates") ? doc["numEdgeCandidates"].AsInt32 : null,
    };
}

public class VectorFieldDefinition : SearchFieldDefinition
{
    public required int NumDimensions { get; init; }
    public required KnnVectorSimilarity Similarity { get; init; }
    public VectorQuantization Quantization { get; init; } = VectorQuantization.None;
    public VectorIndexingMethod IndexingMethod { get; init; } = VectorIndexingMethod.Hnsw;
    public HnswOptions? HnswOptions { get; init; }

    public override BsonDocument ToBsonDocument()
    {
        var doc = new BsonDocument
        {
            { "type", "vector" },
            { "numDimensions", NumDimensions },
            { "similarity", Similarity switch {
                KnnVectorSimilarity.Cosine     => "cosine",
                KnnVectorSimilarity.DotProduct => "dotProduct",
                _                              => "euclidean",
            }},
        };
        if (Quantization != VectorQuantization.None)
            doc["quantization"] = Quantization switch {
                VectorQuantization.Scalar => "scalar",
                VectorQuantization.Binary => "binary",
                _                         => "none",
            };
        if (IndexingMethod == VectorIndexingMethod.Flat)
            doc["indexingMethod"] = "flat";
        if (HnswOptions != null && IndexingMethod == VectorIndexingMethod.Hnsw)
            doc["hnswOptions"] = HnswOptions.ToBsonDocument();
        return doc;
    }
}

/// <remarks>Deprecated. Use <see cref="VectorFieldDefinition"/> instead.</remarks>
public class KnnVectorFieldDefinition : SearchFieldDefinition
{
    public required int Dimensions { get; init; }
    public required KnnVectorSimilarity Similarity { get; init; }

    public override BsonDocument ToBsonDocument() => new()
    {
        { "type", "knnVector" },
        { "dimensions", Dimensions },
        { "similarity", Similarity switch {
            KnnVectorSimilarity.Cosine     => "cosine",
            KnnVectorSimilarity.DotProduct => "dotProduct",
            _                              => "euclidean",
        }},
    };
}
