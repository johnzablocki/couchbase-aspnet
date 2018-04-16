using System;
using System.Threading;
using System.Web;
using Couchbase.AspNet.Session;
using Couchbase.Core;
using Couchbase.IO;
using Moq;
using Xunit;

namespace Couchbase.AspNet.UnitTests
{
    public class CouchbaseSessionStateProviderAsyncTests
    {
        #if NET462
        [Fact]
        public void Deserialize_When_Buffer_Is_Null_Returns_Empty_List()
        {
            var result = new Mock<IOperationResult<SessionStateItem>>();
            result.Setup(x => x.Success).Returns(true);
            result.Setup(x => x.Status).Returns(ResponseStatus.Success);

            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.Insert("testId", It.IsAny<SessionStateItem>(), It.IsAny<TimeSpan>())).
                Returns(result.Object);

            var provider = new CouchbaseSessionStateProviderAsync {Bucket = bucket.Object, ThrowOnError = true};
            var sessionStateItemCollection = provider.Deserialize(null);
            Assert.NotNull(sessionStateItemCollection);
            Assert.Equal(0, sessionStateItemCollection.Count);
        }


        [Fact]
        public void ReleaseItemExclusiveAsync_When_LockId_Is_Null_Do_Not_Throw_Exception()
        {
            var result = new Mock<IOperationResult<SessionStateItem>>();
            result.Setup(x => x.Success).Returns(true);
            result.Setup(x => x.Status).Returns(ResponseStatus.Success);

            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.Insert("testId", It.IsAny<SessionStateItem>(), It.IsAny<TimeSpan>())).
                Returns(result.Object);

            var provider = new CouchbaseSessionStateProviderAsync {Bucket = bucket.Object, ThrowOnError = true};
            var sessionStateItemCollection =
                provider.ReleaseItemExclusiveAsync(new Mock<HttpContextBase>().Object, "theid", null, new CancellationToken(false));

        }
        #endif
    }
}