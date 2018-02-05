using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aika.Tags;
using Xunit;

namespace Aika.Tests {
    public class TagManagementTests : IClassFixture<TagManagementTestsFixture> {

        private readonly TagManagementTestsFixture _fixture;


        public TagManagementTests(TagManagementTestsFixture fixture) {
            _fixture = fixture;
        }


        [Fact]
        public async Task CreateTag_ShouldReturnNewTag() {
            var initialTagCount = (await _fixture.Historian.FindTags(Identities.GetTestIdentity(), new TagDefinitionFilter(), CancellationToken.None).ConfigureAwait(false)).Count();

            var tag = await _fixture.Historian
                                    .CreateTag(Identities.GetTestIdentity(),
                                               new TagSettings() {
                                                   Name = nameof(CreateTag_ShouldReturnNewTag),
                                                   Description = "This is a test tag",
                                                   Units = "km/h"
                                               }, CancellationToken.None).ConfigureAwait(false);

            Assert.NotNull(tag);
            Assert.Equal(_fixture.Historian.Historian, tag.Historian);

            var tags = await _fixture.Historian.FindTags(Identities.GetTestIdentity(), new TagDefinitionFilter(), CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(initialTagCount + 1, tags.Count());
        }


        [Fact]
        public async Task CreateTag_ShouldFailWhenHistorianIsNotInitialized() {
            await Assert.ThrowsAsync<InvalidOperationException>(() => {
                return _fixture.CreateHistorian()
                               .CreateTag(Identities.GetTestIdentity(),
                                           new TagSettings() {
                                               Name = nameof(CreateTag_ShouldFailWhenHistorianIsNotInitialized),
                                               Description = "This is a test tag",
                                               Units = "km/h"
                                           }, CancellationToken.None);
            }).ConfigureAwait(false);
        }


        [Fact]
        public async Task CreateTag_ShouldFailWhenIdentityIsNull() {
            await Assert.ThrowsAsync<ArgumentNullException>(() => {
                return _fixture.Historian
                               .CreateTag(null,
                                           new TagSettings() {
                                               Name = nameof(CreateTag_ShouldFailWhenIdentityIsNull),
                                               Description = "This is a test tag",
                                               Units = "km/h"
                                           }, CancellationToken.None);
            }).ConfigureAwait(false);
        }


    }


    public class TagManagementTestsFixture : InMemoryHistorianTestFixture { }
}
