using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nebulator.Common;


// Test client for the server.


namespace Nebulator.Server
{
    public class TestClient
    {
        /// <summary>
        /// Run the test cases.
        /// </summary>
        /// <returns></returns>
        public async Task Run()
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://localhost:8888/nebulator/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                //// GET
                //Task ti = GetIndex(client);
                //await Task.WhenAll(ti);

                // POST
                Task<string> cmdTask = PostCommandAsync(client, "start");
                await Task.WhenAll(cmdTask);
                Console.WriteLine(cmdTask.Result);

                //// GET
                //Task<List<string>> errorsTask = GetErrorsAsync(client);
                //await Task.WhenAll(errorsTask);
                //errorsTask.Result.ForEach(s => Console.WriteLine(s));
            }
        }

        /// <summary>
        /// Send a command via POST.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="which"></param>
        /// <returns></returns>
        async Task<string> PostCommandAsync(HttpClient client, string which)
        {
            HttpResponseMessage response = await client.PostAsync("command/" + which, null);

            string resultContent = await response.Content.ReadAsStringAsync();

            // URI of the created resource.
            //return response.Headers.Location;

            return resultContent;
        }

        /// <summary>
        /// Send a GET.
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        async Task<List<string>> GetErrorsAsync(HttpClient client)
        {
            List<string> errors = new List<string>();
            errors.Add("type,file,line,message");

            HttpResponseMessage response = await client.GetAsync("errors");

            if (response.IsSuccessStatusCode)
            {
                string sresp = await response.Content.ReadAsStringAsync();
                var json = (JObject)JsonConvert.DeserializeObject(sresp);

                if(json != null)
                {
                    foreach (dynamic jerr in json["errors"])
                    {
                        errors.Add($"{jerr.type},{jerr.file},{jerr.line},{jerr.message}");
                    }
                }
                else
                {
                    errors.Add("?,?,?,?");
                }
            }

            return errors;
        }

        /// <summary>
        /// Send a GET.
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        async Task GetIndex(HttpClient client)
        {
            HttpResponseMessage response = await client.GetAsync("/");

            //if (response.IsSuccessStatusCode)
            //{
            //    string sresp = await response.Content.ReadAsStringAsync();
            //    var json = (JObject)JsonConvert.DeserializeObject(sresp);

            //    foreach (dynamic jerr in json["errors"])
            //    {
            //        errors.Add($"{jerr.type},{jerr.file},{jerr.line},{jerr.message}");
            //    }
            //}

            //return errors;
        }
    }
}
