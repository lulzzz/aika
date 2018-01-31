using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Aika.Tests {
    public class TagSearchTests : IAsyncLifetime {

        private readonly AikaHistorian _historian;


        public TagSearchTests() {
            // TODO: include logging.
            var taskRunner = new DefaultTaskRunner(null);
            _historian = new AikaHistorian(new Aika.Historians.InMemoryHistorian(taskRunner, null), null);
        }


        public async Task InitializeAsync() {
            await _historian.Init(CancellationToken.None).ConfigureAwait(false);
        }


        public Task DisposeAsync() {
            return Task.CompletedTask;
        }


        [Fact]
        public async Task TagSearch_ShouldReturnZeroTags() {
            var tags = await _historian.GetTags(Identities.GetTestIdentity(),
                                                new Tags.TagDefinitionFilter() {
                                                    FilterClauses = new[] {
                                                        new Tags.TagDefinitionFilterClause() {
                                                            Field = Tags.TagDefinitionFilterField.Name,
                                                            Value = "SHOULD_NOT_MATCH"
                                                        }
                                                    }
                                                }, CancellationToken.None).ConfigureAwait(false);

            Assert.Empty(tags);
        }
    }
}
