using System;
using System.Collections.Specialized;
using System.IO;
using System.Web;
using System.Web.SessionState;
using Common.Logging;
using Couchbase.Core;
using System.Web.Configuration;
using Couchbase.AspNet.Caching;
using Couchbase.IO;
using Couchbase.Utils;

namespace Couchbase.AspNet.Session
{
    public class CouchbaseSessionStateProvider : SessionStateStoreProviderBase, ICouchbaseWebProvider
    {
        private readonly object _syncObj = new object();
        private readonly ILog _log = LogManager.GetLogger<CouchbaseSessionStateProvider>();
        public IBucket Bucket { get; set; }
        public string ApplicationName { get; set; }
        public bool ThrowOnError { get; set; }
        public string Prefix { get; set; }
        public string BucketName { get; set; }
        private SessionStateSection Config { get; set; }

        public override void Initialize(string name, NameValueCollection config)
        {
            base.Initialize(name, config);
            lock (_syncObj)
            {
                var appName = System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath;
                var webConfig = WebConfigurationManager.OpenWebConfiguration(appName);
                Config = (SessionStateSection)webConfig.GetSection("system.web/sessionState");

                var bootStapper = new BootStrapper();
                bootStapper.Bootstrap(name, config, this);
            }
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }

        #region Not Supported

        public override bool SetItemExpireCallback(SessionStateItemExpireCallback expireCallback)
        {
            _log.Trace("SetItemExpireCallback called.");
            return false;
        }

        public override void InitializeRequest(HttpContext context)
        {
           _log.Trace("InitializeRequest called.");
        }

        public override void EndRequest(HttpContext context)
        {
            _log.Trace("EndRequest called.");
        }

        #endregion

        /*
      * Takes as input the HttpContext instance for the current request and the SessionID value for the current request. 
      * Retrieves session values and information from the session data store and locks the session-item data at the data 
      * store for the duration of the request. The GetItemExclusive method sets several output-parameter values that inform 
      * the calling SessionStateModule about the state of the current session-state item in the data store.
      *
      * If no session item data is found at the data store, the GetItemExclusive method sets the locked output parameter to 
      * false and returns null. This causes SessionStateModule to call the CreateNewStoreData method to create a new 
      * SessionStateStoreData object for the request.
      *
      * If session-item data is found at the data store but the data is locked, the GetItemExclusive method sets the locked 
      * output parameter to true, sets the lockAge output parameter to the current date and time minus the date and time when 
      * the item was locked, sets the lockId output parameter to the lock identifier retrieved from the data store, and returns 
      * null. This causes SessionStateModule to call the GetItemExclusive method again after a half-second interval, to attempt 
      * to retrieve the session-item information and obtain a lock on the data. If the value that the lockAge output parameter 
      * is set to exceeds the ExecutionTimeout value, SessionStateModule calls the ReleaseItemExclusive method to clear the lock 
      * on the session-item data and then call the GetItemExclusive method again.
      * 
      * The actionFlags parameter is used with sessions whose Cookieless property is true, when the regenerateExpiredSessionId 
      * attribute is set to true. An actionFlags value set to InitializeItem (1) indicates that the entry in the session data 
      * store is a new session that requires initialization. Uninitialized entries in the session data store are created by a 
      * call to the CreateUninitializedItem method. If the item from the session data store is already initialized, the 
      * actionFlags parameter is set to zero.
      * 
      * If your provider supports cookieless sessions, set the actionFlags output parameter to the value returned from the session 
      * data store for the current item. If the actionFlags parameter value for the requested session-store item equals the 
      * InitializeItem enumeration value (1), the GetItemExclusive method should set the value in the data store to zero after 
      * setting the actionFlags out parameter.*/
        private SessionStateStoreData GetSessionStoreItem(bool lockRecord,
            HttpContext context,
            string id,
            out bool locked,
            out TimeSpan lockAge,
            out object lockId,
            out SessionStateActions actions)
        {
            locked = false;
            lockAge = TimeSpan.Zero;
            lockId = 0;
            actions = SessionStateActions.None;

            _log.TraceFormat("GetSessionStoreItem called for item {0} with lockId {1}.", id, lockId);

            if (id == null) return null;
            
            /*
             * If no session item data is found at the data store, the GetItemExclusive method sets the locked output parameter to 
             * false and returns null. This causes SessionStateModule to call the CreateNewStoreData method to create a new 
             * SessionStateStoreData object for the request.
             */
            var sessionData = Bucket.Get<SessionStateItem>(id);
            if (sessionData.Status == ResponseStatus.KeyNotFound)
            {
                lockAge = TimeSpan.Zero;
                actions = SessionStateActions.InitializeItem;
                lockId = null;
                return null;
            }

            /*
             * * If session-item data is found at the data store but the data is locked, the GetItemExclusive method sets the locked 
             * output parameter to true, sets the lockAge output parameter to the current date and time minus the date and time when 
             * the item was locked, sets the lockId output parameter to the lock identifier retrieved from the data store, and returns 
             * null. This causes SessionStateModule to call the GetItemExclusive method again after a half-second interval, to attempt 
             * to retrieve the session-item information and obtain a lock on the data. If the value that the lockAge output parameter 
             * is set to exceeds the ExecutionTimeout value, SessionStateModule calls the ReleaseItemExclusive method to clear the lock 
             * on the session-item data and then call the GetItemExclusive method again.
             */

            SessionStateStoreData item = null;
            if (sessionData.Status == ResponseStatus.Success)
            {
                if (sessionData.Value.Locked)
                {
                    locked = true;
                    lockAge = DateTime.UtcNow - sessionData.Value.LockDate;
                    lockId = sessionData.Value.LockId;
                    actions = sessionData.Value.Actions;
                }
                else
                {
                    lockAge = DateTime.UtcNow - sessionData.Value.LockDate;
                    lockId = sessionData.Value.LockId;
                    var sessionStateItem = sessionData.Value;

                    if (sessionData.Value.Actions == SessionStateActions.InitializeItem)
                    {
                        //create a new item
                        item = CreateNewStoreData(context, (int) Config.Timeout.TotalMinutes);
                    }
                    else
                    {
                        item = new SessionStateStoreData(Deserialize(sessionStateItem.SessionItems),
                            SessionStateUtility.GetSessionStaticObjects(context),
                            (int) sessionData.Value.Timeout.TotalMinutes);
                    }
                }

                if (!lockRecord) return item;
                var upsert = Bucket.Upsert(id, sessionData.Value, Config.Timeout);
                if (!upsert.Success)
                {
                    LogAndOrThrow(upsert, id);
                }
            }
            else
            {
                lockAge = TimeSpan.Zero;
                actions = SessionStateActions.InitializeItem;
                lockId = null;
            }
            return item;
        }

        public override SessionStateStoreData GetItem(HttpContext context, string id, out bool locked, out TimeSpan lockAge, out object lockId,
            out SessionStateActions actions)
        {
            _log.TraceFormat("GetItem called for item {0}", id);
            return GetSessionStoreItem(false, context, id, out locked, out lockAge, out lockId, out actions);
        }

        public override SessionStateStoreData GetItemExclusive(HttpContext context, string id, out bool locked, out TimeSpan lockAge,
            out object lockId, out SessionStateActions actions)
        {
            _log.TraceFormat("GetSessionExclusive called for item {0}", id);
            return GetSessionStoreItem(true, context, id, out locked, out lockAge, out lockId, out actions);
        }

        /*
         * Takes as input the HttpContext instance for the current request, the SessionID value for the current request, 
         * and the lock identifier for the current request, and releases the lock on an item in the session data store. 
         * This method is called when the GetItem or GetItemExclusive method is called and the data store specifies that 
         * the requested item is locked, but the lock age has exceeded the ExecutionTimeout value. The lock is cleared by 
         * this method, freeing the item for use by other requests.
         * */
        public override void ReleaseItemExclusive(HttpContext context, string id, object lockId)
        {
            _log.TraceFormat("ReleaseItemExclusive called for item {0} with lockId {1}.", id, lockId);
            var original = Bucket.Get<SessionStateItem>(id);
            var item = original.Value;
            if (original.Success && item.LockId != (uint)lockId)
            {
                return;
            }

            item.Locked = false;
            item.Expires = DateTime.UtcNow.AddMinutes(Config.Timeout.TotalMinutes);
            item.SessionId = id;
            item.ApplicationName = ApplicationName;
            item.LockId = (uint)lockId;

            var upsert = Bucket.Upsert(id, item, Config.Timeout);
            if (!upsert.Success)
            {
                LogAndOrThrow(upsert, id);
            }
        }

        /*
         * Takes as input the HttpContext instance for the current request, the SessionID value for the current request, 
         * a SessionStateStoreData object that contains the current session values to be stored, the lock identifier for 
         * the current request, and a value that indicates whether the data to be stored is for a new session or an existing 
         * session.
           If the newItem parameter is true, the SetAndReleaseItemExclusive method inserts a new item into the data store 
           with the supplied values. Otherwise, the existing item in the data store is updated with the supplied values, and
           any lock on the data is released. Note that only session data for the current application that matches the supplied 
           SessionID value and lock identifier values is updated.
           After the SetAndReleaseItemExclusive method is called, the ResetItemTimeout method is called by SessionStateModule to 
           update the expiration date and time of the session-item data.
         */
        public override void SetAndReleaseItemExclusive(HttpContext context, string id, SessionStateStoreData item, object lockId,
            bool newItem)
        {
            _log.Trace("SetAndReleaseItemExclusive called.");
            var original = Bucket.Get<SessionStateItem>(id);
            if (original.Success && original.Value.LockId != (uint)lockId)
            {
                return;
            }

            if (newItem)
            {
                var expires = DateTime.UtcNow.AddMinutes(item.Timeout);
                var result = Bucket.Insert(id, new SessionStateItem
                {
                    ApplicationName = ApplicationName,
                    Expires = expires,
                    SessionId = id,
                    SessionItems = Serialize(item.Items),
                    Locked = false
                }, TimeSpan.FromMinutes(item.Timeout));

                if (!result.Success)
                {
                    LogAndOrThrow(result, id);
                }
            }
            else
            {
                var entry = original.Value;
                entry.Expires  = DateTime.UtcNow.AddMinutes(Config.Timeout.TotalMinutes);
                entry.SessionItems = Serialize(item.Items);
                entry.Locked = false;
                entry.SessionId = id;
                entry.ApplicationName = ApplicationName;
                entry.LockId = (uint)lockId;
                entry.Actions = SessionStateActions.None;

                var updated = Bucket.Upsert(new Document<SessionStateItem>
                {
                    Content = entry,
                    Id = id,
                    Expiry = (uint)Config.Timeout.TotalSeconds
                }, TimeSpan.FromMinutes(item.Timeout));

                if (!updated.Success)
                {
                    LogAndOrThrow(updated, id);
                }
            }
        }

        public byte[] Serialize(ISessionStateItemCollection items)
        {
            using (var ms = new MemoryStream())
            {
                var writer = new BinaryWriter(ms);
                ((SessionStateItemCollection)items).Serialize(writer);
                return ms.ToArray();
            }
        }

        public ISessionStateItemCollection Deserialize(byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            {
                var reader = new BinaryReader(ms);
                return SessionStateItemCollection.Deserialize(reader);
            }  
        }

        public override void RemoveItem(HttpContext context, string id, object lockId, SessionStateStoreData item)
        {
            _log.TraceFormat("Remove called for item {0} with lockId {1}.", id, lockId);
            var result = Bucket.Get<SessionStateItem>(id);
            if (result.Success)
            {
                var entry = result.Value;
                if (entry.LockId == (uint) lockId)
                {
                    var deleted = Bucket.Remove(id);
                    if (deleted.Success) return;
                    LogAndOrThrow(deleted, id);
                }
            }
            else
            {
                LogAndOrThrow(result, id);
            }
        }

        public override void ResetItemTimeout(HttpContext context, string id)
        {
            _log.TraceFormat("ResetItemTimeout called for item {0}.", id);
            var result = Bucket.Get<SessionStateItem>(id);
            if (result.Success)
            {
                var item = result.Value;
                item.Timeout = Config.Timeout;
                item.SessionId = id;
                item.ApplicationName = ApplicationName;

                var updated = Bucket.Upsert(id, item, Config.Timeout);
                if (updated.Success) return;
                LogAndOrThrow(updated, id);
            }
            else
            {
                LogAndOrThrow(result, id);
            }
        }

        public override SessionStateStoreData CreateNewStoreData(HttpContext context, int timeout)
        {
            _log.Trace("CreateNewStoreData called.");
            return new SessionStateStoreData(new SessionStateItemCollection(),
                SessionStateUtility.GetSessionStaticObjects(context),
                timeout);   
        }

        /*
         * Takes as input the HttpContext instance for the current request, the SessionID value for the current request, 
         * and the lock identifier for the current request, and adds an uninitialized item to the session data store with 
         * an actionFlags value of InitializeItem.
         * 
         * The CreateUninitializedItem method is used with cookieless sessions when the regenerateExpiredSessionId attribute 
         * is set to true, which causes SessionStateModule to generate a new SessionID value when an expired session ID is encountered.
         * 
         * The process of generating a new SessionID value requires the browser to be redirected to a URL that contains the newly 
         * generated session ID. The CreateUninitializedItem method is called during an initial request that contains an expired
         * session ID. After SessionStateModule acquires a new SessionID value to replace the expired session ID, it calls the 
         * CreateUninitializedItem method to add an uninitialized entry to the session-state data store. The browser is then redirected 
         * to the URL containing the newly generated SessionID value. The existence of the uninitialized entry in the session data 
         * store ensures that the redirected request with the newly generated SessionID value is not mistaken for a request for an 
         * expired session, and instead is treated as a new session.
         * 
         * The uninitialized entry in the session data store is associated with the newly generated SessionID value and contains only 
         * default values, including an expiration date and time, and a value that corresponds to the actionFlags parameter of the GetItem 
         * and GetItemExclusive methods. The uninitialized entry in the session state store should include an actionFlags value equal to 
         * the InitializeItem enumeration value (1). This value is passed to SessionStateModule by the GetItem and GetItemExclusive 
         * methods and specifies for SessionStateModule that the current session is a new session. SessionStateModule will then initialize 
         * the new session and raise the Session_OnStart event.
         */
        public override void CreateUninitializedItem(HttpContext context, string id, int timeout)
        {
            _log.TraceFormat("CreateUninitializedItem called for item {0}.", id);
            try
            {
                var expires = DateTime.UtcNow.AddMinutes(timeout);
                var result = Bucket.Insert(id, new SessionStateItem
                {
                    ApplicationName = ApplicationName,
                    Expires = expires,
                    SessionId = id,
                    Actions = SessionStateActions.InitializeItem
                }, TimeSpan.FromMinutes(timeout));

                if (result.Success) return;
                LogAndOrThrow(result, id);
            }
            catch (Exception e)
            {
                LogAndOrThrow(e, id);
            }
        }

        /// <summary>
        /// Logs the reason why an operation fails and throws and exception if <see cref="ThrowOnError"/> is
        /// <c>true</c> and logging the issue as WARN.
        /// </summary>
        /// <param name="e">The e.</param>
        /// <param name="key">The key.</param>
        /// <exception cref="CouchbaseSessionStateException"></exception>
        private void LogAndOrThrow(Exception e, string key)
        {
            _log.Error($"Could not retrieve, remove or write key '{key}' - reason: {e}");
            if (ThrowOnError)
            {
                throw new CouchbaseSessionStateException($"Could not retrieve, remove or write key '{key}'", e);
            }
        }

        /// <summary>
        /// Logs the reason why an operation fails and throws and exception if <see cref="ThrowOnError"/> is
        /// <c>true</c> and logging the issue as WARN.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="key">The key.</param>
        /// <exception cref="InvalidOperationException"></exception>
        private void LogAndOrThrow(IOperationResult result, string key)
        {
            if (result.Exception != null)
            {
                LogAndOrThrow(result.Exception, key);
                return;
            }
            _log.Error($"Could not retrieve, remove or write key '{key}' - reason: {result.Status}");
            if (ThrowOnError)
            {
                throw new InvalidOperationException(result.Status.ToString());
            }
        }

        /// <summary>
        /// Logs the reason why an operation fails and throws and exception if <see cref="ThrowOnError"/> is
        /// <c>true</c> and logging the issue as WARN.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="key">The key.</param>
        /// <exception cref="InvalidOperationException"></exception>
        private void LogAndOrThrow(IDocumentResult result, string key)
        {
            if (result.Exception != null)
            {
                LogAndOrThrow(result.Exception, key);
                return;
            }
            _log.Error($"Could not retrieve, remove or write key '{key}' - reason: {result.Status}");
            if (ThrowOnError)
            {
                throw new InvalidOperationException(result.Status.ToString());
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
