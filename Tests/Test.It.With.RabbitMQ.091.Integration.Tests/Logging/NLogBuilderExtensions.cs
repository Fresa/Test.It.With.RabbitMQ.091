using NLog.Extensions.Logging;
using NLog.Web;
using Test.It.With.RabbitMQ091.Integration.Tests.Common;
using Test.It.With.RabbitMQ091.Integration.Tests.XUnit;

namespace Test.It.With.RabbitMQ091.Integration.Tests.Logging
{
    internal static class NLogBuilderExtensions
    {
        private static readonly ExclusiveLock NlogConfigurationLock =
            new();

        internal static void ConfigureNLogOnce(
            Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            if (!NlogConfigurationLock.TryAcquire())
            {
                return;
            }

            var nLogConfig = new NLogLoggingConfiguration(
                configuration.GetSection("NLog"));
            NLogBuilder.ConfigureNLog(nLogConfig);
        }
    }
}