using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mapster;
using Shared;
using Shared.Models;
using File = Shared.Models.File;

namespace Server
{
    public static class FileManager
    {
        /// <summary>
        /// Get user dir
        /// </summary>
        /// <returns></returns>
        public static string GetUserDirectory(User user)
        {
            var path = Path.Combine(AppContext.BaseDirectory, user.UserName);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

        /// <summary>
        /// Get files
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static IEnumerable<File> GetFiles(User user)
        {
            var dir = GetUserDirectory(user);
            var files = Directory.GetFiles(dir);
            return files.Select(x =>
            {
                var info = new FileInfo(x);
                return new File
                {
                    Name = info.Name,
                    Type = info.Extension,
                    Path = x,
                    ModifiedDate = info.LastWriteTime,
                    Size = info.Length
                };
            });
        }

        /// <summary>
        /// Get download files
        /// </summary>
        /// <param name="reqFiles"></param>
        /// <returns></returns>
        public static IEnumerable<FileResult> GetDownloadFiles(IEnumerable<File> reqFiles)
        {
            var response = new List<FileResult>();
            foreach (var file in reqFiles)
            {
                var o = file.Adapt<FileResult>();
                o.Blob = System.IO.File.ReadAllBytes(file.Path);

                response.Add(o);
            }

            return response;
        }

        /// <summary>
        /// Upload file
        /// </summary>
        /// <param name="user"></param>
        /// <param name="name"></param>
        /// <param name="blob"></param>
        /// <returns></returns>
        public static async Task<Result> UploadFile(User user, string name, byte[] blob)
        {
            var userDir = GetUserDirectory(user);
            var filePath = Path.Combine(userDir, name);

            if (System.IO.File.Exists(filePath))
            {
                return Result.Fail("File exists");
            }

            try
            {
                await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                fs.Write(blob);
            }
            catch (Exception e)
            {
                return Result.Fail(e.Message);
            }

            return Result.Ok();
        }
    }
}