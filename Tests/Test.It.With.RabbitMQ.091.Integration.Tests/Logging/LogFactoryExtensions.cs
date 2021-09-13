using Log.It;
using Log.It.With.NLog;
using Test.It.With.RabbitMQ091.Integration.Tests.Common;
using Test.It.With.RabbitMQ091.Integration.Tests.XUnit;

namespace Test.It.With.RabbitMQ091.Integration.Tests.Logging
{
    internal static class LogFactoryExtensions
    {
        private static readonly ExclusiveLock Lock = new();

        public static void InitializeOnce()
        {
            if (!Lock.TryAcquire())
            {
                return;
            }

            if (LogFactory.HasFactory)
            {
                return;
            }

            LogFactory.Initialize(new NLogFactory(new LogicalThreadContext()));
        }
    }
}