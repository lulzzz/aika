using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aika.StateSets;
using Aika.Tags;
using Xunit;

namespace Aika.Tests {
    public class TagDataWriteTests : IClassFixture<TagDataWriteTestsFixture> {

        private readonly TagDataWriteTestsFixture _fixture;


        public TagDataWriteTests(TagDataWriteTestsFixture fixture) {
            _fixture = fixture;
        }


        [Fact]
        public async Task WriteSingleFloatValue_ShouldUpdateSnapshotValue() {
            var tag = _fixture.TagMap.Values.First(x => x.Name.Equals("WriteSingleFloatValue_Target"));
            var values = new[] {
                new TagValue(DateTime.UtcNow, 100, null, TagValueQuality.Good, null)
            };

            var writeResult = await _fixture.DefaultHistorian.WriteTagData(Identities.GetTestIdentity(), new Dictionary<string, IEnumerable<TagValue>>() {
                { tag.Name, values }
            }, CancellationToken.None).ConfigureAwait(false);

            Assert.Single(writeResult);
            Assert.Contains(writeResult, x => x.Key.Equals(tag.Name));
            Assert.Equal(1, writeResult.First().Value.SampleCount);
            Assert.Equal(values.First().UtcSampleTime, writeResult.First().Value.UtcEarliestSampleTime);
            Assert.Equal(values.First().UtcSampleTime, writeResult.First().Value.UtcEarliestSampleTime);
            Assert.Equal(values.First(), tag.ReadSnapshotValue(Identities.GetTestIdentity()));
        }


        [Fact]
        public async Task WriteMultipleFloatValues_ShouldUpdateSnapshotValueAndRecordHistory() {
            var tag = _fixture.TagMap.Values.First(x => x.Name.Equals("WriteMultipleFloatValues_Target"));
            var values = Enumerable.Range(0, 10).Select(x => new TagValue(DateTime.UtcNow.AddHours(-1 * x), 100 - x, null, TagValueQuality.Good, null)).OrderBy(x => x.UtcSampleTime).ToArray();

            var writeResult = await _fixture.DefaultHistorian.WriteTagData(Identities.GetTestIdentity(), new Dictionary<string, IEnumerable<TagValue>>() {
                { tag.Name, values }
            }, CancellationToken.None).ConfigureAwait(false);

            Assert.Single(writeResult);
            Assert.Contains(writeResult, x => x.Key.Equals(tag.Name));
            Assert.Equal(values.Length, writeResult.First().Value.SampleCount);
            Assert.Equal(values.First().UtcSampleTime, writeResult.First().Value.UtcEarliestSampleTime);
            Assert.Equal(values.Last().UtcSampleTime, writeResult.First().Value.UtcLatestSampleTime);
        }

    }


    public class TagDataWriteTestsFixture : InMemoryHistorianTestFixture {

        internal StateSet TestStateSet;

        internal Dictionary<string, TagDefinition> TagMap = new Dictionary<string, TagDefinition>();


        public override async Task InitializeAsync() {
            await base.InitializeAsync().ConfigureAwait(false);

            TestStateSet = await DefaultHistorian.CreateStateSet(Identities.GetTestIdentity(), new StateSetSettings() {
                Name = "TestStateSet",
                Description = "Contains states used in tag data write tests",
                States = new [] {
                    new StateSetItem("OFF", 0),
                    new StateSetItem("ON", 1)
                }
            }, CancellationToken.None).ConfigureAwait(false);

            TagDefinition t;

            t = await DefaultHistorian.CreateTag(Identities.GetTestIdentity(),
                                       new TagSettings() {
                                           Name = "WriteSingleFloatValue_Target",
                                           DataType = TagDataType.FloatingPoint
                                       }, CancellationToken.None).ConfigureAwait(false);

            TagMap[t.Id] = t;

            t = await DefaultHistorian.CreateTag(Identities.GetTestIdentity(),
                                       new TagSettings() {
                                           Name = "WriteMultipleFloatValues_Target",
                                           DataType = TagDataType.FloatingPoint
                                       }, CancellationToken.None).ConfigureAwait(false);

            TagMap[t.Id] = t;

            t = await DefaultHistorian.CreateTag(Identities.GetTestIdentity(),
                                       new TagSettings() {
                                           Name = "WriteSingleIntValue_Target",
                                           DataType = TagDataType.Integer
                                       }, CancellationToken.None).ConfigureAwait(false);

            TagMap[t.Id] = t;

            t = await DefaultHistorian.CreateTag(Identities.GetTestIdentity(),
                                       new TagSettings() {
                                           Name = "WriteMultipleIntValues_Target",
                                           DataType = TagDataType.Integer
                                       }, CancellationToken.None).ConfigureAwait(false);

            TagMap[t.Id] = t;

            t = await DefaultHistorian.CreateTag(Identities.GetTestIdentity(),
                                       new TagSettings() {
                                           Name = "WriteSingleTextValue_Target",
                                           DataType = TagDataType.Text
                                       }, CancellationToken.None).ConfigureAwait(false);

            TagMap[t.Id] = t;

            t = await DefaultHistorian.CreateTag(Identities.GetTestIdentity(),
                                       new TagSettings() {
                                           Name = "WriteMultipleTextValues_Target",
                                           DataType = TagDataType.Text
                                       }, CancellationToken.None).ConfigureAwait(false);

            TagMap[t.Id] = t;

            t = await DefaultHistorian.CreateTag(Identities.GetTestIdentity(),
                                       new TagSettings() {
                                           Name = "WriteSingleStateValue_Target",
                                           StateSet = TestStateSet.Name,
                                           DataType = TagDataType.State
                                       }, CancellationToken.None).ConfigureAwait(false);

            TagMap[t.Id] = t;

            t = await DefaultHistorian.CreateTag(Identities.GetTestIdentity(),
                                       new TagSettings() {
                                           Name = "WriteMultipleStateValues_Target",
                                           StateSet = TestStateSet.Name,
                                           DataType = TagDataType.State
                                       }, CancellationToken.None).ConfigureAwait(false);

            TagMap[t.Id] = t;
        }

    }
}
