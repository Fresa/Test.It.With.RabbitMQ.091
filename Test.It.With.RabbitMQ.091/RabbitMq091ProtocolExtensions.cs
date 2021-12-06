using System;
using System.Collections.Generic;
using Test.It.With.Amqp.Protocol;
using Test.It.With.RabbitMQ091.Methods;

namespace Test.It.With.RabbitMQ091
{
    internal class RabbitMq091ProtocolExtensions
    {
        public bool TryGetMethod(IAmqpReader reader, out IMethod method)
        {
            method = default;

            var classId = reader.ReadShortUnsignedInteger();
            if (ClassRegister.TryGetValue(classId, out var methodRegister) == false)
            {
                return false;
            }

            var methodId = reader.ReadShortUnsignedInteger();
            if (methodRegister.TryGetValue(methodId, out var methodFactory) == false)
            {
                return false;
            }

            method = methodFactory();
            method.ReadFrom(reader);
            return true;
        }

        private static readonly Dictionary<int, Dictionary<int, Func<IMethod>>> ClassRegister = new Dictionary<int, Dictionary<int, Func<IMethod>>>
        {
            { Confirm.ClassId, new Dictionary<int, Func<IMethod>> {
                { Confirm.Select.MethodId, () => new Confirm.Select() },
                { Confirm.SelectOk.MethodId, () => new Confirm.SelectOk() }}},
            { Basic.ClassId, new Dictionary<int, Func<IMethod>>
            {
                { Basic.Nack.MethodId, () => new Basic.Nack() },
                { new Basic.Ack().ProtocolMethodId, () => new Basic.Ack() },
                { new Basic.Publish().ProtocolMethodId, () => new Basic.Publish() }
            }}
        };
    }
}