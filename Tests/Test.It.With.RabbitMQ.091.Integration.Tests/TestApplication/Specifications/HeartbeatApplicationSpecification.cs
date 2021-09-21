﻿using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using SimpleInjector;

namespace Test.It.With.RabbitMQ091.Integration.Tests.TestApplication.Specifications
{
    public class HeartbeatApplication : IApplication
    {
        private SimpleInjectorDependencyResolver _configurer;
        private IConnection _connection;
        private RabbitMqLogEventListener _rabbitmqLogger;

        public void Configure(Action<SimpleInjectorDependencyResolver> reconfigurer)
        {
            _rabbitmqLogger = new RabbitMqLogEventListener();
            var container = new Container();
            container.RegisterSingleton<IConnectionFactory, ConnectionFactory>();

            _configurer = new SimpleInjectorDependencyResolver(container);
            reconfigurer(_configurer);
            _configurer.Verify();
        }

        public void Start(params string[] args)
        {
            Task.Run(() =>
            {
                var connectionFactory = _configurer.Resolve<IConnectionFactory>();
                _connection = connectionFactory.CreateConnection();
            }).ContinueWith(task =>
            {
                OnUnhandledException?.Invoke(task.Exception);
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public void Stop()
        {
            try
            {
                _connection.Close(TimeSpan.FromSeconds(1));
            }
            catch 
            {
            }
            //_connection?.Dispose();
            _configurer.Dispose();
            _rabbitmqLogger.Dispose();
        }

        public event Action<Exception> OnUnhandledException;
    }
}