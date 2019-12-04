using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using NUnit.Framework.Api;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new DefaultTestAssemblyBuilder();
            var runner = new NUnitTestAssemblyRunner(builder);
            runner.Load(typeof(Program).GetTypeInfo().Assembly, settings: new Dictionary<string, object> { });
            runner.Run(new ConsoleTestListener(), TestFilter.Empty);
            while (runner.IsTestRunning)
                Thread.Sleep(500);
        }

        class ConsoleTestListener : ITestListener
        {
            private TextWriter _output = Console.Out;

            public void TestStarted(ITest test)
            {
            }

            public void TestFinished(ITestResult result)
            {
                var message = $"{result.ResultState.Status.ToString().ToUpperInvariant()} - {result.Test.FullName}";

                if (result.ResultState.Status == TestStatus.Failed)
                    message += Environment.NewLine + "  " + result.Message;

                _output.WriteLine(message);
            }

            public void TestOutput(TestOutput output)
            {
                _output.WriteLine(output.Text);
            }

            public void SendMessage(TestMessage message)
            {
                _output.WriteLine(message.Message);
            }
        }
    }
}
