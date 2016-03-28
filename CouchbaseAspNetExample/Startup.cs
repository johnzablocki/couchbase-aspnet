using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(CouchbaseAspNetExample.Startup))]
namespace CouchbaseAspNetExample
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
