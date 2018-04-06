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

        #region Definitions
        /// <summary>Base URI.</summary>
        public const string BASE_URI = "http://localhost:8888" + RELATIVE_PATH;

        /// <summary>Where our app lives.</summary>
        public const string RELATIVE_PATH = "/nebulator/";

        /// <summary>Indicates success.</summary>
        public const string OK_NO_DATA = "";
        #endregion

        #region Events
        /// <summary>Incoming request.</summary>
        public static event EventHandler<RequestEventArgs> RequestEvent;

        /// <summary>Host request event args.</summary>
        public class RequestEventArgs : EventArgs
        {
            /// <summary>What do you want.</summary>
            public string Request { get; set; } = "";

            /// <summary>Optional parameter(s).</summary>
            public string Param { get; set; } = "";

            /// <summary>Returned from processing for digestion by client. Null means failed, otherwise a json string with optional data.</summary>
            public object Result { get; set; } = null;
        }

        /// <summary>
        /// Helper for generating events from controller modules.
        /// </summary>
        /// <param name="args"></param>
        public static void FireEvent(RequestEventArgs args)
        {
            RequestEvent?.Invoke(null, args);
        }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Start the server running.
        /// </summary>
        public void Run()
        {
            _server = new WebServer(BASE_URI, RoutingStrategy.Regex);

            // First, we will configure our web server by adding Modules. Please note that order DOES matter.

            // Later add sessions (LocalSessionModule), static file serving (StaticFilesSample)? See https://github.com/unosquare/embedio.

            // My app controller.
            _server.RegisterModule(new WebApiModule());
            _server.Module<WebApiModule>().RegisterController<NebController>();

            // Once we've registered our modules and configured them, we call the RunAsync() method.
            _server.RunAsync();

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
