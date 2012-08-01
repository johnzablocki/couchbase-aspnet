using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Caching;
using Enyim.Caching;
using Enyim.Caching.Memcached;
using System.Diagnostics;

namespace Couchbase.AspNet.OutputCache
{
	public class CouchbaseOutputCacheProvider : OutputCacheProvider
	{
		private IMemcachedClient _client;

		public override void Initialize(string name, System.Collections.Specialized.NameValueCollection config)
		{
			
			base.Initialize(name, config);
			_client = ProviderHelper.GetClient(name, config, () => (ICouchbaseClientFactory)new CouchbaseClientFactory());

			ProviderHelper.CheckForUnknownAttributes(config);
		
		}
	
		public override object Add(string key, object entry, DateTime utcExpiry)
		{
			var item = _client.Get(key);
		
			if (item != null)
			{
				return item;
			}

			_client.Store(StoreMode.Add, key, entry, utcExpiry);
			return entry;
		}

		public override object Get(string key)
		{
			var item = _client.Get(key);
			return (item != null) ? item : null;			
		}

		public override void Remove(string key)
		{
			_client.Remove(key);
		}

		public override void Set(string key, object entry, DateTime utcExpiry)
		{
			var result = _client.Store(StoreMode.Set, key, entry, utcExpiry);			
		}
	}
}

#region [ License information          ]
/* ************************************************************
 * 
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2012 Couchbase, Inc.
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