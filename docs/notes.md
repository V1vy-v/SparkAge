# 星火纪元（SparkAge）决策记录

> 本文件由"问答对话"维护：重要结论、方案决策追加写入。
> 执行对话开始任务前必须读取本文件 + docs/progress.md。

## 项目定位
- 简化版《文明6》求职 demo，核心卖点：回合制架构 + 联机链路。
- 技术栈：Unity 2022.3 LTS + 2D URP，C#，Git。

## 核心架构决策
1. **Core / Game 分层**：Core 为纯 C# 逻辑层（不依赖 UnityEngine），Game 为表现层。可单测、可复现；联机时整个状态序列化同步。
2. **Hex 坐标**：内部统一用轴向坐标 (q, r)，cube 思维辅助。
3. **地图**：程序化生成，值噪声 + seed 确定性 + 边缘强制水域。
4. **不用 Tilemap**：数据归属（Tilemap 把数据存在引擎组件里，违背"Core 唯一数据源"）、手绘用不上、小规模不需要合批优化。
5. **临时状态与永久数据分离**：地形是数据；选中/范围高亮是 UI 临时状态，用独立对象 + SetActive 切换。
6. **PPU = size/2**，保证六边形半径 = hexSize = 1，格子无缝。
7. **相机拖动**：抓取点模式（避免采样反馈振荡）。
8. **单位出生点**：BFS 从中心向外找第一个可行走格子（占位规则，独立成方法，将来可替换）。
9. **移动范围**：带地形代价的扩散（松弛思想），不是普通 BFS。返回值用 Dictionary<HexCoord, int>（每格剩余移动力），供路径/表现复用。
10. **选择逻辑（W2.2 评审）**：收敛为 SelectUnit / ClearSelection 两个操作；"显示范围"必须幂等（先全隐藏再显示），避免用 preClickUnit / isRemoved 这类"记住上次"的补丁变量。
11. **范围高亮对象（W2.2 评审）**：预创建 64 个（移动力 4 最多 61 格）+ 开关，不动态创建、不写通用对象池。通用 GameObjectPool 留给以后弹幕/特效/单位生成再单独实现。

## 待讨论 / 未来
- 联机：主机权威 + 状态同步（GameState 是同步单位）；Photon PUN2 或 NGO + Relay。
- W6 美术：邻居感知渲染、URP 2D 光照；目标"Polytopia 级"，不追文明6 3D 画面。

## 问答记录：FindPath 重写方案（2026-08-26）
- API：建议返回 PathResult{ Found, Path, Cost }（或 TryFindPath），空路径不再用 null/空表意模糊。
- open 集：小二叉堆替代线性扫描；用"重复入堆 + 弹出时校验过期"实现松弛，比 decrease-key 简单且不易错。
- 步骤：初始化 → 弹最小 f → 过期/closed 跳过 → 展开 6 邻（不可通行/未改进跳过）→ 回溯重建。
- 启发式继续用 HexCoord.DistanceTo（可采纳），closed 集合可启用。
- 可选：W2.3 移动力约束时按 g<=MovementLeft 剪枝。
- 配套单测：直线平原、山阻挡返回空、森林绕路、不可达、goal==start。
## 问答记录：手写 PriorityQueue 的位置与写法（2026-08-31）
- 位置：Assets/Scripts/Core/Hex/PriorityQueue.cs（命名空间 SparkAge.Core.Hex，当前唯一使用者是 Pathfinding，就近放）；未来多个模块复用再挪到 Core/DataStructures。
- 写法：数组(列表)实现二叉最小堆，Enqueue 尾部上浮、Dequeue 根下沉，比较左右子取小。O(log n)。
- 用法：PriorityQueue<HexCoord, int>，Enqueue(node, f)，Dequeue() 返回 f 最小节点；重复入堆 + 弹出时校验 g+h 丢弃过期项。
## 问答记录：FindPath 重写代码审查（2026-08-31）
- 主要错误：
  1. g 只初始化 start，从未更新 → 非起点 `g[cur]` 抛 KeyNotFoundException、松弛判断失效。
  2. f[start]=0 应为 g[start]+h(start)；且过期校验 `f[cur]!=g[cur]+h` 会把起点当过期丢弃（start≠goal 时 h>0）→ 算法空转。
  3. cameFrom 无条件覆盖 → 路径被更差路径污染。
  4. 松弛比较用 f（g+h）而非 g；不管是否改进都入队。
  5. 无路径重建，直接 return new PathResult() → 永远 Found=false。
  6. 次要：cost==-1 应改 <0；open 实际是"已见集合"却不移除，命名误导。
- 建议骨架：不维护 f 字典，堆优先级即 f；用 closed 集合（h 一致 → 首次弹出即最优）处理过期重复项；g 负责松弛、f、最终 Cost。
## 问答记录：FindPath 复查（2026-08-31）
- 核心循环已正确：closed 集合、g 松弛更新、f 组成、cameFrom 仅改进时记录，均对。
- 遗留崩溃 bug：路径重建 do-while 在 `goal` 与 `start` 相邻（一步到达）或 `start==goal` 时访问不存在的 `cameFrom[start]` → KeyNotFoundException。
- 修复：改用 `for (var c=goal; !c.Equals(start); c=cameFrom[c]) path.Add(c);` 再 Reverse。
- 建议：无路时返回空 List 而非 null；删除未用的 `using SparkAge.Core.Map;`。
## 问答记录：FindPath 与初始移动力（2026-08-31）
- 确认：FindPath 是通用寻路（只求总代价最小），不含移动力上限——移动力约束放调用方。
- W2.3 接法：MoveUnit 先校验目标在 GetReachableTiles 可达集内（TryGetValue），再 FindPath 求路径，扣减 MovementLeft -= Cost，沿路径移动后刷新范围。
- 可选：给 FindPath 加 maxCost 参数在展开处剪枝，但地图小非必须。
- 注意：Core 是纯 C#，Console.WriteLine 在 Unity 控制台不可见，错误提示应放表现层 Debug.Log，或 Core 返回结果码。
## 问答记录：GetReachableTiles 改返回 List<HexCoord>（2026-08-31）
- 改动：内部仍用 Dictionary<HexCoord,int> 跑扩散（松弛必须按格存剩余值），最后 `return new List<HexCoord>(dict.Keys);` 投影。
- 注意：会丢失"每格剩余移动力"（当初用 Dictionary 的原因）；若高亮要分级/移动要扣减，需 out 参数或配套方法返回字典。
## 问答记录：剩余移动力现阶段确实用不上（2026-08-31，修正先前建议）
- 结论：当前 demo（高亮 + 范围内移动）只需要"哪些格可达"这个集合；移动扣减用 A* 路径总代价即可，不需要每格剩余值。
- 内部扩散仍需按格存代价（松弛必需），只是不必暴露。
- 建议：GetReachableTiles 返回 List<HexCoord>；将来做 Civ6 式分级高亮（到达后剩余 0/≥1 分色）或"移动后还能动"提示时，再加配套方法返回字典，改动一行。
## 问答记录：MoveUnit 返回值设计（2026-08-31）
- 结论：MoveUnit 是"动作 + 多种失败原因"（被占、不可达等），最合适返回枚举结果码，如 `enum MoveResult { Success, TileOccupied, Unreachable }`，调用方按结果给 UI 反馈。
- 通用规则：struct 可能"无值"的三种主流解法——bool TryXxx(out T)（可能失败且要取值）、T? 可空（单一"无值"状态）、枚举/状态码（失败有原因）；自定义结果 struct（Success 标志 + 载荷）适合既要状态又要附带数据。
- 预期失败（占格/不可达）不要用异常。
## 问答记录：MoveUnit 需要把路径给表现层（2026-08-31，升级上一条设计）
- 需求：表现层要拿路径做移动动画 → 枚举不够，需带载荷结果。
- 推荐：MoveResult struct { Success; Reason; Path }，MoveUnit 内部算路径、改数据（Position/MovementLeft），把 Path 一并返回；表现层 Success 后沿 Path 播放动画。
- 备选：bool TryMoveUnit(unit, tar, out List<HexCoord> path)（.NET 风格）。
- 注意路径约定：Path 不含起点，动画起点用单位当前位置。
## 问答记录：MoveUnit 采用 List<HexCoord> 返回（2026-08-31，用户实现复查）
- 用户采用 List<HexCoord> + null 表示失败（未用 MoveResult 结构体）。
- 复查发现：1) 漏了 `unit.Position = tarHex;`（数据层没真正移动，最关键）；2) null 丢失败原因，UI 无法区分不可达/被占；3) 建议防御性检查 pathRes.Found。
- 结论：可用，但至少补 Position 更新；后续要 UI 区分原因时再升级 MoveResult。
## 问答记录：右键移动不生效排查（2026-08-31）
- 症状：每次右键只打印"剩余移动力3"，单位不动。
- 根因：不是算法问题。MapView.Update() 右键直接 `HexLayout.PixelToHex(Input.mousePosition, hexSize)`，把屏幕像素坐标当地图坐标，未先 `Camera.main.ScreenToWorldPoint`（左键 GetClickHex 有转换）→ tarHex 必在地图外 → _state.MoveUnit 校验失败返回 null → 移动力不扣、位置不变。
- 修复：右键复用 GetClickHex()（或先 ScreenToWorldPoint 再 PixelToHex）。
- 次要：Console.WriteLine 在 Unity 不可见，掩盖了"当前地块不可到达"；MapView.MoveUnit 应检查返回 null 并只成功时刷新；返回的路径留作动画用。
## 问答记录：纯 C# Core 如何输出到 Unity 控制台（2026-08-31）
- 原因：Unity Console 窗口只显示 Debug.Log 系列；Console.WriteLine 走 stdout 重定向到日志文件，窗口不显示。
- 方案1（首选，符合分层）：Core 不打印，返回结果/状态码，表现层 Debug.Log。
- 方案2（Core 需要内部日志时）：定义 `public static Action<string> Log = Console.WriteLine;`，Unity 启动时 `GameLogger.Log = Debug.Log;`（依赖注入，保持 Core 无 UnityEngine）。
- 不推荐 Core 直接 using UnityEngine（破坏纯 C#/可单测）。