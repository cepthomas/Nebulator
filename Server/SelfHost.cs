using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Constants;
using Unosquare.Labs.EmbedIO.Modules;


namespace Nebulator.Server
{
    public class SelfHost : IDisposable
    {
        #region Fields
        /// <summary>The internal server component.</summary>
        WebServer _server = null;

        /// <summary>Resource management.</summary>
        bool _disposed = false;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Start the server running.
        /// </summary>
        public void Run()
        {
            string uri = "http://localhost:8888/nebulator/";
            _server = new WebServer(uri, RoutingStrategy.Regex);

            // First, we will configure our web server by adding Modules.
            // Please note that order DOES matter.
            // ================================================================================================
            // If we want to enable sessions, we simply register the LocalSessionModule
            // Beware that this is an in-memory session storage mechanism so, avoid storing very large objects.
            // You can use the server.GetSession() method to get the SessionInfo object and manupulate it.
            // You could potentially implement a distributed session module using something like Redis
            //_server.RegisterModule(new LocalSessionModule());


            // TODO add static file serving, web sockets? https://github.com/unosquare/embedio


            // My app controller.
            _server.RegisterModule(new WebApiModule());
            _server.Module<WebApiModule>().RegisterController<NebController>();

            // Once we've registered our modules and configured them, we call the RunAsync() method.
            _server.RunAsync();

            // Fire up the browser to show the content if we are debugging!
            Process.Start(uri);

            //Console.ReadKey(true);
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        public void Dispose()
        {
            _server?.Dispose();
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _disposed = true;
            }
        }
        #endregion
    }
}
