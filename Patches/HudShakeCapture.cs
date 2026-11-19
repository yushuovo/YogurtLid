using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewValley;

namespace YogurtLid.Patches;

/// <summary>
/// 原版生命/体力条在受击时用 <see cref="Game1.random"/> 的随机偏移抖动条形，
/// 覆盖层如果按未抖动的坐标绘制，就会和条形错位。
/// 这里用极小范围的 Harmony 补丁替换 <see cref="Game1.drawHUD"/> 里的
/// <c>random.Next(-3, 4)</c> 调用：调用本身保持原语义（同一随机数、同一序列），
/// 只是顺手把结果记下来，绘制时按同样的偏移移动覆盖层。
/// </summary>
internal static class HudShakeCapture
{
    private const int ShakeMin = -3;
    private const int ShakeMax = 4;

    private static readonly List<int> Offsets = new();

    /// <summary>每帧 <see cref="Game1.drawHUD"/> 开始时清空，帧内偏移顺序固定。</summary>
    public static void Reset()
    {
        Offsets.Clear();
    }

    /// <summary>替换后的随机调用：语义与原版一致，只在 (-3, 4) 抖动时记录结果。</summary>
    public static int Next(Random random, int minValue, int maxValue)
    {
        int value = random.Next(minValue, maxValue);
        if (minValue == ShakeMin && maxValue == ShakeMax)
        {
            Offsets.Add(value);
        }

        return value;
    }

    /// <summary>
    /// 取本帧原版条形的抖动偏移：
    /// 顺序固定为「体力 X、体力 Y、生命 X」，按当前帧实际发生的抖动数量对应。
    /// </summary>
    public static void GetOffsets(out int staminaDx, out int staminaDy, out int healthDx)
    {
        staminaDx = 0;
        staminaDy = 0;
        healthDx = 0;

        int count = Offsets.Count;
        bool staminaShaking = Game1.staminaShakeTimer > 0;
        bool healthShaking = Game1.hitShakeTimer > 0;

        if (!staminaShaking && !healthShaking)
        {
            return;
        }

        if (staminaShaking && count >= 2)
        {
            staminaDx = Offsets[0];
            staminaDy = Offsets[1];
        }

        if (healthShaking)
        {
            int index = staminaShaking ? 2 : 0;
            if (count > index)
            {
                healthDx = Offsets[index];
            }
        }
    }
}

/// <summary>
/// 只改 <see cref="Game1.drawHUD"/> 一处：把 <c>callvirt Random::Next(int, int)</c>
/// 换成 <see cref="HudShakeCapture.Next"/>。该方法里另外还有一处 Next(动态边界)，
/// 换成静态调用后语义不变（内部仍调用原方法），因此不需要额外区分调用点。
/// 补丁失败时由 <c>ModEntry</c> 捕获并降级，覆盖层退回不跟随抖动。
/// </summary>
[HarmonyPatch(typeof(Game1), "drawHUD")]
internal static class HudShakeCapturePatch
{
    [HarmonyPrefix]
    private static void Prefix()
    {
        HudShakeCapture.Reset();
    }

    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo original = AccessTools.Method(typeof(Random), nameof(Random.Next), new[] { typeof(int), typeof(int) });
        MethodInfo replacement = AccessTools.Method(typeof(HudShakeCapture), nameof(HudShakeCapture.Next));

        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Callvirt && Equals(instruction.operand, original))
            {
                // 原地改操作码，保留原有的分支标签/异常块标记。
                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
            }

            yield return instruction;
        }
    }
}
