using System;
using System.Collections.Generic;
using System.Linq;
using Common.Logging;
using Couchbase.AspNet.Caching;
using Couchbase.Authentication;
using Couchbase.Configuration.Client;

namespace Couchbase.AspNet
{
    internal class BootStrapper
    {
        private readonly ILog _log = LogManager.GetLogger<BootStrapper>();

        public void Bootstrap(string name, System.Collections.Specialized.NameValueCollection config, ICouchbaseWebProvider provider)
        {
            if (Enum.TryParse(ProviderHelper.GetAndRemove(config, "bootstrapStrategy", true),
                true, out BootstrapStrategy configStrategy))
            {
                switch (configStrategy)
                {
                    case BootstrapStrategy.Inline:
                        ConfigureInline(name, config, provider);
                        break;
                    case BootstrapStrategy.Manual:
                        if (!MultiCluster.HasClusters)
                        {
                            const string msg = "If bootstrapStrategy is manual, then a MultiCluster must be configured in Global.asax or Setup.cs";
                            throw new InvalidOperationException(msg);
                        }
                        ConfigureManually(name, config, provider);
                        break;
                    case BootstrapStrategy.Section:
                        ConfigureFromSection(name, config, provider);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void ConfigureManually(string name, System.Collections.Specialized.NameValueCollection config, ICouchbaseWebProvider provider)
        {
            var prefix = ProviderHelper.GetAndRemove(config, "prefix", false);
            var bucket = ProviderHelper.GetAndRemove(config, "bucket", true);
            var throwOnError = ProviderHelper.GetAndRemoveAsBool(config, "throwOnError", false);

            provider.ThrowOnError = throwOnError ?? false;
            provider.Prefix = prefix;
            provider.BucketName = bucket;

            _log.Debug("Creating bucket: " + provider.BucketName);
            provider.Bucket = MultiCluster.GetBucket(name, provider.BucketName);
        }

        private void ConfigureInline(string name, System.Collections.Specialized.NameValueCollection config, ICouchbaseWebProvider provider)
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

            provider.ThrowOnError = throwOnError ?? false;
            provider.Prefix = prefix;
            provider.BucketName = bucket;

            var clientConfig = new ClientConfiguration
            {
                DefaultOperationLifespan = operationLifespan ?? ClientConfiguration.Defaults.DefaultOperationLifespan,
                UseSsl = useSsl ?? ClientConfiguration.Defaults.UseSsl,
                Servers = servers.Any() ? servers : new List<Uri> { new Uri("http://localhost:8091") },
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
            if (string.IsNullOrWhiteSpace(username))
            {
                if (!string.IsNullOrWhiteSpace(password))
                {
                    //assume pre-RBAC or CB < 5.0 if username is empty
                    authenticator = new ClassicAuthenticator(bucket, password);
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(password))
                {
                    authenticator = new PasswordAuthenticator(username, password);
                }
            }

            MultiCluster.Configure(clientConfig, name, authenticator);

            _log.Debug("Creating bucket: " + provider.BucketName);
            provider.Bucket = MultiCluster.GetBucket(name, provider.BucketName);
        }

        private void ConfigureFromSection(string name, System.Collections.Specialized.NameValueCollection config, ICouchbaseWebProvider provider)
        {
            //configure from the CouchbaseClientSection in Web.Config
            MultiCluster.Configure(name, config);

            // Create the bucket based off the name provided in the config
            provider.BucketName = ProviderHelper.GetAndRemove(config, "bucket", false);

            _log.Debug("Creating bucket: " + provider.BucketName);
            provider.Bucket = MultiCluster.GetBucket(name, provider.BucketName);
        }
    }
}

#region [ License information          ]
/* ************************************************************
 * 
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2017 Couchbase, Inc.
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
