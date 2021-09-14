using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using FluentAssertions;
using RabbitMQ.Client;
using Test.It.While.Hosting.Your.Service;
using Test.It.With.Amqp;
using Test.It.With.Amqp.Messages;
using Test.It.With.Amqp091.Protocol;
using Test.It.With.RabbitMQ091.Integration.Tests.Common;
using Test.It.With.RabbitMQ091.Integration.Tests.FrameworkExtensions;
using Test.It.With.RabbitMQ091.Integration.Tests.TestApplication.Specifications;
using Test.It.With.RabbitMQ091.Integration.Tests.XUnit;
using Xunit;
using Xunit.Abstractions;

namespace Test.It.With.RabbitMQ091.Integration.Tests
{
    namespace Given_a_client_application_sending_and_receiving_heartbeats_over_rabbitmq
    {
        public class When_sending_and_receiving_heartbeats : XUnitServiceSpecification<DefaultServiceHostStarter<TestApplicationBuilder<HeartbeatApplication>>>
        {
            private readonly ConcurrentBag<HeartbeatFrame<Heartbeat>> _heartbeats = new ConcurrentBag<HeartbeatFrame<Heartbeat>>();

            public When_sending_and_receiving_heartbeats(ITestOutputHelper output) : base(output)
            {
            }

            protected override TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
            protected override TimeSpan StopTimeout { get; set; } = TimeSpan.FromSeconds(16);

            protected override void Given(IServiceContainer container)
            {
                var testFramework = AmqpTestFramework.WithSocket(Test.It.With.Amqp091.Protocol.Amqp091.ProtocolResolver);
                var stopLock = new ExclusiveLock();

                testFramework
                    .WithDefaultProtocolHeaderNegotiation()
                    .WithDefaultSecurityNegotiation(heartbeatInterval: TimeSpan.FromSeconds(1))
                    .WithDefaultConnectionOpenNegotiation()
                    .WithHeartbeats(interval: TimeSpan.FromSeconds(1))
                    .WithDefaultConnectionCloseNegotiation();

                testFramework.On<Heartbeat>((connectionId, frame) =>
                {
                    _heartbeats.Add(frame);
                    DisposeOnTearDown(stopLock.TryAcquire(out var shouldStop));
                    if (shouldStop)
                    {
                        ServiceController.StopAsync().GetAwaiter().GetResult();
                    }
                });

                DisposeAsyncOnTearDown(testFramework.Start());
                DisposeAsyncOnTearDown(testFramework);

                container.RegisterSingleton<IConnectionFactory>(() => new ConnectionFactory
                {
                    HostName = testFramework.Address.ToString(),
                    Port = testFramework.Port
                });
            }

            [Fact]
            public void It_should_have_received_heartbeats()
            {
                _heartbeats.Should().HaveCountGreaterOrEqualTo(1);
            }
        }
    }
}
