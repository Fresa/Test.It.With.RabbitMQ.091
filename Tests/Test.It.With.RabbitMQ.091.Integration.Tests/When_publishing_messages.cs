using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using FluentAssertions;
using Log.It;
using RabbitMQ.Client;
using Test.It.While.Hosting.Your.Service;
using Test.It.With.Amqp;
using Test.It.With.Amqp.Messages;
using Test.It.With.Amqp091.Protocol;
using Test.It.With.RabbitMQ091.Integration.Tests.Assertion;
using Test.It.With.RabbitMQ091.Integration.Tests.Common;
using Test.It.With.RabbitMQ091.Integration.Tests.FrameworkExtensions;
using Test.It.With.RabbitMQ091.Integration.Tests.TestApplication;
using Test.It.With.RabbitMQ091.Integration.Tests.TestApplication.Specifications;
using Test.It.With.RabbitMQ091.Integration.Tests.XUnit;
using Xunit;
using Xunit.Abstractions;
using Basic = Test.It.With.Amqp091.Protocol.Basic;

namespace Test.It.With.RabbitMQ091.Integration.Tests
{
    namespace Given_a_client_application_sending_messages_over_rabbitmq
    {
        public class When_publishing_a_message : XUnitServiceSpecification<DefaultServiceHostStarter<TestApplicationBuilder<MessageSendingApplication>>>
        {
            private readonly ConcurrentBag<MethodFrame<Exchange.Declare>> _exchangeDeclare =
                new ConcurrentBag<MethodFrame<Exchange.Declare>>();

            private readonly ConcurrentBag<MethodFrame<Basic.Publish>> _basicPublish =
                new ConcurrentBag<MethodFrame<Basic.Publish>>();

            public When_publishing_a_message(ITestOutputHelper output) : base(output)
            {
            }

            private const int NumberOfPublishes = 4;

            protected override string[] StartParameters { get; } = { NumberOfPublishes.ToString() };

            protected override TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
            protected override TimeSpan StopTimeout { get; set; } = TimeSpan.FromSeconds(10);

            protected override void Given(IServiceContainer container)
            {
                var closedChannels = new ConcurrentBag<short>();

                var testServer = AmqpTestFramework.WithSocket(Amqp091.Protocol.Amqp091.ProtocolResolver);
                testServer
                    .WithDefaultProtocolHeaderNegotiation()
                    .WithDefaultSecurityNegotiation(heartbeatInterval: TimeSpan.FromSeconds(5))
                    .WithDefaultConnectionOpenNegotiation()
                    .WithHeartbeats(interval: TimeSpan.FromSeconds(5))
                    .WithDefaultConnectionCloseNegotiation();

                var connections = new List<ConnectionId>();
                testServer.On<Connection.Open>((id, frame) =>
                {
                    connections.Add(id);
                });
                testServer.On<Connection.Close>((id, frame) =>
                {
                    connections.Remove(id);
                });

                testServer.On<Channel.Open, Channel.OpenOk>((connectionId, frame) => new Channel.OpenOk());
                testServer.On<Channel.Close, Channel.CloseOk>((connectionId, frame) => new Channel.CloseOk());
                testServer.On<Exchange.Declare, Exchange.DeclareOk>((connectionId, frame) =>
                {
                    _exchangeDeclare.Add(frame);
                    return new Exchange.DeclareOk();
                });
                testServer.On<Channel.Close>((id, frame) =>
                {
                    closedChannels.Add(frame.Channel);
                    TryStop();
                });
                testServer.On<Basic.Cancel, Basic.CancelOk>((id, frame) => new Basic.CancelOk());
                testServer.On<Basic.Publish>((connectionId, frame) =>
                {
                    _basicPublish.Add(frame);
                    TryStop();
                });

                var server = testServer.Start();
                DisposeAsyncOnTearDown(new AsyncDisposableAction(async () =>
                {
                    try
                    {
                        await server.DisposeAsync();
                    }
                    catch (Exception e)
                    {
                        LogFactory.Create<SocketAmqpTestFramework>().Error(e, "Error when stopping socket server");
                    }
                })); 
                DisposeAsyncOnTearDown(testServer);

                container.RegisterSingleton(testServer.ToRabbitMqConnectionFactory);

                void TryStop()
                {
                    if (_basicPublish.Count == NumberOfPublishes)
                    {
                        var logger = LogFactory.Create<SocketAmqpTestFramework>();
                        foreach (var connection in connections)
                        {
                            logger.Info($"Closing connection {connection}");
                            testServer.Send(connection, new MethodFrame<Connection.Close>(0, new Connection.Close()));
                        }
                        ServiceController.StopAsync();
                    }
                }
            }

            [Fact]
            public void It_should_have_published_messages()
            {
                _basicPublish.Should().HaveCount(4);
            }

            [Fact]
            public void It_should_have_published_the_correct_message_type()
            {
                _basicPublish.Should().Contain(frame =>
                    frame.Message.ContentHeader.Type.Equals(Shortstr.From(typeof(TestMessage).FullName)));
            }

            [Fact]
            public void It_should_have_published_the_correct_message()
            {
                _basicPublish.Should().Contain(frame =>
                    frame.Message.ContentBody.Deserialize<TestMessage>().Message
                        .Equals("Testing sending a message using RabbitMQ"));
            }

            [Fact]
            public void It_should_have_published_the_message_on_the_correct_exchange()
            {
                _basicPublish.Should().Contain(frame => frame.Message.Exchange.Equals(ExchangeName.From("myExchange0")));
                _basicPublish.Should().Contain(frame => frame.Message.Exchange.Equals(ExchangeName.From("myExchange1")));
            }

            [Fact]
            public void It_should_have_declared_exchanges()
            {
                _exchangeDeclare.Should().HaveCount(4);
            }

            [Fact]
            public void It_should_have_declared_an_exchange_with_name()
            {
                _exchangeDeclare.Should().Contain(frame => frame.Message.Exchange.Equals(ExchangeName.From("myExchange0")));
                _exchangeDeclare.Should().Contain(frame => frame.Message.Exchange.Equals(ExchangeName.From("myExchange1")));
            }

            [Fact]
            public void It_should_have_declared_an_exchange_with_type()
            {
                _exchangeDeclare.Should().Contain(frame => frame.Message.Type.Equals(Shortstr.From("topic")));
            }
        }
    }
}
