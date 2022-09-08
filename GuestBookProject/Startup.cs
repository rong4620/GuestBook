using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(GuestBookProject.Startup))]
namespace GuestBookProject
{
    /// <summary>
    /// Class Startup.
    /// </summary>
    public partial class Startup
    {
        /// <summary>
        /// Configurations the specified application.
        /// </summary>
        /// <param name="app">The application.</param>
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}