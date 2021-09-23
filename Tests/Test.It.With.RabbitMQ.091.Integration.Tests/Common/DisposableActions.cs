using System;

namespace Test.It.With.RabbitMQ091.Integration.Tests.Common
{
    internal class DisposableActions : IDisposable
    {
        private readonly Action[] _dispose;

        internal DisposableActions(
            params Action[] dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            foreach (var dispose in _dispose)
            {
                dispose();
            }
        }
    }
}