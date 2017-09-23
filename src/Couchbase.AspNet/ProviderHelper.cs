using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using Couchbase.Configuration.Client;
using Couchbase.Configuration.Client.Providers;
using Couchbase.Core;

namespace Couchbase.AspNet
{
    /// <summary>
    /// 
    /// </summary>
    internal static class ProviderHelper
    {
        /// <summary>
        /// Gets and removes an value from the configuration section
        /// </summary>
        /// <param name="config">Name value collection to examine</param>
        /// <param name="name">Name of the value to retrieve</param>
        /// <param name="required">True if the value is required, false if optional</param>
        /// <returns>Value returned from the configuration section, null if not found</returns>
        public static string GetAndRemove(NameValueCollection config, string name, bool required)
        {
            var value = config[name];
            if (value == null)
            {
                if (required)
                {
                    throw new ConfigurationErrorsException("Missing parameter: " + name);
                }
            }
            else
            {
                config.Remove(name);
            }

            return value;
        }

        /// <summary>
        /// Gets and removes an value from the configuration section as a bool
        /// </summary>
        /// <param name="config">Name value collection to examine</param>
        /// <param name="name">Name of the value to retrieve</param>
        /// <param name="required">True if the value is required, false if optional</param>
        /// <returns>Value returned from the configuration section, null if not found</returns>
        public static bool? GetAndRemoveAsBool(NameValueCollection config, string name, bool required)
        {
            var value = GetAndRemove(config, name, required);
            if (value == null) return null;
            return Convert.ToBoolean(value);
        }

        /// <summary>
        /// Gets and removes an value from the configuration section as a <see cref="int"/>
        /// </summary>
        /// <param name="config">Name value collection to examine</param>
        /// <param name="name">Name of the value to retrieve</param>
        /// <param name="required">True if the value is required, false if optional</param>
        /// <returns>Value returned from the configuration section, null if not found</returns>
        public static int? GetAndRemoveAsInt(NameValueCollection config, string name, bool required)
        {
            var value = GetAndRemove(config, name, required);
            if (value == null) return null;
            return Convert.ToInt32(value);
        }

        /// <summary>
        /// Gets and removes an value from the configuration section as a <see cref="uint"/>
        /// </summary>
        /// <param name="config">Name value collection to examine</param>
        /// <param name="name">Name of the value to retrieve</param>
        /// <param name="required">True if the value is required, false if optional</param>
        /// <returns>Value returned from the configuration section, null if not found</returns>
        public static uint? GetAndRemoveAsUInt(NameValueCollection config, string name, bool required)
        {
            var value = GetAndRemove(config, name, required);
            if (value == null) return null;
            return Convert.ToUInt32(value);
        }

        /// <summary>
        /// Gets and removes an value from the configuration section as a <see cref="Array"/> of <see cref="string"/>.
        /// </summary>
        /// <param name="config">Name value collection to examine</param>
        /// <param name="name">Name of the value to retrieve</param>
        /// <param name="seperator">The seperator to split on.</param>
        /// <param name="required">True if the value is required, false if optional</param>
        /// <returns>Value returned from the configuration section, null if not found</returns>
        public static string[] GetAndRemoveAsArray(NameValueCollection config, string name, char seperator, bool required)
        {
            var value = GetAndRemove(config, name, required);
            if(value == null) return new string[0];
            return value.Split(seperator).ToArray();
        }

        /// <summary>
        /// Helper to make sure there are no unknown attributes left over in the config section
        /// </summary>
        /// <param name="config">Name value collection to examine</param>
        public static void CheckForUnknownAttributes(NameValueCollection config)
        {
            if (config.Count > 0)
            {
                throw new ConfigurationErrorsException("Unknown parameter: " + config.Keys[0]);
            }
        }

        public static CouchbaseClientSection GetCouchbaseClientSection(string name)
        {
            return (CouchbaseClientSection)ConfigurationManager.GetSection(name);
        }

        /// <summary>
        /// Gets a Couchbase bucket using the couchbase factory configured in the Web.config file. If the factory is
        /// not defined in the Web.config file section, then the default factory is used.
        /// </summary>
        /// <param name="name">Name of the bucket</param>
        /// <param name="config">Name value collection from config file</param>
        /// <param name="cluster">The <see cref="ICluster"/> instance.</param>
        /// <returns>Instance of the couchbase bucket to use</returns>
        public static IBucket GetBucket(string name, NameValueCollection config, ICluster cluster)
        {
            var bucketName = GetAndRemove(config, "bucket", false);
            if (!string.IsNullOrEmpty(bucketName))
            {
                return cluster.OpenBucket(bucketName);
            }

            //if no bucket is provide use the default bucket
            return cluster.OpenBucket();
        }

        /// <summary>
        /// Creates a <see cref="Cluster"/> object from the configuration in the App.Config.
        /// </summary>
        /// <param name="name">The name of the <see cref="CouchbaseClientSection"/> that holds the configuration information for the <see cref="Cluster"/>.</param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static ICluster CreateCluster(string name, NameValueCollection config)
        {
            var section = (CouchbaseClientSection)ConfigurationManager.GetSection(name);
            var clientConfig = new ClientConfiguration(section);
            return new Cluster(clientConfig);
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