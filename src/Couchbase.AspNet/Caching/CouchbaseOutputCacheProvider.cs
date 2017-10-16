using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web.Caching;
using Common.Logging;
using Couchbase.Authentication;
using Couchbase.Configuration.Client;
using Couchbase.Core;
using Couchbase.IO;

namespace Couchbase.AspNet.Caching
{
    /// <summary>
    /// A custom output-cache provider that uses Couchbase Server as the backing store.
    /// </summary>
    /// <seealso cref="System.Web.Caching.OutputCacheProvider" />
    // ReSharper disable once InheritdocConsiderUsage
    public class CouchbaseOutputCacheProvider : OutputCacheProvider, ICouchbaseWebProvider
    {
        private readonly object _syncObj = new object();
        private readonly ILog _log = LogManager.GetLogger<CouchbaseOutputCacheProvider>();
        private const string EmptyKeyMessage = "'key' must be non-null, not empty or whitespace.";
        public IBucket Bucket { get; set; }
        public bool ThrowOnError { get; set; }
        public string Prefix { get; set; }
        public string BucketName { get; set; }

        public CouchbaseOutputCacheProvider(){ }

        public CouchbaseOutputCacheProvider(IBucket bucket)
        {
            Bucket = bucket;
        }

        public override void Initialize(string name, NameValueCollection config)
        {
            base.Initialize(name, config);
            lock(_syncObj)
            {
                var bootStapper = new BootStrapper();
                bootStapper.Bootstrap(name, config, this);
            }
        }

        /// <summary>
        /// Returns a reference to the specified entry in the output cache.
        /// </summary>
        /// <param name="key">A unique identifier for a cached entry in the output cache.</param>
        /// <returns>
        /// The <paramref name="key" /> value that identifies the specified entry in the cache, or null if the specified entry is not in the cache.
        /// </returns>
        /// <exception cref="ArgumentException">'key' must be non-null, not empty or whitespace.</exception>
        /// <exception cref="InvalidOperationException"></exception>
        public override object Get(string key)
        {
            _log.Debug("Cache.Get(" + key + ")");
            CheckKey(key);

            try
            {
                // get the item
                var result = Bucket.Get<object>(key);
                if (result.Success)
                {
                    return result.Value;
                }
                if (result.Status == ResponseStatus.KeyNotFound)
                {
                    return null;
                }
                LogAndOrThrow(result, key);
            }
            catch (Exception e)
            {
                LogAndOrThrow(e, key);
            }
            return null;
        }

        /// <summary>
        /// Inserts the specified entry into the output cache.
        /// </summary>
        /// <param name="key">A unique identifier for <paramref name="entry" />.</param>
        /// <param name="entry">The content to add to the output cache.</param>
        /// <param name="utcExpiry">The time and date on which the cached entry expires.</param>
        /// <returns>
        /// A reference to the specified provider.
        /// </returns>
        /// <exception cref="ArgumentException">'key' must be non-null, not empty or whitespace.</exception>
        /// <exception cref="ArgumentNullException">entry</exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <remarks>If there is already a value in the cache for the specified key, the provider must return
        /// that value. The provider must not store the data passed by using the Add method parameters. The
        /// Add method stores the data if it is not already in the cache. If the data is in the cache, the
        /// Add method returns it.</remarks>
         public override object Add(string key, object entry, DateTime utcExpiry)
         {
             _log.Debug("Cache.Add(" + key + ", " + entry + ", " + utcExpiry + ")");
             CheckKey(key);

             try
             {
                 //return the value if the key exists
                 var exists = Bucket.Get<object>(key);
                 if (exists.Success)
                 {
                     return exists.Value;
                 }

                 var expiration = utcExpiry - DateTime.Now.ToUniversalTime();

                 //no key so add the value and return it.
                 var result = Bucket.Insert(key, entry, expiration);
                 if (result.Success)
                 {
                     return entry;
                 }
                 LogAndOrThrow(result, key);
             }
             catch (Exception e)
             {
                 LogAndOrThrow(e, key);
             }
             return null;
         }

        /// <summary>
        /// Inserts the specified entry into the output cache, overwriting the entry if it is already cached.
        /// </summary>
        /// <param name="key">A unique identifier for <paramref name="entry" />.</param>
        /// <param name="entry">The content to add to the output cache.</param>
        /// <param name="utcExpiry">The time and date on which the cached <paramref name="entry" /> expires.</param>
        /// <exception cref="ArgumentException">'key' must be non-null, not empty or whitespace.</exception>
        /// <exception cref="InvalidOperationException"></exception>
        public override void Set(string key, object entry, DateTime utcExpiry)
        {
            _log.Debug("Cache.Set(" + key + ", " + entry + ", " + utcExpiry + ")");
            CheckKey(key);

            try
            {
                var expiration = utcExpiry - DateTime.Now.ToUniversalTime();

                var result = Bucket.Upsert(key, entry, expiration);
                if (result.Success) return;
                LogAndOrThrow(result, key);
            }
            catch (Exception e)
            {
                LogAndOrThrow(e, key);
            }
        }

        /// <summary>
        /// Removes the specified entry from the output cache.
        /// </summary>
        /// <param name="key">The unique identifier for the entry to remove from the output cache.</param>
        public override void Remove(string key)
        {
           _log.Debug("Cache.Remove(" + key + ")");
            CheckKey(key);

            try
            {
                var result = Bucket.Remove(key);
                if (result.Success) return;
                LogAndOrThrow(result, key);
            }
            catch (Exception e)
            {
                LogAndOrThrow(e, key);
            }
        }

        /// <summary>
        /// Logs the reason why an operation fails and throws and exception if <see cref="ThrowOnError"/> is
        /// <c>true</c> and logging the issue as WARN.
        /// </summary>
        /// <param name="e">The e.</param>
        /// <param name="key">The key.</param>
        /// <exception cref="Couchbase.AspNet.Caching.CouchbaseOutputCacheException"></exception>
        private void LogAndOrThrow(Exception e, string key)
        {
            _log.Error($"Could not retrieve, remove or write key '{key}' - reason: {e}");
            if (ThrowOnError)
            {
                throw new CouchbaseOutputCacheException($"Could not retrieve, remove or write key '{key}'", e);
            }
        }

        /// <summary>
        /// Logs the reason why an operation fails and throws and exception if <see cref="ThrowOnError"/> is
        /// <c>true</c> and logging the issue as WARN.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="key">The key.</param>
        /// <exception cref="InvalidOperationException"></exception>
        private void LogAndOrThrow(IOperationResult result, string key)
        {
            if (result.Exception != null)
            {
                LogAndOrThrow(result.Exception, key);
                return;
            }
            _log.Error($"Could not retrieve, remove or write key '{key}' - reason: {result.Status}");
            if (ThrowOnError)
            {
                throw new InvalidOperationException(result.Status.ToString());
            }
        }

        /// <summary>
        /// Checks the key to ensure its not null, empty or a blank space, throwing an exception
        /// if <see cref="ThrowOnError"/> is <c>true</c> and logging the issue as WARN.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <exception cref="ArgumentException"></exception>
        private void CheckKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                if (ThrowOnError) throw new ArgumentException(EmptyKeyMessage);
                _log.Warn(EmptyKeyMessage);
            }
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