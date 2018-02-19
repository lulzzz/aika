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
            var initialTagCount = (await _fixture.DefaultHistorian.FindTags(Identities.GetTestIdentity(), new TagDefinitionFilter(), CancellationToken.None).ConfigureAwait(false)).Count();

            var tag = await _fixture.DefaultHistorian
                                    .CreateTag(Identities.GetTestIdentity(),
                                               new TagSettings() {
                                                   Name = nameof(CreateTag_ShouldReturnNewTag),
                                                   Description = "This is a test tag",
                                                   Units = "km/h"
                                               }, CancellationToken.None).ConfigureAwait(false);

            Assert.NotNull(tag);
            Assert.Equal(_fixture.DefaultHistorian.Historian, tag.Historian);

            var tags = await _fixture.DefaultHistorian.FindTags(Identities.GetTestIdentity(), new TagDefinitionFilter(), CancellationToken.None).ConfigureAwait(false);
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
                return _fixture.DefaultHistorian
                               .CreateTag(null,
                                           new TagSettings() {
                                               Name = nameof(CreateTag_ShouldFailWhenIdentityIsNull),
                                               Description = "This is a test tag",
                                               Units = "km/h"
                                           }, CancellationToken.None);
            }).ConfigureAwait(false);
        }


        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("     ")]
        public async Task CreateTag_ShouldFailWhenNameIsNullOrWhiteSpace(string name) {
            await Assert.ThrowsAsync<ArgumentException>(() => {
                return _fixture.DefaultHistorian
                               .CreateTag(Identities.GetTestIdentity(),
                                           new TagSettings() {
                                               Name = name,
                                               Description = "This is a test tag",
                                               Units = "km/h"
                                           }, CancellationToken.None);
            }).ConfigureAwait(false);
        }


        [Fact]
        public async Task DeleteTag_ShouldFailWhenHistorianIsNotInitialized() {
            await Assert.ThrowsAsync<InvalidOperationException>(() => {
                return _fixture.CreateHistorian().DeleteTag(Identities.GetTestIdentity(), Guid.Empty.ToString(), CancellationToken.None);
            }).ConfigureAwait(false);
        }


        [Fact]
        public async Task DeleteTag_ShouldFailWhenIdentityIsNull() {
            await Assert.ThrowsAsync<ArgumentNullException>(() => {
                return _fixture.DefaultHistorian.DeleteTag(null, Guid.Empty.ToString(), CancellationToken.None);
            }).ConfigureAwait(false);
        }


        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("     ")]
        public async Task DeleteTag_ShouldFailWhenTagIdIsNullOrWhiteSpace(string name) {
            await Assert.ThrowsAsync<ArgumentException>(() => {
                return _fixture.DefaultHistorian.DeleteTag(Identities.GetTestIdentity(), name, CancellationToken.None);
            }).ConfigureAwait(false);
        }


        [Fact]
        public async Task DeleteTag_ShouldReturnFalseWhenTagIdOrNameDoesNotExist() {
            var result = await _fixture.DefaultHistorian.DeleteTag(Identities.GetTestIdentity(), Guid.Empty.ToString(), CancellationToken.None).ConfigureAwait(false);
            Assert.False(result);
        }


        [Fact]
        public async Task DeleteTag_DeleteByTagIdShouldReturnTrue() {
            var tag = _fixture.TagMap.Values.First(x => x.Name.Equals("DeleteTag_Target"));
            var result = await _fixture.DefaultHistorian.DeleteTag(Identities.GetTestIdentity(), tag.Id, CancellationToken.None).ConfigureAwait(false);
            Assert.True(result);
        }


        [Fact]
        public async Task UpdateTag_ShouldFailWhenHistorianIsNotInitialized() {
            await Assert.ThrowsAsync<InvalidOperationException>(() => {
                return _fixture.CreateHistorian().UpdateTag(Identities.GetTestIdentity(), Guid.Empty.ToString(), new TagSettings(), nameof(UpdateTag_ShouldFailWhenHistorianIsNotInitialized), CancellationToken.None);
            }).ConfigureAwait(false);
        }


        [Fact]
        public async Task UpdateTag_ShouldFailWhenIdentityIsNull() {
            await Assert.ThrowsAsync<ArgumentNullException>(() => {
                return _fixture.DefaultHistorian.UpdateTag(null, Guid.Empty.ToString(), new TagSettings(), nameof(UpdateTag_ShouldFailWhenIdentityIsNull), CancellationToken.None);
            }).ConfigureAwait(false);
        }


        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("     ")]
        public async Task UpdateTag_ShouldFailWhenTagIdIsNullOrWhiteSpace(string name) {
            await Assert.ThrowsAsync<ArgumentException>(() => {
                return _fixture.DefaultHistorian.UpdateTag(Identities.GetTestIdentity(), name, new TagSettings(), nameof(UpdateTag_ShouldFailWhenTagIdIsNullOrWhiteSpace), CancellationToken.None);
            }).ConfigureAwait(false);
        }


        [Fact]
        public async Task UpdateTag_ShouldFailWhenTagSettingsAreNull() {
            await Assert.ThrowsAsync<ArgumentNullException>(() => {
                return _fixture.DefaultHistorian.UpdateTag(Identities.GetTestIdentity(), Guid.Empty.ToString(), null, nameof(UpdateTag_ShouldFailWhenTagIdDoesNotExist), CancellationToken.None);
            }).ConfigureAwait(false);
        }


        [Fact]
        public async Task UpdateTag_ShouldFailWhenTagIdDoesNotExist() {
            await Assert.ThrowsAsync<ArgumentException>(() => {
                return _fixture.DefaultHistorian.UpdateTag(Identities.GetTestIdentity(), Guid.Empty.ToString(), new TagSettings(), nameof(UpdateTag_ShouldFailWhenTagIdDoesNotExist), CancellationToken.None);
            }).ConfigureAwait(false);
        }


        [Fact]
        public async Task UpdateTag_DescriptionShouldBeUpdated() {
            var tag = _fixture.TagMap.Values.First(x => x.Name.Equals("UpdateTag_Target"));
            var newDescription = nameof(UpdateTag_DescriptionShouldBeUpdated);

            var updatedTag = await _fixture.DefaultHistorian.UpdateTag(Identities.GetTestIdentity(), tag.Id, new TagSettings() { Description = newDescription }, nameof(UpdateTag_ShouldFailWhenTagIdDoesNotExist), CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(newDescription, updatedTag.Description);
        }

    }


    public class TagManagementTestsFixture : InMemoryHistorianTestFixture {

        internal Dictionary<string, TagDefinition> TagMap = new Dictionary<string, TagDefinition>();


        public override async Task InitializeAsync() {
            await base.InitializeAsync().ConfigureAwait(false);

            TagDefinition t;

            t = await DefaultHistorian.CreateTag(Identities.GetTestIdentity(),
                                       new TagSettings() {
                                           Name = "DeleteTag_Target",
                                           Description = "This is the first test tag",
                                           Units = "km/h"
                                       }, CancellationToken.None).ConfigureAwait(false);

            TagMap[t.Id] = t;

            t = await DefaultHistorian.CreateTag(Identities.GetTestIdentity(),
                                       new TagSettings() {
                                           Name = "UpdateTag_Target",
                                           Description = "This is the second test tag",
                                           Units = "deg C"
                                       }, CancellationToken.None).ConfigureAwait(false);

            TagMap[t.Id] = t;
        }

    }
}
