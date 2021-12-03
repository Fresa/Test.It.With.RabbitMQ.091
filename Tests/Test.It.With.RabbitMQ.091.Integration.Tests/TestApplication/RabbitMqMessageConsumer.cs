using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Test.It.With.RabbitMQ091.Integration.Tests.TestApplication
{
    internal class RabbitMqMessageConsumer<TMessage> : IMessageConsumer
    {
        private readonly ISerializer _serializer;
        private readonly Action<TMessage> _subscription;
        private readonly EventingBasicConsumer _consumer;
        private readonly string _consumerTag;

        public RabbitMqMessageConsumer(IModel model, ISerializer serializer, string queue, Action<TMessage> subscription)
        {
            _serializer = serializer;
            _subscription = subscription;

            _consumer = new EventingBasicConsumer(model);
            _consumer.Received += OnReceived;
            _consumerTag = _consumer.Model.BasicConsume(queue, false, _consumer);
        }

        private void OnReceived(object sender, BasicDeliverEventArgs args)
        {
            try
            {
                _subscription.Invoke(_serializer.Deserialize<TMessage>(args.Body.ToArray()));
                _consumer.Model.BasicAck(args.DeliveryTag, false);
            }
            catch
            {
                _consumer.Model.BasicNack(args.DeliveryTag, false, true);
            }
        }

        public void Dispose()
        {
            if (_consumer.Model.IsOpen)
            {
                try
                {
                    _consumer.Model.BasicCancelNoWait(_consumerTag);
                }
                catch 
                {
                }
            }
            _consumer.Received -= OnReceived;

            try
            {
                _consumer.Model.Close();
            }
            catch 
            {
            }
            _consumer.Model.Dispose();
        }
    }
}