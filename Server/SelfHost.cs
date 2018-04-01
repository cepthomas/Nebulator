using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Nancy.Hosting.Self;
using Nancy.Conventions;


namespace Nebulator.Server
{
    public class SelfHost : IDisposable
    {
        #region Fields
        NancyHost _nancyHost = null;
        bool _disposed = false;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Start the server running.
        /// </summary>
        public void Start()
        {
            HostConfiguration hconf = new HostConfiguration();

            hconf.UrlReservations.CreateAutomatically = true;

            List<Uri> baseUris = new List<Uri>();
            baseUris.Add(new Uri("http://localhost:8888/nebulator/"));

            _nancyHost = new NancyHost(hconf, baseUris.ToArray());

            _nancyHost.Start();

            //Console.WriteLine("Nancy now listening - navigating to http://localhost:8888/nebulator/. Press enter to stop");

            // Process.Start("http://localhost:8888/nebulator/");
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        public void Dispose()
        {
            _nancyHost?.Dispose();
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

    //public class CustomRootPathProvider : IRootPathProvider
    //{
    //    public string GetRootPath()
    //    {
    //        return @"C:\Dev\Nebulator\Server\WebApp";
    //    }
    //}

    //public class ConventionsProvider : NancyConventions
    //{
    //    // edit per https://github.com/NancyFx/Nancy/wiki/View-location-conventions
    //    //DefaultNancyBootstrapper bb = new DefaultNancyBootstrapper();
    //    //NancyConventions conventions = new NancyConventions();
    //    //DefaultViewLocationConventions viewLocationConventions = new DefaultViewLocationConventions();
    //
    //    //Locations inspected: 
    //    //views/Command/CommandView-en-US
    //    //views/Command/CommandView
    //    //Command/CommandView-en-US
    //    //Command/CommandView
    //    //views/CommandView-en-US
    //    //views/CommandView
    //    //CommandView-en-US
    //    //CommandView
    //    //Root path: C:\Dev\Nebulator\Server\TestWebApp
    //
    //    // is: C:\Dev\Nebulator\Server\TestWebApp\CommandView.html
    //}

    //public class CustomBootstrapper : DefaultNancyBootstrapper
    //{
    //    protected override IRootPathProvider RootPathProvider
    //    {
    //        get { return new CustomRootPathProvider(); }
    //    }
    //    //protected override IRootPathProvider RootPathProvider => base.RootPathProvider;
    //
    //    protected override NancyConventions Conventions {  get { return new ConventionsProvider(); } }
    //}
}
