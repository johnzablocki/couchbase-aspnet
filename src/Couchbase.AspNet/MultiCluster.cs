using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Configuration;
using Couchbase.Configuration.Client;
using Couchbase.Configuration.Client.Providers;
using Couchbase.Core;
using Couchbase.Core.Transcoders;

namespace Couchbase.AspNet
{
    internal static class MultiCluster
    {
        private static readonly ConcurrentDictionary<string, ICluster> Clusters = new ConcurrentDictionary<string, ICluster>();
        private static readonly ConcurrentDictionary<string, IBucket> Buckets = new ConcurrentDictionary<string, IBucket>();
        private static readonly object LockObj = new object();

        public static void Configure(string name, NameValueCollection config)
        {
            lock (LockObj)
            {
                var section = (ICouchbaseClientDefinition)ConfigurationManager.GetSection(name);
                var clientConfig = new ClientConfiguration(section) {Transcoder = () => new BinaryTranscoder()};
                var cluster = new Cluster(clientConfig);

                //assume if username was provided were using RBAC and >= CB 5.0
                if (!string.IsNullOrWhiteSpace(section.Username))
                {
                    cluster.Authenticate(section.Username, section.Password);
                }
                Clusters.TryAdd(name, cluster);
            }
        }

        public static IBucket GetBucket(string clusterName, string bucketName)
        {
            lock (LockObj)
            {
                var bucketAlias = clusterName + bucketName;
                if (Buckets.TryGetValue(bucketAlias, out IBucket bucket))
                {
                    return bucket;
                }

                if (Clusters.TryGetValue(clusterName, out ICluster cluster))
                {
                    bucket = cluster.OpenBucket(bucketName);
                    Buckets.TryAdd(bucketAlias, bucket);
                }

                return bucket;
            }
        }

        public static void CloseOne(string clusterName, string bucketName)
        {
            lock (LockObj)
            {
                if (Clusters.TryRemove(clusterName, out ICluster cluster))
                {
                    if (Buckets.TryRemove(clusterName + bucketName, out IBucket bucket))
                    {
                        bucket.Dispose();
                    }
                    cluster.Dispose();
                }
            }
        }

        public static void CloseAll()
        {
            lock (LockObj)
            {
                foreach (var cluster in Clusters)
                {
                    cluster.Value.Dispose();
                }
                Buckets.Clear();
                Clusters.Clear();
            }
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
