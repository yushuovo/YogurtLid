using StardewValley;
using StardewValley.Locations;
using YogurtLid.Integrations;

namespace YogurtLid.Preview;

/// <summary>
/// 还原原版 <c>Farmer.doneEating</c> 的数值结算，用于得到“吃下去之后”的预计值。
/// 恢复量本身直接调用游戏方法，不复制恢复公式。
/// 若 Bulk Eating and Drinking 1.0.2 已安装、且会接管当前物品，则改按它的 EatBulk 批量规则计算。
/// </summary>
internal static class RecoveryCalculator
{
    // 星之果实会永久提升体力上限，预览所需的条几何会随之改变，第一版跳过。
    private const string StardropId = "(O)434";

    public static RecoveryGains Calculate(
        Item? item,
        Farmer player,
        BulkEatingAndDrinkingIntegration? bulkIntegration = null)
    {
        if (item is not StardewValley.Object food)
        {
            return RecoveryGains.None;
        }

        if (food.QualifiedItemId == StardropId)
        {
            return RecoveryGains.None;
        }

        int staminaPerItem = food.staminaRecoveredOnConsumption();
        int healthPerItem = food.healthRecoveredOnConsumption();

        if (bulkIntegration is { Enabled: true }
            && BulkEatingAndDrinkingIntegration.WillTakeOver(food)
            && bulkIntegration.TryReadSettings(out string eatAmount, out int drinkAmount))
        {
            return CalculateBulk(player, food, staminaPerItem, healthPerItem, eatAmount, drinkAmount);
        }

        return CalculateVanilla(player, food.QualifiedItemId, staminaPerItem, healthPerItem);
    }

    /// <summary>
    /// 目标 Mod 接管后的批量预览：份数受 Stack 与“自动补满”限制，结算先乘后裁剪。
    /// 注意目标不套用原版的沙漠节减半与 773/349/351 特例，这里也不能套用。
    /// </summary>
    private static RecoveryGains CalculateBulk(
        Farmer player,
        StardewValley.Object food,
        int staminaPerItem,
        int healthPerItem,
        string eatAmount,
        int drinkAmount)
    {
        bool isDrink = Game1.objectData.TryGetValue(food.ItemId, out var data) && data.IsDrink;
        int count = BulkRecoveryMath.ComputeCount(
            food.Stack,
            isDrink,
            eatAmount,
            drinkAmount,
            player.maxHealth,
            player.health,
            player.MaxStamina,
            player.Stamina,
            healthPerItem,
            staminaPerItem);

        // 目标在 count <= 0 时直接返回、不恢复，预览也必须不画。
        if (count <= 0)
        {
            return RecoveryGains.None;
        }

        (float staminaGain, float healthGain) = BulkRecoveryMath.ComputeGains(
            player.maxHealth,
            player.health,
            player.MaxStamina,
            player.Stamina,
            healthPerItem,
            staminaPerItem,
            count);

        return new RecoveryGains(staminaGain, healthGain);
    }

    private static RecoveryGains CalculateVanilla(
        Farmer player,
        string qualifiedItemId,
        int staminaGain,
        int healthGain)
    {
        // 与 doneEating 中的特例保持一致：这些物品是“直接设成上限”，不是加固定值。
        switch (qualifiedItemId)
        {
            case "(O)773":
                healthGain = player.maxHealth;
                break;
            case "(O)349":
                staminaGain = player.MaxStamina;
                break;
        }

        if (HasDesertFestivalHalving(player))
        {
            staminaGain = Math.Max(1, staminaGain / 2);
            healthGain = Math.Max(1, healthGain / 2);
        }

        float staminaAfter = Math.Min(player.MaxStamina, player.Stamina + staminaGain);
        float healthAfter = Math.Min(player.maxHealth, player.health + healthGain);

        return new RecoveryGains(
            Math.Max(0f, staminaAfter - player.Stamina),
            Math.Max(0f, healthAfter - player.health));
    }

    // 原版在沙漠节期间、且玩家激活了对应的卡利科雕像效果时，会把恢复量减半。
    private static bool HasDesertFestivalHalving(Farmer player)
    {
        return Utility.GetDayOfPassiveFestival("DesertFestival") > 0
            && player.currentLocation is MineShaft mine
            && mine.getMineArea() == 121
            && player.team.calicoStatueEffects.ContainsKey(6);
    }
}
