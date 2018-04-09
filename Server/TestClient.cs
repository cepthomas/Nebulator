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
using System.Diagnostics;


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
                client.BaseAddress = new Uri(SelfHost.BASE_URI);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // GET - simple
                //Task ti = GetIndex(client);
                //await Task.WhenAll(ti);
                // Fire up the browser to show the content.
                Process.Start(SelfHost.BASE_URI);

                // POST
                Task<string> startTask = PostCommandAsync(client, "start");
                await Task.WhenAll(startTask);
                Console.WriteLine("==== start");
                Console.WriteLine(startTask.Result);

                // POST
                Task<string> compileTask = PostCommandAsync(client, "compile");
                await Task.WhenAll(compileTask);
                Console.WriteLine("==== compile");
                Console.WriteLine(compileTask.Result);
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
            return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : "Response failed";
        }

        /// <summary>
        /// Send a GET.
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        async Task<string> GetIndex(HttpClient client)
        {
            HttpResponseMessage response = await client.GetAsync("/");
            return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : "Response failed";
        }
    }
}
