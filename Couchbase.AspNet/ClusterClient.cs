using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Configuration;
using Couchbase.Configuration.Client;
using Couchbase.Configuration.Client.Providers;
using Couchbase.Core;

namespace Couchbase.AspNet
{
    internal static class ClusterClient
    {
        private static readonly ConcurrentDictionary<string, ICluster> _clusters = new ConcurrentDictionary<string, ICluster>();
        private static readonly ConcurrentDictionary<string, IBucket> _buckets = new ConcurrentDictionary<string, IBucket>();
        private static readonly object _lockObj = new object();

        public static void Configure(string name, NameValueCollection config)
        {
            lock (_lockObj)
            {
                var section = (CouchbaseClientSection)ConfigurationManager.GetSection(name);
                var clientConfig = new ClientConfiguration(section);
            
                _clusters.TryAdd(name, new Cluster(clientConfig));
            }
        }

        public static IBucket GetBucket(string clusterName, string bucketName)
        {
            lock (_lockObj)
            {
                var bucketAlias = clusterName + bucketName;
                IBucket bucket;
                if (_buckets.TryGetValue(bucketAlias, out bucket))
                {
                    return bucket;
                }

                ICluster cluster;
                if (_clusters.TryGetValue(clusterName, out cluster))
                {
                    bucket = cluster.OpenBucket(bucketName);
                    _buckets.TryAdd(bucketAlias, bucket);
                }
         
                return bucket;
            }
        }

        public static void CloseOne(string clusterName, string bucketName)
        {
            lock (_lockObj)
            {
                ICluster cluster;
                if (_clusters.TryRemove(clusterName, out cluster))
                {
                    IBucket bucket;
                    if (_buckets.TryRemove(clusterName+bucketName, out bucket))
                    {
                        bucket.Dispose();
                    }
                    cluster.Dispose();
                }
            }
        }

        public static void CloseAll()
        {
            lock (_lockObj)
            {
                foreach (var cluster in _clusters)
                {
                    cluster.Value.Dispose();
                }
                _buckets.Clear();
                _clusters.Clear();
            }
        }
    }
}
