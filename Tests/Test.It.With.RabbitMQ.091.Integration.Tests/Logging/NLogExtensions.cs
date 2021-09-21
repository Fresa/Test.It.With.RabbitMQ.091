using System;
using Log.It.With.NLog;
using Test.It.With.RabbitMQ091.Integration.Tests.Common;

namespace Test.It.With.RabbitMQ091.Integration.Tests.Logging
{
    internal static class NLogExtensions
    {
        private static readonly ExclusiveLock NLogCapturingTargetLock =
            new();
        internal static void RegisterLoggingOnce()
        {
            if (!NLogCapturingTargetLock.TryAcquire())
            {
                return;
            }

            NLogCapturingTarget.Subscribe += Output.Writer.Write;
            Amqp.Logging.Logger.OnLog += NLogLogger.Log;

            AppDomain.CurrentDomain.DomainUnload += (_, _) =>
            {
                NLogCapturingTarget.Subscribe -= Output.Writer.Write;
                Amqp.Logging.Logger.OnLog -= NLogLogger.Log;
            };
        }
    }
}