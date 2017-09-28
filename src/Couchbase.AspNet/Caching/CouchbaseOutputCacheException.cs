using System;

namespace Couchbase.AspNet.Caching
{
    /// <summary>
    /// Thrown when an exception is handled and rethrown if <see cref="CouchbaseOutputCacheProvider.ThrowOnError"/> is <c>true</c>.
    /// </summary>
    /// <seealso cref="System.Exception" />
    // ReSharper disable once InheritdocConsiderUsage
    public class CouchbaseOutputCacheException : Exception
    {
        public CouchbaseOutputCacheException()
        {
        }

        public CouchbaseOutputCacheException(string message)
            : base(message)
        {
        }

        public CouchbaseOutputCacheException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}

#region [ License information ]
/* ************************************************************
 *
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2017 Couchbase, Inc.
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *    http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *
 * ************************************************************/
#endregion