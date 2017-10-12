using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.SessionState;
using Microsoft.AspNet.SessionState;

namespace Couchbase.AspNet.Session
{
    public class CouchbaseSessionStateStoreProviderAsync : SessionStateStoreProviderAsyncBase
    {
        public override SessionStateStoreData CreateNewStoreData(HttpContextBase context, int timeout)
        {
            throw new NotImplementedException();
        }

        public override Task CreateUninitializedItemAsync(HttpContextBase context, string id, int timeout, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }

        public override Task EndRequestAsync(HttpContextBase context)
        {
            throw new NotImplementedException();
        }

        public override Task<GetItemResult> GetItemAsync(HttpContextBase context, string id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task<GetItemResult> GetItemExclusiveAsync(HttpContextBase context, string id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override void InitializeRequest(HttpContextBase context)
        {
            throw new NotImplementedException();
        }

        public override Task ReleaseItemExclusiveAsync(HttpContextBase context, string id, object lockId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task RemoveItemAsync(HttpContextBase context, string id, object lockId, SessionStateStoreData item,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task ResetItemTimeoutAsync(HttpContextBase context, string id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task SetAndReleaseItemExclusiveAsync(HttpContextBase context, string id, SessionStateStoreData item, object lockId,
            bool newItem, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override bool SetItemExpireCallback(SessionStateItemExpireCallback expireCallback)
        {
            throw new NotImplementedException();
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
