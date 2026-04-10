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

> [!WARNING]
> **Never double-await a `ValueTask`.** Unlike `Task`, a `ValueTask` may be backed by a pooled object. Awaiting it twice or calling `.Result` after awaiting can lead to race conditions or crashes. If you need to await multiple times, call `.AsTask()`.

## Producer/Consumer with Channels
For high-performance background processing, use `System.Threading.Channels` instead of `BlockingCollection`.

```csharp
var channel = Channel.CreateBounded<string>(100);

// Producer
await channel.Writer.WriteAsync("Job Data");

// Consumer
await foreach (var item in channel.Reader.ReadAllAsync())
{
    Process(item);
}
```

## Async Resource Management
Use `IAsyncDisposable` for types that need to perform async cleanup (e.g., closing a network stream).

```csharp
public async Task ProcessData()
{
    await using var client = new MyAsyncClient();
    await client.DoWorkAsync();
} // client.DisposeAsync() called here
```

## Async Lazy Initialization
Avoid locks by using `Lazy<Task<T>>` or a specialized `AsyncLazy` pattern.

```csharp
private readonly Lazy<Task<Config>> _config = new(async () => 
    await LoadConfigFromApiAsync());

public Task<Config> GetConfigAsync() => _config.Value;
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
| Ignoring `CancellationToken`           | Pass `ct` to all async child methods     |
| Long-running `Task.Run`                | Use `TaskCreationOptions.LongRunning`    |
| Blocking with `.Result`                | `await` all the way up                   |
| Double-awaiting `ValueTask`            | Call `.AsTask()` if multiple awaits needed|
