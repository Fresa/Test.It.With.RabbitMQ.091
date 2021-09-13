using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Test.It.While.Hosting.Your.Windows.Service;
using Test.It.With.RabbitMQ091.Integration.Tests.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Test.It.With.RabbitMQ091.Integration.Tests.XUnit
{
    public abstract class XUnitWindowsServiceSpecification<THostStarter> : WindowsServiceSpecification<THostStarter>,
        IClassFixture<THostStarter>, IAsyncLifetime
        where THostStarter : class, IWindowsServiceHostStarter, new()
    {
        private readonly List<IDisposable> _disposables = new();
        private readonly List<IAsyncDisposable> _asyncDisposables = new();

        protected TextWriter Output { get; }

        static XUnitWindowsServiceSpecification()
        {
            LogFactoryExtensions.InitializeOnce();
            NLogBuilderExtensions.ConfigureNLogOnce(new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build());
            NLogCapturingTargetExtensions.RegisterOutputOnce();
        }
        
        protected XUnitWindowsServiceSpecification(ITestOutputHelper output)
        {
            DisposeOnTearDown(With.XUnit.Output.WriteTo(output));
            Output = With.XUnit.Output.Writer;
        }

        protected T DisposeOnTearDown<T>(
            T disposable)
            where T : IDisposable
        {
            _disposables.Add(disposable);
            return disposable;
        }

        protected T DisposeAsyncOnTearDown<T>(
            T disposable)
            where T : IAsyncDisposable
        {
            _asyncDisposables.Add(disposable);
            return disposable;
        }

        public Task InitializeAsync()
        {
            SetConfiguration(new THostStarter());
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            var exceptions = new List<Exception>();
            foreach (var disposable in _disposables)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            foreach (var asyncDisposable in _asyncDisposables)
            {
                try
                {
                    await asyncDisposable.DisposeAsync()
                        .ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            if (exceptions.Any())
            {
                if (exceptions.Count == 1)
                {
                    ExceptionDispatchInfo.Capture(exceptions.First()).Throw();
                }

                throw new AggregateException(exceptions);
            }
        }
    }
}