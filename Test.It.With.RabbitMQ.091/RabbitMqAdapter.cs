﻿using RabbitMQ.Client;
using Test.It.With.Amqp.NetworkClient;

namespace Test.It.With.RabbitMQ091
{
    public static class NetworkClientFactoryExtensions
    {
        public static IConnectionFactory ToRabbitMqConnectionFactory(
            this INetworkClientFactory networkClientFactory)
        {
            return new TestConnectionFactory(networkClientFactory);
        }
    }
}