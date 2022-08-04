using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Test.It.While.Hosting.Your.Service;
using Test.It.With.Amqp;
using Test.It.With.Amqp.Messages;
using Test.It.With.Amqp.NetworkClient;
using Test.It.With.Amqp091.Protocol;
using Test.It.With.RabbitMQ091.Integration.Tests.FrameworkExtensions;
using Test.It.With.RabbitMQ091.Integration.Tests.TestApplication.Specifications;
using Test.It.With.RabbitMQ091.Integration.Tests.XUnit;
using Xunit;
using Xunit.Abstractions;
using Queue = Test.It.With.Amqp091.Protocol.Queue;

namespace Test.It.With.RabbitMQ091.Integration.Tests
{
    namespace Given_a_client_application_receiving_messages_over_rabbitmq
    {
        internal class ConcurrentHashSet<T> : IEnumerable<T>
        {
            private readonly ConcurrentDictionary<T, T> _data;

            public ConcurrentHashSet(params T[] data) => 
                _data = new ConcurrentDictionary<T, T>(data.Select(value => new KeyValuePair<T, T>(value, value)));

            internal bool TryRemove(T value)
            {
                return _data.TryRemove(value, out _);
            }

            internal bool TryAdd(T value)
            {
                return _data.TryAdd(value, value);
            }

            internal bool IsEmpty => _data.IsEmpty;

            public IEnumerator<T> GetEnumerator()
            {
                return _data.Keys.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public class When_consuming_messages_and_broker_disconnects : XUnitServiceSpecification<DefaultServiceHostStarter<TestApplicationBuilder<MessageConsumingApplication>>>
        {
            private readonly ConcurrentBag<MethodFrame<Exchange.Declare>> _exchangesDeclared = new ConcurrentBag<MethodFrame<Exchange.Declare>>();
            private readonly ConcurrentBag<MethodFrame<Queue.Declare>> _queuesDeclared = new ConcurrentBag<MethodFrame<Queue.Declare>>();
            private readonly ConcurrentBag<MethodFrame<Queue.Bind>> _queuesBound = new ConcurrentBag<MethodFrame<Queue.Bind>>();
            private readonly ConcurrentBag<MethodFrame<Basic.Consume>> _basicConsumes = new ConcurrentBag<MethodFrame<Basic.Consume>>();
            private readonly SemaphoreSlim _waitForAllChannelsClosed = new(0);
            private SocketAmqpTestFramework _testFramework;
            private readonly ConcurrentDictionary<ConnectionId, ConcurrentHashSet<short>> _connections = new();
            private readonly SemaphoreSlim _consumeReceived = new(0);
            private IServer _server;
            
            public When_consuming_messages_and_broker_disconnects(ITestOutputHelper output) : base(output)
            {
            }

            //protected override TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
            //protected override TimeSpan StopTimeout { get; set; } = TimeSpan.FromSeconds(10);
            private const int Consumers = 1;
            protected override string[] StartParameters { get; } = { Consumers.ToString() };

            protected override void Given(IServiceContainer container)
            {
                _testFramework = AmqpTestFramework.WithSocket(Amqp091.Protocol.Amqp091.ProtocolResolver);
                _testFramework
                    .WithDefaultProtocolHeaderNegotiation()
                    .WithDefaultSecurityNegotiation(heartbeatInterval: TimeSpan.FromSeconds(5))
                    .WithDefaultConnectionOpenNegotiation()
                    .WithHeartbeats(interval: TimeSpan.FromSeconds(5))
                    .WithDefaultConnectionCloseNegotiation();
                _server = _testFramework.Start();
                DisposeAsyncOnTearDown(_server);
                DisposeAsyncOnTearDown(_testFramework);

                _testFramework.On<Channel.Open, Channel.OpenOk>((connectionId, frame) =>
                {
                    _connections.AddOrUpdate(connectionId, _ => new ConcurrentHashSet<short>(frame.Channel), (_, channels) =>
                    {
                        channels.TryAdd(frame.Channel);
                        return channels;
                    });
                    return new Channel.OpenOk();
                });
                _testFramework.On<Channel.CloseOk>((id, frame) =>
                {
                    _connections.AddOrUpdate(id, _ => new ConcurrentHashSet<short>(), (_, channels) =>
                    {
                        channels.TryRemove(frame.Channel);
                        return channels;
                    });
                    if (_connections.Values.All(channels => channels.IsEmpty))
                    {
                        _waitForAllChannelsClosed.Release();
                    }
                });
                _testFramework.On<Exchange.Declare, Exchange.DeclareOk>((connectionId, frame) =>
                {
                    _exchangesDeclared.Add(frame);
                    return new Exchange.DeclareOk();
                });
                _testFramework.On<Queue.Declare, Queue.DeclareOk>((connectionId, frame) =>
                {
                    _queuesDeclared.Add(frame);
                    return new Queue.DeclareOk();
                });
                _testFramework.On<Queue.Bind, Queue.BindOk>((connectionId, frame) =>
                {
                    _queuesBound.Add(frame);
                    return new Queue.BindOk();
                });

                _testFramework.On<Basic.Consume>((connectionId, frame) =>
                {
                    var consumerTag = Guid.NewGuid().ToString();
                    _basicConsumes.Add(frame);

                    // We need to respond with ConsumeOk before we can start delivering messages to the client
                    _testFramework.Send(connectionId, new MethodFrame<Basic.ConsumeOk>(frame.Channel,
                        new Basic.ConsumeOk
                        {
                            ConsumerTag = ConsumerTag.From(consumerTag)
                        }));

                    _consumeReceived.Release();
                });
                _testFramework.On<Basic.Cancel, Basic.CancelOk>((connectionId, frame) =>
                    new Basic.CancelOk { ConsumerTag = frame.Message.ConsumerTag });

                container.RegisterSingleton(_server.ToRabbitMqConnectionFactory);
            }

            protected override async Task WhenAsync(CancellationToken cancellationToken)
            {
                await _consumeReceived.WaitAsync(cancellationToken)
                    .ConfigureAwait(false);
                var closedConnection = _connections.First().Key;
                _connections.TryRemove(closedConnection, out _);
                await _server.DisconnectAsync(closedConnection, cancellationToken)
                    .ConfigureAwait(false);
                await _consumeReceived.WaitAsync(cancellationToken)
                    .ConfigureAwait(false);
                // The RabbitMQ client is very sensitive about closing channels and connections.
                // It does not necessary respond to connection close so we let the client close the connections
                foreach (var (connectionId, channels) in _connections)
                {
                    foreach (var channel in channels)
                    {
                        _testFramework.Send(connectionId,
                            new MethodFrame<Channel.Close>(channel,
                                new Channel.Close
                                    { ReplyCode = ReplyCode.From(200), ReplyText = ReplyText.From("bye") }));
                    }
                }
                await _waitForAllChannelsClosed.WaitAsync(cancellationToken)
                    .ConfigureAwait(false);
                await ServiceController.StopAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            
            [Fact]
            public void It_should_have_declared_exchanges()
            {
                _exchangesDeclared.Should().HaveCount(2)
                    .And
                    .OnlyContain(frame => frame.Message.Exchange.Equals(ExchangeName.From("myExchange0")));
            }

            [Fact]
            public void It_should_have_declared_queues()
            {
                _queuesDeclared.Should().HaveCount(2)
                    .And
                    .OnlyContain(frame => frame.Message.Queue.Equals(QueueName.From("queue0")));
            }

            //[Fact]
            //public void It_should_have_declared_queues_with_name()
            //{
            //    _queuesDeclared.Should().ContainSingle(frame => frame.Message.Queue.Equals(QueueName.From("queue0")));
            //    _queuesDeclared.Should().ContainSingle(frame => frame.Message.Queue.Equals(QueueName.From("queue1")));
            //}

            //[Fact]
            //public void It_should_have_bound_the_queues()
            //{
            //    _queuesBound.Should().HaveCount(2);
            //}

            //[Fact]
            //public void It_should_have_bound_queues_to_exchanges()
            //{
            //    _queuesBound.Should().ContainSingle(frame => 
            //            frame.Message.Queue.Equals(QueueName.From("queue0")) &&
            //            frame.Message.Exchange.Equals(ExchangeName.From("myExchange0")) &&
            //            frame.Message.RoutingKey.Equals(Shortstr.From("routing0")));

            //    _queuesBound.Should().ContainSingle(frame => 
            //            frame.Message.Queue.Equals(QueueName.From("queue1")) &&
            //            frame.Message.Exchange.Equals(ExchangeName.From("myExchange1")) &&
            //            frame.Message.RoutingKey.Equals(Shortstr.From("routing1")));
            //}

            //[Fact]
            //public void It_should_have_declared_an_exchange_with_type()
            //{
            //    _exchangesDeclared.Should().Contain(frame => frame.Message.Type.Equals(Shortstr.From("topic")));
            //}

            //[Fact]
            //public void It_should_have_starting_consuming_messages()
            //{
            //    _basicConsumes.Should().HaveCount(2);
            //}

            //[Fact]
            //public void It_should_have_starting_consuming_messages_from_queues()
            //{
            //    _basicConsumes.Should().ContainSingle(frame => frame.Message.Queue.Equals(QueueName.From("queue0")));
            //    _basicConsumes.Should().ContainSingle(frame => frame.Message.Queue.Equals(QueueName.From("queue1")));
            //}
        }
    }
}
