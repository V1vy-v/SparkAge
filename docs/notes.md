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
10. **选择逻辑**：收敛为 SelectUnit / ClearSelection 两个操作；"显示范围"幂等（先全隐藏再显示），避免"记住上次"的补丁变量。
11. **范围高亮对象**：预创建 + 开关（固定容量池）；通用 GameObjectPool 留给以后弹幕/特效/单位生成。
12. **不缓存可达范围**：每次选中现算（20×20 成本低），避免移动后脏数据。
13. **Core 层零 UnityEngine（W2.3 评审）**：Debug.Log 也不行。失败用"结果对象"表达（如 MoveResult + 失败原因枚举），表现层负责打印/反馈。
14. **移动交互（W2.3 评审）**：当前右键移动与相机右键拖拽冲突，后续考虑左键移动（选中后点目标）或拖拽阈值区分点击/拖拽。

## 待讨论 / 未来
- 联机：主机权威 + 状态同步（GameState 是同步单位）；Photon PUN2 或 NGO + Relay。
- W6 美术：邻居感知渲染、URP 2D 光照；目标"Polytopia 级"，不追文明6 3D 画面。
- 单位逐格移动动画（协程沿 Path 走）留到打磨期。

## 问答记录：MoveResult 如何到表现层（2026-08-31）
- MoveResult 就是普通返回值：Core 的 GameState.MoveUnit 返回它，表现层 MapView 直接接收并 switch。
- 定义放 Core（SparkAge.Core），MapView 已 using SparkAge.Core 可直接用。
- 失败时 Path 给空列表非 null；表现层 Success 才用 Path 播动画，否则按 Reason 提示。
## 问答记录：移动动画期间禁止点击（2026-08-31）
- 方案：MapView 加 bool _isMoving；移动开始置 true，动画协程结束置 false；Update() 开头 `if (_isMoving) return;` 同时挡住左键选中与右键移动。
- 建议：移动成功后在动画结束后再 SelectUnit 刷新范围；协程内用 `yield return StartCoroutine(UnitMoveAnimation(...))` 链式等待。
## 问答记录：MoveUnit / FindPath 单测写法（2026-08-31）
- 原则：期望值手工算好、确定性地图（不要 Random）；3A 结构（准备-执行-断言）；一个用例断言一类行为。
- FindPath 用例：平原直线（路径/代价）、森林累计代价、山堵路绕行、起点被困不可达、goal==start。
- MoveUnit 用例：成功（扣 1）、超出移动力（Unreachable 且状态不变）、目标被占（TileOccupied）、森林扣 2。
- 运行：Unity Test Runner → EditMode → Run All；测试放 Assets/Editor/Tests。
- 注意：用户现有 FindPathTests/MoveUnitTests 是复制的空壳（含 Random 地图），需替换为确定性用例。