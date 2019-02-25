using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AuthConsoleApp
{
    static class Program
    {
        public static string Token = string.Empty;
        public static string FetchUrl = "http://localhost:4001/api/values";
        public static string LoginUrl = "http://localhost:4001/api/auth/login";
        public static string RefreshUrl = "http://localhost:4001/api/auth/refresh";
        static void Main(string[] args)
        {
            Timer timer = new Timer()
            {
                Interval = 2000,
                Enabled = true
            };

            timer.Elapsed += Timer_Elapsed;
            timer.Start();
            Console.ReadKey();
        }

        private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Fetch();
        }

        public static void Fetch()
        {
            Console.WriteLine($"Fetch started - {Token}");

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            var result = client.GetAsync(FetchUrl).GetAwaiter().GetResult();
            if (result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (result.Headers.Contains("Token-Expired") && bool.Parse(result.Headers.FirstOrDefault(x => x.Key == "Token-Expired").Value.FirstOrDefault()))
                {
                    Refresh();
                }
                else
                {
                    Login();
                }
                Fetch();
            }
            Console.WriteLine(result.Content.ReadAsStringAsync().GetAwaiter().GetResult());
        }

        public static void Login()
        {
            Console.WriteLine($"Login started - {Token}");

            HttpClient client = new HttpClient();
            var postContent = new
            {
                Username = "Kratos",
                Password = "Password@123"
            };
            var result = client.PostAsync(LoginUrl, new StringContent(JsonConvert.SerializeObject(postContent), Encoding.UTF8, "application/json")).GetAwaiter().GetResult();
            Token = JObject.Parse(result.Content.ReadAsStringAsync().GetAwaiter().GetResult())["token"].Value<string>();
        }

        public static void Refresh()
        {
            Console.WriteLine($"Refresh started - {Token}");

            HttpClient client = new HttpClient();
            var postContent = new
            {
                ExpiredToken = Token
            };
            var result = client.PostAsync(RefreshUrl, new StringContent(JsonConvert.SerializeObject(postContent), Encoding.UTF8, "application/json")).GetAwaiter().GetResult();
            Token = JObject.Parse(result.Content.ReadAsStringAsync().GetAwaiter().GetResult())["token"].Value<string>();
        }
    }
}
