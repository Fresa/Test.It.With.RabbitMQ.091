using System;

namespace Test.It.With.RabbitMQ091.Integration.Tests.TestApplication
{
    internal interface IMessagePublisherFactory : IDisposable
    {
        IMessagePublisher Create(string exchange);
    }
}