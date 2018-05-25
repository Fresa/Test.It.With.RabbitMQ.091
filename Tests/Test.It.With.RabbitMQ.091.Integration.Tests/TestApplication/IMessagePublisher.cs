using System;

namespace Test.It.With.RabbitMQ091.Integration.Tests.TestApplication
{
    internal interface IMessagePublisher : IDisposable
    {
        PublishResult Publish<TMessage>(string key, TMessage message);
    }
}