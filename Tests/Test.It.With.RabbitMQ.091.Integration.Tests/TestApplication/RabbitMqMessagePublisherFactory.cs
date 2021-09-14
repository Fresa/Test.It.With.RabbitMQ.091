using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace Test.It.With.RabbitMQ091.Integration.Tests.TestApplication
{
    internal class RabbitMqMessagePublisherFactory : IMessagePublisherFactory
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ISerializer _serializer;
        private readonly IRabbitMqPublisherSettings _settings;
        private readonly ConcurrentDictionary<string, IConnection> _connections = new ConcurrentDictionary<string, IConnection>();

        public RabbitMqMessagePublisherFactory(IConnectionFactory connectionFactory, ISerializer serializer, IRabbitMqPublisherSettings settings)
        {
            _connectionFactory = connectionFactory;
            _serializer = serializer;
            _settings = settings;
        }

        public IMessagePublisher Create(string exchange)
        {
            var connection = _connections.GetOrAdd(exchange, ex => _connectionFactory.CreateConnection());
            var model = connection.CreateModel();
            model.ExchangeDeclare(exchange, "topic");

            ApplySettings(model);

            return new RabbitMqMessagePublisher(model, exchange, _serializer);
        }

        private void ApplySettings(IModel model)
        {
            if (_settings.EnablePublisherConfirms)
            {
                model.ConfirmSelect();
            }
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
                connection.Dispose();
            });
        }
    }
}