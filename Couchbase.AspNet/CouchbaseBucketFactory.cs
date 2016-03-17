using System;
using System.Collections.Specialized;
using Couchbase.Core;

namespace Couchbase.AspNet
{
    [Obsolete("Use CouchbaseConfigSection as factory instead.")]
    public sealed class CouchbaseBucketFactory : ICouchbaseBucketFactory
    {
        /// <summary>
        /// Returns a Couchbase bucket or create one if it does not exist
        /// </summary>
        /// <param name="name">Name of the section from the configuration file</param>
        /// <param name="config">Configuration section information from the config file</param>
        /// <returns>Instance of the couchbase bucket to use</returns>
        public IBucket GetBucket(
            string name,
            NameValueCollection config)
        {
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