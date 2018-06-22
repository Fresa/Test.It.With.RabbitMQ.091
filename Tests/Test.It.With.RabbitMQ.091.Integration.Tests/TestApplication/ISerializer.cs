namespace Test.It.With.RabbitMQ091.Integration.Tests.TestApplication
{
    public interface ISerializer
    {
        T Deserialize<T>(byte[] data);
        byte[] Serialize<T>(T data);
    }
}