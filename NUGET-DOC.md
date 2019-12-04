__1: How to use this library?__

See examples above. You can the core code and lots of useful extension methods in the `Dasync.Collections` namespace.


__2: Using CancellationToken__

```csharp
    using Dasync.Collections;
    
    IAsyncEnumerable<int> ProduceNumbers()
    {
      return new AsyncEnumerable<int>(async yield => {

        await FooAsync(yield.CancellationToken);
      });
    }
```

__3: Always remember about ConfigureAwait(false)__

To avoid performance degradation and possible dead-locks in ASP.NET or WPF applications (or any `SynchronizationContext`-dependent environment), you should always put `ConfigureAwait(false)` in your `await` statements:

```csharp
    using Dasync.Collections;
    
    IAsyncEnumerable<int> GetValues()
    {
      return new AsyncEnumerable<int>(async yield =>
      {
        await FooAsync().ConfigureAwait(false);

        // Yes, it's even needed for 'yield.ReturnAsync'
        await yield.ReturnAsync(123).ConfigureAwait(false);
      });
    }
```

__4: Clean-up on incomplete enumeration__

Imagine such situation:

```csharp
    using Dasync.Collections;

    IAsyncEnumerable<int> ReadValuesFromQueue()
    {
      return new AsyncEnumerable<int>(async yield =>
      {
        using (var queueClient = CreateQueueClient())
        {
          while (true)
          {
            var message = queueClient.DequeueMessageAsync();
            if (message == null)
              break;
            
            await yield.ReturnAsync(message.Value);
          }
        }
      });
    }

    Task<int> ReadFirstValueOrDefaultAsync()
    {
      return ReadValuesFromQueue().FirstOrDefaultAsync();
    }
```

The `FirstOrDefaultAsync` method will try to read first value from the `IAsyncEnumerator`, and then will just dispose it. However, disposing `AsyncEnumerator` does not mean that the `queueClient` in the lambda function will be disposed automatically as well, because async methods are just state machines which need somehow to go to a particular state to do the clean-up. 
To provide such behavior, when you dispose an `AsyncEnumerator` before you reach the end of enumeration, it will tell to resume your async lambda function (at `await yield.ReturnAsync()`) with the `AsyncEnumerationCanceledException` (derives from `OperationCanceledException`). Having such exception in your lambda method will break normal flow of enumeration and will go to terminal state of the underlying state machine, what will do the clean-up, i.e. dispose the `queueClient` in this case. You don't need (and shouldn't) catch that exception type, because it's handled internally by `AsyncEnumerator`. The same exception is thrown when you call `yield.Break()`.

There is another option to do the cleanup on `Dispose`:

```csharp
    using Dasync.Collections;

    IAsyncEnumerator<int> GetQueueEnumerator()
    {
      var queueClient = CreateQueueClient();

      return new AsyncEnumerable<int>(async yield =>
      {
        while (true)
        {
          var message = queueClient.DequeueMessageAsync();
          if (message == null)
            break;
            
          await yield.ReturnAsync(message.Value);
        }
      },
      onDispose: () => queueClient.Dispose());
    }
```

__5: Why is GetAsyncEnumeratorAsync async?__

The `IAsyncEnumerable.GetAsyncEnumeratorAsync()` method is async and returns a `Task<IAsyncEnumerator>`, where the current implementation of `AsyncEnumerable` always runs that method synchronously and just returns an instance of `AsyncEnumerator`. Having interfaces allows you to do your own implementation, where classes mentioned above are just helpers. The initial idea was to be able to support database-like scenarios, where `GetAsyncEnumeratorAsync()` executes a query first (what internally returns a pointer), and the `MoveNextAsync()` enumerates through rows (by using that pointer).

__6: Returning IAsyncEnumerable vs IAsyncEnumerator__

When you implement a method that returns an async-enumerable collection you have a choice to return either `IAsyncEnumerable` or `IAsyncEnumerator` - the constructors of the helper classes `AsyncEnumerable` and `AsyncEnumerator` are absolutely identical. Both interfaces have same set of useful extension methods, like `ForEachAsync`.

When you create an 'enumerable', you create a factory that produces 'enumerators', i.e. you can enumerate through a collection many times. On the other hand, creating an 'enumerator' is needed when you can through a collection only once.

__7: Where is Reset or ResetAsync?__

The `Reset` method must not be on the `IEnumerator` interface, and should be considered as deprecated. Create a new enumerator instead. This is the reason why the 'oneTimeUse' flag was removed in version 2 of this library.

__8: How can I do synchronous for-each enumeration through IAsyncEnumerable?__

You can use extension methods like `IAsyncEnumerable.ToEnumerable()` to use built-in `foreach` enumeration, BUT you should never do that! The general idea of this library is to avoid thread-blocking calls on worker threads, where converting an `IAsyncEnumerable` to `IEnumerable` will just defeat the whole purpose of this library. This is the reason why such synchronous extension methods are marked with `[Obsolete]` attribute.

__9: What's the difference between ForEachAsync and ParallelForEachAsync?__

The `ForEachAsync` allows you to go through a collection and perform an action on every single item in sequential manner. On the other hand, `ParallelForEachAsync` allows you to run the action on multiple items at the same time where the sequential 
order of completion is not guaranteed. For the latter, the degree of the parallelism is controlled by the `maxDegreeOfParalellism` 
argument, however it does not guarantee to spin up the exact amount of threads, because it depends on the [thread pool size](https://msdn.microsoft.com/en-us/library/system.threading.threadpool.setmaxthreads(v=vs.110).aspx) and its occupancy at a moment of time. Such parallel approach is much better than trying to create a task for an action for every single item on the collection and then awaiting on all of them with `Task.WhenAll`, because it adds less overhead to the runtime, better with memory usage, and helps with throttling-sensitive scenarios.