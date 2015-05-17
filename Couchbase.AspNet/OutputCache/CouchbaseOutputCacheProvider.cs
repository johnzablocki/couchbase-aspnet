using System;
using System.Text;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web.Caching;
using Couchbase.Core;

namespace Couchbase.AspNet.OutputCache
{
    public class CouchbaseOutputCacheProvider : OutputCacheProvider
    {
        private IBucket _bucket;

        /// <summary>
        /// Defines the prefix for the actual cache data stored in the Couchbase bucket. Must be unique to ensure it does not conflict with 
        /// other applications that might be using the Couchbase bucket.
        /// </summary>
        private static readonly string _prefix =
            (System.Web.Hosting.HostingEnvironment.SiteName ?? string.Empty).Replace(" ", "-") + "+" +
            System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath + "cache-";

        /// <summary>
        /// Function to initialize the provider
        /// </summary>
        /// <param name="name">Name of the element in the configuration file</param>
        /// <param name="config">Configuration values for the provider from the Web.config file</param>
        public override void Initialize(
            string name,
            NameValueCollection config)
        {
            // Initialize the base class
            base.Initialize(name, config);

            // Create our Couchbase bucket instance
            _bucket = ProviderHelper.GetBucket(name, config);

            // Make sure no extra attributes are included
            ProviderHelper.CheckForUnknownAttributes(config);
        }

        /// <summary>
        /// Function to sanitize the key for use with Couchbase. We simply convert it to a Base 64 representation so that it will be unique and will allow
        /// encoding of any URL
        /// </summary>
        /// <param name="key">Key to sanitize</param>
        /// <returns>Sanitized key</returns>
        private static string SanitizeKey(
            string key)
        {
            return _prefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(key), Base64FormattingOptions.None);
        }

        /// <summary>
        /// Convert a UTC expiration date/time value into a timespan
        /// </summary>
        /// <param name="utcExpiry">The time and date on which the cached entry expires</param>
        /// <returns>Timespan relative to the current time</returns>
        private static uint ToExpiration(
            DateTime utcExpiry)
        {
            return (uint)(DateTime.SpecifyKind(utcExpiry, DateTimeKind.Utc) - DateTime.UtcNow).TotalSeconds;
        }

        /// <summary>
        /// Function to add a new item to the output cache. If there is already a value in the cache for the 
        /// specified key, the provider must return that value and must not store the data passed by using the Add method 
        /// parameters. The Add method stores the data if it is not already in the cache and returns the value
        /// read from the cache if it already exists.
        /// </summary>
        /// <param name="key">A unique identifier for entry</param>
        /// <param name="entry">The content to add to the output cache</param>
        /// <param name="utcExpiry">The time and date on which the cached entry expires</param>
        /// <returns>
        /// The value that identifies what was in the cache, or the value that was just added if it was not
        /// </returns>
        public override object Add(
            string key,
            object entry,
            DateTime utcExpiry)
        {
            // Fix the key
            key = SanitizeKey(key);

            // We should only store the item if it's not in the cache. So try to add it and if it 
            // succeeds, return the value we just stored
            var expiration = ToExpiration(utcExpiry);
            if (_bucket.Insert(key, Serialize(entry), expiration).Success)
                return entry;

            // If it's in the cache we should return it
            var retval = DeSerialize(_bucket.Get<byte[]>(key).Value);

            // If the item got evicted between the Add and the Get (very rare) we store it anyway, 
            // but this time with Set to make sure it always gets into the cache
            if (retval == null) {
                _bucket.Insert(key, entry, expiration);
                retval = entry;
            }

            // Return the value read from the cache if it was present
            return retval;
        }

        /// <summary>
        /// Function to read an item from the output cache and returns it
        /// </summary>
        /// <param name="key">A unique identifier for entry</param>
        /// <returns>
        /// The value that identifies the specified entry in the cache, or null if the specified entry is not in the cache.
        /// </returns>
        public override object Get(
            string key)
        {
            var result = _bucket.Get<byte[]>(SanitizeKey(key));
            return DeSerialize(result.Value);
        }

        /// <summary>
        /// Function to remove an item from the output cache
        /// </summary>
        /// <param name="key">The unique identifier for the entry to remove from the output cache</param>
        public override void Remove(
            string key)
        {
            _bucket.Remove(SanitizeKey(key));
        }

        /// <summary>
        /// Function to set an item in the output cache
        /// </summary>
        /// <param name="key">A unique identifier for entry</param>
        /// <param name="entry">The content to add to the output cache</param>
        /// <param name="utcExpiry">The time and date on which the cached entry expires</param>
        public override void Set(
            string key,
            object entry,
            DateTime utcExpiry)
        {
            _bucket.Insert(SanitizeKey(key), Serialize(entry), ToExpiration(utcExpiry));
        }

        /// <summary>
        /// Serializes the object to a byte array
        /// </summary>
        /// <param name="value">Object value to seralize</param>
        /// <returns>Value as a byte array</returns>
        private byte[] Serialize(
            object value)
        {
            using (var ms = new MemoryStream()) {
                new BinaryFormatter().Serialize(ms, value);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Deserializes a byte array to an object
        /// </summary>
        /// <param name="bytes">Bytes to deserialize</param>
        /// <returns>Object that was deserialized</returns>
        private object DeSerialize(
            byte[] bytes)
        {
            if (bytes == null) {
                return null;
            }
            using (var ms = new MemoryStream(bytes)) {
                return new BinaryFormatter().Deserialize(ms);
            }
        }
    }
}

#region [ License information          ]
/* ************************************************************
 * 
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2012 Couchbase, Inc.
 *    @copyright 2012 Attila Kiskó, enyim.com
 *    @copyright 2012 Good Time Hobbies, Inc.
 *    @copyright 2015 AMain.com, Inc.
 *    
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *    
 *        http://www.apache.org/licenses/LICENSE-2.0
 *    
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *    
 * ************************************************************/
#endregion