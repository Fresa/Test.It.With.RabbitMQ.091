using System;

namespace Test.It.With.RabbitMQ091.Integration.Tests.TestApplication
{
    internal interface IMessageConsumerFactory : IDisposable
    {
        IMessageConsumer Consume<TMessage>(string exchange, string queue, string routingKey, Action<TMessage> subscription);
    }
}