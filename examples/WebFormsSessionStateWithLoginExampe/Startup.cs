using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(WebFormsSessionStateWithLoginExampe.Startup))]
namespace WebFormsSessionStateWithLoginExampe
{
    public partial class Startup {
        public void Configuration(IAppBuilder app) {
            ConfigureAuth(app);
        }
    }
}
