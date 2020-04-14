using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using GR.Core.Extensions;
using IdentityModel.Client;
using Newtonsoft.Json;

namespace HttpClientApp.HttpClients
{
    public class CustomHttpClient : HttpClient
    {
        /// <summary>
        /// Proxy host
        /// </summary>
        protected string ProxyHost { get; set; }

        /// <summary>
        /// Proxy port
        /// </summary>
        protected int ProxyPort { get; set; }

        /// <summary>
        /// Welcome message
        /// </summary>
        protected string WelcomeMessage = "Welcome to custom HttpClient";

        /// <summary>
        /// Discovery response
        /// </summary>
        protected DiscoveryDocumentResponse DiscoveryDocument;

        /// <summary>
        /// Token
        /// </summary>
        protected string Token { get; set; }

        /// <summary>
        /// Is auth state
        /// </summary>
        private bool _isAuthenticated;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="proxyHost"></param>
        /// <param name="proxyPort"></param>
        public CustomHttpClient(string proxyHost, int proxyPort) : base(CreateProxyHttpHandler(proxyHost, proxyPort))
        {
            ProxyHost = proxyHost;
            ProxyPort = proxyPort;
        }

        /// <summary>
        /// Init
        /// </summary>
        public async Task StartAsync()
        {
            ShowWelcomeMessage();
            var disco = await this.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
            {
                Address = BaseAddress.ToString(),
                Policy = new DiscoveryPolicy
                {
                    RequireHttps = false
                }
            });

            if (!disco.IsError)
            {
                DiscoveryDocument = disco;
            }
            else
            {
                Console.WriteLine(disco.Error);
                throw new Exception(disco.Error);
            }
        }

        /// <summary>
        /// Show welcome message
        /// </summary>
        public void ShowWelcomeMessage()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            var topAndBottom = new string('-', Console.WindowWidth - 1);
            Console.Write(topAndBottom + "\n");
            Console.Write(topAndBottom + "\n");
            var padding = new string('-', (Console.WindowWidth - WelcomeMessage.Length) / 2);
            Console.Write(padding);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(WelcomeMessage);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(padding + "\n");
            Console.Write(topAndBottom + "\n");
            Console.Write(topAndBottom + "\n");
            Console.ForegroundColor = ConsoleColor.White;
        }


        /// <summary>
        /// Authorize
        /// </summary>
        /// <returns></returns>
        public virtual async Task AuthorizeViaConsoleAsync()
        {
            Console.Write("UserName:");
            var user = Console.ReadLine();
            Console.Write("Password:");
            var password = ReadPassword();

            var identityServerResponse = await this.RequestPasswordTokenAsync(new PasswordTokenRequest
            {
                Address = DiscoveryDocument.TokenEndpoint,
                ClientId = "xamarin password",
                ClientSecret = "secret",
                Scope = "openid profile offline_access core email",
                UserName = user,
                Password = password
            });

            if (identityServerResponse.IsError)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(identityServerResponse.Error);
                Console.ForegroundColor = ConsoleColor.White;
                throw new Exception(identityServerResponse.Error);
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Authentication successful!!!");
            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine("Set Bearer token");
            DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", identityServerResponse.AccessToken);
            Token = identityServerResponse.AccessToken;
            _isAuthenticated = true;
        }

        /// <summary>
        /// Get user info
        /// </summary>
        /// <returns></returns>
        public virtual async Task<UserInfoResponse> GetUserInfoAsync()
        {
            if (!_isAuthenticated) DisplayAndThrowError("Unauthorized");

            Console.WriteLine("Start to get user info");

            var userInfo = await this.GetUserInfoAsync(new UserInfoRequest
            {
                Address = DiscoveryDocument.UserInfoEndpoint,
                Token = Token
            });

            if (userInfo.IsError)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(userInfo.Error);
                Console.ForegroundColor = ConsoleColor.White;
                throw new Exception(userInfo.Error);
            }

            return userInfo;
        }

        /// <summary>
        /// Send
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="method"></param>
        /// <param name="url"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Task<HttpResponseMessage> SendJsonAsync<T>(HttpMethod method, string url, T value)
        {
            var data = value.ToDictionary<string>();
            var request = new HttpRequestMessage(method, url)
            {
                Content = new FormUrlEncodedContent(data)
            };

            return SendAsync(request);
        }


        /// <summary>
        /// Get and parse
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="httpContent"></param>
        /// <returns></returns>
        public async Task<T> ReadAsJsonAsync<T>(HttpContent httpContent)
        {
            await using var stream = await httpContent.ReadAsStreamAsync();
            var jsonReader = new JsonTextReader(new StreamReader(stream));

            return JsonSerializer.Deserialize<T>(jsonReader);
        }

        /// <summary>
        /// Post
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Task<HttpResponseMessage> PostJsonAsync<T>(string url, T value)
        {
            return SendJsonAsync(HttpMethod.Post, url, value);
        }

        /// <summary>
        /// Options
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Task<HttpResponseMessage> OptionsJsonAsync<T>(string url, T value)
        {
            return SendJsonAsync(HttpMethod.Options, url, value);
        }

        /// <summary>
        /// Head
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Task<HttpResponseMessage> HeadJsonAsync<T>(string url, T value)
        {
            return SendJsonAsync(HttpMethod.Head, url, value);
        }

        /// <summary>
        /// Put
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Task<HttpResponseMessage> PutJsonAsync<T>(string url, T value)
        {
            return SendJsonAsync(HttpMethod.Put, url, value);
        }

        /// <summary>
        /// Display to console and exit
        /// </summary>
        /// <param name="message"></param>
        private static void DisplayAndThrowError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
            throw new Exception(message);
        }


        /// <summary>
        /// Create proxy http handler
        /// </summary>
        /// <param name="proxyHost"></param>
        /// <param name="proxyPort"></param>
        /// <returns></returns>
        private static HttpClientHandler CreateProxyHttpHandler(string proxyHost, int proxyPort)
        {
            // First create a proxy object
            var proxy = new WebProxy($"http://{proxyHost}:{proxyPort}", false);

            // Now create a client handler which uses that proxy
            var httpClientHandler = new HttpClientHandler
            {
                Proxy = proxy,
                UseProxy = true,
                Credentials = CredentialCache.DefaultCredentials,
                CookieContainer = new CookieContainer(),
                UseDefaultCredentials = true,
                UseCookies = true
            };

            return httpClientHandler;
        }

        /// <summary>
        /// Read password
        /// </summary>
        /// <returns></returns>
        private static string ReadPassword()
        {
            var pass = "";
            do
            {
                var key = Console.ReadKey(true);
                // Backspace Should Not Work
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    pass += key.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                    {
                        pass = pass.Substring(0, (pass.Length - 1));
                        Console.Write("\b \b");
                    }
                    else if (key.Key == ConsoleKey.Enter)
                    {
                        break;
                    }
                }
            } while (true);
            Console.WriteLine();
            return pass;
        }

        /// <summary>
        /// Settings
        /// </summary>
        private static readonly JsonSerializer JsonSerializer = new JsonSerializer();
    }
}
