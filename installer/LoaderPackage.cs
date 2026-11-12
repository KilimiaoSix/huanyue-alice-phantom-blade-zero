using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;

namespace HuanyueAliceInstaller
{
    internal static class LoaderPackage
    {
        private static readonly HashSet<string> DownloadHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "github.com", "release-assets.githubusercontent.com", "objects.githubusercontent.com", "github-releases.githubusercontent.com"
        };

        internal static byte[] Download(Action<int> progress)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            Uri location = new Uri(PinnedLoaderManifest.DownloadUrl);
            Stopwatch timer = Stopwatch.StartNew();
            try
            {
                for (int redirect = 0; redirect <= 4; redirect++)
                {
                    if (location.Scheme != Uri.UriSchemeHttps || !DownloadHosts.Contains(location.Host))
                        throw new InvalidDataException("UE4SS 下载地址已更改。");
                    var request = (HttpWebRequest)WebRequest.Create(location);
                    request.AllowAutoRedirect = false;
                    request.Timeout = 20000;
                    request.ReadWriteTimeout = 20000;
                    request.UserAgent = "HuanyueAlicePBZInstaller/1.0.1";
                    using (var response = (HttpWebResponse)request.GetResponse())
                    {
                        int status = (int)response.StatusCode;
                        if (status == 301 || status == 302 || status == 303 || status == 307 || status == 308)
                        {
                            string next = response.Headers["Location"];
                            if (String.IsNullOrWhiteSpace(next) || !Uri.TryCreate(location, next, out location))
                                throw new InvalidDataException("UE4SS 下载重定向无效。");
                            continue;
                        }
                        if (response.StatusCode != HttpStatusCode.OK)
                            throw new IOException("UE4SS 下载服务返回 HTTP " + status + "。");
                        if (response.ContentLength >= 0 && response.ContentLength != PinnedLoaderManifest.ArchiveSize)
                            throw new InvalidDataException("UE4SS 下载大小与固定版本不符。");

                        using (Stream input = response.GetResponseStream())
                        using (var buffer = new MemoryStream(PinnedLoaderManifest.ArchiveSize))
                        {
                            byte[] block = new byte[65536];
                            int lastPercent = -1;
                            int count;
                            while ((count = input.Read(block, 0, block.Length)) > 0)
                            {
                                if (buffer.Length + count > PinnedLoaderManifest.ArchiveSize)
                                    throw new InvalidDataException("UE4SS 下载大小与固定版本不符。");
                                if (timer.Elapsed > TimeSpan.FromMinutes(8))
                                    throw new IOException("UE4SS 下载超时，请检查网络后重试。");
                                buffer.Write(block, 0, count);
                                int percent = (int)(buffer.Length * 100 / PinnedLoaderManifest.ArchiveSize);
                                if (progress != null && percent != lastPercent) progress(percent);
                                lastPercent = percent;
                            }
                            return buffer.ToArray();
                        }
                    }
                }
            }
            catch (WebException error)
            {
                if (error.Response != null) error.Response.Dispose();
                throw new IOException("无法下载 UE4SS。请检查网络是否能访问 GitHub，然后重试。", error);
            }
            throw new IOException("UE4SS 下载发生过多重定向。");
        }

        internal static Dictionary<string, byte[]> ReadPayload(byte[] package)
        {
            if (package == null || package.Length != PinnedLoaderManifest.ArchiveSize || Sha256(package) != PinnedLoaderManifest.ArchiveSha256)
                throw new InvalidDataException("UE4SS 上游包校验失败，未开始安装。");

            using (var stream = new MemoryStream(package, false))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
                foreach (string name in PinnedLoaderManifest.Entries)
                {
                    ZipArchiveEntry entry = archive.GetEntry(name);
                    if (entry == null) throw new InvalidDataException("UE4SS 上游包缺少文件。\n" + name);
                    using (var memory = new MemoryStream())
                    using (Stream content = entry.Open())
                    {
                        content.CopyTo(memory);
                        byte[] bytes = memory.ToArray();
                        if (Sha256(bytes) != PinnedLoaderManifest.FileHashes[name])
                            throw new InvalidDataException("UE4SS 文件校验失败。\n" + name);
                        files.Add(name, bytes);
                    }
                }
                return files;
            }
        }

        private static string Sha256(byte[] bytes)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
    }
}
