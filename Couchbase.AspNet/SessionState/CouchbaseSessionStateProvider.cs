using System;
using System.Collections.Specialized;
using System.Web.SessionState;
using System.Web;
using System.IO;
using System.Linq;
using System.Web.UI;
using Couchbase.Core;
using Couchbase.IO;

namespace Couchbase.AspNet.SessionState
{
    public class CouchbaseSessionStateProvider : SessionStateStoreProviderBase
    {
        private IBucket _bucket;
        private static bool _exclusiveAccess;

        /// <summary>
        /// Defines the prefix for header data in the Couchbase bucket. Must be unique to ensure it does not conflict with 
        /// other applications that might be using the Couchbase bucket.
        /// </summary>
        private static string _headerPrefix =
            (System.Web.Hosting.HostingEnvironment.SiteName ?? string.Empty).Replace(" ", "-") + "+" +
            System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath + "info-";

        /// <summary>
        /// Defines the prefix for the actual session store data stored in the Couchbase bucket. Must also be unique for
        /// the same reasons outlined above.
        /// </summary>
        private static string _dataPrefix =
            (System.Web.Hosting.HostingEnvironment.SiteName ?? string.Empty).Replace(" ", "-") + "+" +
            System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath + "data-";


        /// <summary>
        /// Function to initialize the provider
        /// </summary>
        /// <param name="name">Name of the element in the configuration file</param>
        /// <param name="config">Configuration values for the provider from the Web.config file</param>
        public override void Initialize(
            string name,
            NameValueCollection config)
        {
            // Initialize the base class
            base.Initialize(name, config);

            // Create our Couchbase bucket instance
            _bucket = ProviderHelper.GetBucket(name, config);

            // By default use exclusive session access. But allow it to be overridden in the config file
            var exclusive = ProviderHelper.GetAndRemove(config, "exclusiveAccess", false) ?? "true";
            _exclusiveAccess = (string.Compare(exclusive, "true", StringComparison.OrdinalIgnoreCase) == 0);

            // Allow optional header and data prefixes to be used for this application
            var headerPrefix = ProviderHelper.GetAndRemove(config, "headerPrefix", false);
            if (headerPrefix != null) {
                _headerPrefix = headerPrefix;
            }
            var dataPrefix = ProviderHelper.GetAndRemove(config, "dataPrefix", false);
            if (dataPrefix != null) {
                _dataPrefix = dataPrefix;
            }

            // Make sure no extra attributes are included
            ProviderHelper.CheckForUnknownAttributes(config);
        }

        /// <summary>
        /// Handle disposing of the session-state store object
        /// </summary>
        public override void Dispose()
        {
        }

        /// <summary>
        /// Takes as input the HttpContext instance for the current request and performs any 
        /// initialization required by the session-state store provider. 
        /// </summary>
        /// <param name="context">HttpContext for the current request</param>
        public override void InitializeRequest(
            HttpContext context)
        {
        }

        /// <summary>
        /// Takes as input the HttpContext instance for the current request and performs any 
        /// cleanup required by the session-state store provider.
        /// </summary>
        /// <param name="context">HttpContext for the current request</param>
        public override void EndRequest(
            HttpContext context)
        {
        }

        /// <summary>
        /// Takes as input the HttpContext instance for the current request and the Timeout value for the 
        /// current session, and returns a new SessionStateStoreData object with an empty 
        /// ISessionStateItemCollection object, an HttpStaticObjectsCollection collection, and the 
        /// specified Timeout value. The HttpStaticObjectsCollection instance for the ASP.NET application 
        /// can be retrieved using the GetSessionStaticObjects method.
        /// </summary>
        /// <param name="context">HttpContext for the current request</param>
        /// <param name="timeout">Timeout value for the session</param>
        /// <returns>New SessionStateStoreData object for storing the session state data</returns>
        public override SessionStateStoreData CreateNewStoreData(
            HttpContext context,
            int timeout)
        {
            return new SessionStateStoreData(new SessionStateItemCollection(),
                SessionStateUtility.GetSessionStaticObjects(context),
                timeout);
        }

        /// <summary>
        /// Creates an uninitialized item in the database. This is only used for cookieless sessions
        /// regenerateExpiredSessionId attribute is set to true, which causes SessionStateModule to 
        /// generate a new SessionID value when an expired session ID is encountered.
        /// </summary>
        /// <param name="context">HttpContext for the current request</param>
        /// <param name="id">Session ID for the new session</param>
        /// <param name="timeout">Timeout value for the session</param>
        public override void CreateUninitializedItem(
            HttpContext context,
            string id,
            int timeout)
        {
            var e = new SessionStateItem {
                Data = new SessionStateItemCollection(),
                Flag = SessionStateActions.InitializeItem,
                LockId = 0,
                Timeout = timeout
            };

            e.SaveAll(_bucket, id, false);
        }

        /// <summary>
        /// Returns read-only session-state data from the session data store
        /// </summary>
        /// <param name="context">HttpContext for the current request</param>
        /// <param name="id">Session ID for the session</param>
        /// <param name="locked">Returns true if the session item is locked, otherwise false</param>
        /// <param name="lockAge">Returns the amount of time that the item has been locked</param>
        /// <param name="lockId">Returns lock identifier for the current request</param>
        /// <param name="actions">Indicates whether the current sessions is an uninitialized, cookieless session</param>
        /// <returns>SessionStateStoreData object containing the session state data</returns>
        public override SessionStateStoreData GetItem(
            HttpContext context,
            string id,
            out bool locked,
            out TimeSpan lockAge,
            out object lockId,
            out SessionStateActions actions)
        {
            var e = GetSessionStoreItem(_bucket, context, false, id, out locked, out lockAge, out lockId, out actions);
            return (e == null) ? null : e.ToStoreData(context);
        }

        /// <summary>
        /// Returns read-write session-state data from the session data store
        /// </summary>
        /// <param name="context">HttpContext for the current request</param>
        /// <param name="id">Session ID for the session</param>
        /// <param name="locked">Returns true if the session item is locked, otherwise false</param>
        /// <param name="lockAge">Returns the amount of time that the item has been locked</param>
        /// <param name="lockId">Returns lock identifier for the current request</param>
        /// <param name="actions">Indicates whether the current sessions is an uninitialized, cookieless session</param>
        /// <returns>SessionStateStoreData object containing the session state data</returns>
        public override SessionStateStoreData GetItemExclusive(
            HttpContext context,
            string id,
            out bool locked,
            out TimeSpan lockAge,
            out object lockId,
            out SessionStateActions actions)
        {
            var e = GetSessionStoreItem(_bucket, context, true, id, out locked, out lockAge, out lockId, out actions);
            return (e == null) ? null : e.ToStoreData(context);
        }

        /// <summary>
        /// Retrieves the session data from the data source. If the lockRecord parameter is true 
        /// (in the case of GetItemExclusive), then the record is locked and we return a new lockId 
        /// and lockAge.
        /// </summary>
        /// <param name="bucket">Reference to the couchbase bucket we are using</param>
        /// <param name="context">HttpContext for the current request</param>
        /// <param name="acquireLock">True to aquire the lock, false to not aquire it</param>
        /// <param name="id">Session ID for the session</param>
        /// <param name="locked">Returns true if the session item is locked, otherwise false</param>
        /// <param name="lockAge">Returns the amount of time that the item has been locked</param>
        /// <param name="lockId">Returns lock identifier for the current request</param>
        /// <param name="actions">Indicates whether the current sessions is an uninitialized, cookieless session</param>
        /// <returns>SessionStateItem object containing the session state data</returns>
        public static SessionStateItem GetSessionStoreItem(
            IBucket bucket,
            HttpContext context,
            bool acquireLock,
            string id,
            out bool locked,
            out TimeSpan lockAge,
            out object lockId,
            out SessionStateActions actions)
        {
            locked = false;
            lockId = null;
            lockAge = TimeSpan.Zero;
            actions = SessionStateActions.None;

            var e = SessionStateItem.Load(bucket, id, false);
            if (e == null)
                return null;

            if (acquireLock) {
                // repeat until we can update the retrieved 
                // item (i.e. nobody changes it between the 
                // time we get it from the store and updates it s attributes)
                // Save() will return false if Cas() fails
                while (true) {
                    if (e.LockId > 0)
                        break;

                    actions = e.Flag;

                    e.LockId = _exclusiveAccess ? e.HeadCas : 0;
                    e.LockTime = DateTime.UtcNow;
                    e.Flag = SessionStateActions.None;

                    // try to update the item in the store
                    if (e.SaveHeader(bucket, id, _exclusiveAccess))
                    {
                        locked = true;
                        lockId = e.LockId;

                        return e;
                    }

                    // it has been modified between we loaded and tried to save it
                    e = SessionStateItem.Load(bucket, id, false);
                    if (e == null)
                        return null;
                }
            }

            locked = true;
            lockAge = DateTime.UtcNow - e.LockTime;
            lockId = e.LockId;
            actions = SessionStateActions.None;

            return acquireLock ? null : e;
        }

        /// <summary>
        /// Updates the session-item information in the session-state data store with values 
        /// from the current request, and clears the lock on the data
        /// </summary>
        /// <param name="context">HttpContext for the current request</param>
        /// <param name="id">Session ID for the session</param>
        /// <param name="item">The session state item to be stored</param>
        /// <param name="lockId">The lock identifier for the current request</param>
        /// <param name="newItem">True if this is a new session item, false if it is an existing item</param>
        public override void SetAndReleaseItemExclusive(
            HttpContext context,
            string id,
            SessionStateStoreData item,
            object lockId,
            bool newItem)
        {
            SessionStateItem e;
            do {
                if (!newItem) {
                    var tmp = (ulong)lockId;

                    // Load the entire item with CAS (need the DataCas value also for the save)
                    e = SessionStateItem.Load(_bucket, id, false);

                    // if we're expecting an existing item, but
                    // it's not in the cache
                    // or it's locked by someone else, then quit
                    if (e == null || e.LockId != tmp) {
                        return;
                    }
                } else {
                    // Create a new item if it requested
                    e = new SessionStateItem();
                }

                // Set the new data and reset the locks
                e.Timeout = item.Timeout;
                e.Data = (SessionStateItemCollection)item.Items;
                e.Flag = SessionStateActions.None;
                e.LockId = 0;
                e.LockTime = DateTime.MinValue;

                // Attempt to save with CAS and loop around if it fails
            } while (!e.SaveAll(_bucket, id, _exclusiveAccess && !newItem));
        }

        /// <summary>
        /// Releases a lock on an item in the session data store
        /// </summary>
        /// <param name="context">HttpContext for the current request</param>
        /// <param name="id">Session ID for the session</param>
        /// <param name="lockId">The lock identifier for the current request</param>
        public override void ReleaseItemExclusive(
            HttpContext context,
            string id,
            object lockId)
        {
            var tmp = (ulong)lockId;
            SessionStateItem e;
            do {
                // Load the header for the item with CAS
                e = SessionStateItem.Load(_bucket, id, true);

                // Bail if the entry does not exist, or the lock ID does not match our lock ID
                if (e == null || e.LockId != tmp) {
                    break;
                }

                // Attempt to clear the lock for this item and loop around until we succeed
                e.LockId = 0;
                e.LockTime = DateTime.MinValue;
            } while (!e.SaveHeader(_bucket, id, _exclusiveAccess));
        }

        /// <summary>
        /// Deletes item data from the session data store
        /// </summary>
        /// <param name="context">HttpContext for the current request</param>
        /// <param name="id">Session ID for the session</param>
        /// <param name="lockId">The lock identifier for the current request</param>
        /// <param name="item">Item to be deleted (ignored)</param>
        public override void RemoveItem(
            HttpContext context,
            string id,
            object lockId,
            SessionStateStoreData item)
        {
            var tmp = (ulong)lockId;
            var e = SessionStateItem.Load(_bucket, id, true);

            if (e != null && e.LockId == tmp) {
                SessionStateItem.Remove(_bucket, id);
            }
        }

        /// <summary>
        /// Updates the expiration date and time of an item in the session data store
        /// </summary>
        /// <param name="context">HttpContext for the current request</param>
        /// <param name="id">Session ID for the session</param>
        public override void ResetItemTimeout(
            HttpContext context,
            string id)
        {
            SessionStateItem e;
            do {
                // Load the item with CAS
                e = SessionStateItem.Load(_bucket, id, false);
                if (e == null) {
                    break;
                }

                // Try to save with CAS, and loop around until we succeed
            } while (!e.SaveAll(_bucket, id, _exclusiveAccess));
        }

        /// <summary>
        /// Function to set the session data expiration callback. Since this is not supported
        /// by our database, we simply return false here and do manual session cleanup.
        /// </summary>
        /// <param name="expireCallback">Session expiration callback to set</param>
        /// <returns>False, since we don't support this feature</returns>
        public override bool SetItemExpireCallback(
            SessionStateItemExpireCallback expireCallback)
        {
            return false;
        }

        #region [ SessionStateItem             ]

        /// <summary>
        /// Internal class for handling the storage of the session items in Couchbase
        /// </summary>
        public class SessionStateItem
        {
            public SessionStateItemCollection Data;
            public SessionStateActions Flag;
            public ulong LockId;
            public DateTime LockTime;

            // Timeout value for the session store item (defined in minutes)
            public int Timeout;

            public ulong HeadCas;
            public ulong DataCas;

            /// <summary>
            /// Writes the header to the stream
            /// </summary>
            /// <param name="s">Stream to write the header to</param>
            private void WriteHeader(
                Stream s)
            {
                var p = new Pair(
                    (byte)1,
                    new Triplet(
                        (byte)Flag,
                        Timeout,
                        new Pair(
                            LockId,
                            LockTime.ToBinary()))
                    );
                new ObjectStateFormatter().Serialize(s, p);
            }

            /// <summary>
            /// Saves the session store header into Couchbase
            /// </summary>
            /// <param name="bucket">Couchbase bucket to save to</param>
            /// <param name="id">Session ID</param>
            /// <param name="useCas">True to use a check and set, false to simply store it</param>
            /// <returns>True if the value was saved, false if not</returns>
            public bool SaveHeader(
                IBucket bucket,
                string id,
                bool useCas)
            {
                using (var ms = new MemoryStream())
                {
                    var ts = TimeSpan.FromMinutes(Timeout);

                    // Attempt to write the header and fail if the CAS fails
                    var retval = useCas
                        ? bucket.Upsert(_headerPrefix + id, ms.ToArray(), HeadCas, ts)
                        : bucket.Upsert(_headerPrefix + id, ms.ToArray(), ts);
                    return retval.Success;
                }
            }

            /// <summary>
            /// Saves the session store data into Couchbase
            /// </summary>
            /// <param name="bucket">Couchbase bucket to save to</param>
            /// <param name="id">Session ID</param>
            /// <param name="useCas">True to use a check and set, false to simply store it</param>
            /// <returns>True if the value was saved, false if not</returns>
            public bool SaveData(
                IBucket bucket,
                string id,
                bool useCas)
            {
                var ts = TimeSpan.FromMinutes(Timeout);
                using (var ms = new MemoryStream())
                using (var bw = new BinaryWriter(ms))
                {
                    Data.Serialize(bw);

                    // Attempt to save the data and fail if the CAS fails
                    var retval = useCas
                        ? bucket.Upsert(_dataPrefix + id, ms.ToArray(), DataCas, ts)
                        : bucket.Upsert(_dataPrefix + id, ms.ToArray(), ts);

                    return retval.Success;
                }
            }

            /// <summary>
            /// Saves the session store into Couchbase
            /// </summary>
            /// <param name="bucket">Couchbase bucket to save to</param>
            /// <param name="id">Session ID</param>
            /// <param name="useCas">True to use a check and set, false to simply store it</param>
            /// <returns>True if the value was saved, false if not</returns>
            public bool SaveAll(
                IBucket client,
                string id,
                bool useCas)
            {
                return SaveData(client, id, useCas) && SaveHeader(client, id, useCas);
            }

            /// <summary>
            /// Loads a sessions store header data from the passed in stream
            /// </summary>
            /// <param name="s">Stream to load the item from</param>
            /// <returns>Value read from the stream, null on failure</returns>
            private static SessionStateItem LoadHeader(
                Stream s)
            {
                var graph = new ObjectStateFormatter().Deserialize(s) as Pair;
                if (graph == null)
                    return null;

                if (((byte)graph.First) != 1)
                    return null;

                var t = (Triplet)graph.Second;
                var retval = new SessionStateItem {
                    Flag = (SessionStateActions)((byte)t.First),
                    Timeout = (int)t.Second
                };

                var lockInfo = (Pair)t.Third;

                retval.LockId = (ulong)lockInfo.First;
                retval.LockTime = DateTime.FromBinary((long)lockInfo.Second);

                return retval;
            }

            /// <summary>
            /// Loads a session state item from the bucket
            /// </summary>
            /// <param name="bucket">Couchbase bucket to load from</param>
            /// <param name="id">Session ID</param>
            /// <param name="metaOnly">True to load only meta data</param>
            /// <returns>Session store item read, null on failure</returns>
            public static SessionStateItem Load(
                IBucket bucket,
                string id,
                bool metaOnly)
            {
                return Load(_headerPrefix, _dataPrefix, bucket, id, metaOnly);
            }

            /// <summary>
            /// Loads a session state item from the bucket. This function is publicly accessible
            /// so that you have direct access to session data from another application if necesssary.
            /// We use this so our front end code can determine if an employee is logged into our back
            /// end application to give them special permissions, without the session data being actually common
            /// between the two applications.
            /// </summary>
            /// <param name="headerPrefix">Prefix for the header data</param>
            /// <param name="dataPrefix">Prefix for the real data</param>
            /// <param name="bucket">Couchbase bucket to load from</param>
            /// <param name="id">Session ID</param>
            /// <param name="metaOnly">True to load only meta data</param>
            /// <returns>Session store item read, null on failure</returns>
            public static SessionStateItem Load(
                string headerPrefix,
                string dataPrefix,
                IBucket bucket,
                string id,
                bool metaOnly)
            {
                // Read the header value from Couchbase
                var header = bucket.Get<byte[]>(_headerPrefix + id);
                if (header.Status == ResponseStatus.KeyNotFound) {
                    return null;
                }

                // Deserialize the header values
                SessionStateItem entry;
                using (var ms = new MemoryStream(header.Value)) {
                    entry = LoadHeader(ms);
                }
                entry.HeadCas = header.Cas;

                // Bail early if we are only loading the meta data
                if (metaOnly) {
                    return entry;
                }

                // Read the data for the item from Couchbase
                var data = bucket.Get<byte[]>(_dataPrefix + id);
                if (data.Value == null) {
                    return null;
                }
                entry.DataCas = data.Cas;

                // Deserialize the data
                using (var ms = new MemoryStream(data.Value)) {
                    using (var br = new BinaryReader(ms)) {
                        entry.Data = SessionStateItemCollection.Deserialize(br);
                    }
                }

                // Return the session entry
                return entry;
            }

            /// <summary>
            /// Creates a session store data object from the session data
            /// </summary>
            /// <param name="context">HttpContext to use</param>
            /// <returns>Session store data for this session item</returns>
            public SessionStateStoreData ToStoreData(
                HttpContext context)
            {
                return new SessionStateStoreData(Data, SessionStateUtility.GetSessionStaticObjects(context), Timeout);
            }

            /// <summary>
            /// Removes a session store item from the bucket
            /// </summary>
            /// <param name="bucket">Bucket to remove from</param>
            /// <param name="id">Session ID</param>
            public static void Remove(
                IBucket bucket,
                string id)
            {
                bucket.Remove(_dataPrefix + id);
                bucket.Remove(_headerPrefix + id);
            }
        }

        #endregion
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