﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using SimpleInjector;

namespace Test.It.With.RabbitMQ091.Integration.Tests.TestApplication.Specifications
{
    public class MessageSendingApplication : IApplication
    {
        private SimpleInjectorDependencyResolver _configurer;
        private RabbitMqLogEventListener _rabbitmqLogger;

        public void Configure(Action<SimpleInjectorDependencyResolver> reconfigurer)
        {
            _rabbitmqLogger = new RabbitMqLogEventListener();
            var container = new Container();
            container.RegisterSingleton<IConnectionFactory, ConnectionFactory>();
            container.RegisterSingleton<ISerializer>(() => new NewtonsoftSerializer(Encoding.UTF8));
            container.RegisterSingleton<IMessagePublisherFactory, RabbitMqMessagePublisherFactory>();
            container.RegisterSingleton<IRabbitMqPublisherSettings, DefaultRabbitMqPublisherSettings>();

            _configurer = new SimpleInjectorDependencyResolver(container);
            reconfigurer(_configurer);
            _configurer.Verify();
        }

        public void Start(params string[] args)
        {
            var messagesToPublish = int.Parse(args.First());
            var messagePublisherFactory = _configurer.Resolve<IMessagePublisherFactory>();

            Task.Run(() =>
            {
                Parallel.For(0, messagesToPublish, i =>
                {
                    using (var messagePublisher = messagePublisherFactory.Create("myExchange" + i % 2))
                    {
                        messagePublisher.Publish("myMessage",
                            new TestMessage("Testing sending a message using RabbitMQ"));
                    }
                });
            }).ContinueWith(task =>
            {
                OnUnhandledException?.Invoke(task.Exception);
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public void Stop()
        {
            _configurer.Dispose();
            _rabbitmqLogger.Dispose();
        }

        public event Action<Exception> OnUnhandledException;
    }
}