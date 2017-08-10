using System;
using System.Collections.Specialized;
using System.Web.Caching;
using Couchbase.Core;
using Couchbase.IO;

namespace Couchbase.AspNet.Caching
{
    /// <summary>
    /// A custom output-cache provider that uses Couchbase Server as the backing store. 
    /// </summary>
    /// <seealso cref="System.Web.Caching.OutputCacheProvider" />
    public class CouchbaseCacheProvider : OutputCacheProvider
    {
        private IBucket _bucket;

        public CouchbaseCacheProvider()
        {
            
        }

        public CouchbaseCacheProvider(IBucket bucket)
        {
            _bucket = bucket;
        }

        public override void Initialize(string name, NameValueCollection config)
        {
            base.Initialize(name, config);
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
        public override object Get(
            string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("'key' must be non-null, not empty or whitespace.");
            }

            // get the item
            var result = _bucket.Get<dynamic>(key);
            if (result.Success)
            {
                return result.Value;
            }
            if (result.Status == ResponseStatus.KeyNotFound)
            {
                return null;
            }
            if (result.Exception != null)
            {
                throw result.Exception;
            }
            throw new InvalidOperationException(result.Status.ToString());
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
        public override object Add(
            string key,
            object entry,
            DateTime utcExpiry)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("'key' must be non-null, not empty or whitespace.");
            }
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            //return the value if the key exists
            var exists = _bucket.Get<object>(key);
            if (exists.Success)
            {
                return exists.Value;
            }

            var expiration = DateTime.SpecifyKind(utcExpiry, DateTimeKind.Utc).TimeOfDay;

            //no key so add the value and return it.
            var result = _bucket.Insert(key, entry, expiration);
            if (result.Success)
            {
                return entry;
            }
            if (result.Exception != null)
            {
                throw result.Exception;
            }
            throw new InvalidOperationException(result.Status.ToString());
        }


        /// <summary>
        /// Inserts the specified entry into the output cache, overwriting the entry if it is already cached.
        /// </summary>
        /// <param name="key">A unique identifier for <paramref name="entry" />.</param>
        /// <param name="entry">The content to add to the output cache.</param>
        /// <param name="utcExpiry">The time and date on which the cached <paramref name="entry" /> expires.</param>
        /// <exception cref="ArgumentException">'key' must be non-null, not empty or whitespace.</exception>
        /// <exception cref="ArgumentNullException">entry</exception>
        /// <exception cref="InvalidOperationException"></exception>
        public override void Set(
            string key,
            object entry,
            DateTime utcExpiry)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("'key' must be non-null, not empty or whitespace.");
            }
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            var expiration = DateTime.SpecifyKind(utcExpiry, DateTimeKind.Utc).TimeOfDay;

            var result = _bucket.Upsert(key, entry, expiration);
            if (result.Success) return;
            if (result.Exception != null)
            {
                throw result.Exception;
            }
            throw new InvalidOperationException(result.Status.ToString());
        }

        /// <summary>
        /// Removes the specified entry from the output cache.
        /// </summary>
        /// <param name="key">The unique identifier for the entry to remove from the output cache.</param>
        /// <exception cref="NotImplementedException"></exception>
        public override void Remove(
            string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("'key' must be non-null, not empty or whitespace.");
            }

            var result = _bucket.Remove(key);
            if (result.Success) return;
            if (result.Exception != null)
            {
                throw result.Exception;
            }
            throw new InvalidOperationException(result.Status.ToString());
        }
    }
}
