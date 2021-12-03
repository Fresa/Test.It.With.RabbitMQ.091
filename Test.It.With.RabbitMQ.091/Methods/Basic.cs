using System;
using Test.It.With.Amqp.Protocol;
using Test.It.With.Amqp091.Protocol;

namespace Test.It.With.RabbitMQ091.Methods
{
    public class Basic
    {
        internal const int ClassId = 60;

        public class Publish : Amqp091.Protocol.Basic.Publish, IRespond<Ack>, IRespond<Nack>
        {
            public Ack Respond(Ack method)
            {
                return method;
            }

            public Nack Respond(Nack method)
            {
                return method;
            }
        }

        public class Ack : Amqp091.Protocol.Basic.Ack, IServerMethod { }

        public class Nack : IMethod, IMessage, INonContentMethod, IClientMethod, IServerMethod
        {
            private DeliveryTag _deliveryTag;
            private Bit _multiple;
            private Bit _requeue;

            public int ProtocolClassId => ClassId;

            internal const int MethodId = 120;
            public int ProtocolMethodId => MethodId;

            public bool SentOnValidChannel(int channel) => channel > 0;

            public Type[] Responses() => Type.EmptyTypes;

            public DeliveryTag DeliveryTag
            {
                get => _deliveryTag;
                set => _deliveryTag = value;
            }

            public Bit Requeue
            {
                get => _requeue;
                set => _requeue = value;
            }
            
            public Bit Multiple
            {
                get => _multiple;
                set => _multiple = value;
            }

            public void ReadFrom(IAmqpReader reader)
            {
                _deliveryTag = new DeliveryTag(reader.ReadLongLongInteger());
                _multiple = new Bit(reader.ReadBit());
                _requeue = new Bit(reader.ReadBit());
            }

            public void WriteTo(IAmqpWriter writer)
            {
                writer.WriteShortUnsignedInteger((ushort)ProtocolClassId);
                writer.WriteShortUnsignedInteger((ushort)ProtocolMethodId);
                writer.WriteLongLongInteger(_deliveryTag.Value);
                writer.WriteBit(_multiple.Value);
                writer.WriteBit(_requeue.Value);
            }
        }
    }
}