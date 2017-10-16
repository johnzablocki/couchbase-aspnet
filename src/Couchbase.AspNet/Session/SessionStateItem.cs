using System;
using System.Web.SessionState;

namespace Couchbase.AspNet.Session
{
    [Serializable]
    public class SessionStateItem
    {
        public SessionStateItem()
        {
            //start with initialize items per msdn
            Flags = SessionStateActions.InitializeItem;
            Created = DateTime.Now;
            LockDate = DateTime.Now;
            Locked = false;
        }

        public string SessionId { get; set; }

        public string ApplicationName { get; set; }

        public DateTime Created { get; set; }

        public DateTime Expires { get; set; }

        public DateTime LockDate { get; set; }

        public uint LockId { get; set; }

        public TimeSpan Timeout { get; set; }

        public bool Locked { get; set; }

        public byte[] SessionItems { get; set; }

        public SessionStateActions Flags { get; set; }
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
