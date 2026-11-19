namespace YogurtLid.Integrations;

/// <summary>
/// Bulk Eating and Drinking 1.0.2 的批量规则（纯计算，不依赖游戏类型，便于参数化验证）。
/// 逐项对应目标 Mod 的 <c>EatBulk</c>：批次数量、整数截断、按当前上限裁剪。
/// 目标接管时刻意不套用原版 <c>doneEating</c> 的沙漠节减半与特殊物品直接回满，
/// 预览只预测对方实际会发生的事，不替对方修正规则。
/// </summary>
internal static class BulkRecoveryMath
{
    /// <summary>目标 Mod 的“自动补满”选项文本，必须逐字符一致。</summary>
    public const string UntilFullOption = "Until Full Health & Stamina";

    /// <summary>
    /// 目标 Prefix 只在“Stardew 的 Object、可食用、且不是星之果实”时接管；
    /// 其余物品仍走原版，预览也必须走原版计算。
    /// </summary>
    public static bool IsTakenOver(bool isStardewObject, int edibility, string qualifiedItemId)
    {
        return isStardewObject && edibility > 0 && qualifiedItemId != "(O)434";
    }

    /// <summary>
    /// 目标实际会吃下的份数。与 <c>EatBulk</c> 的取值顺序一致：
    /// 饮料取 <c>BulkDrinkAmount</c>；食物取 <c>BulkEatAmount</c> 的整数（解析失败按 1 份）；
    /// “自动补满”按生命、体力两种缺口各自所需份数取较大者，再受当前 Stack 限制。
    /// 返回值可能 ≤ 0（目标会直接返回、不恢复），调用方不要擅自钳制成 1。
    /// </summary>
    public static int ComputeCount(
        int stack,
        bool isDrink,
        string? bulkEatAmount,
        int bulkDrinkAmount,
        int maxHealth,
        int health,
        float maxStamina,
        float stamina,
        int healthPerItem,
        int staminaPerItem)
    {
        if (isDrink)
        {
            return Math.Min(bulkDrinkAmount, stack);
        }

        if (bulkEatAmount != UntilFullOption)
        {
            // 与原版目标一致：解析失败时直接吃 1 份，不与 Stack 取 min。
            return int.TryParse(bulkEatAmount, out int amount) ? Math.Min(amount, stack) : 1;
        }

        int healthMissing = maxHealth - health;
        int staminaMissing = (int)(maxStamina - stamina);
        int healthNeeded = healthPerItem > 0 ? (healthMissing + healthPerItem - 1) / healthPerItem : 0;
        int staminaNeeded = staminaPerItem > 0 ? (staminaMissing + staminaPerItem - 1) / staminaPerItem : 0;
        return Math.Min(stack, Math.Max(1, Math.Max(healthNeeded, staminaNeeded)));
    }

    /// <summary>
    /// 按份数批量恢复并裁剪到当前上限，返回相对当前值的预计增量。
    /// 不能用“单份已裁剪增量 × 份数”，否则夹在区间里时会被裁两次。
    /// </summary>
    public static (float Stamina, float Health) ComputeGains(
        int maxHealth,
        int health,
        float maxStamina,
        float stamina,
        int healthPerItem,
        int staminaPerItem,
        int count)
    {
        int healthAfter = Math.Min(maxHealth, health + (healthPerItem * count));
        float staminaAfter = Math.Min(maxStamina, stamina + (staminaPerItem * count));
        return (Math.Max(0f, staminaAfter - stamina), Math.Max(0f, healthAfter - health));
    }
}
