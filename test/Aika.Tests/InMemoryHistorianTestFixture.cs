using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Aika.Tests
{
    public class InMemoryHistorianTestFixture : IAsyncLifetime {

        internal AikaHistorian Historian { get; }


        public InMemoryHistorianTestFixture() {
            // TODO: include logging.
            var taskRunner = new DefaultTaskRunner(null);
            Historian = new AikaHistorian(new Aika.Historians.InMemoryHistorian(taskRunner, null), null);
        }


        public virtual async Task InitializeAsync() {
            await Historian.Init(CancellationToken.None).ConfigureAwait(false);
        }


        public virtual Task DisposeAsync() {
            return Task.CompletedTask;
        }

    }
}
