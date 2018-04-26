using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Couchbase.AspNet.UnitTests
{
    public class CouchbaseWebProviderExtensionsTests
    {
        [InlineData("xxx", "mysession", "xxx-mysession")]
        [InlineData(null, "mysession", "mysession")]
        [Theory]
        public void Test_PrefixIdentifier(string prefix, string sessionId, string expected)
        {
            var provider = new Mock<ICouchbaseWebProvider>();
            provider.Setup(x => x.Prefix).Returns(prefix);

            var actual = provider.Object.PrefixIdentifier(sessionId);
            Assert.Equal(expected, actual);
        }

        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [Theory]
        public void Test_PrefixIdentifier_WhenNullOrWhiteSpace_ThrowException(string sessionId)
        {
            var provider = new Mock<ICouchbaseWebProvider>();
            Assert.Throws<ArgumentNullException>(() => provider.Object.PrefixIdentifier(sessionId));
        }
    }
}
