﻿using Test.It.With.Amqp.Protocol;

namespace Test.It.With.RabbitMQ091
{
    public class RabbitMq091
    {
        public static IProtocolResolver ProtocolResolver => RabbitMq091ProtocolResolver.Create();
    }
}