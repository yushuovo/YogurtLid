# Yogurt Lid / 酸奶盖



See your recovery before you eat. / 吃之前，看见恢复效果。

Current release / 当前版本：**1.0.1** · Author / 作者：**ayushua**

[Download on Nexus Mods / 下载](https://www.nexusmods.com/stardewvalley/mods/55704)

## English

Yogurt Lid previews the health and energy you would recover from the food selected in your toolbar, directly inside the vanilla HUD bars. A softly pulsing fill shows the added recovery, with a brown line marking the predicted endpoint.

The preview stays visible and animated in the vanilla eating confirmation dialog, even while the game is paused. It only previews recovery: it does not eat items or change your stats.

### Install and use

1. Install SMAPI: https://smapi.io/
2. Extract `YogurtLid 1.0.1.zip` into your game's `Mods` folder. You should have `Mods/YogurtLid/manifest.json` and `Mods/YogurtLid/YogurtLid.dll`.
3. Launch through SMAPI and select food in your toolbar.

No hotkey or configuration is needed. The health preview appears only when the vanilla health bar is visible. Full bars and non-positive recovery have no preview.

To uninstall, close the game and remove `Mods/YogurtLid`. This mod does not add items or save data.

### Tested environment and limitations

- User-tested on Windows with Stardew Valley 1.6.15 and SMAPI 4.5.2. Other versions and platforms have not been verified.
- SMAPI is the only required mod dependency.
- Optional compatibility: if [Bulk Eating and Drinking](https://www.nexusmods.com/stardewvalley/mods/32809) 1.0.2 by Darkmushu1 is installed, the preview follows its bulk EatBulk rules (batch size, config options, health/energy clamping) instead of a single serving, and hides during its animation. Without that mod, the mod works exactly as before. The user has reported that the compatibility issue is resolved; not every manual regression scenario has been individually recorded.
- No overflow/waste numbers, buff preview, inventory-hover preview, auto-eating, or configuration menu.
- Stardrops are excluded because they change maximum energy.
- Qi Seasoning dishes still need a targeted comparison against actual consumption; accuracy is not guaranteed for that case.
- Custom food effects and mods that replace the vanilla HUD have not been verified.
- Multiplayer has not been tested. The implementation only draws locally and does not send multiplayer messages or modify shared state.
- On a small window or high UI scale, the eating dialog may cover the bars.
- The HUD shake-following patch targets the tested game version. Game updates may require a mod update; a patch installation failure is logged and the preview remains available without shake following.

For bug reports, include game/SMAPI/mod versions, food name and quality, other HUD/food mods, a screenshot/video, and a SMAPI log link: https://smapi.io/log

## 中文

酸奶盖会在快捷栏选中食物时，直接在原版生命条和体力条内预览新增恢复区间。半透明填充缓慢呼吸，棕色顶线标出预计恢复终点。

原版“要吃掉……吗？”确认框出现后，预览仍持续显示和呼吸，方便在时停时决定是否进食。Mod 只做预览，不自动吃东西，也不修改玩家数值。

### 安装与使用

1. 安装 SMAPI：https://smapi.io/
2. 将 `YogurtLid 1.0.1.zip` 解压至游戏的 `Mods` 目录，确认得到 `Mods/YogurtLid/manifest.json` 和 `Mods/YogurtLid/YogurtLid.dll`。
3. 通过 SMAPI 启动游戏，选中快捷栏里的食物即可。

无需额外按键或配置。生命条沿用原版显示条件；已满值或恢复量非正时不显示对应预览。

卸载时关闭游戏，删除 `Mods/YogurtLid` 文件夹即可。本 Mod 不添加物品或存档数据。

### 已验证环境与限制

- 用户已在 Windows、Stardew Valley 1.6.15、SMAPI 4.5.2 环境下游戏内验收。其他版本和平台尚未验证。
- 除 SMAPI 外，无其他必需 Mod 依赖。可选兼容 Bulk Eating and Drinking 1.0.2（Darkmushu1）：安装后预览按对方的批量规则计算并隐藏其进食动画，未安装时保持原版单份预览和原有动画结算行为。用户已反馈该兼容问题解决；已有计算检查通过，不代表全部人工回归场景均已逐项记录。
- 不显示溢出或浪费数字，不预览增益，不支持背包悬停，不自动进食，无配置菜单。
- 星之果实改变体力上限，暂不预览。
- 齐氏调味料料理仍需专项验证，不承诺该场景的预览精度。
- 自定义食用效果及替换原版 HUD 的 Mod 尚未验证。
- 未做多人实测；实现仅绘制本地预览，不发送多人消息或修改共享状态。
- 小窗口或高 UI 缩放下，进食确认框可能遮住原版条。
- 跟随原版条抖动的补丁针对已验证游戏版本；游戏更新后可能需要适配。补丁安装失败会记录警告，预览仍可使用，但不跟随抖动。

反馈问题时请附游戏、SMAPI、Mod 版本，食物名称与品质，其他 HUD/食物 Mod，截图或录像，以及 SMAPI 日志链接：https://smapi.io/log

## Build / 构建

Requires .NET SDK 6.0.428 and local game assemblies. Copy `local.props.example` to `local.props` and set `GamePath`, or set `STARDEW_GAME_PATH`.

```powershell
dotnet build YogurtLid.csproj -c Release -p:EnableModDeploy=false
```

Run the existing computation checks with `dotnet run --project tests/BulkRecoveryMath.Checks/BulkRecoveryMath.Checks.csproj -c Release`. Package with `./scripts/package.ps1` (use `-Dotnet` for an isolated SDK path). The packaging script checks manifest and DLL versions against VERSION.

The project explicitly references game assemblies for offline builds. Do not redistribute game, SMAPI, or Harmony assemblies. Install the release ZIP rather than copying the entire build directory.

## Source permissions / 源码权限

Publicly viewable source; no open-source license is granted. See [NOTICE.md](NOTICE.md) for permissions. 源码公开供查看，修改和再发布等用途需取得作者许可。

## Versioning / 版本规则

The current version is stored in VERSION. Keep manifest.json, Nexus and GitHub release versions identical. Build and package with scripts/package.ps1; it checks manifest and DLL versions. Historical development 0.1.10 maps to public 1.0.0; 0.1.11 maps to public 1.0.1. 后续不再使用独立内部发布编号。
