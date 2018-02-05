using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aika.Tags;
using Xunit;

namespace Aika.Tests {
    public class TagSearchTests : IClassFixture<TagSearchTestsFixture> {

        private readonly TagSearchTestsFixture _fixture;


        public TagSearchTests(TagSearchTestsFixture fixture) {
            _fixture = fixture;
        }
        


        [Fact]
        public async Task FindTags_ShouldReturnZeroTags() {
            var tags = await _fixture.Historian.FindTags(Identities.GetTestIdentity(),
                                                new TagDefinitionFilter() {
                                                    FilterClauses = new[] {
                                                        new TagDefinitionFilterClause() {
                                                            Field = TagDefinitionFilterField.Name,
                                                            Value = "SHOULD_NOT_MATCH"
                                                        }
                                                    }
                                                }, CancellationToken.None).ConfigureAwait(false);

            Assert.Empty(tags);
        }


        [Theory]
        [InlineData("*fir*t*", "First_Test_Tag")]
        [InlineData("*se*ond*", "Second_Test_Tag")]
        public async Task FindTags_ShouldFindTagsByNameFilter(string filter, string expectedResult) {
            var tags = await _fixture.Historian.FindTags(Identities.GetTestIdentity(),
                                                new TagDefinitionFilter() {
                                                    FilterClauses = new[] {
                                                        new TagDefinitionFilterClause() {
                                                            Field = TagDefinitionFilterField.Name,
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
        public async Task FindTags_ShouldFindTagsByDescriptionFilter(string filter, string expectedResult) {
            var tags = await _fixture.Historian.FindTags(Identities.GetTestIdentity(),
                                                new TagDefinitionFilter() {
                                                    FilterClauses = new[] {
                                                        new TagDefinitionFilterClause() {
                                                            Field = TagDefinitionFilterField.Description,
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
        public async Task FindTags_ShouldFindTagsByUnitFilter(string filter, string expectedResult) {
            var tags = await _fixture.Historian.FindTags(Identities.GetTestIdentity(),
                                                new TagDefinitionFilter() {
                                                    FilterClauses = new[] {
                                                        new TagDefinitionFilterClause() {
                                                            Field = TagDefinitionFilterField.Units,
                                                            Value = filter
                                                        }
                                                    }
                                                }, CancellationToken.None).ConfigureAwait(false);

            Assert.Single(tags);
            Assert.Equal(tags.First().Name, expectedResult);
        }


        [Fact]
        public async Task FindTags_ShouldFailWhenIdentityIsNull() {
            await Assert.ThrowsAsync<ArgumentNullException>(() => {
                return _fixture.Historian.FindTags(null,
                                           new TagDefinitionFilter() {
                                               FilterClauses = new[] {
                                                   new TagDefinitionFilterClause() {
                                                       Field = TagDefinitionFilterField.Name,
                                                       Value = "*"
                                                   }
                                               }
                                          }, CancellationToken.None);
            }).ConfigureAwait(false);
        }


        [Fact]
        public async Task FindTags_ShouldFailWhenFilterIsNull() {
            await Assert.ThrowsAsync<ArgumentNullException>(() => {
                return _fixture.Historian.FindTags(Identities.GetTestIdentity(),
                                           null,
                                           CancellationToken.None);
            }).ConfigureAwait(false);
        }


        [Fact]
        public async Task GetTags_ShouldReturnTagsByName() {
            var tags = await _fixture.Historian.GetTags(Identities.GetTestIdentity(), new[] { "First_Test_Tag", "Second_Test_Tag" }, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(2, tags.Count);
        }


        [Fact]
        public async Task GetTags_ShouldReturnTagsById() {
            var tags = await _fixture.Historian.GetTags(Identities.GetTestIdentity(), _fixture.TagMap.Keys, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(2, tags.Count);
        }


        [Fact]
        public async Task GetTags_ShouldFailWhenIdentityIsNull() {
            await Assert.ThrowsAsync<ArgumentNullException>(() => {
                return _fixture.Historian.GetTags(null, new[] { "First_Test_Tag", "Second_Test_Tag" }, CancellationToken.None);
            }).ConfigureAwait(false);
        }


        [Fact]
        public async Task GetTags_ShouldFailWhenListIsNull() {
            await Assert.ThrowsAsync<ArgumentNullException>(() => {
                return _fixture.Historian.GetTags(Identities.GetTestIdentity(), null, CancellationToken.None);
            }).ConfigureAwait(false);
        }


        [Fact]
        public async Task GetTags_ShouldFailWhenListIsEmpty() {
            await Assert.ThrowsAsync<ArgumentException>(() => {
                return _fixture.Historian.GetTags(Identities.GetTestIdentity(), new string[0], CancellationToken.None);
            }).ConfigureAwait(false);
        }


        [Fact]
        public async Task GetTags_ShouldFailWhenListContainsOnlyBlankValues() {
            await Assert.ThrowsAsync<ArgumentException>(() => {
                return _fixture.Historian.GetTags(Identities.GetTestIdentity(), new[] { "", " ", "   " }, CancellationToken.None);
            }).ConfigureAwait(false);
        }


        [Fact]
        public async Task GetTags_ShouldNotReturnDuplicates() {
            var tags = await _fixture.Historian.GetTags(Identities.GetTestIdentity(), new[] { "First_Test_Tag", "First_Test_Tag" }, CancellationToken.None).ConfigureAwait(false);
            Assert.Single(tags);
        }

    }


    public class TagSearchTestsFixture : InMemoryHistorianTestFixture {

        internal Dictionary<string, TagDefinition> TagMap = new Dictionary<string, TagDefinition>();


        public override async Task InitializeAsync() {
            await base.InitializeAsync().ConfigureAwait(false);

            TagDefinition t;

            t = await Historian.CreateTag(Identities.GetTestIdentity(),
                                       new TagSettings() {
                                           Name = "First_Test_Tag",
                                           Description = "This is the first test tag",
                                           Units = "km/h"
                                       }, CancellationToken.None).ConfigureAwait(false);

            TagMap[t.Id] = t;

            t = await Historian.CreateTag(Identities.GetTestIdentity(),
                                       new TagSettings() {
                                           Name = "Second_Test_Tag",
                                           Description = "This is the second test tag",
                                           Units = "deg C"
                                       }, CancellationToken.None).ConfigureAwait(false);

            TagMap[t.Id] = t;

        }

    }
}
