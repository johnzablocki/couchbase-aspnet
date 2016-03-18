using System;
using System.Web;
using System.Web.SessionState;
using Couchbase.AspNet.SessionState;
using Couchbase.Core;
using Couchbase.IO;
using Moq;
using NUnit.Framework;

namespace Couchbase.AspNet.UnitTests.SessionState
{
    [TestFixture()]
    public class CouchbaseSessionStateProviderTests
    {
        [Test()]
        public void GetSessionStoreItem_WhenKeyNotFound_ReturnsNull()
        {
            //arrange
            var result = new Mock<IOperationResult<byte[]>>();
            result.Setup(x => x.Status).Returns(ResponseStatus.KeyNotFound);
            result.Setup(x => x.Value).Returns(new byte[0]);

            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.Get<byte[]>(It.IsAny<string>())).Returns(result.Object);         

            bool locked;
            TimeSpan lockAge;
            object lockId;
            SessionStateActions actions;

            //Act
            var sessionStateItem = CouchbaseSessionStateProvider.GetSessionStoreItem(
               bucket.Object, null, false, "thekey", out locked, out lockAge, out lockId, out actions);

            //Assert
            Assert.IsNull(sessionStateItem);
        }

        [Test()]
        public void SetAndReleaseItemExclusive_WhenKeyNotFound_ReturnsNull()
        {
            //arrange
            var result = new Mock<IOperationResult<byte[]>>();
            result.Setup(x => x.Status).Returns(ResponseStatus.KeyNotFound);
            result.Setup(x => x.Value).Returns(new byte[0]);

            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.Get<byte[]>(It.IsAny<string>())).Returns(result.Object);

            var provider = new CouchbaseSessionStateProvider(new Mock<ICluster>().Object, bucket.Object);

            bool locked;
            TimeSpan lockAge;
            object lockId = 10ul;
            SessionStateActions actions;

            //Act
            provider.SetAndReleaseItemExclusive(null, "thekey", new SessionStateStoreData(new SessionStateItemCollection(), new HttpStaticObjectsCollection(), 10), lockId, false);



        }
    }
}