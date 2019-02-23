using System;
using System.Collections.Async;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class AsyncEnumeratorTests
    {
        [Test]
        public async Task RaceConditionOnEndOfEnumeration()
        {
            var enumerator = new AsyncEnumerator<int>(async yield =>
            {
                await Task.Run(async () =>
                {
                    await yield.ReturnAsync(1);
                });
            });

            var moveResult1 = await enumerator.MoveNextAsync();
            var moveResult2 = await enumerator.MoveNextAsync();
            var moveResult3 = await enumerator.MoveNextAsync();

            Assert.IsTrue(moveResult1);
            Assert.IsFalse(moveResult2);
            Assert.IsFalse(moveResult3);
        }

        [Test]
        public void CancelEnumeration()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var enumerable = new AsyncEnumerable<int>(async yield =>
            {
                await Task.Yield();
                yield.CancellationToken.ThrowIfCancellationRequested();
            });

            Assert.ThrowsAsync<TaskCanceledException>(() => enumerable.ToListAsync(cts.Token));
        }

        [Test]
        public async Task DisposeAfterPartialEnumeration()
        {
            // ARRANGE

            var testDisposable = new TestDisposable();
            var enumerator = new AsyncEnumerator<int>(async yield =>
            {
                using (testDisposable)
                {
                    await yield.ReturnAsync(1);
                    await yield.ReturnAsync(2);
                    await yield.ReturnAsync(3);
                }
            });

            // ACT

            await enumerator.MoveNextAsync();
            enumerator.Dispose();

            // ASSERT

            Assert.IsTrue(testDisposable.HasDisposed);
        }

        [Test]
        public async Task DisposeByGCAfterPartialEnumeration()
        {
            // ARRANGE

            var testDisposable = new TestDisposable();

            void CreateEnumeratorAndMoveNext()
            {
                var enumerator = new AsyncEnumerator<int>(async yield =>
                {
                    using (testDisposable)
                    {
                        await yield.ReturnAsync(1);
                        await yield.ReturnAsync(2);
                        await yield.ReturnAsync(3);
                    }
                });

                // Do partial enumeration.
                enumerator.MoveNextAsync().GetAwaiter().GetResult();
            }

            // ACT
            CreateEnumeratorAndMoveNext();

            // Instead of calling enumerator.Dispose(), do garbage collection.
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);

            // Give some time to other thread that does the disposal of the enumerator.
            // (see finalizer of the AsyncEnumerator for details)
            await Task.Delay(16);

            // ASSERT

            Assert.IsTrue(testDisposable.HasDisposed);
        }

        [Test]
        public void OnDisposeActionIsCalled()
        {
            // ARRANGE

            var testDisposable = new TestDisposable();

            var enumerator = new AsyncEnumerator<int>(async yield =>
            {
                await yield.ReturnAsync(1);
            },
            onDispose: () => testDisposable.Dispose());

            // ACT

            enumerator.Dispose();

            // ASSERT

            Assert.IsTrue(testDisposable.HasDisposed);
        }

        [Test]
        public void OnDisposeMustBeCalledOnGcWhenEnumerationHasNotBeenStarted()
        {
            // ARRANGE

            var testDisposable = new TestDisposable();

            void CreateEnumerator()
            {
                var enumerator = new AsyncEnumerator<int>(async yield =>
                {
                    await yield.ReturnAsync(1);
                },
                onDispose: () => testDisposable.Dispose());
            }

            // ACT

            CreateEnumerator();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
            Thread.Sleep(16);

            // ASSERT

            Assert.IsTrue(testDisposable.HasDisposed);
        }

        [Test]
        public async Task DisposeWaitsForFinalization()
        {
            var tcs = new TaskCompletionSource<object>();
            var isFinalized = false;

            var enumerable = new AsyncEnumerable<int>(async yield =>
            {
                try
                {
                    await yield.ReturnAsync(1);
                    await yield.ReturnAsync(2);
                }
                finally
                {
                    await tcs.Task;
                    isFinalized = true;
                }
            });

#if NETCOREAPP3_0
            var enumerator = enumerable.GetAsyncEnumerator();
#else
            var enumerator = await enumerable.GetAsyncEnumeratorAsync();
#endif
            await enumerator.MoveNextAsync();

            var disposeTask = enumerator.DisposeAsync();
            await Task.Yield();
            Assert.IsFalse(disposeTask.IsCompleted);

            tcs.SetResult(null);
            await disposeTask;
            Assert.IsTrue(isFinalized);
        }

        private class TestDisposable : IDisposable
        {
            public bool HasDisposed { get; private set; }
            public void Dispose()
            {
                HasDisposed = true;
            }
        }

        [Test]
        public async Task EnumerationMustEndAfterDispose()
        {
            // ARRANGE

            var enumerator = new AsyncEnumerator<int>(async yield =>
            {
                await yield.ReturnAsync(1);
                await yield.ReturnAsync(2);
            });

            await enumerator.MoveNextAsync();

            // ACT

            enumerator.Dispose();
            bool moveNextResult = await enumerator.MoveNextAsync();
            int currentElement = enumerator.Current;

            // ASSERT

            Assert.IsFalse(moveNextResult, "MoveNextAsync must return False after Dispose");
            Assert.AreEqual(1, currentElement, "Current must not change after Dispose");
        }

        [Test]
        public async Task YieldBreak()
        {
            // ARRANGE

            var asyncEnumerationCanceledExceptionRaised = false;

            var enumerator = new AsyncEnumerator<int>(async yield =>
            {
                try
                {
                    yield.Break();
                }
                catch (AsyncEnumerationCanceledException)
                {
                    asyncEnumerationCanceledExceptionRaised = true;
                }

                await yield.ReturnAsync(1);
            });

            // ACT

            var result = await enumerator.MoveNextAsync();

            await Task.Yield();

            // ASSERT

            Assert.IsFalse(result, "MoveNextAsync must return False due to Yield.Break");
            Assert.IsTrue(asyncEnumerationCanceledExceptionRaised, "Yield.Break must throw AsyncEnumerationCanceledException so the enumerator body can perform finalization");
        }
    }
}
