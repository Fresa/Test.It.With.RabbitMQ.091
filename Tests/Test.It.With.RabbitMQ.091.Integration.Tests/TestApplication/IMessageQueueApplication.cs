﻿namespace Test.It.With.RabbitMQ091.Integration.Tests.TestApplication
{
    public interface IMessageQueueApplication
    {
        void DeclareExchange(string exchange);
        void DeclareQueue(string queue);
        void BindQueueToExchange(string queue, string exchange, string routingkey);
        void Send<TMessage>(TMessage message);
    }
}