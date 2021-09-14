using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Test.It.With.RabbitMQ091.Integration.Tests.Common
{
    internal static class AsyncDisposableExtensions
    {
        internal static Task DisposeAllAsync(
            this IEnumerable<IAsyncDisposable> disposables)
            => disposables.Select(client => client.DisposeAsync())
                .WhenAllAsync();
    }
}