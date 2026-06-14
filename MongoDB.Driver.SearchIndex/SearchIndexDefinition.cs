using MongoDB.Bson;
using MongoDB.Driver.SearchIndex.Mappings;

namespace MongoDB.Driver.SearchIndex;

public class SearchIndexDefinition
{
    private bool _dynamic;
    private readonly Dictionary<string, List<SearchFieldDefinition>> _fields = new();

    public static SearchIndexDefinition Dynamic() => new() { _dynamic = true };
    public static SearchIndexDefinition Static() => new();

    public SearchIndexDefinition StringField(string path,
        string? analyzer = null,
        string? searchAnalyzer = null,
        SearchIndexOptions? indexOptions = null,
        bool? store = null,
        int? ignoreAbove = null,
        SearchSimilarityType? similarity = null,
        SearchFieldNorms? norms = null,
        Dictionary<string, SearchFieldDefinition>? multi = null) =>
        AddField(path, new StringFieldDefinition
        {
            Analyzer       = analyzer,
            SearchAnalyzer = searchAnalyzer,
            IndexOptions   = indexOptions,
            Store          = store,
            IgnoreAbove    = ignoreAbove,
            Similarity     = similarity,
            Norms          = norms,
            Multi          = multi,
        });

    public SearchIndexDefinition ObjectIdField(string path) =>
        AddField(path, new ObjectIdFieldDefinition());

    public SearchIndexDefinition VectorField(string path, int numDimensions, KnnVectorSimilarity similarity,
        VectorQuantization quantization = VectorQuantization.None,
        VectorIndexingMethod indexingMethod = VectorIndexingMethod.Hnsw,
        HnswOptions? hnswOptions = null) =>
        AddField(path, new VectorFieldDefinition
        {
            NumDimensions  = numDimensions,
            Similarity     = similarity,
            Quantization   = quantization,
            IndexingMethod = indexingMethod,
            HnswOptions    = hnswOptions,
        });

    public SearchIndexDefinition UuidField(string path) =>
        AddField(path, new UuidFieldDefinition());

    public SearchIndexDefinition TokenField(string path, TokenNormalizer normalizer = TokenNormalizer.None) =>
        AddField(path, new TokenFieldDefinition { Normalizer = normalizer });

    /// <remarks>Deprecated. Use <see cref="TokenField"/> instead.</remarks>
    public SearchIndexDefinition StringFacetField(string path) =>
        AddField(path, new StringFacetFieldDefinition());

    /// <remarks>Deprecated. Use <see cref="NumberField"/> instead.</remarks>
    public SearchIndexDefinition NumberFacetField(string path,
        NumberRepresentation representation = NumberRepresentation.Double,
        bool indexIntegers = true, bool indexDoubles = true) =>
        AddField(path, new NumberFacetFieldDefinition
        {
            Representation = representation,
            IndexIntegers  = indexIntegers,
            IndexDoubles   = indexDoubles,
        });

    public SearchIndexDefinition NumberField(string path,
        NumberRepresentation representation = NumberRepresentation.Double,
        bool indexIntegers = true, bool indexDoubles = true) =>
        AddField(path, new NumberFieldDefinition
        {
            Representation = representation,
            IndexIntegers  = indexIntegers,
            IndexDoubles   = indexDoubles,
        });

    /// <remarks>Deprecated. Use <see cref="VectorField"/> instead.</remarks>
    public SearchIndexDefinition KnnVectorField(string path, int dimensions, KnnVectorSimilarity similarity) =>
        AddField(path, new KnnVectorFieldDefinition { Dimensions = dimensions, Similarity = similarity });

    public SearchIndexDefinition GeoField(string path, bool indexShapes = false) =>
        AddField(path, new GeoFieldDefinition { IndexShapes = indexShapes });

    public SearchIndexDefinition EmbeddedDocumentsField(string path, Action<SearchIndexDefinition> configure)
    {
        var nested = Static();
        configure(nested);
        return AddField(path, new EmbeddedDocumentsFieldDefinition
        {
            Dynamic = nested.IsDynamic,
            Fields  = nested._fields,
        });
    }

    public SearchIndexDefinition DocumentField(string path, Action<SearchIndexDefinition> configure)
    {
        var nested = Static();
        configure(nested);
        return AddField(path, new DocumentFieldDefinition
        {
            Dynamic = nested.IsDynamic,
            Fields  = nested._fields,
        });
    }

    public SearchIndexDefinition DateField(string path) =>
        AddField(path, new DateFieldDefinition());

    /// <remarks>Deprecated. Use <see cref="DateField"/> instead.</remarks>
    public SearchIndexDefinition DateFacetField(string path) =>
        AddField(path, new DateFacetFieldDefinition());

    public SearchIndexDefinition BooleanField(string path) =>
        AddField(path, new BooleanFieldDefinition());

    public SearchIndexDefinition ArrayField(string path, SearchFieldDefinition elementType) =>
        AddField(path, new ArrayFieldDefinition { ElementType = elementType });

    public SearchIndexDefinition AutocompleteField(string path,
        AutocompleteTokenization tokenization = AutocompleteTokenization.EdgeGram,
        int minGrams = 2, int maxGrams = 15, bool foldDiacritics = true, string? analyzer = null) =>
        AddField(path, new AutocompleteFieldDefinition
        {
            Tokenization   = tokenization,
            MinGrams       = minGrams,
            MaxGrams       = maxGrams,
            FoldDiacritics = foldDiacritics,
            Analyzer       = analyzer,
        });

    private SearchIndexDefinition AddField(string path, SearchFieldDefinition fieldDef)
    {
        if (!_fields.TryGetValue(path, out var list))
            _fields[path] = list = new();
        list.Add(fieldDef);
        return this;
    }

    public BsonDocument ToBsonDocument()
    {
        var mappings = new BsonDocument("dynamic", _dynamic);
        if (!_dynamic && _fields.Count > 0)
        {
            var fields = new BsonDocument();
            foreach (var (path, defs) in _fields)
                fields[path] = new BsonArray(defs.Select(d => d.ToBsonDocument()));
            mappings["fields"] = fields;
        }
        return new BsonDocument("mappings", mappings);
    }

    public static SearchIndexDefinition? Parse(BsonDocument? doc)
    {
        if (doc == null) return null;

        if (!doc.TryGetValue("mappings", out var mappingsValue) || mappingsValue is not BsonDocument mappings)
            return null;

        var definition = mappings.GetValue("dynamic", false).AsBoolean
            ? Dynamic()
            : Static();

        if (!definition._dynamic && mappings.TryGetValue("fields", out var fieldsValue) && fieldsValue is BsonDocument fields)
        {
            foreach (var (name, defs) in SearchFieldDefinition.ParseFields(fields))
                definition._fields[name] = defs;
        }

        return definition;
    }

    public bool IsDynamic => _dynamic;
    public IReadOnlyDictionary<string, IReadOnlyList<SearchFieldDefinition>> Fields =>
        _fields.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<SearchFieldDefinition>)kvp.Value);
}
