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
        public void Go()
        {
            RunAsync().GetAwaiter().GetResult();
        }

        public async Task RunAsync()
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://localhost:8888/nebulator/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                Task<string> post = PostCommandAsync(client, "start");
                Task<List<string>> errors = GetErrorsAsync(client);

                await Task.WhenAll(post, errors);

                Console.WriteLine(post.Result);
                errors.Result.ForEach(s => Console.WriteLine(s));
            }
        }

        async Task<string> PostCommandAsync(HttpClient client, string which)
        {
            HttpResponseMessage response = await client.PostAsync(which, null);

            string resultContent = await response.Content.ReadAsStringAsync();

            // URI of the created resource.
            //return response.Headers.Location;

            return resultContent;
        }

        async Task<List<string>> GetErrorsAsync(HttpClient client)
        {
            List<string> errors = new List<string>();
            errors.Add("type,file,line,message");

            HttpResponseMessage response = await client.GetAsync("errors");

            if (response.IsSuccessStatusCode)
            {
                string sresp = await response.Content.ReadAsStringAsync();
                var json = (JObject)JsonConvert.DeserializeObject(sresp);

                foreach (dynamic jerr in json["errors"])
                {
                    errors.Add($"{jerr.type},{jerr.file},{jerr.line},{jerr.message}");
                }
            }

            return errors;
        }
    }
}
