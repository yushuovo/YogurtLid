using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;

namespace YogurtLid.Preview;

/// <summary>
/// 复刻 <c>Game1.drawHUD</c> 里生命/体力条的几何，保证预览严格落在原版条内部。
/// 数值全部使用屏幕像素坐标，随窗口大小、UI 缩放与镜头位置逐帧重算。
/// </summary>
internal static class HudGeometry
{
    /// <summary>原版条内部填充宽度（外框 48 宽，左右各 12 边框）。</summary>
    public const int InteriorWidth = 24;

    /// <summary>每点体力对应的像素高度。</summary>
    public const float StaminaPixelsPerPoint = 0.625f;

    /// <summary>体力条左上角（与 drawHUD 中的 vector 一致）。</summary>
    public static Vector2 GetStaminaOrigin(Farmer player)
    {
        Rectangle titleSafe = Game1.graphics.GraphicsDevice.Viewport.Bounds;
        var origin = new Vector2(
            titleSafe.Right - 48 - 8,
            titleSafe.Bottom - 224 - 16 - (int)((player.MaxStamina - 270) * StaminaPixelsPerPoint));

        // 原版：室外地图小于视口时，把条往左收，避免压到地图边缘。
        if (Game1.isOutdoorMapSmallerThanViewport() && Game1.currentLocation is { } location)
        {
            origin.X = Math.Min(origin.X, -Game1.viewport.X + location.map.Layers[0].LayerWidth * 64 - 48);
        }

        return origin;
    }

    /// <summary>体力条填充区域的上边界（<paramref name="stamina"/> 为任意数值时）。</summary>
    public static int GetStaminaFillTop(Vector2 origin, Farmer player, float stamina)
    {
        return (int)origin.Y
            + 48
            + (int)(player.MaxStamina * StaminaPixelsPerPoint)
            - (int)(Math.Max(0f, stamina) * StaminaPixelsPerPoint);
    }

    /// <summary>生命条左上角（原版把体力条整体左移 56 像素）。</summary>
    public static Vector2 GetHealthOrigin(Vector2 staminaOrigin, Farmer player)
    {
        Rectangle titleSafe = Game1.graphics.GraphicsDevice.Viewport.Bounds;
        return new Vector2(
            staminaOrigin.X - 56,
            titleSafe.Bottom - 224 - 16 - (player.maxHealth - 100));
    }

    /// <summary>原版 HUD 是否正在显示生命条。</summary>
    public static bool IsHealthBarVisible(Farmer player)
    {
        return player.currentLocation is MineShaft or Woods or SlimeHutch or VolcanoDungeon
            || player.health < player.maxHealth;
    }

    /// <summary>生命条填充区域的上边界（<paramref name="health"/> 为任意数值时）。</summary>
    public static int GetHealthFillTop(Vector2 origin, Farmer player, int health)
    {
        int totalPixels = 168 + (player.maxHealth - 100);
        int filledPixels = (int)(health / (float)player.maxHealth * totalPixels);
        return (int)origin.Y + 48 + totalPixels - filledPixels;
    }
}
