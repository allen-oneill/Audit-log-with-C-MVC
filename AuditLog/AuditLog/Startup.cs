using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(AuditLog.Startup))]
namespace AuditLog
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
