using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using YogurtLid.Integrations;
using YogurtLid.Patches;

namespace YogurtLid.Preview;

/// <summary>
/// 在生命条/体力条上绘制“食用后新增恢复区间”：
/// 半透明呼吸填充 + 上下两条 2 像素粗线（顶线标恢复终点、底线把已有填充和预览分开），
/// 左右不封边。只做绘制，不改玩家数值、不消耗物品。
/// </summary>
internal sealed class RecoveryPreviewRenderer
{
    // 呼吸：0.1.10 把周期缩到 1.6 秒（0.1.9 的一半），范围仍为 0.12～0.72。
    private const float MinAlpha = 0.12f;
    private const float MaxAlpha = 0.72f;
    private const double PulsePeriodSeconds = 1.6;

    private const int BarInteriorOffsetX = 12;  // 原版条内部填充相对条左边缘的偏移
    private const int EdgeLineThickness = 2;    // 上下粗线的像素厚度
    private const float EdgeLineAlpha = 0.95f;  // 粗线不做呼吸，保证始终看得清边界

    /// <summary>顶线固定用棕色：标出恢复终点，又不会和绿色填充或奶白槽底混在一起。</summary>
    private static readonly Color TopLineBrown = new(120, 76, 34);

    /// <summary>真实时间计时器：时停时游戏时间会停，呼吸相位只由真实经过时间决定。</summary>
    private readonly Stopwatch clock = Stopwatch.StartNew();

    /// <summary>可选兼容：目标 Mod 接管后的动画期间不画预览（数值已由对方结算）。</summary>
    private readonly BulkEatingAndDrinkingIntegration bulkIntegration;

    public RecoveryPreviewRenderer(BulkEatingAndDrinkingIntegration bulkIntegration)
    {
        this.bulkIntegration = bulkIntegration;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!ShouldDraw())
        {
            return;
        }

        // 目标 Mod 的自定义进食/饮用动画期间，数值已经结算，不能再把“还要吃一批”的旧区间叠上去。
        if (this.bulkIntegration.IsAnimationPlaying())
        {
            return;
        }

        Farmer player = Game1.player;
        RecoveryGains gains = RecoveryCalculator.Calculate(GetPreviewItem(player), player, this.bulkIntegration);
        if (gains.IsEmpty)
        {
            return;
        }

        Vector2 staminaOrigin = HudGeometry.GetStaminaOrigin(player);

        // 原版条受击时会被随机偏移抖动（体力 X/Y、生命 X），覆盖层必须用同一偏移，
        // 否则就会像贴纸一样停在原地、和条错位。
        HudShakeCapture.GetOffsets(out int shakeDx, out int shakeDy, out int healthShakeDx);
        staminaOrigin = new Vector2(staminaOrigin.X + shakeDx, staminaOrigin.Y + shakeDy);

        float alpha = GetPulseAlpha();

        if (gains.Stamina > 0f)
        {
            float staminaAfter = Math.Min(player.MaxStamina, player.Stamina + gains.Stamina);
            this.DrawSegment(
                spriteBatch,
                staminaOrigin,
                HudGeometry.GetStaminaFillTop(staminaOrigin, player, player.Stamina),
                HudGeometry.GetStaminaFillTop(staminaOrigin, player, staminaAfter),
                GetBarColor(staminaAfter / player.MaxStamina),
                alpha);
        }

        if (gains.Health > 0f && HudGeometry.IsHealthBarVisible(player))
        {
            int healthAfter = Math.Min(player.maxHealth, player.health + (int)gains.Health);
            Vector2 healthOrigin = HudGeometry.GetHealthOrigin(staminaOrigin, player);
            healthOrigin.X -= healthShakeDx;
            this.DrawSegment(
                spriteBatch,
                healthOrigin,
                HudGeometry.GetHealthFillTop(healthOrigin, player, player.health),
                HudGeometry.GetHealthFillTop(healthOrigin, player, healthAfter),
                GetBarColor(healthAfter / (float)player.maxHealth),
                alpha);
        }
    }

    /// <summary>把 [topAfter, topBefore) 这一段画成呼吸填充 + 上下粗线。</summary>
    private void DrawSegment(
        SpriteBatch spriteBatch,
        Vector2 barOrigin,
        int topBefore,
        int topAfter,
        Color color,
        float alpha)
    {
        int height = topBefore - topAfter;
        if (height <= 0)
        {
            return;
        }

        int x = (int)barOrigin.X + BarInteriorOffsetX;
        int width = HudGeometry.InteriorWidth;

        spriteBatch.Draw(
            Game1.staminaRect,
            new Rectangle(x, topAfter, width, height),
            color * alpha);

        int thickness = Math.Min(EdgeLineThickness, height);
        spriteBatch.Draw(
            Game1.staminaRect,
            new Rectangle(x, topAfter, width, thickness),
            TopLineBrown * EdgeLineAlpha);
        spriteBatch.Draw(
            Game1.staminaRect,
            new Rectangle(x, topBefore - thickness, width, thickness),
            Darken(color) * EdgeLineAlpha);
    }

    /// <summary>与原版条同源的 红→绿 颜色：比例越高越绿。</summary>
    private static Color GetBarColor(float ratio)
    {
        return Utility.getRedToGreenLerpColor(Math.Clamp(ratio, 0f, 1f));
    }

    /// <summary>底线用比填充略深的颜色，把已有填充和预览分开。</summary>
    private static Color Darken(Color color)
    {
        return new Color(
            (int)(color.R * 0.55f),
            (int)(color.G * 0.55f),
            (int)(color.B * 0.55f));
    }

    /// <summary>
    /// 当前应该预览的物品：进食确认框中以“本次待吃的物品”为准；
    /// 确认后的进食动画期间继续预览这一份；其余时间回落到快捷栏当前选中物品。
    /// 目标 Mod 接管时它读的是 CurrentItem 及其 Stack，两者引用不同就不混用。
    /// </summary>
    private Item GetPreviewItem(Farmer player)
    {
        if (IsEatConfirmationOpen() && player.itemToEat is StardewValley.Object pending)
        {
            if (this.bulkIntegration.Enabled && !ReferenceEquals(pending, player.CurrentItem))
            {
                return player.CurrentItem;
            }

            return pending;
        }

        if (player.isEating && player.itemToEat is StardewValley.Object eating)
        {
            return eating;
        }

        return player.CurrentItem;
    }

    /// <summary>原版“要吃掉……吗？”确认框：询问对话 + `Eat` 问题键。</summary>
    private static bool IsEatConfirmationOpen()
    {
        return Game1.activeClickableMenu is DialogueBox { isQuestion: true }
            && Game1.currentLocation?.lastQuestionKey == "Eat";
    }

    /// <summary>原版 HUD 正在正常显示时才画；菜单期间只对进食确认框放行。</summary>
    private static bool ShouldDraw()
    {
        if (!Context.IsWorldReady || !Game1.displayHUD || !Game1.IsHudDrawn)
        {
            return false;
        }

        if (Game1.eventUp || Game1.farmEvent != null)
        {
            return false;
        }

        // 背包、箱子、商店、普通对话等其他菜单仍然隐藏预览。
        return Game1.activeClickableMenu == null || IsEatConfirmationOpen();
    }

    /// <summary>0～1 的平滑呼吸波（0.1.0 的观感，周期 3.2 秒）。</summary>
    private float GetPulseAlpha()
    {
        float wave = (float)(0.5 + (0.5 * Math.Sin(this.clock.Elapsed.TotalSeconds * Math.PI * 2.0 / PulsePeriodSeconds)));
        return MinAlpha + ((MaxAlpha - MinAlpha) * wave);
    }
}
