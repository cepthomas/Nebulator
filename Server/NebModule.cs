using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Newtonsoft;
using Nebulator.Common;
using Newtonsoft.Json.Linq;



// http://www.restapitutorial.com/lessons/httpmethods.html
// HTTP Verb   CRUD    Entire Collection (e.g. /customers) Specific Item (e.g. /customers/{id})
// POST Create  
//    201 (Created), 'Location' header with link to /customers/{id} containing new ID.
//    404 (Not Found), 
//    409 (Conflict) if resource already exists..
// GET Read
//    200 (OK), list of customers. Use pagination, sorting and filtering to navigate big lists.
//    200 (OK), single customer.
//    404 (Not Found), if ID not found or invalid.
// PUT Update/Replace
//    405 (Method Not Allowed), unless you want to update/replace every resource in the entire collection.
//    200 (OK) or 204 (No Content).
//    404 (Not Found), if ID not found or invalid.
// PATCH  Update/Modify
//    405 (Method Not Allowed), unless you want to modify the collection itself.
//    200 (OK) or 204 (No Content).
//    404 (Not Found), if ID not found or invalid.
// DELETE  Delete
//    405 (Method Not Allowed), unless you want to delete the whole collection—not often desirable.
//    200 (OK).
//    404 (Not Found), if ID not found or invalid.


namespace Nebulator.Server
{
    public class NebModule : NancyModule
    {
        class CompileErrorXX // temp class
        {
            public string ErrorType { get; set; } = Utils.UNKNOWN_STRING;
            public string SourceFile { get; set; } = Utils.UNKNOWN_STRING;
            public int LineNumber { get; set; } = 0;
            public string Message { get; set; } = Utils.UNKNOWN_STRING;
        }

        public NebModule()
        {
            Post["start"] = _ =>
            {
                return $"You pressed start!";
            };

            Post["stop"] = _ =>
            {
                return $"You pressed stop!";
            };

            Post["rewind"] = _ =>
            {
                return $"You pressed rewind!";
            };

            Post["compile"] = _ =>
            {
                return $"You pressed compile!";
            };


            Get["errors"] = _ =>
            {
                List<CompileErrorXX> errorxxxs = new List<CompileErrorXX>();

                for (int i = 0; i < 5; i++)
                {
                    errorxxxs.Add(new CompileErrorXX() { ErrorType = $"ET{i + 10}", SourceFile = "fffff", LineNumber = i * 50, Message = "uui reurueui oruwe" });
                }

                JObject j = JObject.FromObject(new
                {
                    errors =
                    from e in errorxxxs
                    select new
                    {
                        type = e.ErrorType,
                        file = e.SourceFile,
                        line = e.LineNumber,
                        message = e.Message
                    }
                });

                return Response.AsText(j.ToString());
            };

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

        private string HelloName(dynamic parms)
        {
            var name = parms.name;

            return ($"Hello there {name}");
        }
    }
}
