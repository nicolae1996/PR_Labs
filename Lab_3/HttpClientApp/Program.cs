using System;
using System.Globalization;
using System.Threading.Tasks;
using GR.Core.Helpers;
using HttpClientApp.HttpClients;

namespace HttpClientApp
{
    class Program
    {
        private static readonly bool IsDevelopment = false;
        static void Main()
        {
            Task.Run(async () =>
            {
                var baseAddress = IsDevelopment ? "http://localhost:9099" : "http://savecrypto.dev.indrivo.com";

                var client = new CustomHttpClient("127.0.0.1", 8000)
                {
                    BaseAddress = new Uri(baseAddress)
                };

                await client.StartAsync();
                await client.AuthorizeViaConsoleAsync();
                var user = await client.GetUserInfoAsync();
                Console.WriteLine(user.Json);

                var jsonResponse = await client.PostJsonAsync("/api/Profile/EditProfile", new
                {
                    FirstName = "test",
                    LastName = "test",
                    PhoneNumber = "+37369991207",
                    Birthday = DateTime.Now.ToString(CultureInfo.InvariantCulture),
                    Email = "nicolae.lupei.1996@gmail.com"
                });


                if (!jsonResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine(jsonResponse.StatusCode);
                }
                else
                {
                    var content = await jsonResponse.Content.ReadAsStringAsync();

                    var jsonObject = await client.ReadAsJsonAsync<ResultModel>(jsonResponse.Content);
                    if (jsonObject.IsSuccess)
                    {
                        Console.WriteLine(content);
                    }
                }

                Console.ReadKey();
            }).Wait();

            Console.ReadKey();
        }
    }
}
