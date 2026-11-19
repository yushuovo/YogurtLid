using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using YogurtLid.Integrations;
using YogurtLid.Patches;
using YogurtLid.Preview;

namespace YogurtLid;

/// <summary>Mod 入口：只做事件挂载，绘制逻辑在 <see cref="RecoveryPreviewRenderer"/>。</summary>
public sealed class ModEntry : Mod
{
    private RecoveryPreviewRenderer? renderer;
    private BulkEatingAndDrinkingIntegration? bulkIntegration;

    public override void Entry(IModHelper helper)
    {
        this.bulkIntegration = new BulkEatingAndDrinkingIntegration(this.Monitor);
        this.renderer = new RecoveryPreviewRenderer(this.bulkIntegration);
        this.ApplyShakePatch();

        // HUD 绘制发生在 SpriteBatch 已 Begin 的 UI 空间里，RenderedHud 是插入覆盖层的最小改动点。
        helper.Events.Display.RenderedHud += this.OnRenderedHud;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;

        // 可选兼容在 GameLaunched 检测：此时所有 Mod 都已加载，能读到对方实例。
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
    }

    /// <summary>
    /// 只打一个小补丁：捕获原版 HUD 在受击抖动时使用的随机偏移，
    /// 让恢复预览跟着生命/体力条一起抖，而不是像贴纸一样钉在原地。
    /// </summary>
    private void ApplyShakePatch()
    {
        try
        {
            new Harmony(this.ModManifest.UniqueID).PatchAll();
        }
        catch (Exception ex)
        {
            // 补丁失败不影响主功能：预览仍会显示，只是不跟随受击抖动。
            this.Monitor.Log($"受击抖动跟随补丁未生效，预览位置将不跟随原版条抖动：{ex}", LogLevel.Warn);
        }
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        // 未安装目标 Mod 时这次调用直接返回，后续全部走 0.1.10 的原版路径。
        this.bulkIntegration?.Initialize(this.Helper);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        // 带上版本号：以后“看起来没变化”时，直接对照日志就能确认装的是哪一版。
        this.Monitor.Log(
            $"Yogurt Lid {this.ModManifest.Version} 已加载，选中食物时会在生命/体力条上显示恢复预览。",
            LogLevel.Debug);
    }

    private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    {
        this.renderer?.Draw(e.SpriteBatch);
    }
}
