using System;
using Common.Logging;

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

            return provider.Prefix != null && !id.StartsWith(provider.Prefix) ? string.Concat(provider.Prefix, "-", id) : id;
        }
    }
}
