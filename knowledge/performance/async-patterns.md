# Async Patterns in .NET

## Core Principle: async all the way down

Never block async code with `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` unless at the top-level entry point.

```csharp
// ❌ BAD — causes deadlocks in ASP.NET Core
var result = someService.GetDataAsync().Result;

// ✅ GOOD
var result = await someService.GetDataAsync();
```

## ConfigureAwait

In library code (not application code), always use `ConfigureAwait(false)` to avoid context capture:

```csharp
// Library code
var data = await repo.GetAsync().ConfigureAwait(false);
```

In ASP.NET Core application code, `ConfigureAwait(false)` is not needed (no SynchronizationContext).

## Cancellation Tokens

Accept and propagate `CancellationToken` through the entire call chain:

```csharp
public async Task<IEnumerable<Product>> GetProductsAsync(CancellationToken ct = default)
{
    return await _context.Products
        .AsNoTracking()
        .ToListAsync(ct); // ← always pass ct
}
```

## IAsyncEnumerable — Streaming Results

Use `IAsyncEnumerable<T>` when streaming large result sets:

```csharp
public async IAsyncEnumerable<Order> StreamOrdersAsync(
    [EnumeratorCancellation] CancellationToken ct = default)
{
    await foreach (var order in _context.Orders.AsAsyncEnumerable().WithCancellation(ct))
        yield return order;
}
```

## ValueTask vs Task

| Use `Task<T>`           | Use `ValueTask<T>`               |
| ----------------------- | -------------------------------- |
| Always async (I/O, DB)  | Usually synchronous (cache hits) |
| Returned multiple times | Awaited exactly once             |
| Stored, chained         | Short-lived operations           |

```csharp
// ValueTask shines for cache-first scenarios
public ValueTask<Product?> GetFromCacheAsync(Guid id)
{
    if (_cache.TryGetValue(id, out var p)) return ValueTask.FromResult(p);
    return new ValueTask<Product?>(LoadFromDbAsync(id));
}
```

## Parallel Execution

```csharp
// Run multiple async operations concurrently
var (users, orders) = await (
    _userRepo.GetAllAsync(ct),
    _orderRepo.GetAllAsync(ct)
).WhenAll(); // Use Task.WhenAll

// Limit parallelism to avoid overwhelming resources
var semaphore = new SemaphoreSlim(4);
await Task.WhenAll(items.Select(async item =>
{
    await semaphore.WaitAsync(ct);
    try { await ProcessAsync(item, ct); }
    finally { semaphore.Release(); }
}));
```

## Common Pitfalls

| Pitfall                                | Fix                                      |
| -------------------------------------- | ---------------------------------------- |
| `async void`                           | Use `async Task` (except event handlers) |
| Fire-and-forget without error handling | Use `_ = Task.Run(...)` with try/catch   |
| `Task.Run` around async code           | Remove `Task.Run` — await directly       |
| Unobserved task exceptions             | Always await or handle                   |
