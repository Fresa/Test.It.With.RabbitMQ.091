using System;
using RabbitMQ.Client;

namespace Test.It.With.RabbitMQ091.Integration.Tests.TestApplication
{
    internal class RabbitMqMessagePublisher : IMessagePublisher
    {
        private readonly IModel _model;
        private readonly string _exchange;
        private readonly ISerializer _serializer;

        public RabbitMqMessagePublisher(IModel model, string exchange, ISerializer serializer)
        {
            _model = model;
            _exchange = exchange;
            _serializer = serializer;
        }

        public PublishResult Publish<TMessage>(string key, TMessage message)
        {
            var correlationId = Guid.NewGuid().ToString();
            
            var properties = _model.CreateBasicProperties();
            properties.Type = message.GetType().FullName;
            properties.CorrelationId = correlationId;
            
            _model.BasicPublish(_exchange, key, properties, _serializer.Serialize(message));
            return new PublishResult(_model, correlationId);
        }

        public void Dispose()
        {
            _model.Dispose();
        }
    }
}