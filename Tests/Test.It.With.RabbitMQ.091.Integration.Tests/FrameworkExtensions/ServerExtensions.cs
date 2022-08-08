using System;
using RabbitMQ.Client;
using Test.It.With.Amqp.NetworkClient;

namespace Test.It.With.RabbitMQ091.Integration.Tests.FrameworkExtensions
{
    internal static class ServerExtensions
    {
        internal static IConnectionFactory ToRabbitMqConnectionFactory(this IServer server)
        {
            return server.ToRabbitMqConnectionFactory(false);
        }

        internal static IConnectionFactory ToRabbitMqConnectionFactory(this IServer server, bool automaticRecovery)
        {
            return new ConnectionFactory
            {
                HostName = server.Address.ToString(),
                Port = server.Port,
                AutomaticRecoveryEnabled = automaticRecovery,
                NetworkRecoveryInterval = TimeSpan.Zero,
                TopologyRecoveryEnabled = true
            };
        }
    }
}