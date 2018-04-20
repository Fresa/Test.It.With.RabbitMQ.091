using Test.It.With.Amqp.Protocol;

namespace Test.It.With.RabbitMQ._091
{
    internal class RabbitMq091ReaderFactory : IAmqpReaderFactory
    {
        public IAmqpReader Create(byte[] data)
        {
            return new RabbitMQ091Reader(data);
        }
    }
}