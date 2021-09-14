using System;
using System.Threading.Tasks;

namespace Test.It.With.RabbitMQ091.Integration.Tests.Common
{
    internal sealed class AsyncDisposableAction : IAsyncDisposable
    {
        private readonly Func<ValueTask> _onDispose;

        public AsyncDisposableAction(Func<ValueTask> onDispose)
        {
            _onDispose = onDispose;
        }

        public ValueTask DisposeAsync()
        {
            return _onDispose();
        }
    }
}