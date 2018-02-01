using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Aika.Tests {
    public class TagSearchTests : IClassFixture<TagSearchTestsFixture> {

        private readonly AikaHistorian _historian;


        public TagSearchTests(TagSearchTestsFixture fixture) {
            _historian = fixture.Historian;
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


        [Theory]
        [InlineData("*fir*t*", "First_Test_Tag")]
        [InlineData("*se*ond*", "Second_Test_Tag")]
        public async Task TagSearch_ShouldFindTagsByNameFilter(string filter, string expectedResult) {
            var tags = await _historian.GetTags(Identities.GetTestIdentity(),
                                                new Tags.TagDefinitionFilter() {
                                                    FilterClauses = new[] {
                                                        new Tags.TagDefinitionFilterClause() {
                                                            Field = Tags.TagDefinitionFilterField.Name,
                                                            Value = filter
                                                        }
                                                    }
                                                }, CancellationToken.None).ConfigureAwait(false);

            Assert.Single(tags);
            Assert.Equal(tags.First().Name, expectedResult);
        }


        [Theory]
        [InlineData("*fi*t*", "First_Test_Tag")]
        [InlineData("*seco*d*", "Second_Test_Tag")]
        public async Task TagSearch_ShouldFindTagsByDescriptionFilter(string filter, string expectedResult) {
            var tags = await _historian.GetTags(Identities.GetTestIdentity(),
                                                new Tags.TagDefinitionFilter() {
                                                    FilterClauses = new[] {
                                                        new Tags.TagDefinitionFilterClause() {
                                                            Field = Tags.TagDefinitionFilterField.Description,
                                                            Value = filter
                                                        }
                                                    }
                                                }, CancellationToken.None).ConfigureAwait(false);

            Assert.Single(tags);
            Assert.Equal(tags.First().Name, expectedResult);
        }


        [Theory]
        [InlineData("*Km*h", "First_Test_Tag")]
        [InlineData("*De*C", "Second_Test_Tag")]
        public async Task TagSearch_ShouldFindTagsByUnitFilter(string filter, string expectedResult) {
            var tags = await _historian.GetTags(Identities.GetTestIdentity(),
                                                new Tags.TagDefinitionFilter() {
                                                    FilterClauses = new[] {
                                                        new Tags.TagDefinitionFilterClause() {
                                                            Field = Tags.TagDefinitionFilterField.Units,
                                                            Value = filter
                                                        }
                                                    }
                                                }, CancellationToken.None).ConfigureAwait(false);

            Assert.Single(tags);
            Assert.Equal(tags.First().Name, expectedResult);
        }
    }


    public class TagSearchTestsFixture : IAsyncLifetime {

        internal AikaHistorian Historian { get; }


        public TagSearchTestsFixture() {
            // TODO: include logging.
            var taskRunner = new DefaultTaskRunner(null);
            Historian = new AikaHistorian(new Aika.Historians.InMemoryHistorian(taskRunner, null), null);
        }


        public async Task InitializeAsync() {
            await Historian.Init(CancellationToken.None).ConfigureAwait(false);

            await Historian.CreateTag(Identities.GetTestIdentity(),
                                       new Tags.TagSettings() {
                                           Name = "First_Test_Tag",
                                           Description = "This is the first test tag",
                                           Units = "km/h"
                                       }, CancellationToken.None).ConfigureAwait(false);

            await Historian.CreateTag(Identities.GetTestIdentity(),
                                       new Tags.TagSettings() {
                                           Name = "Second_Test_Tag",
                                           Description = "This is the second test tag",
                                           Units = "deg C"
                                       }, CancellationToken.None).ConfigureAwait(false);
        }


        public Task DisposeAsync() {
            return Task.CompletedTask;
        }

    }
}
