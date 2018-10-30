using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft;
using Newtonsoft.Json.Linq;
using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Constants;
using Unosquare.Labs.EmbedIO.Modules;
using Unosquare.Net;
using Nebulator.Common;



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
        [WebApiHandler(HttpVerbs.Get, SelfHost.RELATIVE_PATH + "")]
        public bool GetIndex(WebServer server, HttpListenerContext context)
        {
            bool ret = true;
            ret = context.JsonResponse("I am the root!");
            return ret;
        }

        /// <summary>
        /// Incoming command:
        ///     POST /nebulator/command/{which}
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="context">The context.</param>
        /// <param name="which">Specific command.</param>
        /// <returns></returns>
        [WebApiHandler(HttpVerbs.Post, SelfHost.RELATIVE_PATH + "command/{which}")]
        public bool PostCommand(WebServer server, HttpListenerContext context, string which)
        {
            bool ret = true;

            var args = new SelfHost.RequestEventArgs() { Request = which, Param = "" };
            SelfHost.FireEvent(args);

            if (args.Result == null)
            {
                ret = context.JsonResponse($"Error for command: {which}");
            }
            else
            {
                ret = context.JsonResponse(args.Result);
            }

            return ret;
        }
    }
}
