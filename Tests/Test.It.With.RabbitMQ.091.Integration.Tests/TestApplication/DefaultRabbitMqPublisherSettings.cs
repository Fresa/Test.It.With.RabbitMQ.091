namespace Test.It.With.RabbitMQ091.Integration.Tests.TestApplication
{
    internal class DefaultRabbitMqPublisherSettings : IRabbitMqPublisherSettings
    {
        public bool EnablePublisherConfirms { get; set; }
    }
}