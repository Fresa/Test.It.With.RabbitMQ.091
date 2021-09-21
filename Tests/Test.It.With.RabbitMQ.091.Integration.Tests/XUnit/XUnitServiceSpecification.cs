﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Test.It.While.Hosting.Your.Service;
using Test.It.With.RabbitMQ091.Integration.Tests.Common;
using Test.It.With.RabbitMQ091.Integration.Tests.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Test.It.With.RabbitMQ091.Integration.Tests.XUnit
{
    public abstract class XUnitServiceSpecification<THostStarter> : ServiceSpecification<THostStarter>,
        IClassFixture<THostStarter>, IAsyncLifetime
        where THostStarter : class, IServiceHostStarter, new()
    {
        private readonly List<IAsyncDisposable> _asyncDisposables = new();

        protected TextWriter Output { get; }

        static XUnitServiceSpecification()
        {
            LogFactoryExtensions.InitializeOnce();
            NLogBuilderExtensions.ConfigureNLogOnce(new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build());
            NLogExtensions.RegisterLoggingOnce();
        }
        
        protected XUnitServiceSpecification(ITestOutputHelper output)
        {
            DisposeOnTearDown(With.XUnit.Output.WriteTo(output));
            Output = With.XUnit.Output.Writer;
        }

        protected T DisposeOnTearDown<T>(
            T disposable)
            where T : IDisposable
        {
            _asyncDisposables.Add(new AsyncDisposableAction(() =>
            {
                disposable.Dispose();
                return ValueTask.CompletedTask;
            }));
            return disposable;
        }

        protected T DisposeAsyncOnTearDown<T>(
            T disposable)
            where T : IAsyncDisposable
        {
            _asyncDisposables.Add(disposable);
            return disposable;
        }

        public async Task InitializeAsync()
        {
            try
            {
                await SetConfigurationAsync(new THostStarter())
                    .ConfigureAwait(false);
            }
            catch (Exception initException)
            {
                try
                {
                    await DisposeAsync()
                        .ConfigureAwait(false);
                }
                catch (Exception disposeException)
                {
                    throw new AggregateException(initException, disposeException);
                }
                throw;
            }
        }

        public Task DisposeAsync() => _asyncDisposables.DisposeAllAsync();
    }
}