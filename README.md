# 幻月爱丽丝 ·《影之刃零》

Windows Steam 外观替换 Mod。成年魂穿「黑伤（逃亡）」读入主线后会自动显示爱丽丝。换到其他服装后恢复相应的原版外观。

## 兼容性

- Steam App 4115450，Build **26367565**，游戏版本 **302256-17280**。
- 安装器面向 Windows 10 / 11，依赖 .NET Framework 4.8。
- 需要先安装 [UE4SS 的《影之刃零》配套版本](https://github.com/KintaroModding/ue4ss/releases/tag/v3.0.1-1195-g1ceefac8)。选择 `phantombladezero_ue4ss_v3.0.1-1195-g1ceefac8.zip`，按该版本说明安装。安装器检查游戏目录内的 UE4SS 文件。

## 安装

1. 完全退出游戏，从 [v1.0.0 Release](https://github.com/KilimiaoSix/huanyue-alice-phantom-blade-zero/releases/tag/v1.0.0) 下载 `HuanyueAlice-PBZ-Setup-v1.0.0.exe`。
2. 打开安装器。它会扫描 Steam 的库目录并显示检测到的游戏路径。如有多份安装，可以选择对应路径。未自动找到时，点「浏览」选择 Steam 游戏根目录。
3. 点击「安装 v1.0.0」。安装器仅向兼容构建写入 Mod 文件，覆盖前会备份旧文件并显示备份位置。
4. 从 Steam 启动游戏，读入穿「黑伤（逃亡）」的成年魂主线。

安装器当前未做代码签名。可通过 Release 中的 `SHA256SUMS.txt` 核对下载文件。手动安装时，解压 `HuanyueAlice-PBZ-v1.0.0-manual.zip`，把 `ProjectPBZ` 和 `platform` 两个目录复制到游戏根目录。三个容器请配套使用。

## 卸载与回退

退出游戏后，在安装器里选择同一个游戏目录，点击「卸载 v1.0.0」。它会移除与该发行版一致的 Mod 文件，并保留其他 Mod、UE4SS 和存档。检测到已修改或不同版本的文件时会停止，可按下述方式处理。

手动卸载时，移出 `ProjectPBZ/Content/Paks/~mods` 下以 `z_HuanyueAlice_NativeShader_Diagnostic_P` 为前缀的三个配套容器，以及 `platform/steam/ue4ss/Mods/HuanyueAlice`。若需回退到之前的版本，将安装时显示的备份中的对应文件按原目录结构复制回游戏目录。

## 实机结果

本版对应实机验收过的 R13。确认了自动读档切换和跑动，后裙仍可能有少量穿模。闪避、单双手持握、受击、重生及双向换装曾在同构建的 R9 实测。部分实时过场支持替换，全游戏演出尚未逐镜验收。序章与 Q 发光未纳入本版验收。

## 构建安装器

在 Windows PowerShell 中提供已验收 R13 ZIP 的路径。构建脚本会核对 ZIP 的 SHA-256，并生成包含该 ZIP 的本地 EXE。

```powershell
./installer/Build.ps1 -PackagePath ./HuanyueAlice-PBZ-v1.0.0-manual.zip -OutputPath ./HuanyueAlice-PBZ-Setup-v1.0.0.exe
```

## 素材署名

角色形象为幻月爱丽丝，模型母版制作使用了 Tripo3D。
