using System;
using System.Collections.Generic;

namespace HuanyueAliceInstaller
{
    internal static class PinnedLoaderManifest
    {
        internal const string ArchiveSha256 = "0b6b7f9430502c1fbeadb09740e586063dce42fe4f7d7a3ee7be51c9bd6ca304";
        internal const int ArchiveSize = 8757612;
        internal const string DownloadUrl = "https://github.com/KintaroModding/ue4ss/releases/download/v3.0.1-1195-g1ceefac8/phantombladezero_ue4ss_v3.0.1-1195-g1ceefac8.zip";

        internal static readonly string[] Entries =
        {
            "dwmapi.dll",
            "ue4ss/LICENSE",
            "ue4ss/UE4SS.dll",
            "ue4ss/MemberVariableLayout.ini",
            "ue4ss/UE4SS-settings.ini",
            "ue4ss/UE4SS_SDK_Backends/UE4SS.json",
            "ue4ss/UE4SS_Signatures/GNatives.lua",
            "ue4ss/UE4SS_Signatures/GUObjectArray.lua",
            "ue4ss/Mods/shared/UEHelpers/UEHelpers.lua"
        };

        internal static readonly Dictionary<string, string> FileHashes =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "dwmapi.dll", "bea75c36c6824a36a36e640b4f246b5eaff108a968e4648ed4500f5ef25d6946" },
                { "ue4ss/LICENSE", "99e1d8f8b7cb68e8b2313380ae3b0c8115e39dfbc443fcf21fd67ab74ce1d57e" },
                { "ue4ss/UE4SS.dll", "e1fb9a7e1d868afef6a091c2012e711c3ec47f05d100ba37f9631a4f75f31df2" },
                { "ue4ss/MemberVariableLayout.ini", "8c5017d84c4553632b2ffba320ff62d3998b273640e302a28b60d4be98703fd0" },
                { "ue4ss/UE4SS-settings.ini", "c96a3b1ac2f29f4aca3c72198eb557075ee30fc1d1186c332a025a8d2e66bd48" },
                { "ue4ss/UE4SS_SDK_Backends/UE4SS.json", "6b8c1725ff59a318b812d5952f63107778b4ceb22b882b297a3666ee9e06c8ff" },
                { "ue4ss/UE4SS_Signatures/GNatives.lua", "c7ba42f56c04c784217cbf5b50ffc11f8e66aa246df5813321c4d4a7a06d6598" },
                { "ue4ss/UE4SS_Signatures/GUObjectArray.lua", "0066ca60427679e1b9d86d18857eaab61047c0e6959a45a863d9f9dd3cb7efce" },
                { "ue4ss/Mods/shared/UEHelpers/UEHelpers.lua", "7476f02eef0c87bfeab169f2ffe2e704363faeaae4bf9f1bda574b1e34f4a0e9" }
            };
    }
}
