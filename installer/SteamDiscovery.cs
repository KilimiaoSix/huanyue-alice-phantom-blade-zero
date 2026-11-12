using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using Microsoft.Win32;

namespace HuanyueAliceInstaller
{
    internal sealed class SteamGame
    {
        internal string Root;
        internal string Build;
    }

    internal static class SteamDiscovery
    {
        internal const string AppId = "4115450";
        internal const string CompatibleBuild = "26367565";
        private const string ManifestName = "appmanifest_" + AppId + ".acf";
        private static readonly StringComparer Paths = StringComparer.OrdinalIgnoreCase;

        internal static IList<SteamGame> FindGames()
        {
            var roots = new HashSet<string>(Paths);
            foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
            {
                foreach (var view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
                {
                    ReadSteamRegistry(hive, view, roots);
                }
            }

            var results = new Dictionary<string, SteamGame>(Paths);
            foreach (string root in roots)
            {
                foreach (SteamGame game in FindGamesFromSteamRoot(root))
                    results[game.Root] = game;
            }
            return results.Values.OrderBy(game => game.Build != CompatibleBuild)
                .ThenBy(game => game.Root, Paths).ToList();
        }

        private static void ReadSteamRegistry(RegistryHive hive, RegistryView view, ISet<string> roots)
        {
            try
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, view))
                using (RegistryKey steam = baseKey.OpenSubKey(@"Software\Valve\Steam"))
                {
                    if (steam == null)
                        return;

                    foreach (string name in new[] { "SteamPath", "InstallPath" })
                    {
                        var path = steam.GetValue(name) as string;
                        AddLibrary(roots, path);
                    }
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (SecurityException) { }
            catch (IOException) { }
        }

        internal static IList<SteamGame> FindGamesFromSteamRoot(string steamRoot)
        {
            var games = new List<SteamGame>();
            var libraries = new HashSet<string>(Paths);
            if (!AddLibrary(libraries, steamRoot))
                return games;

            string primary = libraries.First();
            string path = Path.Combine(primary, "steamapps", "libraryfolders.vdf");
            if (File.Exists(path))
            {
                try
                {
                    VdfNode folders = ReadVdf(path).Child("libraryfolders");
                    if (folders != null && folders.Children != null)
                    {
                        foreach (var entry in folders.Children)
                        {
                            if (!IsNumber(entry.Key))
                                continue;
                            VdfNode modern = entry.Value.Child("path");
                            AddLibrary(libraries, modern == null ? entry.Value.Text : modern.Text);
                        }
                    }
                }
                catch (InvalidDataException) { }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                catch (SecurityException) { }
            }

            foreach (string library in libraries)
            {
                try
                {
                    string steamapps = Path.Combine(library, "steamapps");
                    string manifest = Path.Combine(steamapps, ManifestName);
                    if (File.Exists(manifest))
                        games.Add(ReadGame(steamapps));
                }
                catch (InvalidOperationException) { }
            }
            return games.OrderBy(game => game.Build != CompatibleBuild)
                .ThenBy(game => game.Root, Paths).ToList();
        }

        private static bool AddLibrary(ISet<string> libraries, string path)
        {
            if (String.IsNullOrWhiteSpace(path))
                return false;
            try
            {
                string fullPath = Path.GetFullPath(path);
                if (!Path.IsPathRooted(path) || !Directory.Exists(fullPath))
                    return false;
                libraries.Add(TrimEndSeparators(fullPath));
                return true;
            }
            catch (ArgumentException) { return false; }
            catch (IOException) { return false; }
            catch (NotSupportedException) { return false; }
            catch (SecurityException) { return false; }
        }

        internal static SteamGame Inspect(string selectedGameRoot)
        {
            if (String.IsNullOrWhiteSpace(selectedGameRoot))
                throw new InvalidOperationException("请选择《影之刃零》的 Steam 游戏目录。");

            string root;
            try
            {
                root = TrimEndSeparators(Path.GetFullPath(selectedGameRoot));
            }
            catch (ArgumentException error)
            {
                throw new InvalidOperationException("所选游戏目录无效。", error);
            }
            catch (IOException error)
            {
                throw new InvalidOperationException("无法读取所选游戏目录。", error);
            }
            catch (NotSupportedException error)
            {
                throw new InvalidOperationException("所选游戏目录的格式无效。", error);
            }
            catch (SecurityException error)
            {
                throw new InvalidOperationException("没有权限读取所选游戏目录。", error);
            }

            var gameDirectory = new DirectoryInfo(root);
            DirectoryInfo common = gameDirectory.Parent;
            DirectoryInfo steamapps = common == null ? null : common.Parent;
            if (steamapps == null || !Paths.Equals(common.Name, "common") ||
                !Paths.Equals(steamapps.Name, "steamapps"))
                throw new InvalidOperationException("请选择 Steam 库的 steamapps\\common 下的游戏目录。");

            SteamGame game = ReadGame(steamapps.FullName);
            if (!Paths.Equals(root, game.Root))
                throw new InvalidOperationException("所选目录与 Steam 清单记录的游戏目录不同。");
            return game;
        }

        private static SteamGame ReadGame(string steamapps)
        {
            try
            {
                string manifestPath = Path.Combine(steamapps, ManifestName);
                if (!File.Exists(manifestPath))
                    throw new InvalidOperationException("找不到 Steam App " + AppId + " 的安装清单。");

                VdfNode app = ReadVdf(manifestPath).Child("AppState");
                if (app == null || app.Value("appid") != AppId)
                    throw new InvalidOperationException("Steam 清单中的应用编号与《影之刃零》不符。");

                string installDirectory = app.Value("installdir");
                string build = app.Value("buildid");
                if (String.IsNullOrWhiteSpace(installDirectory) ||
                    installDirectory.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                    installDirectory == "." || installDirectory == ".." ||
                    installDirectory.EndsWith(".", StringComparison.Ordinal) ||
                    installDirectory.EndsWith(" ", StringComparison.Ordinal) ||
                    !IsNumber(build))
                    throw new InvalidOperationException("Steam 清单缺少有效的游戏目录或构建号。");

                string root = Path.Combine(steamapps, "common", installDirectory);
                if (!File.Exists(Path.Combine(root, "platform", "steam", "PBZ.exe")))
                    throw new InvalidOperationException("游戏目录中找不到 platform\\steam\\PBZ.exe。");
                return new SteamGame { Root = root, Build = build };
            }
            catch (InvalidDataException error)
            {
                throw new InvalidOperationException("Steam 安装清单格式无效。", error);
            }
            catch (IOException error)
            {
                throw new InvalidOperationException("无法读取 Steam 安装清单。", error);
            }
            catch (UnauthorizedAccessException error)
            {
                throw new InvalidOperationException("没有权限读取 Steam 安装清单。", error);
            }
            catch (SecurityException error)
            {
                throw new InvalidOperationException("没有权限读取 Steam 安装清单。", error);
            }
        }

        private static string TrimEndSeparators(string path)
        {
            string root = Path.GetPathRoot(path);
            return path.Length == root.Length ? path : path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static bool IsNumber(string value)
        {
            return !String.IsNullOrEmpty(value) && value.All(character => character >= '0' && character <= '9');
        }

        private static VdfNode ReadVdf(string path)
        {
            return new VdfReader(File.ReadAllText(path)).ReadObject(0);
        }

        private sealed class VdfNode
        {
            internal string Text;
            internal IDictionary<string, VdfNode> Children;

            internal VdfNode Child(string key)
            {
                VdfNode value;
                return Children != null && Children.TryGetValue(key, out value) ? value : null;
            }

            internal string Value(string key)
            {
                VdfNode value = Child(key);
                return value == null ? null : value.Text;
            }
        }

        private sealed class VdfReader
        {
            private readonly string source;
            private int position;

            internal VdfReader(string source)
            {
                this.source = source;
            }

            internal VdfNode ReadObject(int depth)
            {
                if (depth > 32)
                    throw new InvalidDataException("VDF 嵌套层级过多。");
                var children = new Dictionary<string, VdfNode>(StringComparer.OrdinalIgnoreCase);
                while (true)
                {
                    SkipSpaceAndComments();
                    if (position == source.Length)
                    {
                        if (depth != 0)
                            throw new InvalidDataException("VDF 对象缺少右括号。");
                        break;
                    }
                    if (source[position] == '}')
                    {
                        if (depth == 0)
                            throw new InvalidDataException("VDF 右括号位置无效。");
                        position++;
                        break;
                    }

                    string key = ReadText();
                    SkipSpaceAndComments();
                    if (position < source.Length && source[position] == '{')
                    {
                        position++;
                        children[key] = ReadObject(depth + 1);
                    }
                    else
                    {
                        children[key] = new VdfNode { Text = ReadText() };
                    }
                }
                return new VdfNode { Children = children };
            }

            private void SkipSpaceAndComments()
            {
                while (position < source.Length)
                {
                    if (Char.IsWhiteSpace(source[position]))
                    {
                        position++;
                    }
                    else if (source[position] == '/' && position + 1 < source.Length && source[position + 1] == '/')
                    {
                        position += 2;
                        while (position < source.Length && source[position] != '\r' && source[position] != '\n')
                            position++;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            private string ReadText()
            {
                if (position == source.Length || source[position] == '{' || source[position] == '}')
                    throw new InvalidDataException("VDF 缺少键或值。");

                if (source[position] != '"')
                {
                    int start = position;
                    while (position < source.Length && !Char.IsWhiteSpace(source[position]) &&
                        source[position] != '{' && source[position] != '}')
                        position++;
                    return source.Substring(start, position - start);
                }

                position++;
                var text = new StringBuilder();
                while (position < source.Length)
                {
                    char character = source[position++];
                    if (character == '"')
                        return text.ToString();
                    if (character == '\\' && position < source.Length &&
                        (source[position] == '"' || source[position] == '\\'))
                        character = source[position++];
                    text.Append(character);
                }
                throw new InvalidDataException("VDF 字符串缺少结束引号。");
            }
        }
    }
}
