using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Aika.Tests
{
    public class InMemoryHistorianTestFixture : IAsyncLifetime {

        internal AikaHistorian DefaultHistorian { get; }


        public InMemoryHistorianTestFixture() {
            DefaultHistorian = CreateHistorian();
        }


        internal AikaHistorian CreateHistorian() {
            // TODO: include logging.
            var taskRunner = new DefaultTaskRunner(null);
            return new AikaHistorian(new Aika.Implementations.InMemory.InMemoryHistorian(taskRunner, null), null);
        }


        public virtual async Task InitializeAsync() {
            await DefaultHistorian.Init(CancellationToken.None).ConfigureAwait(false);
        }


        public virtual Task DisposeAsync() {
            DefaultHistorian.Dispose();
            return Task.CompletedTask;
        }

    }
}
