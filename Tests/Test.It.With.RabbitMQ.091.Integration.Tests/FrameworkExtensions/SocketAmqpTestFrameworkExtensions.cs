using RabbitMQ.Client;
using Test.It.With.Amqp;

namespace Test.It.With.RabbitMQ091.Integration.Tests.FrameworkExtensions
{
    internal static class SocketAmqpTestFrameworkExtensions
    {
        internal static IConnectionFactory ToRabbitMqConnectionFactory(this SocketAmqpTestFramework testFramework)
        {
            return new ConnectionFactory
            {
                HostName = testFramework.Address.ToString(),
                Port = testFramework.Port
            };
        }
    }
}