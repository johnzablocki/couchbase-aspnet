using System;

namespace Couchbase.AspNet
{

    internal static class CouchbaseWebProviderExtensions
    {
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
