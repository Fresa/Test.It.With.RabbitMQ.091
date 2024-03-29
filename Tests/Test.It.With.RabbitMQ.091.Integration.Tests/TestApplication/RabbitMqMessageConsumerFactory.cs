﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace Test.It.With.RabbitMQ091.Integration.Tests.TestApplication
{
    internal class RabbitMqMessageConsumerFactory : IMessageConsumerFactory
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ISerializer _serializer;
        private readonly ConcurrentDictionary<string, IConnection> _connections = new ConcurrentDictionary<string, IConnection>();

        public RabbitMqMessageConsumerFactory(IConnectionFactory connectionFactory, ISerializer serializer)
        {
            _connectionFactory = connectionFactory;
            _serializer = serializer;
        }

        public IMessageConsumer Consume<TMessage>(string exchange, string queue, string routingKey, Action<TMessage> subscription)
        {
            var connection = _connections.GetOrAdd(exchange, ex => _connectionFactory.CreateConnection());
            var model = connection.CreateModel();
            model.ExchangeDeclare(exchange, "topic");
            model.QueueDeclare(queue, false, false, false, new Dictionary<string, object>());
            model.QueueBind(queue, exchange, routingKey);
            return new RabbitMqMessageConsumer<TMessage>(model, _serializer, queue, subscription);
        }

        public void Dispose()
        {
            Parallel.ForEach(_connections.Values, connection =>
            {
                try
                {
                    connection.Close(TimeSpan.FromSeconds(1));
                }
                catch 
                {
                }
                // todo: disposing the connection times out sometimes because the connection implementation seems buggy and does not shutdown the background worker every time
                //connection.Dispose();
            });
        }
    }
}