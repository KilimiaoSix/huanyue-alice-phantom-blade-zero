using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace HuanyueAliceInstaller
{
    internal static class ModInstaller
    {
        internal const string PackageHash = "624bf932fcd8a18519e8492fa27607f58fab35a0fd59ed87fe335023eec49220";
        private const string Container = "ProjectPBZ/Content/Paks/~mods/z_HuanyueAlice_NativeShader_Diagnostic_P";
        private const string Mod = "platform/steam/ue4ss/Mods/HuanyueAlice/";
        private static readonly string[] Managed = {
            Container + ".pak", Container + ".utoc", Container + ".ucas",
            Mod + "Scripts/main.lua", Mod + "Scripts/story_scope.lua", Mod + "enabled.txt"
        };

        internal static bool HasLoader(string root)
        {
            return File.Exists(Path.Combine(root, "platform/steam/dwmapi.dll"))
                && File.Exists(Path.Combine(root, "platform/steam/ue4ss/UE4SS.dll"));
        }

        internal static string Apply(string root, bool uninstall)
        {
            SteamGame game = SteamDiscovery.Inspect(root);
            string mutexName = @"Local\HuanyueAlicePBZ-" + HashBytes(Encoding.UTF8.GetBytes(game.Root.ToUpperInvariant()));
            using (var mutex = new Mutex(false, mutexName))
            {
                bool acquired;
                try { acquired = mutex.WaitOne(0); }
                catch (AbandonedMutexException) { acquired = true; }
                if (!acquired) throw new InvalidOperationException("另一个安装器正在处理这个游戏目录。");
                try { return ApplyLocked(game, uninstall, EnsureClosed); }
                finally { mutex.ReleaseMutex(); }
            }
        }

        private static string ApplyLocked(SteamGame game, bool uninstall, Action ensureClosed)
        {
            ensureClosed();
            if (!uninstall && game.Build != SteamDiscovery.CompatibleBuild)
                throw new InvalidOperationException("当前 Steam Build 为 " + game.Build + "，此版要求 26367565。");
            if (!uninstall && !HasLoader(game.Root))
                throw new InvalidOperationException("未找到 UE4SS。请先安装已实测的 v3.0.1-1195-g1ceefac8，再重试。");

            var payload = ReadPackage();
            var changes = new List<string>();
            foreach (string name in Managed)
            {
                string path = Path.Combine(game.Root, name);
                EnsureSafePath(game.Root, name);
                if (Directory.Exists(path)) throw new IOException("Mod 文件位置已被目录占用。\n" + path);
                bool exists = File.Exists(path);
                bool matches = exists && FileHash(path) == HashBytes(payload[name]);
                if (uninstall && exists && !matches)
                    throw new InvalidOperationException("发现其他版本或已修改的 Mod 文件，已停止卸载。\n" + path);
                if (uninstall ? exists : !matches)
                    changes.Add(name);
            }
            if (changes.Count == 0)
                return uninstall ? "本版文件已移除。" : "v1.0.0 已安装，文件校验通过。";

            string backup = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HuanyueAlicePBZ", "backups", DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(backup);
            File.WriteAllLines(Path.Combine(backup, "backup.txt"),
                new[] { game.Root, uninstall ? "Uninstall" : "Install" }.Concat(changes), Encoding.UTF8);
            foreach (string name in changes)
            {
                string path = Path.Combine(game.Root, name);
                if (!File.Exists(path)) continue;
                string snapshot = Path.Combine(backup, name);
                Directory.CreateDirectory(Path.GetDirectoryName(snapshot));
                File.Copy(path, snapshot);
                if (FileHash(path) != FileHash(snapshot)) throw new IOException("备份校验失败。\n" + snapshot);
            }

            var attempted = new List<string>();
            try
            {
                SteamGame currentGame = SteamDiscovery.Inspect(game.Root);
                if (!uninstall && currentGame.Build != SteamDiscovery.CompatibleBuild)
                    throw new InvalidOperationException("Steam 构建已变更，请重新检测游戏。");
                IEnumerable<string> order = uninstall ? changes.AsEnumerable().Reverse() : changes;
                foreach (string name in order)
                {
                    ensureClosed();
                    string path = Path.Combine(game.Root, name);
                    string snapshot = Path.Combine(backup, name);
                    EnsureSafePath(game.Root, name);
                    bool exists = File.Exists(path);
                    if (exists != File.Exists(snapshot) || (exists && FileHash(path) != FileHash(snapshot)))
                        throw new IOException("文件在备份后已变更。\n" + path);

                    if (uninstall) File.Delete(path);
                    else Replace(path, delegate(string temp) { File.WriteAllBytes(temp, payload[name]); });
                    attempted.Add(name);
                    if (!uninstall && FileHash(path) != HashBytes(payload[name]))
                        throw new IOException("安装文件校验失败。\n" + path);
                }
            }
            catch (Exception failure)
            {
                var failedRestores = new List<string>();
                foreach (string name in attempted.AsEnumerable().Reverse())
                {
                    try
                    {
                        string path = Path.Combine(game.Root, name);
                        string snapshot = Path.Combine(backup, name);
                        EnsureSafePath(game.Root, name);
                        if (File.Exists(snapshot)) Replace(path, delegate(string temp) { File.Copy(snapshot, temp); });
                        else File.Delete(path);
                    }
                    catch (Exception) { failedRestores.Add(name); }
                }
                string recovery = failedRestores.Count == 0 ? "已恢复操作前的 Mod 文件。" :
                    "以下文件未能自动恢复，请从备份恢复。\n" + string.Join("\n", failedRestores);
                throw new IOException(failure.Message + "\n" + recovery + "\n备份 " + backup, failure);
            }
            return (uninstall ? "已卸载 v1.0.0。" : "v1.0.0 安装完成，文件校验通过。") + "\n备份 " + backup;
        }

        private static void EnsureClosed()
        {
            foreach (string name in new[] { "PBZ", "ProjectPBZ", "ProjectPBZ-Win64-Shipping" })
            {
                Process[] processes = Process.GetProcessesByName(name);
                foreach (Process process in processes) process.Dispose();
                if (processes.Length != 0)
                    throw new InvalidOperationException("请退出《影之刃零》后再操作。");
            }
        }

        private static void EnsureSafePath(string root, string name)
        {
            string current = root;
            foreach (string part in name.Split('/'))
            {
                current = Path.Combine(current, part);
                try
                {
                    if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                        throw new IOException("Mod 目标目录或文件包含重解析点。\n" + current);
                }
                catch (FileNotFoundException) { return; }
                catch (DirectoryNotFoundException) { return; }
            }
        }

        private static void Replace(string path, Action<string> write)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + ".huanyue-" + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                write(temp);
                if (File.Exists(path)) File.Replace(temp, path, null);
                else File.Move(temp, path);
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
        }

        private static Dictionary<string, byte[]> ReadPackage()
        {
            using (Stream package = Assembly.GetExecutingAssembly().GetManifestResourceStream("AliceR13.zip"))
            {
                if (package == null || Hash(package) != PackageHash)
                    throw new InvalidDataException("安装包内容校验失败，请重新下载。");
                package.Position = 0;
                using (var archive = new ZipArchive(package, ZipArchiveMode.Read))
                {
                    var payload = new Dictionary<string, byte[]>(StringComparer.Ordinal);
                    foreach (string name in Managed)
                    {
                        ZipArchiveEntry entry = archive.GetEntry(name);
                        if (entry == null) throw new InvalidDataException("安装包缺少文件。\n" + name);
                        using (var memory = new MemoryStream())
                        using (Stream content = entry.Open())
                        {
                            content.CopyTo(memory);
                            payload.Add(name, memory.ToArray());
                        }
                    }
                    return payload;
                }
            }
        }

        private static string FileHash(string path)
        {
            using (var stream = File.OpenRead(path)) return Hash(stream);
        }

        private static string HashBytes(byte[] bytes)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }

        private static string Hash(Stream stream)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }
}
