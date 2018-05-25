using System;

namespace Test.It.With.RabbitMQ091.Integration.Tests.TestApplication
{
    public interface IApplication
    {
        void Configure(Action<SimpleInjectorDependencyResolver> reconfigurer);
        void Start(params string[] args);
        void Stop();
        event Action<Exception> OnUnhandledException;
    }
}