using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Collections;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class PerformanceTests
    {
        [Test]
        [Ignore("Manual testing, no assertions")] 
        public async Task MeasureEnumerationTime()
        {
            var iterations = 1000000;
            var enumerator = new AsyncEnumerator<int>(async yield =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    await yield.ReturnAsync(1);
                }
            });

            var sw = Stopwatch.StartNew();
            int sum = 0;

            while (await enumerator.MoveNextAsync())
                sum += enumerator.Current;

            var time = sw.Elapsed;
            Console.WriteLine($"Time taken: {time},   Sum: {sum}");



            sw = Stopwatch.StartNew();
            sum = 0;

            foreach (var number in EnumerateNumbers())
                sum += number;

            time = sw.Elapsed;
            Console.WriteLine($"Time taken: {time},   Sum: {sum}");



            sw = Stopwatch.StartNew();
            sum = 0;

            int _lock = 0;
            for (int i = 0; i < iterations; i++)
            {
                Interlocked.CompareExchange(ref _lock, 1, 0);
                var tcs = new TaskCompletionSource<int>();
                tcs.TrySetResult(1);
                sum += await tcs.Task;
                //await Task.Yield();
                //await Task.Yield();
                Interlocked.Exchange(ref _lock, 0);
            }

            time = sw.Elapsed;
            Console.WriteLine($"Time taken: {time},   Sum: {sum}");



            sw = Stopwatch.StartNew();
            sum = 0;

            var t = Task.FromResult(1);
            for (int i = 0; i < iterations; i++)
                sum += await t;

            time = sw.Elapsed;
            Console.WriteLine($"Time taken: {time},   Sum: {sum}");
        }

        public IEnumerable<int> EnumerateNumbers()
        {
            for (int i = 0; i < 1000000; i++)
                yield return 1;
        }
    }
}
