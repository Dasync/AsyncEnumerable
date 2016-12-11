using System;
using System.Collections.Async;
using System.Threading.Tasks;
using NUnit.Framework;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Tests
{
    [TestFixture]
    public class ForEachAsyncTests
    {
        [Test]
        public void SimpleSyncForEach()
        {
            var enumerable = new AsyncEnumerable<int>(
                async yield =>
                {
                    for (int i = 0; i < 5; i++)
                        await yield.ReturnAsync(i);
                });

            int counter = 0;
            foreach (var number in enumerable)
            {
                Assert.AreEqual(counter, number);
                counter++;
            }
        }

        [Test]
        public async Task SimpleAsyncForEach()
        {
            var enumerable = new AsyncEnumerable<int>(
                async yield =>
                {
                    for (int i = 0; i < 5; i++)
                        await yield.ReturnAsync(i);
                });

            int counter = 0;
            await enumerable.ForEachAsync(
                number =>
                {
                    Assert.AreEqual(counter, number);
                    counter++;
                });
        }

        [Test]
        public async Task RethrowProducerException()
        {
            var enumerable = new AsyncEnumerable<int>(
                async yield =>
                {
                    throw new ArgumentException("test");
                });

            try
            {
                await enumerable.ForEachAsync(
                    number =>
                    {
                        Assert.Fail("must never be called due to the exception");
                    });
            }
            catch (ArgumentException)
            {
                Assert.Pass();
            }

            Assert.Fail("Expected an exception to be thrown");
        }

        [Test]
        public async Task RethrowConsumerException()
        {
            var enumerable = new AsyncEnumerable<int>(
                async yield =>
                {
                    await yield.ReturnAsync(123);
                });

            try
            {
                await enumerable.ForEachAsync(
                    number =>
                    {
                        throw new ArgumentException("test");
                    });
            }
            catch (ArgumentException)
            {
                Assert.Pass();
            }

            Assert.Fail("Expected an exception to be thrown");
        }
    }
}
