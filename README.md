# Test.It.With.RabbitMQ.091
An integration of the [RabbitMQ][RabbitMQRepository] client and [Test.It.With.AMQP][TestItWithAMQPRepository] test framework.

[![Build status](https://ci.appveyor.com/api/projects/status/m220tb483r7qqcvv?svg=true)](https://ci.appveyor.com/project/Fresa/test-it-with-rabbitmq-091/)

[![Build history](https://buildstats.info/appveyor/chart/Fresa/test-it-with-rabbitmq-091)](https://ci.appveyor.com/project/Fresa/test-it-with-rabbitmq-091/history)

## Download
https://www.nuget.org/packages/Test.It.With.RabbitMQ.091/

## Example
The following is an example how an integration test might look like. Be aware that the test is pretty raw specifying communcation on a low level. Most often you'll write the handshaking routines once and abstract them away focusing instead on your high level protocol (consuming and producing messages).
[Given_a_client_application_sending_messages_over_rabbitmq When_publishing_a_message][TestExample]

## Getting Started
[RabbitMq091.cs] is the test framework protocol resolver for the RabbitMQ extended AMQP 0.9.1 protocol. Integration is done by using the extension method `ToRabbitMqConnectionFactory` for `INetworkClientFactory` (used by [Test.It.With.AMQP][TestItWithAMQPRepository]) returning a RabbitMQ `IConnectionFactory` which can be injected to the application  using the RabbitMQ client.

### Protocol Definitions
It extends [AMQP 0.9.1][AMQP091Repository] protocol definition with some [custom methods][ExtendedProtocolMethods].

[RabbitMQRepository]:
<https://github.com/rabbitmq/rabbitmq-dotnet-client>
[TestItWithAMQPRepository]:
<https://github.com/Fresa/Test.It.With.AMQP>
[RabbitMq091.cs]:
<https://github.com/Fresa/Test.It.With.RabbitMQ.091/blob/master/Test.It.With.RabbitMQ.091/RabbitMq091.cs>
[TestExample]:
https://github.com/Fresa/Test.It.With.RabbitMQ.091/blob/master/Tests/Test.It.With.RabbitMQ.091.Integration.Tests/When_publishing_messages.cs
[ExtendedProtocolMethods]:
<https://github.com/Fresa/Test.It.With.RabbitMQ.091/tree/master/Test.It.With.RabbitMQ.091/Methods>
[AMQP091Repository]:
<https://github.com/Fresa/Test.It.With.AMQP.091.Protocol>
