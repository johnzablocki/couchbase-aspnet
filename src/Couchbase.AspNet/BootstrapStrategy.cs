
using Couchbase.Configuration.Client.Providers;

namespace Couchbase.AspNet
{
    /// <summary>
    /// The specified configuration strategy to use for bootstrapping the underlying Couchbase SDK.
    /// </summary>
    public enum BootstrapStrategy
    {
        /// <summary>
        /// Use the configuration from the Caching providers "add" section in the Web.Config
        /// </summary>
        Inline,

        /// <summary>
        /// Use the <see cref="CouchbaseClientSection"/> section in the Web.Config
        /// </summary>
        Section,

        /// <summary>
        /// Use the programatically defined configuration of <see cref="MultiCluster"/> usually done in the Global.asax.
        /// </summary>
        Manual
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
