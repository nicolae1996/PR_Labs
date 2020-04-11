using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace TcpClient
{
    class Program
    {
        /// <summary>
        /// Host
        /// </summary>
        private const string Host = "unite.md";

        /// <summary>
        /// Site port
        /// </summary>
        private const int Port = 80;

        /// <summary>
        /// Images
        /// </summary>
        private static ConcurrentQueue<string> _imagePaths = new ConcurrentQueue<string>();

        /// <summary>
        /// Main
        /// </summary>
        private static void Main()
        {
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine("Start to read data from host");
            var htmlString = HttpRequestAsync().GetAwaiter().GetResult();
            Console.WriteLine("Html string downloaded");
            Console.WriteLine("Start to extract images from string");
            var paths = ExtractImagesFromStringWithRegex(htmlString).ToList();
            Console.ForegroundColor = ConsoleColor.Green;
            var pathStr = string.Join("\n", paths);
            Console.WriteLine(pathStr);
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine("Extract images complete");
            _imagePaths = new ConcurrentQueue<string>(paths);
            DownloadAll();
        }

        /// <summary>
        /// Download all images
        /// </summary>
        private static void DownloadAll()
        {
            const int threadCount = 4;

            var dir = Path.Combine(AppContext.BaseDirectory, "images");
            if (Directory.Exists(dir)) Directory.Delete(dir, true);

            var tasks = new List<Task>();

            Console.WriteLine("Start to download images:");
            for (var i = 0; i < threadCount; i++)
            {
                var index = i;
                var task = Task.Factory.StartNew(() => RunThread(index));
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine("Download complete");
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Run thread
        /// </summary>
        /// <param name="threadIndex"></param>
        private static void RunThread(int threadIndex)
        {
            if (_imagePaths.IsEmpty) return;
            var extracted = _imagePaths.TryDequeue(out var path);
            if (!extracted) return;
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"Start to download image: {path} with thread: {threadIndex + 1}");
            DownloadImage(path);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"----------------Download complete for image: {path} with thread: {threadIndex + 1}");
            RunThread(threadIndex);
        }

        /// <summary>
        /// Extract image paths
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private static IEnumerable<string> ExtractImagesFromStringWithHtmlDocument(string source)
        {
            source = WebUtility.HtmlDecode(source);
            var doc = new HtmlDocument();
            doc.LoadHtml(source);
            var imagePaths = doc.DocumentNode
                .Descendants()
                .Where(x => x.Name == "img" && !string.IsNullOrEmpty(x.Attributes["src"].Value))
                .Select(x => x.GetAttributeValue("src", null))
                .ToList();

            return imagePaths;
        }

        /// <summary>
        /// Extract image paths
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private static IEnumerable<string> ExtractImagesFromStringWithRegex(string source)
        {
            const string expr = "<img.*?lazy=\"(.*?)\"[^\\>]+>";
            var result = Regex.Matches(source, expr);
            var images = result.Select(x => x.Groups[1].Value)
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .ToList();
            return images;
        }

        /// <summary>
        /// Download images
        /// </summary>
        /// <param name="path"></param>
        public static void DownloadImage(string path)
        {
            DownloadImageAsync(path).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Http request
        /// </summary>
        /// <returns></returns>
        private static async Task<string> HttpRequestAsync()
        {
            using var tcp = new System.Net.Sockets.TcpClient(Host, Port);
            await using var stream = tcp.GetStream();
            tcp.SendTimeout = 500;
            tcp.ReceiveTimeout = 1000;
            // Send request headers
            var builder = new StringBuilder();
            builder.AppendLine(GetHeaders());
            builder.AppendLine();

            var requestHeaders = builder.ToString();
            var header = Encoding.ASCII.GetBytes(requestHeaders);
            await stream.WriteAsync(header, 0, header.Length);

            // receive data
            string result;
            await using (var memory = new MemoryStream())
            {
                await stream.CopyToAsync(memory);
                memory.Position = 0;
                var data = memory.ToArray();

                var index = BinaryMatch(data, Encoding.ASCII.GetBytes("\r\n\r\n")) + 4;
                var headers = Encoding.ASCII.GetString(data, 0, index);
                memory.Position = index;

                if (headers.IndexOf("Content-Encoding: gzip", StringComparison.Ordinal) > 0)
                {
                    await using var decompressionStream = new GZipStream(memory, CompressionMode.Decompress);
                    await using var decompressedMemory = new MemoryStream();
                    decompressionStream.CopyTo(decompressedMemory);
                    decompressedMemory.Position = 0;
                    result = Encoding.UTF8.GetString(decompressedMemory.ToArray());
                }
                else
                {
                    result = Encoding.UTF8.GetString(data, index, data.Length - index);
                    //result = Encoding.GetEncoding("gbk").GetString(data, index, data.Length - index);
                }
            }

            //Debug.WriteLine(result);
            return result;
        }


        private static async Task DownloadImageAsync(string imagePath)
        {
            using (var tcp = new System.Net.Sockets.TcpClient(Host, Port))
            using (var stream = tcp.GetStream())
            {
                tcp.SendTimeout = 500;
                tcp.ReceiveTimeout = 1000;
                // Send request headers
                var builder = new StringBuilder();
                builder.AppendLine(GetHeaders(imagePath));
                builder.AppendLine();
                var header = Encoding.ASCII.GetBytes(builder.ToString());
                await stream.WriteAsync(header, 0, header.Length);
                // receive data
                var memory = new MemoryStream();


                await stream.CopyToAsync(memory);
                memory.Position = 0;
                var data = memory.ToArray();
                var index = BinaryMatch(data, Encoding.ASCII.GetBytes("\r\n\r\n")) + 4;
                var imgName = imagePath.Split("/").LastOrDefault();
                var dir = Path.Combine(AppContext.BaseDirectory, "images");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                var filePath = Path.Combine(dir, $"{imgName}");
                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(data, index, data.Length - index);
                }

                memory.Position = index;
                memory.Close();
            }
        }

        private static int BinaryMatch(byte[] input, byte[] pattern)
        {
            var sLen = input.Length - pattern.Length + 1;
            for (var i = 0; i < sLen; ++i)
            {
                var match = true;
                for (var j = 0; j < pattern.Length; ++j)
                {
                    if (input[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    return i;
                }
            }
            return -1;
        }

        private static string GetHeaders(string path = "/")
            => $@"GET {path} HTTP/1.1
Host: {Host}
Accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/*,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9
Connection: close
Pragma: no-cache
Upgrade-Insecure-Requests: 1
User-Agent: Programarea_In_Retea
Accept-Encoding: gzip, deflate, br
Accept-Language: en-US,en-GB;q=0.9,en;q=0.8,ro-MD;q=0.7,ro;q=0.6,fr;q=0.5";
    }
}
