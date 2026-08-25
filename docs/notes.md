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
9. **移动范围**：带地形代价的扩散（松弛思想），不是普通 BFS。

## 待讨论 / 未来
- 联机：主机权威 + 状态同步（GameState 是同步单位）；Photon PUN2 或 NGO + Relay。
- W6 美术：邻居感知渲染、URP 2D 光照；目标"Polytopia 级"，不追文明6 3D 画面。

## 问答记录：struct 返回"无结果"的处理（2026-08-25）
- 问题：HexCoord 是 struct，函数返回它时无法用 null 表示"未找到"。
- 推荐方案（按优先级）：
  1. `HexCoord?` 可空值类型——改动最小，适合 FindSpawnPoint。
  2. `bool TryFindSpawnPoint(center, out HexCoord)`——.NET 惯例，调用方必须处理失败，W2.3 寻路等查询 API 建议统一用此风格。
  3. （备选）哨兵值 HexCoord.None + IsValid，靠约定维护，不优先。
- 需修复：FindSpawnPoint 现兜底 `return visited[visited.Count-1]` 会在全水地图返回水格，应改为显式"未找到"。- 补充（2026-08-25）：`HexCoord?` 与 `Try...out` 两种写法均无装箱；装箱只发生在转 object/接口或字符串插值时（如 `object o = coord;`、`$"{coord}"`）。HexCoord 已实现 IEquatable，字典键/Equals 均无装箱。- 补充（2026-08-25）：问答对话回复规范——工具调用会折叠同回合中位于其之前的文本，因此完整回答必须写在所有工具调用之后；先执行文件更新，再输出答案全文。
## 问答记录：GetReachableTiles 返回 HashSet 拿不到剩余移动力（2026-08-25）
- 问题：任务卡要求返回 HashSet<HexCoord>，丢失"到达后剩余移动力"，重算浪费。
- 结论：不重算。扩散算法天然产出"coord→剩余移动力"的字典，HashSet 只是 .Keys 的投影。
- 推荐：
  1. 允许改签名 → GetReachableTiles 返回 Dictionary<HexCoord,int>（键=可达点，值=剩余移动力），要 HashSet 就用 .Keys 投影。
  2. 签名硬性要求 → 算法本体 internal 返回字典，公开 GetReachableTiles 只做 `new HashSet(dict.Keys)` 投影，另提供 GetReachableWithMovement(unit) 返回字典。
- 现状提醒：现 GetReachableTiles 为半成品——movementLeftDic 实际存的是方向下标 i 而非剩余移动力，且未做移动力上限/地形代价/松弛，res 恒为空。
## 协作角色确认（2026-08-25）
- 问答对话（本对话）：答疑、方案讨论，结论写入 docs/notes.md；不写代码。
- 执行对话：发任务卡 + review 用户提交的 commit；不写代码。
- 代码由用户本人编写并 commit。
## 问答记录：GetReachableTiles 单测方案 + 发现的 bug（2026-08-26）
- 单测概念与写法已讲解（见对话）；测试放 Assets/Editor/Tests/，Test Runner → EditMode 运行，NUnit [Test] + Assert。
- 重点用例：全平原距离全覆盖、山/水阻挡、森林代价消耗、松弛绕路取优、出地图不崩、移动力不足。
- 发现算法 bug：`A && B && C && D || E` 存在运算符优先级问题——`!Map.IsInMap(newHex)` 或移动力不足时仍会求值 E，导致 `Map.Tiles[newHex]` / `movementLeftDic[newHex]` 抛 KeyNotFoundException（崩溃）。
- 建议写法：先 `continue` 掉越界/不可通行/移动力不足，再 `TryGetValue` 判断是否松弛改进。
## 问答记录：点击单位不显示深蓝可到达区域（2026-08-26）
- 根因：MapView.Start() 里测试单位创建为 `new Unit(spawn, 0, 0, 0)`，移动力为 0 → GetReachableTiles 只返回单位所在格 → 唯一的深蓝圈被不透明红色单位盖住，视觉上等于没显示。
- 修复：改为 `new Unit(spawn, 0, 2, 2)`（movementLeft>0）。
- 顺带发现显示层 bug（建议一起修）：
  1. 隐藏可到达对象时 reachableObjs 列表未清空 → 重复点击/切换单位时旧高亮不消失、对象池重复入队、对象翻倍增长。
  2. 点击单位 A 再点单位 B 时，旧 A 高亮未回收直接叠加新 B 高亮。
  3. 建议：显示分支先统一回收旧的 reachableObjs 再重建。