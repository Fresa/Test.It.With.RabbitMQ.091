using System;
using System.Threading;
using System.Threading.Tasks;
using Test.It.While.Hosting.Your.Service;
using Test.It.With.RabbitMQ091.Integration.Tests.TestApplication;

namespace Test.It.With.RabbitMQ091.Integration.Tests
{
    internal class ApplicationWrapper : IServiceHost
    {
        private readonly IApplication _app;
        private bool _stopping;

        public ApplicationWrapper(IApplication app)
        {
            _app = app;
            app.OnUnhandledException += exception =>
            {
                OnUnhandledException?.Invoke(exception);
            };
        }
        
        public Task<int> StartAsync(CancellationToken cancellationToken = new CancellationToken(), params string[] args)
        {
            _app.Start(args);
            return Task.FromResult(0);
        }

        public Task<int> StopAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            if (_stopping)
            {
                return Task.FromResult(0);
            }
            _stopping = true;
            _app.Stop();
            return Task.FromResult(0);
        }

        public event Action<Exception> OnUnhandledException;
    }
}