using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using FluentAssertions;
using RabbitMQ.Client;
using Test.It.While.Hosting.Your.Windows.Service;
using Test.It.With.Amqp;
using Test.It.With.Amqp.Messages;
using Test.It.With.Amqp091.Protocol;
using Test.It.With.RabbitMQ091.Integration.Tests.FrameworkExtensions;
using Test.It.With.RabbitMQ091.Integration.Tests.TestApplication.Specifications;
using Test.It.With.RabbitMQ091.Integration.Tests.XUnit;
using Xunit;
using Xunit.Abstractions;

namespace Test.It.With.RabbitMQ091.Integration.Tests
{
    namespace Given_a_client_application_sending_and_receiving_heartbeats_over_rabbitmq
    {
        public class When_sending_and_receiving_heartbeats : XUnitWindowsServiceSpecification<DefaultWindowsServiceHostStarter<TestApplicationBuilder<HeartbeatApplication>>>
        {
            private readonly ConcurrentBag<HeartbeatFrame<Heartbeat>> _heartbeats = new ConcurrentBag<HeartbeatFrame<Heartbeat>>();

            public When_sending_and_receiving_heartbeats(ITestOutputHelper output) : base(output)
            {
            }

            protected override TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

            protected override void Given(IServiceContainer container)
            {
                var testFramework = AmqpTestFramework.WithSocket(Test.It.With.Amqp091.Protocol.Amqp091.ProtocolResolver);

                testFramework
                    .WithDefaultProtocolHeaderNegotiation()
                    .WithDefaultSecurityNegotiation(heartbeatInterval: TimeSpan.FromSeconds(1))
                    .WithDefaultConnectionOpenNegotiation()
                    .WithHeartbeats(interval: TimeSpan.FromSeconds(1))
                    .WithDefaultConnectionCloseNegotiation();

                testFramework.On<Heartbeat>((connectionId, frame) =>
                {
                    _heartbeats.Add(frame);
                    ServiceController.Stop();
                });

                Task.Delay(TimeSpan.FromSeconds(10)).ContinueWith(task =>
                {
                    ServiceController.Stop();
                });

                DisposeAsyncOnTearDown(testFramework.Start());
                DisposeAsyncOnTearDown(testFramework);

                container.RegisterSingleton<IConnectionFactory>(() => new ConnectionFactory
                {
                    HostName = "localhost",
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
