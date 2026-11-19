using System.Reflection;
using StardewModdingAPI;

namespace YogurtLid.Integrations;

/// <summary>
/// 可选的 Bulk Eating and Drinking 1.0.2 适配。
/// 只读：通过反射读取对方的 <c>ModEntry.Instance</c>、当前 <c>Config</c> 和两个动画标志，
/// 不引用对方程序集、不调用其进食方法、不读写任何配置文件。
/// 未安装、版本不符或成员结构不认识时整体退出（每次启动只警告一次），完全保留原版行为。
/// </summary>
internal sealed class BulkEatingAndDrinkingIntegration
{
    public const string UniqueId = "Darkmushu1.BulkEatingAndDrinking";
    public const string SupportedVersion = "1.0.2";

    private const string ModEntryTypeName = "BulkEatingAndDrinking.ModEntry";
    private const string ConfigPropertyName = "Config";
    private const string EatAmountPropertyName = "BulkEatAmount";
    private const string DrinkAmountPropertyName = "BulkDrinkAmount";
    private const string EatAnimationPropertyName = "isPlayingEatAnimation";
    private const string DrinkAnimationPropertyName = "isPlayingDrinkAnimation";

    private readonly IMonitor monitor;

    private object? targetInstance;
    private PropertyInfo? configProperty;
    private PropertyInfo? eatAmountProperty;
    private PropertyInfo? drinkAmountProperty;
    private PropertyInfo? eatAnimationProperty;
    private PropertyInfo? drinkAnimationProperty;
    private bool warned;

    public BulkEatingAndDrinkingIntegration(IMonitor monitor)
    {
        this.monitor = monitor;
    }

    /// <summary>检测、版本与反射全部成功后才为 true；批量预览与动画隐藏只在此后生效。</summary>
    public bool Enabled { get; private set; }

    public void Initialize(IModHelper helper)
    {
        try
        {
            IModInfo? modInfo = helper.ModRegistry.Get(UniqueId);
            if (modInfo == null)
            {
                // 未安装：保持 0.1.10 的全部行为，连日志都不写。
                return;
            }

            string installedVersion = modInfo.Manifest.Version.ToString();
            if (installedVersion != SupportedVersion)
            {
                this.WarnOnce($"检测到 {UniqueId} {installedVersion}，本版只适配 {SupportedVersion}；批量预览未启用，按原版单份预览处理。");
                return;
            }

            Type? modEntryType = FindLoadedType(ModEntryTypeName);
            if (modEntryType == null)
            {
                this.WarnOnce($"已安装 {UniqueId}，但未能定位 {ModEntryTypeName}；批量预览未启用，按原版单份预览处理。");
                return;
            }

            PropertyInfo? instanceProperty = modEntryType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            object? instance = instanceProperty?.GetValue(null);
            PropertyInfo? config = modEntryType.GetProperty(ConfigPropertyName, BindingFlags.Public | BindingFlags.Instance);
            if (instance == null || config == null || config.PropertyType.IsValueType)
            {
                this.WarnOnce($"读取 {ModEntryTypeName}.Instance / Config 失败；批量预览未启用，按原版单份预览处理。");
                return;
            }

            Type configType = config.PropertyType;
            PropertyInfo? eatAmount = configType.GetProperty(EatAmountPropertyName, BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo? drinkAmount = configType.GetProperty(DrinkAmountPropertyName, BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo? eatAnimation = modEntryType.GetProperty(EatAnimationPropertyName, BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo? drinkAnimation = modEntryType.GetProperty(DrinkAnimationPropertyName, BindingFlags.Public | BindingFlags.Instance);

            if (!HasExpectedShape(eatAmount, typeof(string))
                || !HasExpectedShape(drinkAmount, typeof(int))
                || !HasExpectedShape(eatAnimation, typeof(bool))
                || !HasExpectedShape(drinkAnimation, typeof(bool)))
            {
                this.WarnOnce("Bulk Eating and Drinking 的成员结构与 1.0.2 预期不符；批量预览未启用，按原版单份预览处理。");
                return;
            }

            this.targetInstance = instance;
            this.configProperty = config;
            this.eatAmountProperty = eatAmount;
            this.drinkAmountProperty = drinkAmount;
            this.eatAnimationProperty = eatAnimation;
            this.drinkAnimationProperty = drinkAnimation;
            this.Enabled = true;

            this.monitor.Log(
                $"已启用 Bulk Eating and Drinking {SupportedVersion} 兼容：只读取对方内存配置，批量预览与动画隐藏按实际接管状态生效。",
                LogLevel.Info);
        }
        catch (Exception ex)
        {
            this.WarnOnce($"Bulk Eating and Drinking 兼容初始化失败，回退原版单份预览：{ex.Message}");
        }
    }

    /// <summary>
    /// 读取对方当前配置。GMCM“恢复默认设置”会替换整个 <c>Config</c> 实例，
    /// 所以每次都从 <c>Instance.Config</c> 重新取，不能缓存配置对象本身。
    /// </summary>
    public bool TryReadSettings(out string eatAmount, out int drinkAmount)
    {
        eatAmount = BulkRecoveryMath.UntilFullOption;
        drinkAmount = 1;

        if (!this.Enabled || this.targetInstance == null || this.configProperty == null)
        {
            return false;
        }

        try
        {
            object? config = this.configProperty.GetValue(this.targetInstance);
            if (config == null)
            {
                return false;
            }

            eatAmount = this.eatAmountProperty?.GetValue(config) as string ?? BulkRecoveryMath.UntilFullOption;
            if (this.drinkAmountProperty?.GetValue(config) is int value)
            {
                drinkAmount = value;
            }

            return true;
        }
        catch (Exception ex)
        {
            this.WarnOnce($"读取 Bulk Eating and Drinking 配置失败，本次回退原版单份预览：{ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 目标接管后播放的自定义进食/饮用动画。这两个标志只有实际执行 <c>EatBulk</c> 时才为 true，
    /// 因此可以安全地用它判断“是否真的接管了”，而不是仅凭安装。
    /// </summary>
    public bool IsAnimationPlaying()
    {
        if (!this.Enabled || this.targetInstance == null)
        {
            return false;
        }

        try
        {
            return this.eatAnimationProperty?.GetValue(this.targetInstance) is true
                || this.drinkAnimationProperty?.GetValue(this.targetInstance) is true;
        }
        catch (Exception ex)
        {
            this.WarnOnce($"读取 Bulk Eating and Drinking 动画状态失败：{ex.Message}");
            return false;
        }
    }

    /// <summary>目标只在 edibility &gt; 0 且非星之果实时接管；这里与目标 Prefix 保持同一判定。</summary>
    public static bool WillTakeOver(StardewValley.Object food)
    {
        return food is not null
            && BulkRecoveryMath.IsTakenOver(true, food.Edibility, food.QualifiedItemId);
    }

    private void WarnOnce(string message)
    {
        if (this.warned)
        {
            return;
        }

        this.warned = true;
        this.monitor.Log(message, LogLevel.Warn);
    }

    private static bool HasExpectedShape(PropertyInfo? property, Type expectedType)
    {
        return property != null
            && property.PropertyType == expectedType
            && property.GetMethod != null
            && property.GetMethod.IsPublic
            && !property.GetMethod.IsStatic;
    }

    private static Type? FindLoadedType(string fullName)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (Type type in GetLoadableTypes(assembly))
            {
                if (type.FullName == fullName)
                {
                    return type;
                }
            }
        }

        return null;
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type != null)!;
        }
        catch
        {
            return Array.Empty<Type>();
        }
    }
}
