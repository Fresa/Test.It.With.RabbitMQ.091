using Test.It.With.Amqp.Protocol;

namespace Test.It.With.RabbitMQ091.Methods
{
    public class Basic
    {
        public class Ack : Amqp091.Protocol.Basic.Ack, IServerMethod { }
    }
}