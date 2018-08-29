using System;
using System.IO;
using Log.It.With.NLog;
using Test.It.While.Hosting.Your.Windows.Service;
using Xunit;
using Xunit.Abstractions;

namespace Test.It.With.RabbitMQ091.Integration.Tests.XUnit
{
    public abstract class XUnitWindowsServiceSpecification<THostStarter> : WindowsServiceSpecification<THostStarter>,
        IClassFixture<THostStarter>, IDisposable
        where THostStarter : class, IWindowsServiceHostStarter, new()
    {
        private readonly IDisposable _output;
        protected TextWriter Output { get; }

        protected XUnitWindowsServiceSpecification(ITestOutputHelper output)
        {
            _output = With.XUnit.Output.WriteTo(output);
            Output = With.XUnit.Output.Writer;
            NLogCapturingTarget.Subscribe += Output.WriteLine;

            SetConfiguration(new THostStarter());
        }

        public void Dispose()
        {
            NLogCapturingTarget.Subscribe -= Output.WriteLine;
            _output.Dispose();
        }
    }
}