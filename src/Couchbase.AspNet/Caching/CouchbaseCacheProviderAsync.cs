using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Caching;
using System.Web;

namespace Couchbase.AspNet.Caching
{
    public class CouchbaseCacheProviderAsync : OutputCacheProviderAsync
    {
        public override object Get(
            string key)
        {
            throw new NotImplementedException();
        }

        public override object Add(
            string key,
            object entry,
            DateTime utcExpiry)
        {
            throw new NotImplementedException();
        }

        public override void Set(
            string key,
            object entry,
            DateTime utcExpiry)
        {
            throw new NotImplementedException();
        }

        public override void Remove(
            string key)
        {
            throw new NotImplementedException();
        }

        public override Task<object> GetAsync(
            string key)
        {
            throw new NotImplementedException();
        }

        public override Task<object> AddAsync(
            string key,
            object entry,
            DateTime utcExpiry)
        {
            throw new NotImplementedException();
        }

        public override Task SetAsync(
            string key,
            object entry,
            DateTime utcExpiry)
        {
            throw new NotImplementedException();
        }

        public override Task RemoveAsync(
            string key)
        {
            throw new NotImplementedException();
        }
    }
}
