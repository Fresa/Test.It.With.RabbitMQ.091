﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
using Test.It.With.RabbitMQ091.Methods;
using Xunit;
using Xunit.Abstractions;
using Basic = Test.It.With.RabbitMQ091.Methods.Basic;

namespace Test.It.With.RabbitMQ091.Integration.Tests
{
    namespace Given_a_client_application_sending_messages_over_rabbitmq
    {
        public class When_publishing_a_message_and_waiting_for_confirm : XUnitServiceSpecification<DefaultServiceHostStarter<TestApplicationBuilder<ConfirmSelectApplication>>>
        {
            private readonly ConcurrentBag<MethodFrame<Exchange.Declare>> _exchangeDeclare =
                new ConcurrentBag<MethodFrame<Exchange.Declare>>();

            private readonly ConcurrentBag<MethodFrame<Basic.Publish>> _basicPublish =
                new ConcurrentBag<MethodFrame<Basic.Publish>>();

            private bool _selectOkSent;

            public When_publishing_a_message_and_waiting_for_confirm(ITestOutputHelper output) : base(output)
            {
            }

            protected override TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
            protected override TimeSpan StopTimeout { get; set; } = TimeSpan.FromSeconds(10);

            protected override void Given(IServiceContainer container)
            {
                var channelClosed = false;

                var testServer = AmqpTestFramework.WithSocket(RabbitMq091.ProtocolResolver);
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
                    channelClosed = true;
                    TryStop();
                });
                testServer.On<Basic.Publish>((connectionId, frame) =>
                {
                    _basicPublish.Add(frame);
                    testServer.Send(connectionId,
                        new MethodFrame<Basic.Ack>(frame.Channel,
                            new Basic.Ack
                            {
                                DeliveryTag =
                                    DeliveryTag.From(_basicPublish.Count(methodFrame =>
                                        methodFrame.Channel == frame.Channel))
                            }));
                });
                testServer.On<Confirm.Select, Confirm.SelectOk>((connectionId, frame) =>
                {
                    _selectOkSent = true;
                    return new Confirm.SelectOk();
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

                container.RegisterSingleton(server.ToRabbitMqConnectionFactory);

                void TryStop()
                {
                    if (channelClosed && _basicPublish.Count == 2)
                    {
                        foreach (var connection in connections)
                        {
                            testServer.Send(connection, new MethodFrame<Connection.Close>(0, new Connection.Close()));
                        }
                        ServiceController.StopAsync();
                    }
                }
            }

            [Fact]
            public void It_should_have_published_messages()
            {
                _basicPublish.Should().HaveCount(2);
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
            public void It_should_have_published_a_confirming_message()
            {
                _basicPublish.Should().Contain(frame =>
                    frame.Message.ContentBody.Deserialize<TestMessage>().Message
                        .Equals("Test message has been confirmed"));
            }

            [Fact]
            public void It_should_have_published_the_message_on_the_correct_exchange()
            {
                _basicPublish.Should().Contain(frame => frame.Message.Exchange.Equals(ExchangeName.From("myExchange0")));
            }

            [Fact]
            public void It_should_have_declared_exchanges()
            {
                _exchangeDeclare.Should().HaveCount(1);
            }

            [Fact]
            public void It_should_have_declared_an_exchange_with_name()
            {
                _exchangeDeclare.Should().ContainSingle(frame => frame.Message.Exchange.Equals(ExchangeName.From("myExchange0")));
            }

            [Fact]
            public void It_should_have_declared_an_exchange_with_type()
            {
                _exchangeDeclare.Should().Contain(frame => frame.Message.Type.Equals(Shortstr.From("topic")));
            }

            [Fact]
            public void It_should_have_sent_confirm_select_ok()
            {
                _selectOkSent.Should().BeTrue();
            }
        }
    }
}
