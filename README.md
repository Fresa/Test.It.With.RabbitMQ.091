# Test.It.With.RabbitMQ.091
An integration test framework for RabbitMQ based on [Test.It.With.AMQP](https://github.com/Fresa/Test.It.With.AMQP).

[![Continuous Delivery](https://github.com/Fresa/Test.It.With.RabbitMQ.091/actions/workflows/ci.yml/badge.svg)](https://github.com/Fresa/Test.It.With.RabbitMQ.091/actions/workflows/ci.yml)

## Download
https://www.nuget.org/packages/Test.It.With.RabbitMQ.091/

## Example
The following is an example how an integration test might look like. Be aware that the test is pretty raw specifying communcation on a low level. Most often you'll write the handshaking routines once and abstract them away focusing instead on your high level protocol (consuming and publishing messages).

Test an application that publishes messages:
[Given_a_client_application_sending_messages_over_rabbitmq.When_publishing_a_message](https://github.com/Fresa/Test.It.With.RabbitMQ.091/blob/master/Tests/Test.It.With.RabbitMQ.091.Integration.Tests/When_publishing_messages.cs)

Test an application that consumes messages:
[Given_a_client_application_receiving_messages_over_rabbitmq.When_consuming_messages](https://github.com/Fresa/Test.It.With.RabbitMQ.091/blob/master/Tests/Test.It.With.RabbitMQ.091.Integration.Tests/When_consuming_messages.cs)

## Getting Started
```c#
await using var testFramework = AmqpTestFramework.WithSocket(Amqp091.Protocol.Amqp091.ProtocolResolver);
```
[RabbitMq091.cs](https://github.com/Fresa/Test.It.With.RabbitMQ.091/blob/master/Test.It.With.RabbitMQ.091/RabbitMq091.cs) is the test framework protocol resolver for the RabbitMQ extended AMQP 0.9.1 protocol. ~~Integration is done by using the extension method `ToRabbitMqConnectionFactory` for `INetworkClientFactory` (used by [Test.It.With.AMQP](https://github.com/Fresa/Test.It.With.AMQP)) returning a RabbitMQ `IConnectionFactory` which can be injected to the application using the RabbitMQ client.~~

### Upgrading from 1.x -> 2.x
Since [rabbitmq-dotnet-client 6.x](https://github.com/rabbitmq/rabbitmq-dotnet-client) internalized a lot of connection related abstractions the test framework only works with the socket implementation from 2.0. See [Test.It.With.AMQP](https://github.com/Fresa/Test.It.With.AMQP) for instructions on how to upgrade. This means the framework is now client agnostic and works with any RabbitMQ 0.9.1 capable clients.

### Protocol Definitions
It extends [AMQP 0.9.1](https://github.com/Fresa/Test.It.With.AMQP.091.Protocol) protocol definition with some [custom methods](https://github.com/Fresa/Test.It.With.RabbitMQ.091/tree/master/Test.It.With.RabbitMQ.091/Methods).

### Logging
See [Test.It.With.AMQP](https://github.com/Fresa/Test.It.With.AMQP#logging)