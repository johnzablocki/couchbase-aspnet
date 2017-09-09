using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Couchbase.AspNet.Caching
{
    /// <summary>
    /// Thrown when an exception is handled and rethrown if <see cref="CouchbaseCacheProvider.ThrowOnError"/> is <c>true</c>.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class CouchbaseCacheException : Exception
    {
        public CouchbaseCacheException()
        {
        }

        public CouchbaseCacheException(string message)
            : base(message)
        {
        }

        public CouchbaseCacheException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
