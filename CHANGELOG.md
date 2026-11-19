# 变更记录

## 1.0.1 — 2026-11-19

- Adds optional Bulk Eating and Drinking 1.0.2 compatibility: batch recovery previews and animation handling only when that mod takes over consumption.
- Keeps vanilla single-serving preview and animation behavior when the optional mod is absent.
- Normalizes public version numbers across manifest, assembly, packages, Nexus and GitHub. Internal compatibility build 0.1.11 is now public 1.0.1.
- Adds a shared VERSION file, package validation, portable computation checks and source-use notice. Existing visual parameters are unchanged.

## 1.0.0 — Initial public release

Publicly displayed as 1.0 on Nexus; historical package/assembly version was 0.1.10. This entry documents the mapping and does not rename or rebuild the old release.

## Historical development records / 历史开发记录

The 0.1.x entries below retain their original development numbers. They are not the current public numbering scheme.


## 0.1.11

- 新增 **Bulk Eating and Drinking 1.0.2** 的可选兼容（`Darkmushu1.BulkEatingAndDrinking`，manifest 里为 `IsRequired: false`）。
- 未安装目标 Mod、版本不符或成员结构不认识时，完全保留 0.1.10 的行为：仍按原版 `doneEating` 结算单份。
- 检测到目标 Mod 且它实际接管当前物品（`Object`、`Edibility > 0`、非星之果实）时，预览改按对方的 `EatBulk` 规则批量计算：份数受 `Stack` 与配置文件里 `BulkEatAmount`（含“自动补满”）/`BulkDrinkAmount` 限制，先乘后按上限裁剪；不套用原版沙漠节减半与 `(O)773/349/351` 特例。
- 目标接管后的自定义进食/饮用动画期间不绘制预览（数值已由对方结算），避免把旧区间叠在已经恢复的条上。
- 兼容层只读对方的 `Instance`/`Config`/两个动画标志，不引用对方程序集、不调用对方方法、不读写对方配置；配置每次预览时重读，兼容 GMCM“恢复默认”。
- 视觉参数与 0.1.10 完全一致，未做任何调整。

## 0.1.10

- 呼吸周期从 3.2 秒缩到 1.6 秒，透明度范围不变（0.12～0.72）。

## 0.1.9

- 呼吸范围改为 0.12～0.72（周期仍 3.2 秒），比 0.1.8 的摆动更大。
- 顶线保持 0.1.8 的固定棕色 `(120, 76, 34)`（上一版用户实际装的仍是 0.1.7，所以没看到棕色）。
- 加载日志现在带版本号：`Yogurt Lid 0.1.9 已加载……`，方便确认装的是哪一版。

## 0.1.8

- 按观感微调：填充透明度提高到 0.32～0.62（周期仍为 3.2 秒），呼吸比 0.1.7 更明显一些。
- 顶线改为固定棕色 `(120, 76, 34)`，不参与呼吸，用来标出恢复终点；底线仍为填充色加深。
- 其余行为不变：单色填充、上下各 2 像素粗线、左右不封边、跟随受击抖动、进食确认框期间持续显示。

## 0.1.7

- 呼吸回到 0.1.0 的观感：填充透明度 0.16～0.42、周期 3.2 秒，不再叠加亮度脉冲与白色混合。
- 取消“区间底部到顶部逐像素渐变”，填充改为单一恢复色（按“吃下去之后”的比例取原版红→绿颜色），避免同一条预览里出现两段颜色。
- 上下两条 2 像素粗线保持不呼吸、始终清晰；跟随受击抖动、进食确认框期间持续显示等行为不变。
- 代码瘦身：删掉渐变逐行绘制、亮度缩放、白色混合及相关辅助方法，渲染器从 238 行减到 178 行。

## 0.1.6

- 修复“看不出呼吸”的问题：原先只改填充透明度（0.50～0.85），当预览色和它下面的条底色接近时（高体力/高血量段都是绿色、中段又都是黄色），合成结果变化很小，看起来像静止。现在呼吸同时驱动三件事：填充透明度 0.30～1.00、只作用于 RGB 的亮度脉冲 0.75～1.25、峰值时向白色混 20%；上下两条粗线也一起呼吸（0.50～1.00）。
- 呼吸周期改为 1.8 秒；相位仍只来自 `Stopwatch` 真实时间，进食确认等时停场景下继续推进。
- 呼吸参数继续集中在 `Preview/RecoveryPreviewRenderer.cs` 顶部常量，方便按手感微调。

## 0.1.5

- 去掉四周金色描边框，改成只在上下两条边画 2 像素粗线：顶线较亮（恢复终点），底线较暗（已有填充与预览的分界），左右不封边。
- 填充颜色改为跟随原版的 红→绿 渐变：区间底部是当前值对应的颜色、顶部是恢复后对应的颜色，纵向平滑过渡，不再一律用绿色块，避免和原版条的黄/红底色割裂。
- 呼吸继续作用于填充与上下两条线，周期 2 秒。

## 0.1.4

- 恢复预览现在会跟随原版生命/体力条的受击抖动：原版条用 `Random.Next(-3, 4)` 每帧随机位移，覆盖层改为读取同一批随机值并施加相同偏移（体力同时跟 X/Y，生命跟 X），不再像贴纸一样钉在原地。
- 实现方式：一处极小范围 Harmony transpiler，把 `Game1.drawHUD` 内的 `Random.Next(-3, 4)` 调用转发到捕获器，随机调用的次数、顺序、返回值都与原版完全一致，不改变游戏随机序列。
- 补丁应用失败时自动降级：预览照常显示，只退回不跟随抖动，并在 SMAPI 日志里给出警告。

## 0.1.3

- 重新启用呼吸：填充透明度与描边高光共用同一条真实时间正弦波（2 秒一个周期，填充 0.50～0.85、描边 0.35～1.00），整块预览一起“呼吸”，不再是只有描边在动。
- 填充增加轻微纵向受光渐变：区间上半段更亮，下半段更沉，避免整块纯色。
- 顶边加一条奶白色“酸奶盖”高光（1 像素亮线 + 1 像素柔光），把恢复终点和两侧金色描边区分开。
- 描边颜色微调：深金底线更深（120,78,4）、亮金高光更亮（255,220,90）。

## 0.1.2

- 预览改为**亮金色脉动描边**：只在“当前值 → 预计恢复值”的新增区间四周画一圈深金底线 + 内侧亮金高光，高光透明度按真实时间平滑脉动；区间内部改为稳定的恢复色填充，取消整体呼吸。
- 描边全部画在区间内部：2 像素深金边 + 内侧 1 像素亮金高光，不盖住原版条框、不越出槽外；恢复终点（顶边）用高光强调。
- 恢复区间只有几像素高时退化为“顶部亮线 + 底部暗线”，保证少量恢复也能看清目标线。
- 脉动相位只来自 `Stopwatch` 真实经过时间，不读取游戏时间、不依赖 `Update` 回调，进食确认框等时停场景下仍然推进。

## 0.1.1

- 提高预览可见度：不透明度改为 65%～90%，呼吸周期缩短到 1.5 秒，最暗阶段仍清晰可辨。
- 进食确认框（“要吃掉……吗？”）打开期间持续显示预览，并继续呼吸。
- 确认框中的预览改用本次待吃物品（`player.itemToEat`），不再依赖快捷栏当前槽位；取消后恢复普通手持预览。
- 呼吸改用真实时间计时，时停期间不中断。
- 确认后、数值结算前的进食动画期间继续预览同一份食物，结算完成后按新状态重算。

## 0.1.0

- 首个可构建版本。
- 选中快捷栏食物时，在生命条 / 体力条上以半透明呼吸覆盖层预览恢复区间。
- 恢复量直接复用游戏 `healthRecoveredOnConsumption` / `staminaRecoveredOnConsumption`，并复刻 `Farmer.doneEating` 的上限裁剪、沙漠节减半与 `(O)773` / `(O)349` 特例。
- 条几何复刻 `Game1.drawHUD`，覆盖层限制在 24 像素内部填充区。
- 跳过星之果实（会改变体力上限）。
