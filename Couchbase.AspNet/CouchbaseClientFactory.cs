using System.Collections.Specialized;
using Couchbase.Core;

namespace Couchbase.AspNet
{
	public sealed class CouchbaseClientFactory : ICouchbaseClientFactory
    {
        public IBucket Create(string name, NameValueCollection config, out bool disposeClient)
        {
            // This client should be disposed of as it is not shared
            disposeClient = true;

            // Get the bucket name to use from the configuration file and use a specific bucket if specified
            var bucketName = ProviderHelper.GetAndRemove(config, "bucket", false);
	        if (!string.IsNullOrEmpty(bucketName))
                return ClusterHelper.GetBucket(bucketName);

            // If no bucket is specified, simply use the default bucket (which will be the first in the list)
            return ClusterHelper.Get().OpenBucket();
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