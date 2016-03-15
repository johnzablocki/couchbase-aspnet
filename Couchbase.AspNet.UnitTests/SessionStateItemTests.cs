using Couchbase.AspNet.SessionState;
using Couchbase.Core;
using Couchbase.IO;
using Moq;
using NUnit.Framework;

namespace Couchbase.AspNet.UnitTests
{
    [TestFixture]
    public class SessionStateItemTests
    {
        [Test(Description = "Note there may be more test cases...")]
        [TestCase(ResponseStatus.VBucketBelongsToAnotherServer)]
        [TestCase(ResponseStatus.KeyNotFound)]
        [TestCase(ResponseStatus.Busy)]
        [TestCase(ResponseStatus.ItemNotStored)]
        [TestCase(ResponseStatus.OutOfMemory)]
        [TestCase(ResponseStatus.TemporaryFailure)]
        [TestCase(ResponseStatus.KeyExists)]
        [TestCase(ResponseStatus.ValueTooLarge)]
        public void Load_WhenNotSuccess_ReturnNull(ResponseStatus status)
        {
            //arrange
            var result = new Mock<IOperationResult<byte[]>>();
            result.Setup(x => x.Status).Returns(status);

            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.Get<byte[]>(It.IsAny<string>())).Returns(result.Object);

            //act
            var item = CouchbaseSessionStateProvider.SessionStateItem.Load(bucket.Object, "thekey", false);

            //assert
            Assert.IsNull(item);
        }
    }
}
