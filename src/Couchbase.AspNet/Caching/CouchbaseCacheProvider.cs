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
    public class CouchbaseCacheProvider : OutputCacheProvider
    {
        private object _syncObj = new object();
        private IBucket _bucket;
        private ILog _log = LogManager.GetLogger<CouchbaseCacheProvider>();
        private const string EmptyKeyMessage = "'key' must be non-null, not empty or whitespace.";
        public bool ThrowOnError { get; set; }
        public string Prefix { get; set; }
        public string BucketName { get; set; }
        public bool? AutoConfigure { get; set; }

        public CouchbaseCacheProvider(){ }

        public CouchbaseCacheProvider(IBucket bucket)
        {
            _bucket = bucket;
        }

        public override void Initialize(string name, NameValueCollection config)
        {
            base.Initialize(name, config);
            lock(_syncObj)
            {
                if (Enum.TryParse(ProviderHelper.GetAndRemove(config, "bootstrapStrategy", true), 
                    true, out BootstrapStrategy configStrategy))
                {
                    switch (configStrategy)
                    {
                        case BootstrapStrategy.Inline:
                            ConfigureInline(name, config);
                            break;
                        case BootstrapStrategy.Manual:
                            if (!MultiCluster.HasClusters)
                            {
                                const string msg = "If configStrategy is manual, then a MultiCluster must be configured in Global.asax or Setup.cs";
                                throw  new InvalidOperationException(msg);
                            }
                            break;
                        case BootstrapStrategy.Section:
                            ConfigureFromSection(name, config);
                            break;
                    }
                }

            }
        }

        private void ConfigureInline(string name, NameValueCollection config)
        {
            var prefix = ProviderHelper.GetAndRemove(config, "prefix", false);
            var servers = ProviderHelper.GetAndRemoveAsArray(config, "servers", ';', false).Select(x => new Uri(x)).ToList();
            var useSsl = ProviderHelper.GetAndRemoveAsBool(config, "useSsl", false);
            var bucket = ProviderHelper.GetAndRemove(config, "bucket", true);
            var operationLifespan = ProviderHelper.GetAndRemoveAsUInt(config, "operationLifespan", false);
            var sendTimeout = ProviderHelper.GetAndRemoveAsInt(config, "sendTimeout", false);
            var connectTimeout = ProviderHelper.GetAndRemoveAsInt(config, "connectTimeout", false);
            var minPoolSize = ProviderHelper.GetAndRemoveAsInt(config, "minPoolSize", false);
            var maxPoolSize = ProviderHelper.GetAndRemoveAsInt(config, "maxPoolSize", false);
            var username = ProviderHelper.GetAndRemove(config, "username", false);
            var password = ProviderHelper.GetAndRemove(config, "password", false);
            var throwOnError = ProviderHelper.GetAndRemoveAsBool(config, "throwOnError", false);

            ThrowOnError = throwOnError ?? false;
            Prefix = prefix;

            var clientConfig = new ClientConfiguration
            {
                DefaultOperationLifespan = operationLifespan ?? ClientConfiguration.Defaults.DefaultOperationLifespan,
                UseSsl = useSsl ?? ClientConfiguration.Defaults.UseSsl,
                Servers = servers.Any() ? servers : new List<Uri> {new Uri("http://localhost:8091")},
                BucketConfigs = new Dictionary<string, BucketConfiguration>
                {
                    {
                        bucket, new BucketConfiguration
                        {
                            BucketName = bucket,
                            PoolConfiguration = new PoolConfiguration
                            {
                                MinSize = minPoolSize ?? PoolConfiguration.Defaults.MinSize,
                                MaxSize = maxPoolSize ?? PoolConfiguration.Defaults.MaxSize,
                                SendTimeout = sendTimeout ?? PoolConfiguration.Defaults.SendTimeout,
                                ConnectTimeout = connectTimeout ?? PoolConfiguration.Defaults.ConnectTimeout
                            }
                        }
                    }
                }
            };

            IAuthenticator authenticator = null;
            if (!string.IsNullOrWhiteSpace(username))
            {
                authenticator = new PasswordAuthenticator(username, password);
            }

            MultiCluster.Configure(clientConfig, name, authenticator);

            _log.Debug("Creating bucket: " + BucketName);
            _bucket = MultiCluster.GetBucket(name, bucket);
        }

        private void ConfigureFromSection(string name, NameValueCollection config)
        {
            //use the config based approach if the user does not indicate that the are doing manual config of cluster
            AutoConfigure = ProviderHelper.GetAndRemoveAsBool(config, "autoConfigure", false);
            if (AutoConfigure.HasValue && AutoConfigure == true)
            {
                MultiCluster.Configure(name, config);
            }

            // Create the bucket based off the name provided in the config
            BucketName = ProviderHelper.GetAndRemove(config, "bucket", false);

            _log.Debug("Creating bucket: " + BucketName);
            _bucket = MultiCluster.GetBucket(name, BucketName);
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
            CheckKey(key);

            try
            {
                // get the item
                var result = _bucket.Get<object>(key);
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
            CheckKey(key);

            try
            {
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
            CheckKey(key);

            try
            {
                var expiration = DateTime.SpecifyKind(utcExpiry, DateTimeKind.Utc).TimeOfDay;

                var result = _bucket.Upsert(key, entry, expiration);
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
            CheckKey(key);

            try
            {
                var result = _bucket.Remove(key);
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
        /// <exception cref="Couchbase.AspNet.Caching.CouchbaseCacheException"></exception>
        private void LogAndOrThrow(Exception e, string key)
        {
            _log.Error($"Could not retrieve, remove or write key '{key}' - reason: {e}");
            if (ThrowOnError)
            {
                throw new CouchbaseCacheException($"Could not retrieve, remove or write key '{key}'", e);
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