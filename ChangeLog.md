# ChangeLog

## Session extensibility

This branch adds two session-level extension points for custom indexing and document lifecycle hooks.

### 1. Extra index descriptors

Use `ISession.ExtraIndexDescriptors` when you already have a fixed descriptor list for the current session:

```csharp
await using var session = store.CreateSession();

session.ExtraIndexDescriptors =
[
    new IndexDescriptor
    {
        Filter = entity => entity is Property,
        Map = entity =>
        {
            var property = (Property)entity;
            return Task.FromResult<IEnumerable<IIndex>>(
            [
                new PropertyIndex
                {
                    Name = property.Name,
                    ForRent = property.ForRent,
                    IsOccupied = property.IsOccupied,
                    Location = property.Location
                }
            ]);
        }
    }
];
```

Use `ISession.BuildExtraIndexDescriptors` when descriptors need to be built dynamically per mapped type or collection:

```csharp
await using var session = store.CreateSession();

session.BuildExtraIndexDescriptors = (type, collection) =>
{
    if (type != typeof(Property))
    {
        return Task.FromResult<IEnumerable<IndexDescriptor>>([]);
    }

    return Task.FromResult<IEnumerable<IndexDescriptor>>(
    [
        new IndexDescriptor
        {
            Filter = entity => entity is Property,
            Map = entity =>
            {
                var property = (Property)entity;
                return Task.FromResult<IEnumerable<IIndex>>(
                [
                    new PropertyIndex { Name = property.Name }
                ]);
            }
        }
    ]);
};
```

### 2. Document command hooks

Use `ISession.DocumentCommandHandler` to observe document create, update, and delete commands generated during `SaveChangesAsync()` or batched flushes:

```csharp
public sealed class AuditDocumentCommandHandler : IDocumentCommandHandler
{
    public Task CreatedAsync(DocumentChangeContext context) => Task.CompletedTask;
    public void CreatedInBatch(DocumentChangeInBatchContext context) { }

    public Task UpdatedAsync(DocumentChangeContext context) => Task.CompletedTask;
    public void UpdatedInBatch(DocumentChangeInBatchContext context) { }

    public Task RemovingAsync(DocumentChangeContext context) => Task.CompletedTask;
    public void RemovingInBatch(DocumentChangeInBatchContext context) { }
}

await using var session = store.CreateSession();
session.DocumentCommandHandler = new AuditDocumentCommandHandler();
```

### 3. JZ-EasyOCV3 usage

In `EasyOC.DynamicTypeIndex`, create session-scoped descriptors with the async builder:

```csharp
session.BuildExtraIndexDescriptors = singleTableIndexDescriptor.BuildIndexDescriptorsAsync;
```

Recommended descriptor builder behavior:

- Return `[]` quickly for non-`ContentItem` mapping types.
- Cache descriptors by tenant + mapped type + collection to avoid stale/over-broad cache hits.

Notes:

- `ExtraIndexDescriptors` and `BuildExtraIndexDescriptors` are applied in addition to store-level and session-registered index providers.
- `BuildExtraIndexDescriptors` is invoked per mapped CLR type and collection during session mapping.
- `DocumentCommandHandler` is session-scoped. The default handler is a no-op implementation.
