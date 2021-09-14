using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Test.It.While.Hosting.Your.Service;
using Test.It.With.Amqp;
using Test.It.With.Amqp.Messages;
using Test.It.With.Amqp091.Protocol;
using Test.It.With.RabbitMQ091.Integration.Tests.Assertion;
using Test.It.With.RabbitMQ091.Integration.Tests.FrameworkExtensions;
using Test.It.With.RabbitMQ091.Integration.Tests.TestApplication;
using Test.It.With.RabbitMQ091.Integration.Tests.TestApplication.Specifications;
using Test.It.With.RabbitMQ091.Integration.Tests.XUnit;
using Xunit;
using Xunit.Abstractions;

namespace Test.It.With.RabbitMQ091.Integration.Tests
{
    namespace Given_a_client_application_receiving_messages_over_rabbitmq
    {
        public class When_consuming_messages : XUnitServiceSpecification<DefaultServiceHostStarter<TestApplicationBuilder<MessageConsumingApplication>>>
        {
            private readonly ConcurrentBag<MethodFrame<Exchange.Declare>> _exchangesDeclared = new ConcurrentBag<MethodFrame<Exchange.Declare>>();
            private readonly ConcurrentBag<MethodFrame<Queue.Declare>> _queuesDeclared = new ConcurrentBag<MethodFrame<Queue.Declare>>();
            private readonly ConcurrentBag<MethodFrame<Queue.Bind>> _queuesBound = new ConcurrentBag<MethodFrame<Queue.Bind>>();
            private readonly ConcurrentBag<MethodFrame<Basic.Publish>> _basicPublishes = new ConcurrentBag<MethodFrame<Basic.Publish>>();
            private readonly ConcurrentBag<MethodFrame<Basic.Consume>> _basicConsumes = new ConcurrentBag<MethodFrame<Basic.Consume>>();
            private readonly ConcurrentBag<MethodFrame<Basic.Ack>> _acks = new ConcurrentBag<MethodFrame<Basic.Ack>>();

            public When_consuming_messages(ITestOutputHelper output) : base(output)
            {
            }

            private const int Parallelism = 2;

            protected override string[] StartParameters { get; } = { Parallelism.ToString() };

            protected override TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);
            
            protected override void Given(IServiceContainer container)
            {
                var channels = new ConcurrentDictionary<(ConnectionId, short), Channel.Open>();

                var testFramework = AmqpTestFramework.WithSocket(Amqp091.Protocol.Amqp091.ProtocolResolver);
                testFramework
                    .WithDefaultProtocolHeaderNegotiation()
                    .WithDefaultSecurityNegotiation(heartbeatInterval: TimeSpan.FromSeconds(5))
                    .WithDefaultConnectionOpenNegotiation()
                    .WithHeartbeats(interval: TimeSpan.FromSeconds(5))
                    .WithDefaultConnectionCloseNegotiation();

                var connections = new List<ConnectionId>();
                testFramework.On<Connection.Open>((id, frame) =>
                {
                    connections.Add(id);
                });
                testFramework.On<Channel.Open, Channel.OpenOk>((connectionId, frame) =>
                {
                    channels.TryAdd((connectionId, frame.Channel), frame.Message);
                    return new Channel.OpenOk();
                });
                testFramework.On<Channel.Close, Channel.CloseOk>((connectionId, frame) =>
                {
                    channels.TryRemove((connectionId, frame.Channel), out _);
                    return new Channel.CloseOk();
                });
                testFramework.On<Exchange.Declare, Exchange.DeclareOk>((connectionId, frame) =>
                {
                    _exchangesDeclared.Add(frame);
                    return new Exchange.DeclareOk();
                });
                testFramework.On<Queue.Declare, Queue.DeclareOk>((connectionId, frame) =>
                {
                    _queuesDeclared.Add(frame);
                    return new Queue.DeclareOk();
                });
                testFramework.On<Queue.Bind, Queue.BindOk>((connectionId, frame) =>
                {
                    _queuesBound.Add(frame);
                    return new Queue.BindOk();
                });

                testFramework.On<Basic.Publish>((connectionId, frame) =>
                {
                    _basicPublishes.Add(frame);
                });
                testFramework.On<Basic.Consume>((connectionId, frame) =>
                {
                    var consumerTag = Guid.NewGuid().ToString();
                    _basicConsumes.Add(frame);

                    // We need to respond with ConsumeOk before we can start delivering messages to the client
                    testFramework.Send(connectionId, new MethodFrame<Basic.ConsumeOk>(frame.Channel,
                        new Basic.ConsumeOk
                        {
                            ConsumerTag = ConsumerTag.From(consumerTag)
                        }));

                    Task.Run(() =>
                    {
                        var testMessage = new TestMessage("This is sent from server.");
                        var payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(testMessage));

                        testFramework.Send<Basic.Deliver, Basic.ContentHeader>(connectionId,
                            new MethodFrame<Basic.Deliver>(frame.Channel,
                                new Basic.Deliver
                                {
                                    ConsumerTag = ConsumerTag.From(consumerTag),
                                    ContentHeader = new Basic.ContentHeader
                                    {
                                        BodySize = payload.Length
                                    },
                                    DeliveryTag = DeliveryTag.From(1)
                                }
                                .AddContentBodyFragments(new ContentBody
                                {
                                    Payload = payload
                                })));
                    });
                });
                testFramework.On<Basic.Cancel, Basic.CancelOk>((connectionId, frame) =>
                    new Basic.CancelOk { ConsumerTag = frame.Message.ConsumerTag });
                testFramework.On<Basic.Ack>((connectionId, frame) =>
                {
                    _acks.Add(frame);
                    TryStop();
                });

                DisposeAsyncOnTearDown(testFramework.Start());
                DisposeAsyncOnTearDown(testFramework);
                
                container.RegisterSingleton(testFramework.ToRabbitMqConnectionFactory);

                void TryStop()
                {
                    if (_basicPublishes.Count == Parallelism && _acks.Count == Parallelism)
                    {
                        foreach (var ((connectionId, channel), _) in channels)
                        {
                            testFramework.Send(connectionId,
                                new MethodFrame<Channel.Close>(channel,
                                    new Channel.Close()
                                        {ReplyCode = ReplyCode.From(200), ReplyText = ReplyText.From("bye")}));
                        }
                        foreach (var connection in connections)
                        {
                            testFramework.Send(connection, new MethodFrame<Connection.Close>(0, new Connection.Close()));
                        }
                        ServiceController.StopAsync().GetAwaiter().GetResult();
                    }
                }
            }

            [Fact]
            public void It_should_have_published_messages()
            {
                _basicPublishes.Should().HaveCount(2);
            }

            [Fact]
            public void It_should_have_published_the_correct_message_type()
            {
                _basicPublishes.Should().Contain(frame =>
                    frame.Message.ContentHeader.Type.Equals(Shortstr.From(typeof(TestMessage).FullName)));
            }

            [Fact]
            public void It_should_have_published_the_correct_message()
            {
                _basicPublishes.Should().ContainSingle(frame =>
                    frame.Message.ContentBody.Deserialize<TestMessage>().Message
                        .Equals("0: This is sent from server."));
                _basicPublishes.Should().ContainSingle(frame =>
                    frame.Message.ContentBody.Deserialize<TestMessage>().Message
                        .Equals("1: This is sent from server."));
            }

            [Fact]
            public void It_should_have_published_the_message_on_the_correct_exchange()
            {
                _basicPublishes.Should().ContainSingle(frame => frame.Message.Exchange.Equals(ExchangeName.From("myExchange0")));
                _basicPublishes.Should().ContainSingle(frame => frame.Message.Exchange.Equals(ExchangeName.From("myExchange1")));
            }

            [Fact]
            public void It_should_have_declared_exchanges()
            {
                _exchangesDeclared.Should().HaveCount(4);
            }

            [Fact]
            public void It_should_have_declared_an_exchange_with_name()
            {
                _exchangesDeclared.Should().Contain(frame => frame.Message.Exchange.Equals(ExchangeName.From("myExchange0")));
                _exchangesDeclared.Should().Contain(frame => frame.Message.Exchange.Equals(ExchangeName.From("myExchange1")));
            }

            [Fact]
            public void It_should_have_declared_queues()
            {
                _queuesDeclared.Should().HaveCount(2);
            }

            [Fact]
            public void It_should_have_declared_queues_with_name()
            {
                _queuesDeclared.Should().ContainSingle(frame => frame.Message.Queue.Equals(QueueName.From("queue0")));
                _queuesDeclared.Should().ContainSingle(frame => frame.Message.Queue.Equals(QueueName.From("queue1")));
            }

            [Fact]
            public void It_should_have_bound_the_queues()
            {
                _queuesBound.Should().HaveCount(2);
            }

            [Fact]
            public void It_should_have_bound_queues_to_exchanges()
            {
                _queuesBound.Should().ContainSingle(frame => 
                        frame.Message.Queue.Equals(QueueName.From("queue0")) &&
                        frame.Message.Exchange.Equals(ExchangeName.From("myExchange0")) &&
                        frame.Message.RoutingKey.Equals(Shortstr.From("routing0")));

                _queuesBound.Should().ContainSingle(frame => 
                        frame.Message.Queue.Equals(QueueName.From("queue1")) &&
                        frame.Message.Exchange.Equals(ExchangeName.From("myExchange1")) &&
                        frame.Message.RoutingKey.Equals(Shortstr.From("routing1")));
            }

            [Fact]
            public void It_should_have_declared_an_exchange_with_type()
            {
                _exchangesDeclared.Should().Contain(frame => frame.Message.Type.Equals(Shortstr.From("topic")));
            }

            [Fact]
            public void It_should_have_starting_consuming_messages()
            {
                _basicConsumes.Should().HaveCount(2);
            }

            [Fact]
            public void It_should_have_starting_consuming_messages_from_queues()
            {
                _basicConsumes.Should().ContainSingle(frame => frame.Message.Queue.Equals(QueueName.From("queue0")));
                _basicConsumes.Should().ContainSingle(frame => frame.Message.Queue.Equals(QueueName.From("queue1")));
            }

            [Fact]
            public void It_should_have_acked_consumed_messages()
            {
                _acks.Should().HaveCount(2);
            }
        }
    }
}
