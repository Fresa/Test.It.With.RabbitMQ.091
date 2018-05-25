using System;
using Test.It.With.Amqp091.Protocol;

namespace Test.It.With.RabbitMQ091
{
    internal class RabbitMQ091Reader : Amqp091Reader
    {
        public RabbitMQ091Reader(byte[] buffer) : base(buffer)
        {
        }

        public override object ReadFieldValue()
        {
            var name = Convert.ToChar((byte) PeekByte());
            switch (name)
            {
                case 'x':
                    ReadByte();
                    return new ByteArray(ReadLongString());
            }

            return base.ReadFieldValue();
        }
    }
}