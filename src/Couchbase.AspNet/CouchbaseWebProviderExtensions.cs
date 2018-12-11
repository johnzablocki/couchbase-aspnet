using System;
using Common.Logging;

namespace Couchbase.AspNet
{

    internal static class CouchbaseWebProviderExtensions
    {
        //Currently not used - when project relies on MS.Logging then we can switch
        public static string PrefixIdentifier(this ICouchbaseWebProvider provider, string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException(nameof(id));
            }
            return provider.Prefix == null ? id : string.Concat(provider.Prefix, "-", id);
        }
    }
}
