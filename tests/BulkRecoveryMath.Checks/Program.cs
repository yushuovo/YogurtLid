using YogurtLid.Integrations;

int failures = 0;
int checks = 0;

void Check(string name, object actual, object expected)
{
    checks++;
    bool ok = Equals(actual, expected);
    if (!ok)
    {
        failures++;
    }

    Console.WriteLine($"{(ok ? "PASS" : "FAIL")} {name}: actual={actual} expected={expected}");
}

const string Full = BulkRecoveryMath.UntilFullOption;

// ---- 接管判定：与目标 Prefix 完全一致 ----
Check("接管: 普通食物", BulkRecoveryMath.IsTakenOver(true, 32, "(O)194"), true);
Check("不接管: 负恢复(树液)", BulkRecoveryMath.IsTakenOver(true, -4, "(O)92"), false);
Check("不接管: 星之果实", BulkRecoveryMath.IsTakenOver(true, 0, "(O)434"), false);
Check("不接管: 非 Object", BulkRecoveryMath.IsTakenOver(false, 32, "(O)194"), false);

// ---- 固定份数 ----
Check("固定 5 份, Stack 10, 缺体力少", BulkRecoveryMath.ComputeCount(10, false, "5", 1, 100, 100, 270, 270, 20, 10), 5);
Check("固定 5 份, Stack 2 只够 2", BulkRecoveryMath.ComputeCount(2, false, "5", 1, 100, 100, 270, 270, 20, 10), 2);
Check("固定文本非数字 -> 1 份", BulkRecoveryMath.ComputeCount(10, false, "abc", 1, 100, 100, 270, 270, 20, 10), 1);
Check("饮料 3 瓶, Stack 2 -> 2", BulkRecoveryMath.ComputeCount(2, true, Full, 3, 100, 100, 270, 270, 20, 10), 2);

// ---- 自动补满：最大缺口份数 ----
// 体力缺 100, 每份 10 -> 10 份；生命缺 0 -> 0 份；取 10 份。
Check("补满: 只缺体力 100/10", BulkRecoveryMath.ComputeCount(99, false, Full, 1, 100, 100, 270, 170, 20, 10), 10);
// 生命缺 50, 每份 10 -> 5 份；体力缺 100/20 -> 5 份；取 5 份。
Check("补满: 双缺口相同", BulkRecoveryMath.ComputeCount(99, false, Full, 1, 100, 50, 270, 170, 10, 20), 5);
// 生命缺 91, 每份 10 -> 向上取整 10 份；体力缺 0。
Check("补满: 生命 91/10 向上取整为 10", BulkRecoveryMath.ComputeCount(99, false, Full, 1, 100, 9, 270, 270, 10, 20), 10);
// 需要 10 份但只有 3 个 -> 3 份。
Check("补满: 受 Stack 限制", BulkRecoveryMath.ComputeCount(3, false, Full, 1, 100, 9, 270, 270, 10, 20), 3);
// 生命/体力都满, 每份 > 0 -> 缺口 0, 目标 Math.Max(1, ...) 仍吃 1 份。
Check("补满: 双满仍算 1 份", BulkRecoveryMath.ComputeCount(99, false, Full, 1, 100, 100, 270, 270, 10, 20), 1);
// 恢复量都是 0（但 Edibility>0）：needed=0 -> count=1，目标照样吃 1 份、数值不变。
Check("补满: 无恢复量算 1 份", BulkRecoveryMath.ComputeCount(99, false, Full, 1, 100, 0, 270, 0, 0, 0), 1);
// 饮料份数为 0：目标 count<=0 直接返回，不恢复。
Check("饮料 0 份 -> 0 预览", BulkRecoveryMath.ComputeCount(99, true, Full, 0, 100, 0, 270, 0, 10, 20), 0);
// 固定文本 0 份：目标 count=0 直接返回。
Check("固定 0 份 -> 0 预览", BulkRecoveryMath.ComputeCount(99, false, "0", 1, 100, 0, 270, 0, 10, 20), 0);

// ---- 批量增益：先乘后裁剪 ----
var nearCap = BulkRecoveryMath.ComputeGains(100, 95, 270f, 260f, 10, 20, 2);
Check("增益: 生命 95+20 裁到 100", nearCap.Health, 5f);
Check("增益: 体力 260+40 裁到 270", nearCap.Stamina, 10f);

var exact = BulkRecoveryMath.ComputeGains(100, 0, 270f, 0f, 10, 20, 5);
Check("增益: 生命 0+5*10=50", exact.Health, 50f);
Check("增益: 体力 0+5*20=100", exact.Stamina, 100f);

var huge = BulkRecoveryMath.ComputeGains(100, 0, 270f, 0f, 10, 20, 1000);
Check("增益: 超量件数裁到上限", huge, (270f, 100f));

var none = BulkRecoveryMath.ComputeGains(100, 100, 270f, 270f, 10, 20, 3);
Check("增益: 全满为 0", none, (0f, 0f));

Console.WriteLine();
Console.WriteLine($"checks={checks} failures={failures}");
return failures == 0 ? 0 : 1;
