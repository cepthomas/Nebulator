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


namespace Nebulator.Server
{
    /// <summary>
    /// Test client for the server.
    /// </summary>
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
                // Fire up the browser to show the default content.
                //Process.Start(SelfHost.BASE_URI);

                System.Threading.Thread.Sleep(100);

                // POST
                Console.WriteLine("TestClient POST open");
                Task<string> openTask = PostCommandAsync(client, "open", @"C:\Dev\Nebulator\Examples\dev.np");
                await Task.WhenAll(openTask);
                Console.WriteLine($"TestClient result: {openTask.Result}");

                // POST
                Console.WriteLine("TestClient POST compile");
                Task<string> compileTask = PostCommandAsync(client, "compile");
                await Task.WhenAll(compileTask);
                Console.WriteLine($"TestClient result: {compileTask.Result}");

                // POST
                Console.WriteLine("TestClient POST start");
                Task<string> startTask = PostCommandAsync(client, "start");
                await Task.WhenAll(startTask);
                Console.WriteLine($"TestClient result: {startTask.Result}");
            }
        }

        /// <summary>
        /// Send a command via POST.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="which">Specific command</param>
        /// <param name="arg">Optional argument</param>
        /// <returns></returns>
        async Task<string> PostCommandAsync(HttpClient client, string which, string arg = "")
        {
            string cmd = "command/" + which;
            if(arg != "")
            {
                cmd += "/" + arg;
            }
            HttpResponseMessage response = await client.PostAsync(cmd, null);
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
