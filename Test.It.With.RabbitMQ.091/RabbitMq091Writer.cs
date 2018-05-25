using System.IO;
using Test.It.With.Amqp091.Protocol;

namespace Test.It.With.RabbitMQ091
{
    internal class RabbitMq091Writer : Amqp091Writer
    {
        public RabbitMq091Writer(Stream buffer) : base(buffer)
        {
        }

        public override void WriteFieldValue(object value)
        {
            switch (value)
            {
                case ByteArray convertedValue:
                    WriteByte((byte) 'x');
                    WriteLongString(convertedValue.Bytes);
                    return;
            }

            base.WriteFieldValue(value);
        }
    }
}