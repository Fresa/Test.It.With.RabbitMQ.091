namespace Test.It.With.RabbitMQ091.Integration.Tests.TestApplication
{
    public class TestMessage
    {
        public TestMessage(string message)
        {
            Message = message;
        }

        public string Message { get; private set; }
    }
}