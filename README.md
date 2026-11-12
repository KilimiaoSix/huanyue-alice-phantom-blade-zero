# 幻月爱丽丝 ·《影之刃零》

Windows Steam 外观替换 Mod。成年魂穿「黑伤（逃亡）」读入主线后会自动显示爱丽丝。换到其他服装后恢复相应的原版外观。

## 兼容性

- Steam App 4115450，Build **26367565**，游戏版本 **302256-17280**。
- 安装器面向 Windows 10 / 11，依赖 .NET Framework 4.8。

## 单 EXE 安装

1. 完全退出游戏，下载 [HuanyueAlice-PBZ-Setup-v1.0.1.exe](https://github.com/KilimiaoSix/huanyue-alice-phantom-blade-zero/releases/download/v1.0.1/HuanyueAlice-PBZ-Setup-v1.0.1.exe)。
2. 打开安装器。它会扫描 Steam 库并显示检测到的游戏路径，也可以点「浏览」选择 Steam 游戏根目录。
3. 点击「安装 v1.0.1」。首次安装会自动从 GitHub 下载并校验固定的 UE4SS 配套版本，下载量约 8.8 MB。已经装有完整兼容版 UE4SS 时可离线安装。
4. 从 Steam 启动游戏，读入穿「黑伤（逃亡）」的成年魂主线。

安装器会检查构建，覆盖 Mod 文件前会备份。检测到冲突的 UE4SS 文件时会在写入前停止，并显示文件路径。现有 UE4SS 设置会保留。当前 EXE 未做代码签名，可用发行页上的 `SHA256SUMS.txt` 核对。 [v1.0.1 发行页](https://github.com/KilimiaoSix/huanyue-alice-phantom-blade-zero/releases/tag/v1.0.1)

UE4SS 来自 UE4SS-RE，游戏配套包来自 KintaroModding。安装器只从固定上游包选择所需文件，并保留上游许可证。 [已验证的 UE4SS v3.0.1-1195-g1ceefac8](https://github.com/KintaroModding/ue4ss/releases/tag/v3.0.1-1195-g1ceefac8)

## 卸载与回退

退出游戏后打开同一个安装器，选择游戏目录，点击「卸载 Mod」。它会移除此发行版对应的 Mod 文件，保留 UE4SS、其他 Mod 和存档。安装器会显示备份位置，可按原目录结构复制回游戏目录。 [手动安装包与卸载说明](https://github.com/KilimiaoSix/huanyue-alice-phantom-blade-zero/tree/v1.0.0)

## 实机结果

Mod 资源与已实机验收的 R13 相同。确认了自动读档切换和跑动，后裙仍可能有少量穿模。闪避、单双手持握、受击、重生及双向换装曾在同构建的 R9 实测。部分实时过场支持替换，全游戏演出尚未逐镜验收。序章与 Q 发光未纳入本版验收。

## 构建安装器

在 Windows PowerShell 中提供已验收的 R13 ZIP。构建脚本会核对 ZIP 的 SHA-256，并生成包含该 ZIP 的 EXE。

```powershell
./installer/Build.ps1 -PackagePath ./HuanyueAlice-PBZ-v1.0.0-manual.zip -OutputPath ./HuanyueAlice-PBZ-Setup-v1.0.1.exe
```

## 素材署名

角色形象为幻月爱丽丝，模型母版制作使用了 Tripo3D。
