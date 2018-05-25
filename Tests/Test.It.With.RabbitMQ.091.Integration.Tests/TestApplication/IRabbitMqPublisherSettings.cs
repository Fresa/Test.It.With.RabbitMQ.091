namespace Test.It.With.RabbitMQ091.Integration.Tests.TestApplication
{
    internal interface IRabbitMqPublisherSettings
    {
        bool EnablePublisherConfirms { get; }
    }
}