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
10. **选择逻辑**：收敛为 SelectUnit / ClearSelection；"显示范围"幂等（先全隐藏再显示），避免补丁变量。
11. **范围高亮对象**：预创建 + 开关（固定容量池）；通用 GameObjectPool 留给以后。
12. **不缓存可达范围**：每次选中现算，避免移动后脏数据。
13. **Core 层零 UnityEngine**：Debug.Log 也不行。失败用结果对象表达（MoveResult + 失败原因），表现层负责反馈。
14. **移动交互**：右键移动与相机右键拖拽冲突，后续考虑左键移动或拖拽阈值。
15. **测试策略（W2.3）**：求职 demo 优先功能迭代，新单测延后到 W6 集中补齐；但已有测试必须保持有效（有断言、全绿），防止"假绿"。

## 待讨论 / 未来
- 联机：主机权威 + 状态同步（GameState 是同步单位）；Photon PUN2 或 NGO + Relay。
- W6 美术：邻居感知渲染、URP 2D 光照；目标"Polytopia 级"。
- 单位逐格移动动画（协程沿 Path 走）留到打磨期。

## 问答记录：W3.0 拆分 MapView 的整体思路（2026-08-31）
- 任务本质：按职责把"什么都干"的 MapView 拆成 MapView(协调+地图渲染) / UnitView / SelectionController，纯搬代码、行为零变化。
- 通用拆法：①列出字段方法按职责分组 ②数据跟着职责走 ③定依赖方向（MapView 协调者注入 _state/hexSize，Selection/UnitView 不互相依赖）④搬方法修引用 ⑤编译+手测验收。
- 分配：地图渲染/相机/GetTerrainColor 留 MapView；单位创建、_unitObjs、移动动画去 UnitView；选中、高亮、范围、_selectedUnit 去 SelectionController。
- 注意：当前 SelectionController 是半成品——引用了 _state/_highlight/reachableHex 等不存在的字段，编译不过；拆时字段必须跟着方法一起搬，依赖由 MapView 注入。
## 问答记录：W3.0 拆分方案（细化版，2026-09-01）
- 原则：每个字段只有一个"家"；共享字段由 MapView 配置/创建后**构造函数注入**给子类，子类不重复持有 [SerializeField]。
- 字段归属：
  - MapView：seed、hexSize、地形色×4、_hexSprite、_state、_isMoving、moveDeltaTime、地图渲染/相机方法
  - UnitView：_unitSprite、_unitObjs、Spawn/Animate；注入(hexSize, unitSprite)
  - SelectionController：_selectedUnit、_highlight、_unitHighlight、高亮色×3、_unitHighlightSprite、_reachableSprite、reachableHex、reachableObjs、选中/范围方法；注入(state, hexSize, hexSprite)；_selectedUnit 用属性暴露给 MapView
- 注意：SelectionController 离开 MonoBehaviour 后 print 要改 Debug.Log。
## 问答记录：3D 化可行性评估（2026-09-01）
- 结论：现在改是最佳时机，工作量不大——Core（Hex/寻路/移动/GameState）零改动，全部复用；表现层 4-5 个文件重写，纯功能约 1-2 天，美术资产另算。
- 最省力路线：俯视角 3D 棋盘（不是自由相机）——HexLayout 只在 XZ 平面工作、地块换 3D 六棱柱、高亮用贴地半透明平面、相机保持正交/弱透视。改动最小。
- 主要成本在美术：3D 六棱柱 + 单位低模 + 材质光照；求职 demo 建议 blockout/低模风格控制成本。
## 3D 迁移决策（2026-09-01）
16. **项目改为 3D（更贴近文明6）**，经另一对话确认。原则：
    - Core 层零改动（Hex 数学/MapData/GameState/Unit/Pathfinding/单测 全部不动）——分层的红利在此兑现。
    - 只迁移 Game 表现层：HexSpriteFactory → HexMeshFactory（程序化六边形 Mesh）；单位用内置 3D 图元占位；高亮/范围用半透明 3D Mesh。
    - 坐标：HexToPixel(x,y) → 3D (x, 0, y)；拾取改为"射线 + y=0 平面求交"（Plane.Raycast），再 PixelToHex。
    - 相机：Perspective 俯视，缩放=调高度/FOV，平移沿用抓取点模式。
    - 现阶段不做：相机旋转、地形高度起伏（y=0）、真实模型（占位图元，美术 W6）。
    - 顺序：先完成 W3.0 拆分，再逐组件 3D 化，再清理 2D 残留，最后回归验证（单测+手动）。
17. **表现层组件用 MonoBehaviour（W3.0 修正）**：需要生命周期（协程/Update/OnEnable）的 Game 层组件用 MonoBehaviour，用运行时 `new GameObject(...).AddComponent<T>()` + Init 创建，避免 Inspector 布线；Core 层保持纯 C# 不变。普通类 + 手动 Tick（由 MonoBehaviour 每帧调用）是"显式更新"的可测试替代方案，记入备选。

## 问答记录：_isMoving 的归属与跨类使用（2026-09-01）
- 方案A（最贴合当前结构）：_isMoving 留在 UnitView（private），公开只读属性 `IsMoving`，MapView.Update 读它挡输入。
- 方案B（更干净）：_isMoving 归 MapView，UnitView 只提供协程方法，动画完成用回调 `Action onDone` 通知 MapView 解锁。二选一即可。