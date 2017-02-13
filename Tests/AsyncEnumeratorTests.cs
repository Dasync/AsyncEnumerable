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

            var enumerator = new AsyncEnumerator<int>(yield =>
            {
                cts.Cancel();
                yield.CancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(0);
            });

            Assert.ThrowsAsync<OperationCanceledException>(() => enumerator.MoveNextAsync(cts.Token));
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

            // Do partial enumeration.
            await enumerator.MoveNextAsync();

            // Instead of calling enumerator.Dispose(), do garbage collection.
            enumerator = null;
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

            var enumerator = new AsyncEnumerator<int>(async yield =>
            {
                await yield.ReturnAsync(1);
            },
            onDispose: () => testDisposable.Dispose());

            // ACT

            enumerator = null;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
            Thread.Sleep(16);

            // ASSERT

            Assert.IsTrue(testDisposable.HasDisposed);
        }

        private class TestDisposable : IDisposable
        {
            public bool HasDisposed { get; private set; }
            public void Dispose()
            {
                HasDisposed = true;
            }
        }
    }
}
