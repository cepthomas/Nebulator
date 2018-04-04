using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
//using Nancy;
//using Nancy.ModelBinding;
using Newtonsoft;
using Newtonsoft.Json.Linq;
using Nebulator.Common;
using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Constants;
using Unosquare.Labs.EmbedIO.Modules;
using Unosquare.Net;
using System.Threading.Tasks;
//using System.Net;


// http://www.restapitutorial.com/lessons/httpmethods.html
// HTTP Verb   CRUD    Entire Collection (e.g. /customers) Specific Item (e.g. /customers/{id})
// POST    Create  201 (Created), 'Location' header with link to /customers/{id} containing new ID.
//    404 (Not Found), 409 (Conflict) if resource already exists..
// GET Read    200 (OK), list of customers. Use pagination, sorting and filtering to navigate big lists.
//    200 (OK), single customer. 404 (Not Found), if ID not found or invalid.
// PUT Update/Replace  405 (Method Not Allowed), unless you want to update/replace every resource in the entire collection.
//    200 (OK) or 204 (No Content). 404 (Not Found), if ID not found or invalid.
// PATCH   Update/Modify   405 (Method Not Allowed), unless you want to modify the collection itself.
//    200 (OK) or 204 (No Content). 404 (Not Found), if ID not found or invalid.
// DELETE  Delete  405 (Method Not Allowed), unless you want to delete the whole collection—not often desirable.
//    200 (OK). 404 (Not Found), if ID not found or invalid.


namespace Nebulator.Server
{
    public class NebController : WebApiController
    {
        #region Enums
        /// <summary>External request.</summary>
        public enum RemoteCommand { None, Start, Stop, Rewind, Compile }
        #endregion

        #region Events
        /// <summary>Incoming request.</summary>
        public event EventHandler<RemoteCommandEventArgs> RemoteCommandEvent;

        /// <summary>FastTimer event args.</summary>
        public class RemoteCommandEventArgs : EventArgs
        {
            /// <summary>What do you want.</summary>
            public RemoteCommand Command { get; set; } = RemoteCommand.Stop;

            /// <summary>Returned from command for digestion by client. Most common use is for compiler error info.</summary>
            public List<string> Result { get; set; } = new List<string>();
        }
        #endregion



        private const string RELATIVE_PATH = "/nebulator/";

        [WebApiHandler(HttpVerbs.Get, RELATIVE_PATH)]
        public bool GetIndex(WebServer server, HttpListenerContext context)
        {
            context.JsonResponse("At the bottom");
            return true;
        }

        //[WebApiHandler(HttpVerbs.Post, RELATIVE_PATH + "*")]
        //public bool GetAll(WebServer server, HttpListenerContext context)
        //{
        //    return true;
        //}


        /// <summary>
        /// Gets the people.
        /// This will respond to 
        ///     POST /nebulator/command/{which}
        /// 
        /// Notice the wildcard is important
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        [WebApiHandler(HttpVerbs.Post, RELATIVE_PATH + "command/{which}")]
        //[WebApiHandler(HttpVerbs.Post, RELATIVE_PATH + "command/*")]
        public bool PostCommand(WebServer server, HttpListenerContext context, string which)
        {
            RemoteCommand cmd = RemoteCommand.None;

            try
            {
                switch(which)
                {
                    case "start": cmd = RemoteCommand.Start; break;
                    case "stop": cmd = RemoteCommand.Stop; break;
                    case "rewind": cmd = RemoteCommand.Rewind; break;
                    case "compile": cmd = RemoteCommand.Compile; break;
                }

                if(cmd == RemoteCommand.None)
                {
                    throw new Exception($"Invalid command: {which}");
                }


                string ret = "{ \"result\" : \"OK\" }";
                if (RemoteCommandEvent != null)
                {
                    RemoteCommandEventArgs args = new RemoteCommandEventArgs() { Command = cmd };
                    RemoteCommandEvent.Invoke(this, args);

                    args.Result.Add("fdfdfddfd");
                    args.Result.Add("656565656");

                    if (args.Result.Count > 0)
                    {

                        return context.JsonResponse(args.Result);

                        JObject j = JObject.FromObject(new
                        {
                            result =
                            from v in args.Result
                            select new
                            {
                                val = v,
                            }
                        });

                        ret = j.ToString();
                    }


                }

                return context.JsonResponse("No data");




                //// read the last segment
                //var lastSegment = context.Request.Url.Segments.Last();

                //// if it ends with a / means we need to list people
                //if (lastSegment.EndsWith("/"))
                //    return context.JsonResponse(People);
                ////return context.JsonResponse(People.SelectAll());

                //// if it ends with "first" means we need to show first record of people
                //if (lastSegment.EndsWith("first"))
                //    return context.JsonResponse(People.First());
                ////return context.JsonResponse(People.SelectAll().First());

                //// otherwise, we need to parse the key and respond with the entity accordingly
                //if (!int.TryParse(lastSegment, out var key))
                //    throw new KeyNotFoundException("Key Not Found: " + lastSegment);

                //var single = People[key];
                ////var single = People.Single(key);

                //if (single != null)
                //    return context.JsonResponse(single);

                //throw new KeyNotFoundException("Key Not Found: " + lastSegment);
            }
            catch (Exception ex)
            {
                return context.JsonExceptionResponse(ex);
            }
        }


        string DoCommand(RemoteCommand cmd)
        {
            string ret = "{ \"result\" : \"OK\" }";
            if (RemoteCommandEvent != null)
            {
                RemoteCommandEventArgs args = new RemoteCommandEventArgs() { Command = cmd };
                RemoteCommandEvent.Invoke(this, args);

                if (args.Result.Count > 0)
                {
                    JObject j = JObject.FromObject(new
                    {
                        result =
                        from v in args.Result
                        select new
                        {
                            val = v,
                        }
                    });

                    ret = j.ToString();
                }
            }
            return ret;
        }

        string HelloName(dynamic parms)
        {
            var name = parms.name;

            return ($"Hello there {name}");
        }




        /////////////////////////////// from example ////////////////////////////////////////////////


        public class Person // : LiteModel
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public string EmailAddress { get; set; }
            public string PhotoUrl => $"http://www.gravatar.com/avatar/{EmailAddress}.png?s=100";
        }

        public List<Person> People { get; set; } = new List<Person>();

        private const string RELATIVE_PATH_EX = "/api/";



        /// <summary>
        /// Gets the people.
        /// This will respond to 
        ///     GET http://localhost:9696/api/people/
        ///     GET http://localhost:9696/api/people/1
        ///     GET http://localhost:9696/api/people/{n}
        /// 
        /// Notice the wildcard is important
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException">Key Not Found:  + lastSegment</exception>
        [WebApiHandler(HttpVerbs.Get, RELATIVE_PATH_EX + "people/*")]
        public bool GetPeople(WebServer server, HttpListenerContext context)
        {
            try
            {
                // read the last segment
                var lastSegment = context.Request.Url.Segments.Last();

                // if it ends with a / means we need to list people
                if (lastSegment.EndsWith("/"))
                    return context.JsonResponse(People);
                //return context.JsonResponse(People.SelectAll());

                // if it ends with "first" means we need to show first record of people
                if (lastSegment.EndsWith("first"))
                    return context.JsonResponse(People.First());
                //return context.JsonResponse(People.SelectAll().First());

                // otherwise, we need to parse the key and respond with the entity accordingly
                if (!int.TryParse(lastSegment, out var key))
                    throw new KeyNotFoundException("Key Not Found: " + lastSegment);

                var single = People[key];
                //var single = People.Single(key);

                if (single != null)
                    return context.JsonResponse(single);

                throw new KeyNotFoundException("Key Not Found: " + lastSegment);
            }
            catch (Exception ex)
            {
                return context.JsonExceptionResponse(ex);
            }
        }

        /// <summary>
        /// Echoes the request form data in JSON format
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        [WebApiHandler(HttpVerbs.Post, RELATIVE_PATH_EX + "echo/*")]
        public bool Echo(WebServer server, HttpListenerContext context)
        {
            try
            {
                var content = context.RequestFormDataDictionary();

                return context.JsonResponse(content);
            }
            catch (Exception ex)
            {
                return context.JsonExceptionResponse(ex);
            }
        }


        ///// <summary>
        ///// Posts the people Tubular model.
        ///// This will respond to 
        /////     GET http://localhost:9696/api/people/
        /////     GET http://localhost:9696/api/people/1
        /////     GET http://localhost:9696/api/people/{n}
        ///// 
        ///// Notice the wildcard is important
        ///// </summary>
        ///// <param name="server">The server.</param>
        ///// <param name="context">The context.</param>
        ///// <returns></returns>
        ///// <exception cref="KeyNotFoundException">Key Not Found:  + lastSegment</exception>
        //[WebApiHandler(HttpVerbs.Post, RelativePath + "people/*")]
        //public async Task<bool> PostPeople(WebServer server, HttpListenerContext context)
        //{
        //    try
        //    {
        //        var model = context.ParseJson<GridDataRequest>();
        //        var data = await People.SelectAllAsync();

        //        return context.JsonResponse(model.CreateGridDataResponse(data.AsQueryable()));
        //    }
        //    catch (Exception ex)
        //    {
        //        return context.JsonExceptionResponse(ex);
        //    }
        //}

        // TODO this is a regex version:
        //[WebApiHandler(HttpVerbs.Get, "/api/people/{id}")]
        //public bool GetPeople(WebServer server, HttpListenerContext context, int id)
        //{
        //    try
        //    {
        //        if (People.Any(p => p.Key == id))
        //        {
        //            return context.JsonResponse(People.FirstOrDefault(p => p.Key == id));
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return context.JsonExceptionResponse(ex);
        //    }
        //}
    }

    /// <summary>
    /// Sample helper
    /// </summary>
    public static class StaticFilesSample
    {
        /// <summary>
        /// Gets the HTML root path.
        /// </summary>
        /// <value>
        /// The HTML root path.
        /// </value>
        public static string HtmlRootPath
        {
            get
            {
                // var assemblyPath = Path.GetDirectoryName(typeof(Program).GetTypeInfo().Assembly.Location);
                var assemblyPath = ".";

#if DEBUG && !MONO
                // This lets you edit the files without restarting the server.
                return Path.Combine(Directory.GetParent(assemblyPath).Parent.Parent.FullName, "html");
#else
                // This is when you have deployed the server.
                return Path.Combine(assemblyPath, "html");
#endif
            }
        }

        /// <summary>
        /// Setups the specified server.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="useGzip">if set to <c>true</c> [use gzip].</param>
        public static void Setup(WebServer server, bool useGzip)
        {
            server.RegisterModule(new StaticFilesModule(HtmlRootPath));
            // The static files module will cache small files in ram until it detects they have been modified.
            server.Module<StaticFilesModule>().UseRamCache = false;
            server.Module<StaticFilesModule>().DefaultExtension = ".html";
            server.Module<StaticFilesModule>().UseGzip = useGzip;
        }
    }


#if _NANCY

        /// <summary>
        /// Constructor builds the routes.
        /// </summary>
        public NebModule(IMyContext mc) //?? : base("/dinner")
        {
            //_thing = thing;

            //var h = HttpContext.Current;


            var h = mc;

            Post["start"] = _ =>
            {
                return Response.AsText(DoCommand(RemoteCommand.Start));
            };

            Post["stop"] = _ =>
            {
                return Response.AsText(DoCommand(RemoteCommand.Stop));
            };

            Post["rewind"] = _ =>
            {
                return Response.AsText(DoCommand(RemoteCommand.Rewind));
            };

            Post["compile"] = _ =>
            {
                return Response.AsText(DoCommand(RemoteCommand.Compile));
            };

            //Get["errors"] = _ =>
            //{
            //    List<CompileErrorXX> errorxxxs = new List<CompileErrorXX>();

            //    for (int i = 0; i < 5; i++)
            //    {
            //        errorxxxs.Add(new CompileErrorXX() { ErrorType = $"ET{i + 10}", SourceFile = "fffff", LineNumber = i * 50, Message = "uui reurueui oruwe" });
            //    }

            //    JObject j = JObject.FromObject(new
            //    {
            //        errors =
            //        from e in errorxxxs
            //        select new
            //        {
            //            type = e.ErrorType,
            //            file = e.SourceFile,
            //            line = e.LineNumber,
            //            message = e.Message
            //        }
            //    });

            //    return Response.AsText(j.ToString());
            //};

            Get["/"] = _ => "Default";

            Get["/hello"] = _ => "Hello World";

            Get["hello/{name}"] = HelloName; // like this...


            Get["/Item/{id}"] = parms =>
            {
                var id = parms.id;
                //...
                return Response.AsJson("id101");
            };


            Before += ctx =>
            {
                var creq = ctx.Request;

                return null;
            };
        }

        string DoCommand(RemoteCommand cmd)
        {
            string ret = "{ \"result\" : \"OK\" }";
            if (RemoteCommandEvent != null)
            {
                RemoteCommandEventArgs args = new RemoteCommandEventArgs() { Command = cmd };
                RemoteCommandEvent.Invoke(this, args);

                if (args.Result.Count > 0)
                {
                    JObject j = JObject.FromObject(new
                    {
                        result =
                        from v in args.Result
                        select new
                        {
                            val = v,
                        }
                    });

                    ret = j.ToString();
                }
            }
            return ret;
        }

        string HelloName(dynamic parms)
        {
            var name = parms.name;

            return ($"Hello there {name}");
        }
#endif
}
