using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using Test.It.While.Hosting.Your.Service;
using Test.It.With.Amqp;
using Test.It.With.Amqp.Messages;
using Test.It.With.Amqp091.Protocol;
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
        public class When_consuming_messages_and_nacking : XUnitServiceSpecification<DefaultServiceHostStarter<TestApplicationBuilder<MessageConsumingApplication>>>
        {
            private readonly ConcurrentBag<MethodFrame<Exchange.Declare>> _exchangesDeclared = new();
            private readonly ConcurrentBag<MethodFrame<Queue.Declare>> _queuesDeclared = new();
            private readonly ConcurrentBag<MethodFrame<Queue.Bind>> _queuesBound = new();
            private readonly ConcurrentBag<MethodFrame<Basic.Consume>> _basicConsumes = new();
            private readonly ConcurrentBag<MethodFrame<Methods.Basic.Nack>> _nacks = new();
            private readonly SemaphoreSlim _waitForAllChannelsClosed = new(0);

            public When_consuming_messages_and_nacking(ITestOutputHelper output) : base(output)
            {
            }

            private const int Parallelism = 2;

            protected override string[] StartParameters { get; } = { Parallelism.ToString(), "true" };

            protected override TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
            protected override TimeSpan StopTimeout { get; set; } = TimeSpan.FromSeconds(10);

            protected override void Given(IServiceContainer container)
            {
                var channels = new ConcurrentDictionary<(ConnectionId, short), Channel.Open>();

                var testFramework = AmqpTestFramework.WithSocket(RabbitMq091.ProtocolResolver);
                testFramework
                    .WithDefaultProtocolHeaderNegotiation()
                    .WithDefaultSecurityNegotiation(heartbeatInterval: TimeSpan.FromSeconds(5))
                    .WithDefaultConnectionOpenNegotiation()
                    .WithHeartbeats(interval: TimeSpan.FromSeconds(5))
                    .WithDefaultConnectionCloseNegotiation();

                testFramework.On<Channel.Open, Channel.OpenOk>((connectionId, frame) =>
                {
                    channels.TryAdd((connectionId, frame.Channel), frame.Message);
                    return new Channel.OpenOk();
                });
                testFramework.On<Channel.CloseOk>((connectionId, frame) =>
                {
                    channels.TryRemove((connectionId, frame.Channel), out _);
                    if (channels.IsEmpty)
                    {
                        _waitForAllChannelsClosed.Release();
                    }
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
                testFramework.On<Methods.Basic.Nack>((connectionId, frame) =>
                {
                    _nacks.Add(frame);
                    TryStop();
                });

                var server = testFramework.Start();
                DisposeAsyncOnTearDown(server);
                DisposeAsyncOnTearDown(testFramework);

                container.RegisterSingleton(server.ToRabbitMqConnectionFactory);

                async Task TryStop()
                {
                    if (_nacks.Count == Parallelism)
                    {
                        // The RabbitMQ client is very sensitive about closing channels and connections.
                        // It does not necessary respond to connection close so we let the client close the connections
                        foreach (var (connectionId, channel) in channels.Keys.ToArray())
                        {
                            testFramework.Send(connectionId,
                                new MethodFrame<Channel.Close>(channel,
                                    new Channel.Close()
                                    { ReplyCode = ReplyCode.From(200), ReplyText = ReplyText.From("bye") }));
                        }

                        await _waitForAllChannelsClosed.WaitAsync(Timeout)
                            .ConfigureAwait(false);
                        await ServiceController.StopAsync()
                            .ConfigureAwait(false);
                    }
                }
            }

            [Fact]
            public void It_should_have_declared_exchanges()
            {
                _exchangesDeclared.Should().HaveCount(2);
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
            public void It_should_have_nacked_consumed_messages()
            {
                _nacks.Should().HaveCount(2)
                    .And.AllBeEquivalentTo(new MethodFrame<Methods.Basic.Nack>(1,
                        new Methods.Basic.Nack
                        {
                            DeliveryTag = DeliveryTag.From(1),
                            Multiple = Bit.From(false),
                            Requeue = Bit.From(true)
                        }));

            }
        }
    }
}
