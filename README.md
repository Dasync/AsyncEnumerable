## SUMMARY

Introduces `IAsyncEnumerable`, `IAsyncEnumerator`, `ForEachAsync()`, and `ParallelForEachAsync()`


## PROBLEM SPACE

Helps to (a) create an element provider, where producing an element can take a lot of time
due to dependency on other asynchronous events (e.g. wait handles, network streams), and
(b) a consumer that processes those element as soon as they are ready without blocking
the thread (the processing is scheduled on a worker thread instead).


## EXAMPLE 1 (demonstrates usage only)

```csharp
    using System.Collections.Async;

    static IAsyncEnumerable<int> ProduceAsyncNumbers(int start, int end)
    {
      return new AsyncEnumerable<int>(async yield => {

        // Just to show that ReturnAsync can be used multiple times
        await yield.ReturnAsync(start);

        for (int number = start + 1; number <= end; number++)
          await yield.ReturnAsync(number);

        // You can break the enumeration loop with the following call:
        yield.Break();

        // This won't be executed due to the loop break above
        await yield.ReturnAsync(12345);
      });
    }

    // Just to compare with synchronous version of enumerator
    static IEnumerable<int> ProduceNumbers(int start, int end)
    {
      yield return start;

      for (int number = start + 1; number <= end; number++)
        yield return number;

      yield break;

      yield return 12345;
    }

    static async Task ConsumeNumbersAsync()
    {
      var asyncEnumerableCollection = ProduceAsyncNumbers(start: 1, end: 10);
      await asyncEnumerableCollection.ForEachAsync(async number => {
        await Console.Out.WriteLineAsync($"{number}");
      });
    }

    // Just to compare with synchronous version of enumeration
    static void ConsumeNumbers()
    {
      // NOTE: IAsyncEnumerable is derived from IEnumerable, so you can use either
      var enumerableCollection = ProduceAsyncNumbers(start: 1, end: 10);
      //var enumerableCollection = ProduceNumbers(start: 1, end: 10);

      foreach (var number in enumerableCollection) {
        Console.Out.WriteLine($"{number}");
      }
    }
```


## EXAMPLE 2 (real scenario, pseudo code)

```csharp
    using System.Collections.Async;

    static IAsyncEnumerable<KeyValuePair<string, string>> ReadRemoteSettings(Uri resourceUri)
    {
      return new AsyncEnumerable<KeyValuePair<string, string>>(async yield => {
        using (var client = new HttpClient()) {

          client.BaseAddress = resourceUri;

          using (var request = new HttpRequestMessage(HttpMethod.Get, resourceUri)) {

            using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, yield.CancellationToken)) {

              if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception($"The server returned: {response.ReasonPhrase}");

              using (var stream = await response.Content.ReadAsStreamAsync()) {

                var xmlSettings = new XmlReaderSettings() { IgnoreComments = true, IgnoreWhitespace = true };

                using (var xmlReader = XmlReader.Create(stream, xmlSettings)) {

                  if (!await xmlReader.ReadAsync())
                    yield.Break();

                  if (xmlReader.NodeType == XmlNodeType.XmlDeclaration)
                    await xmlReader.SkipAsync();

                  if (xmlReader.NodeType != XmlNodeType.Element && xmlReader.LocalName != "Settings")
                    yield.Break();

                  while (await xmlReader.ReadAsync()) {
                    if (xmlReader.NodeType == XmlNodeType.Element && !xmlReader.IsEmptyElement) {
                      var settingName = xmlReader.LocalName;
                      var settingValue = xmlReader.Value;
                      await yield.ReturnAsync(new KeyValuePair<string, string>(settingName, settingValue));
                    }
                  }
                }
              }
            }
          }
        }
      });
    }

    static async Task FetchAndPrintSettingsAsync()
    {
      var resourceUri = new Uri("http://localhost:12345/Settings.XML");
      var timeout = TimeSpan.FromSeconds(30);
      var cts = new CancellationTokenSource(timeout);
      var settingCollection = ReadRemoteSettings(resourceUri);

      await settingCollection.ForEachAsync(async setting => {
        await Console.Out.WriteLineAsync($"{setting.Key} = {setting.Value}");
      },
      cancellationToken: cts.Token);
    }
```


## EXAMPLE 3 (async Linq)

```csharp
    IAsyncEnumerable<Bar> ConvertGoodFoosToBars(IAsyncEnumerable<Foo> items)
    {
        return items
          .WhereAsync(foo => foo.IsGood)
          .SelectAsync(foo => Bar.FromFoo(foo));
    }
```


## EXAMPLE 4 (async parallel for-each)

```csharp
    async Task<IReadOnlyCollection<string>> GetStringsAsync(IEnumerable<T> uris, HttpClient httpClient, CancellationToken cancellationToken)
    {
        var result = new ConcurrentBag<string>();
        
        await uris.ParallelForEachAsync(
            async uri =>
            {
                var str = await httpClient.GetStringAsync(uri, cancellationToken);
                result.Add(str);
            },
            maxDegreeOfParallelism: 5,
            cancellationToken);
        
        return result;
    }
```


## WILL THIS MAKE MY APP FASTER?

No and Yes. Just making everything `async` makes your app tiny little bit slower because it
adds overhead in form of state machines and tasks. However, this will help you to better
utilize worker threads in the app because you don't need to block them anymore by waiting
on the next element to be produced - i.e. this will make your app better in general when it
has such multiple enumerations running in parallel. The best fit for `IAsyncEnumerable` is a
case when you read elements from a network stream, like HTTP + XML (as shown above; SOAP),
or a database client implementation where result of a query is a set or rows.


## REFERENCES

GitHub: https://github.com/tyrotoxin/AsyncEnumerable

NuGet.org: https://www.nuget.org/packages/AsyncEnumerator/

License: https://opensource.org/licenses/MIT


## IMPLEMENTATION DETAILS

__1: Using CancellationToken__
   * Do not pass a CancellationToken to a method that returns IAsyncEnumerable, because it is not async, but just a factory
   * Use `yield.CancellationToken` in your enumeration lambda function, which is the same token which gets passed to `IAsyncEnumerator.MoveNextAsync()`

```csharp
    IAsyncEnumerable<int> ProduceNumbers()
    {
      return new AsyncEnumerable<int>(async yield => {

        // This cancellation token is the same token which
        // is passed to very first call of MoveNextAsync().
        var cancellationToken1 = yield.CancellationToken;
        await yield.ReturnAsync(start);

        // This cancellation token can be different, because
        // we are inside second MoveNextAsync() call.
        var cancellationToken2 = yield.CancellationToken;
        await yield.ReturnAsync(start);

        // As a rule of thumb, always use yield.CancellationToken
        // when calling underlying async methods to be able to
        // cancel the MoveNextAsync() method.
        await FooAsync(yield.CancellationToken);
      });
    }
```

__2: Clean-up on incomplete enumeration__

Imagine such situation:

```csharp
    IAsyncEnumerable<int> ReadValuesFromQueue()
    {
      return new AsyncEnumerable<int>(async yield => {

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

The `FirstOrDefaultAsync` method will try to read first value from the `IAsyncEnumerator`, 
and then will just dispose it. However, disposing `AsyncEnumerator` does not mean that the 
`queueClient` in the lambda function will be disposed automatically as well, because async 
methods are just state machines which need somehow to go to a particular state to do the clean-up. 
To provide such behavior, when you dispose an `AsyncEnumerator` before you reach the end of 
enumeration, it will tell to resume your async lambda function (at `await yield.ReturnAsync()`) 
with the `AsyncEnumerationCanceledException` (derives from `OperationCanceledException`). 
Having such exception in your lambda method will break normal flow of enumeration and will go 
to terminal state of the underlying state machine, what will do the clean-up, i.e. dispose 
the `queueClient` in this case. You don't need (and shouldn't) catch that exception type, 
because it's handled internally by `AsyncEnumerator`. The same exception is thrown when 
you call `yield.Break()`.

__3: Why is GetAsyncEnumeratorAsync async?__

The `IAsyncEnumerable.GetAsyncEnumeratorAsync()` method is async and returns a `Task<IAsyncEnumerator>`,
where the current implementation of `AsyncEnumerable` always runs that method synchronously and just
returns an instance of `AsyncEnumerator`. Having interfaces allows you to do your own implementation,
where classes mentioned above are just helpers. The initial idea was to be able to support database-like
scenarios, where `GetAsyncEnumeratorAsync()` executes a query first (what internally returns a pointer),
and the `MoveNextAsync()` enumerates through rows (by using that pointer).

__4: Returning IAsyncEnumerable vs IAsyncEnumerator__

When you implemented a method that returns an async-enumerable collection you have a choice to
return either `IAsyncEnumerable` or `IAsyncEnumerator` - the constructors of the helper classes
`AsyncEnumerable` and `AsyncEnumerator` are absolutely identical. Both interfaces have same set
of useful extension methods, like `ForEachAsync`. In some cases you can have only one immediate
reader of elements, so `IAsyncEnumerator` can be preferable. It's up to your design.

Here are two possibilities:

```csharp
    // A provider/factory of enumerators
    IAsyncEnumerable<int> GetNumberCollectionProvider()
    {
      return new AsyncEnumerable<int>(async yield =>
	  {
	    for (int i = 0;i < 10; i++)
          await yield.ReturnAsync(message.Value);
      });
    }

	// An enumerator
    IAsyncEnumerator<int> GetNumberCollection()
    {
      return new AsyncEnumerator<int>(async yield =>
	  {
	    for (int i = 0;i < 10; i++)
          await yield.ReturnAsync(message.Value);
      });
    }
```