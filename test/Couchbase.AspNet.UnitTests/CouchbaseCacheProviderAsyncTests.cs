using System;
using System.Threading.Tasks;
using Couchbase.AspNet.Caching;
using Couchbase.Core;
using Couchbase.IO;
using Couchbase.IO.Operations;
using Moq;
using Xunit;

namespace Couchbase.AspNet.UnitTests
{
    public class CouchbaseCacheProviderAsyncTests
    {
        #region Get tests

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData(" ", true)]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData(" ", false)]
        public void Get_When_Key_IsNullEmptyOrSpace_DoNot_Throw_ArgumentException_If_ThrowOnError(string key,
            bool throwOnError)
        {
            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.Get<dynamic>(It.IsAny<string>())).Throws<MissingKeyException>();

            var provider = new CouchbaseCacheProviderAsync(bucket.Object)
            {
                ThrowOnError = throwOnError
            };
            if (throwOnError)
            {
                Assert.Throws<ArgumentException>(() => provider.Get(key));
            }
            else
            {
                var result = provider.Get(key);
                Assert.Null(result);
            }
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData(" ", true)]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData(" ", false)]
        public async Task GetAsync_When_Key_IsNullEmptyOrSpace_DoNot_Throw_ArgumentException_If_ThrowOnError(string key,
            bool throwOnError)
        {
            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.GetAsync<dynamic>(It.IsAny<string>())).Throws<MissingKeyException>();

            var provider = new CouchbaseCacheProviderAsync(bucket.Object)
            {
                ThrowOnError = throwOnError
            };
            if (throwOnError)
            {
                await Assert.ThrowsAsync<ArgumentException>(() => provider.GetAsync(key));
            }
            else
            {
                var result = provider.Get(key);
                Assert.Null(result);
            }
        }

        [Theory]
        [InlineData(true)]
        public async Task GetAsync_When_Key_DoesNotExist_Return_Null(bool throwOnError)
        {
            var result = new Mock<IOperationResult<dynamic>>();
            result.Setup(x => x.Success).Returns(false);
            result.Setup(x => x.Status).Returns(ResponseStatus.KeyNotFound);

            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.GetAsync<dynamic>(It.IsAny<string>())).Returns(Task.FromResult(result.Object));

            var provider = new CouchbaseCacheProviderAsync(bucket.Object) { ThrowOnError = throwOnError };
            Assert.Null(await provider.GetAsync("thekey"));
        }

        [Theory]
        [InlineData(true)]
        public void Get_When_Key_DoesNotExist_Return_Null(bool throwOnError)
        {
            var result = new Mock<IOperationResult<dynamic>>();
            result.Setup(x => x.Success).Returns(false);
            result.Setup(x => x.Status).Returns(ResponseStatus.KeyNotFound);

            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.Get<dynamic>(It.IsAny<string>())).Returns(result.Object);

            var provider = new CouchbaseCacheProviderAsync(bucket.Object) { ThrowOnError = throwOnError };
            Assert.Null(provider.Get("thekey"));
        }

        [Fact]
        public async Task GetAsync_When_Operation_Causes_Exception_Throw_Exception()
        {
            var result = new Mock<IOperationResult<dynamic>>();
            result.Setup(x => x.Success).Returns(false);
            result.Setup(x => x.Status).Returns(ResponseStatus.ClientFailure);
            result.Setup(x => x.Exception).Returns(new CouchbaseCacheException());

            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.Get<dynamic>(It.IsAny<string>())).Returns(result.Object);

            var provider = new CouchbaseCacheProviderAsync(bucket.Object) {ThrowOnError = true};
            await Assert.ThrowsAsync<CouchbaseCacheException>(() => provider.GetAsync("thekey"));
        }

        [Fact]
        public void Get_When_Operation_Causes_Exception_Throw_Exception()
        {
            var result = new Mock<IOperationResult<dynamic>>();
            result.Setup(x => x.Success).Returns(false);
            result.Setup(x => x.Status).Returns(ResponseStatus.ClientFailure);
            result.Setup(x => x.Exception).Returns(new CouchbaseCacheException());

            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.Get<dynamic>(It.IsAny<string>())).Returns(result.Object);

            var provider = new CouchbaseCacheProviderAsync(bucket.Object) { ThrowOnError = true };
            Assert.Throws<CouchbaseCacheException>(() => provider.Get("thekey"));
        }

        [Fact]
        public void Get_When_Operation_Causes_Exception_Throw_CouchbaseCacheException()
        {
            var result = new Mock<IOperationResult<dynamic>>();
            result.Setup(x => x.Success).Returns(false);
            result.Setup(x => x.Status).Returns(ResponseStatus.OutOfMemory);

            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.Get<dynamic>(It.IsAny<string>())).Returns(result.Object);

            var provider = new CouchbaseCacheProviderAsync(bucket.Object) {ThrowOnError = true};
            Assert.Throws<CouchbaseCacheException>(() => provider.Get("thekey"));
        }

        [Fact]
        public async Task GetAsync_When_Operation_Causes_Exception_Throw_CouchbaseCacheException()
        {
            var result = new Mock<IOperationResult<dynamic>>();
            result.Setup(x => x.Success).Returns(false);
            result.Setup(x => x.Status).Returns(ResponseStatus.OutOfMemory);

            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.Get<dynamic>(It.IsAny<string>())).Returns(result.Object);

            var provider = new CouchbaseCacheProviderAsync(bucket.Object) { ThrowOnError = true };
            await Assert.ThrowsAsync<CouchbaseCacheException>(() => provider.GetAsync("thekey"));
        }

        #endregion

        #region Set tests

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData(" ", true)]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData(" ", false)]
        public void Set_When_Key_IsNullEmptyOrSpace_Throw_ArgumentException(string key, bool throwOneError)
        {
            var provider = new CouchbaseCacheProviderAsync(null) {ThrowOnError = throwOneError};

            if (throwOneError)
            {
                Assert.Throws<ArgumentException>(() => provider.Set(null, null, DateTime.Now));
            }
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData(" ", true)]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData(" ", false)]
        public async Task SetAsync_When_Key_IsNullEmptyOrSpace_Throw_ArgumentException(string key, bool throwOneError)
        {
            var provider = new CouchbaseCacheProviderAsync(null) { ThrowOnError = throwOneError };

            if (throwOneError)
            {
                await Assert.ThrowsAsync<ArgumentException>(() => provider.SetAsync(null, null, DateTime.Now));
            }
        }

        [Fact]
        public void Set_When_Operation_Causes_Exception_Throw_CouchbaseCacheException()
        {
            var result = new Mock<IOperationResult<object>>();
            result.Setup(x => x.Success).Returns(false);
            result.Setup(x => x.Status).Returns(ResponseStatus.OperationTimeout);
            result.Setup(x => x.Exception).Returns(new TimeoutException());

            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.Upsert(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>()))
                .Returns(result.Object);

            var provider = new CouchbaseCacheProviderAsync(bucket.Object) {ThrowOnError = true};
            Assert.Throws<CouchbaseCacheException>(() => provider.Set("thekey", new object(), DateTime.Now));
        }

        [Fact]
        public async Task SetAsync_When_Operation_Causes_Exception_Throw_CouchbaseCacheException()
        {
            var result = new Mock<IOperationResult<object>>();
            result.Setup(x => x.Success).Returns(false);
            result.Setup(x => x.Status).Returns(ResponseStatus.OperationTimeout);
            result.Setup(x => x.Exception).Returns(new TimeoutException());

            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.UpsertAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan>()))
                .Returns(Task.FromResult(result.Object));

            var provider = new CouchbaseCacheProviderAsync(bucket.Object) { ThrowOnError = true };
            await Assert.ThrowsAsync<CouchbaseCacheException>(() => provider.SetAsync("thekey", new object(), DateTime.Now));
        }

        [Fact]
        public void Set_When_Operation_Fails_Throw_CouchbaseCacheException()
        {
            var result = new Mock<IOperationResult<dynamic>>();
            result.Setup(x => x.Success).Returns(false);
            result.Setup(x => x.Status).Returns(ResponseStatus.OutOfMemory);

            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.Upsert<dynamic>(It.IsAny<string>(), "thevalue")).Returns(result.Object);

            var provider = new CouchbaseCacheProviderAsync(bucket.Object) {ThrowOnError = true};
            Assert.Throws<CouchbaseCacheException>(() => provider.Set("thekey", "thevalue", DateTime.MaxValue));
        }

        #endregion

        #region Add tests

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Add_When_Operation_Fails_Throw_CouchbaseCacheException(bool throwOnError)
        {
            var result = new Mock<IOperationResult<dynamic>>();
            result.Setup(x => x.Success).Returns(false);
            result.Setup(x => x.Status).Returns(ResponseStatus.OutOfMemory);

            var get = new Mock<IOperationResult<dynamic>>();
            result.Setup(x => x.Success).Returns(false);
            result.Setup(x => x.Status).Returns(ResponseStatus.KeyNotFound);

            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.Get<dynamic>(It.IsAny<string>())).Returns(get.Object);
            bucket.Setup(x => x.Insert<dynamic>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .Returns(result.Object);

            var provider = new CouchbaseCacheProviderAsync(bucket.Object) {ThrowOnError = throwOnError};

            if (throwOnError)
            {
                Assert.Throws<CouchbaseCacheException>(() => provider.Add("thekey", "thevalue", DateTime.MaxValue));
            }
            else
            {
                Assert.Null(provider.Add("thekey", "thevalue", DateTime.MaxValue));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Add_When_Key_DoesNotExst_Inserts_Value(bool throwOnError)
        {
            var get = new Mock<IOperationResult<dynamic>>();
            get.Setup(x => x.OpCode).Returns(OperationCode.Get);
            get.Setup(x => x.Success).Returns(false);
            get.Setup(x => x.Status).Returns(ResponseStatus.KeyNotFound);

            var insert = new Mock<IOperationResult<dynamic>>();
            insert.Setup(x => x.OpCode).Returns(OperationCode.Add);
            insert.Setup(x => x.Success).Returns(true);
            insert.Setup(x => x.Status).Returns(ResponseStatus.Success);
            insert.Setup(x => x.Value).Returns("value");

            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.GetAsync<dynamic>(It.IsAny<string>())).Returns(Task.FromResult(get.Object)).Verifiable();
            bucket.Setup(x => x.InsertAsync<dynamic>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .Returns(Task.FromResult(insert.Object)).Verifiable();

            var provider = new CouchbaseCacheProviderAsync(bucket.Object) {ThrowOnError = throwOnError};
            var val = provider.AddAsync("key", "value", DateTime.MaxValue);
            Assert.NotNull(val);
            bucket.Verify();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Add_When_Key_Exists_Gets_Value(bool throwOnError)
        {
            var get = new Mock<IOperationResult<dynamic>>();
            get.Setup(x => x.OpCode).Returns(OperationCode.Get);
            get.Setup(x => x.Success).Returns(true);
            get.Setup(x => x.Status).Returns(ResponseStatus.Success);
            get.Setup(x => x.Value).Returns("value");

            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.GetAsync<dynamic>(It.IsAny<string>())).Returns(Task.FromResult(get.Object)).Verifiable();

            var provider = new CouchbaseCacheProviderAsync(bucket.Object) {ThrowOnError = throwOnError};
            var val = provider.AddAsync("key", "value", DateTime.MaxValue);
            Assert.NotNull(val);
            bucket.Verify();
        }

        #endregion

        #region Remove tests

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Remove_When_Operation_Fails_Throw_CouchbaseCacheException(bool throwOnError)
        {
            var result = new Mock<IOperationResult<dynamic>>();
            result.Setup(x => x.Success).Returns(false);
            result.Setup(x => x.Status).Returns(ResponseStatus.KeyNotFound);

            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.Remove(It.IsAny<string>())).Returns(result.Object);

            var provider = new CouchbaseCacheProviderAsync(bucket.Object) {ThrowOnError = throwOnError};

            if (throwOnError)
            {
                Assert.Throws<CouchbaseCacheException>(() => provider.Remove("thekey"));
            }
            else
            {
                provider.Remove("thekey");
            }

        }
        #endregion

        [Fact]
        public void Test()
        {
            var result = Convert.ToBoolean("false");
            Assert.True(result);
        }
    }
}
