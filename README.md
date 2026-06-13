# MongoDB.Driver.SearchIndex

A small .NET library that fills the gap in the official MongoDB C# driver around Atlas Search index management. The driver exposes `IMongoSearchIndexManager` but returns raw `BsonDocument` for everything — this library wraps it with typed classes and a fluent index definition builder.

## Requirements

- .NET 10+
- MongoDB.Driver 3.9.0+

## What it provides

| Feature | Description |
|---------|-------------|
| `SearchIndexDefinition` | Fluent builder for creating Atlas Search index definitions |
| `SearchIndexInfo` | Typed result class for listing indexes (status as enum, definition parsed) |
| `IMongoSearchIndexManager` extensions | `CreateOne`, `CreateOneAsync`, `GetSearchIndexes`, `GetSearchIndexesAsync`, `UpdateOne`, `UpdateOneAsync` |
| `SearchAnalyzer` | Constants for all built-in Lucene analyzers |

## Usage

### Creating an index

```csharp
using MongoDB.Driver.SearchIndex;

var collection = database.GetCollection<BsonDocument>("articles");

collection.SearchIndexes.CreateOne(
    name: "search_index",
    definition: SearchIndexDefinition.Static()
        .StringField("title", analyzer: SearchAnalyzer.English)
        .StringField("body",  analyzer: SearchAnalyzer.English)
        .AutocompleteField("title")
);
```

#### Dynamic mapping

```csharp
collection.SearchIndexes.CreateOne(
    name: "dynamic_index",
    definition: SearchIndexDefinition.Dynamic()
);
```

#### Nested document fields

```csharp
SearchIndexDefinition.Static()
    .DocumentField("address", nested => nested
        .StringField("city")
        .StringField("country"))
    .EmbeddedDocumentsField("reviews", nested => nested
        .StringField("author")
        .StringField("text"))
```

#### Vector field index

```csharp
collection.SearchIndexes.CreateOne(
    name: "vector_index",
    definition: SearchIndexDefinition.Static()
        .VectorField("embedding",
            numDimensions: 1536,
            similarity: KnnVectorSimilarity.Cosine)
);
```

### Listing indexes

```csharp
List<SearchIndexInfo> indexes = collection.SearchIndexes.GetSearchIndexes();

foreach (var index in indexes)
{
    Console.WriteLine($"{index.Name} — {index.Status} (queryable: {index.Queryable})");

    if (index.LatestDefinition is { IsDynamic: false } def)
    {
        foreach (var (path, fields) in def.Fields)
            Console.WriteLine($"  {path}: {string.Join(", ", fields.Select(f => f.GetType().Name))}");
    }
}
```

### Listing a specific index by name

```csharp
var indexes = collection.SearchIndexes.GetSearchIndexes("search_index");
```

### Updating an index

```csharp
await collection.SearchIndexes.UpdateOneAsync(
    name: "search_index",
    definition: SearchIndexDefinition.Static()
        .StringField("title", analyzer: SearchAnalyzer.English)
        .StringField("summary"));
```

### Async variants

```csharp
var indexes = await collection.SearchIndexes.GetSearchIndexesAsync();

await collection.SearchIndexes.CreateOneAsync("search_index", definition);
await collection.SearchIndexes.UpdateOneAsync("search_index", definition);
```

## Field types

### StringFieldDefinition

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Analyzer` | `string?` | — | Analyzer applied at index time (`SearchAnalyzer.*`) |
| `SearchAnalyzer` | `string?` | — | Analyzer applied at query time |
| `IndexOptions` | `SearchIndexOptions?` | — | `Offsets`, `Freqs`, or `Positions` |
| `Store` | `bool?` | — | Store original value for `returnedStoredSource` |
| `Norms` | `SearchFieldNorms?` | — | `Include` or `Omit` field length from scoring |
| `IgnoreAbove` | `int?` | — | Skip strings longer than this many characters |
| `Similarity` | `SearchSimilarityType?` | — | `Bm25`, `Boolean`, or `StableTfl` |
| `Multi` | `Dictionary<string, SearchFieldDefinition>?` | — | Index the same field with multiple analyzers |

### AutocompleteFieldDefinition

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Tokenization` | `AutocompleteTokenization` | `EdgeGram` | `EdgeGram`, `RightEdgeGram`, or `NGram` |
| `MinGrams` | `int` | `2` | Minimum token length |
| `MaxGrams` | `int` | `15` | Maximum token length |
| `FoldDiacritics` | `bool` | `true` | Normalize diacritics (`café` matches `cafe`) |
| `Analyzer` | `string?` | — | Analyzer applied at index time |
| `Similarity` | `SearchSimilarityType?` | — | `Bm25`, `Boolean`, or `StableTfl` |
| `Multi` | `Dictionary<string, SearchFieldDefinition>?` | — | Index the same field with multiple analyzers |

### NumberFieldDefinition

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Representation` | `NumberRepresentation` | `Double` | `Double` or `Int64` |
| `IndexIntegers` | `bool` | `true` | Index int32/int64 values |
| `IndexDoubles` | `bool` | `true` | Index double values |

### VectorFieldDefinition

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `NumDimensions` | `int` | required | Number of vector dimensions (max 8192) |
| `Similarity` | `KnnVectorSimilarity` | required | `Euclidean`, `Cosine`, or `DotProduct` |
| `Quantization` | `VectorQuantization` | `None` | `None`, `Scalar`, or `Binary` |
| `IndexingMethod` | `VectorIndexingMethod` | `Hnsw` | `Hnsw` or `Flat` |
| `HnswOptions` | `HnswOptions?` | — | `MaxEdges` (16–64) and `NumEdgeCandidates` (100–3200); only used when `IndexingMethod` is `Hnsw` |

### GeoFieldDefinition

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `IndexShapes` | `bool` | `false` | `true` to index shapes and points, `false` for points only |

### TokenFieldDefinition

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Normalizer` | `TokenNormalizer` | `None` | `None` or `Lowercase` |

### Simple types (no options)

| Type | Builder method |
|------|---------------|
| `BooleanFieldDefinition` | `.BooleanField(path)` |
| `DateFieldDefinition` | `.DateField(path)` |
| `ObjectIdFieldDefinition` | `.ObjectIdField(path)` |
| `UuidFieldDefinition` | `.UuidField(path)` |
| `StringFacetFieldDefinition` | `.StringFacetField(path)` ⚠️ deprecated, use `TokenField` |
| `DateFacetFieldDefinition` | `.DateFacetField(path)` ⚠️ deprecated, use `DateField` |
| `NumberFacetFieldDefinition` | `.NumberFacetField(path)` ⚠️ deprecated, use `NumberField` |
| `KnnVectorFieldDefinition` | `.KnnVectorField(path, ...)` ⚠️ deprecated, use `VectorField` |

### Composite types

| Type | Builder method | Description |
|------|---------------|-------------|
| `DocumentFieldDefinition` | `.DocumentField(path, nested => ...)` | Nested object fields |
| `EmbeddedDocumentsFieldDefinition` | `.EmbeddedDocumentsField(path, nested => ...)` | Fields in arrays of objects |
| `ArrayFieldDefinition` | `.ArrayField(path, elementType)` | Array of a specific field type |

## Analyzers

`SearchAnalyzer` provides constants for all built-in Lucene analyzers:

```csharp
SearchAnalyzer.Standard    // lucene.standard  (default)
SearchAnalyzer.English     // lucene.english
SearchAnalyzer.Russian     // lucene.russian
SearchAnalyzer.Keyword     // lucene.keyword
SearchAnalyzer.Whitespace  // lucene.whitespace
SearchAnalyzer.Simple      // lucene.simple
// ... and all other language analyzers (Arabic, French, German, Japanese, etc.)
```

## SearchIndexInfo

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Index ID |
| `Name` | `string` | Index name |
| `Type` | `SearchIndexType` | `Search` or `VectorSearch` |
| `Status` | `SearchIndexStatus` | `Pending`, `Building`, `Ready`, `Stale`, `Deleting`, `Failed`, `Unknown` |
| `Queryable` | `bool` | Whether the index is ready to accept queries |
| `LatestVersion` | `int` | Index definition version |
| `LatestDefinition` | `SearchIndexDefinition?` | Parsed index definition |
