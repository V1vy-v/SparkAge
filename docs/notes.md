# 星火纪元（SparkAge）决策记录

> 本文件由"问答对话"维护：重要结论、方案决策追加写入。
> 执行对话开始任务前必须读取本文件 + docs/progress.md。

## 项目定位
- 简化版《文明6》求职 demo，核心卖点：回合制架构 + 联机链路。
- 技术栈：Unity 2022.3 LTS + 2D URP，C#，Git。

## 核心架构决策
1. **Model / Game 分层**：Model 为纯 C# 逻辑层（不依赖 UnityEngine），Game 为表现层。可单测、可复现；联机时整个状态序列化同步。
2. **Hex 坐标**：内部统一用轴向坐标 (q, r)，cube 思维辅助。
3. **地图**：程序化生成，值噪声 + seed 确定性 + 边缘强制水域。
4. **不用 Tilemap**：数据归属（Tilemap 把数据存在引擎组件里，违背"Model 唯一数据源"）、手绘用不上、小规模不需要合批优化。
5. **临时状态与永久数据分离**：地形是数据；选中/范围高亮是 UI 临时状态，用独立对象 + SetActive 切换。
6. **PPU = size/2**，保证六边形半径 = hexSize = 1，格子无缝。
7. **相机拖动**：抓取点模式（避免采样反馈振荡）。
8. **单位出生点**：BFS 从中心向外找第一个可行走格子（占位规则，独立成方法，将来可替换）。
9. **移动范围**：带地形代价的扩散（松弛思想），不是普通 BFS。
10. **选择逻辑**：收敛为 SelectUnit / ClearSelection；"显示范围"幂等（先全隐藏再显示），避免补丁变量。
11. **范围高亮对象**：预创建 + 开关（固定容量池）；通用 GameObjectPool 留给以后。
12. **不缓存可达范围**：每次选中现算，避免移动后脏数据。
13. **Model 层零 UnityEngine**：Debug.Log 也不行。失败用结果对象表达（MoveResult + 失败原因），表现层负责反馈。
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
- 结论：现在改是最佳时机，工作量不大——Model（Hex/寻路/移动/GameState）零改动，全部复用；表现层 4-5 个文件重写，纯功能约 1-2 天，美术资产另算。
- 最省力路线：俯视角 3D 棋盘（不是自由相机）——HexLayout 只在 XZ 平面工作、地块换 3D 六棱柱、高亮用贴地半透明平面、相机保持正交/弱透视。改动最小。
- 主要成本在美术：3D 六棱柱 + 单位低模 + 材质光照；求职 demo 建议 blockout/低模风格控制成本。
## 3D 迁移决策（2026-09-01）
16. **项目改为 3D（更贴近文明6）**，经另一对话确认。原则：
    - Model 层零改动（Hex 数学/MapData/GameState/Unit/Pathfinding/单测 全部不动）——分层的红利在此兑现。
    - 只迁移 Game 表现层：HexSpriteFactory → HexMeshFactory（程序化六边形 Mesh）；单位用内置 3D 图元占位；高亮/范围用半透明 3D Mesh。
    - 坐标：HexToPixel(x,y) → 3D (x, 0, y)；拾取改为"射线 + y=0 平面求交"（Plane.Raycast），再 PixelToHex。
    - 相机：Perspective 俯视，缩放=调高度/FOV，平移沿用抓取点模式。
    - 现阶段不做：相机旋转、地形高度起伏（y=0）、真实模型（占位图元，美术 W6）。
    - 顺序：先完成 W3.0 拆分，再逐组件 3D 化，再清理 2D 残留，最后回归验证（单测+手动）。
17. **表现层组件用 MonoBehaviour（W3.0 修正）**：需要生命周期（协程/Update/OnEnable）的 Game 层组件用 MonoBehaviour，用运行时 `new GameObject(...).AddComponent<T>()` + Init 创建，避免 Inspector 布线；Model 层保持纯 C# 不变。普通类 + 手动 Tick（由 MonoBehaviour 每帧调用）是"显式更新"的可测试替代方案，记入备选。

## 问答记录：_isMoving 的归属与跨类使用（2026-09-01）
- 方案A（最贴合当前结构）：_isMoving 留在 UnitView（private），公开只读属性 `IsMoving`，MapView.Update 读它挡输入。
- 方案B（更干净）：_isMoving 归 MapView，UnitView 只提供协程方法，动画完成用回调 `Action onDone` 通知 MapView 解锁。二选一即可。
## 问答记录：地块为什么仍代码创建而非预制体（2026-09-02，3D 化期间）
- 结论：没有美术素材时做预制体 = 存占位内容，美术到位还得重做，做两遍。换美术的改动面只需一个创建点（BuildTiles），代码创建 ≠ 难换素材。
- 预制体用于"复杂配置 + 手工调参 + 内容固定"；地块是程序化 + 数据驱动颜色 + 简单组件，无可预设内容。
- 3D 化时：BuildTiles 换成生成 3D 六棱柱（或之后 Instantiate 预制体），创建点不变；3D 地形更常用"动态合并 Mesh"而非 400 个预制体。
- 正确抽象是"创建点唯一"，不是"提前预制体化"。
## 问答记录：3D 化是否要改 HexToPixel（2026-09-02）
- 结论：六边形数学不变，不需要改 HexToPixel 算法/签名（单测 PixelRoundTrip 依赖它）。
- 做法：新增 HexToWorld(hex, size) 返回 Vector3（或表现层包一层 new Vector3(v.x,v.y,0)）；先定棋盘平面（XY=最小改动沿用现相机 / XZ=更 3D 需俯视相机）。
- 反向换算 GetClickHex 也要改：3D 下用 射线+Plane.Raycast 投影到棋盘平面，不能再用 ScreenToWorldPoint。
- 备注：progress.md 里 W3.0b-2 任务卡内容被 `n 弄乱缺失，需执行对话重新整理。
## 问答记录：3D 斜俯视相机控制（2026-09-02）
- 标准做法：target(关注点) + distance + pitch(俯仰角) + yaw(水平角)，每帧 `rotation = Euler(pitch,yaw,0); position = target - forward*distance`。
- 交互：滚轮调 distance（clamp）、拖拽平移 target（Plane.Raycast 投影到地面）、可选 Q/E 转 yaw；pitch 限制 10°~80° 防穿地。
- 用 LateUpdate；初始 target 取 HexToWorld(地图中心)。
## 问答记录：3D 相机 z 锁死 + 滚轮无效排查（2026-09-02）
- Bug1（z 锁死）：CameraMove 里用 `bottomLeft.y / topRight.y` 夹 z 值——平地图 y 恒 0，clamp 后恒 0 → 相机 z 被锁死。应改用 `.z`。
- Bug2（滚轮无效）：PerspectiveZoom 只改 distance，没重算 `transform.position = target - forward*distance`，相机没动。
- 次要：clamp 基准应为 target 而非 transform.position（否则相机偏移混入 target）。
## 问答记录：3D 化后点击无反应排查（2026-09-02）
- 主要嫌疑1：CreateHexMesh 顶点在 XY 平面（z=0），而棋盘摆放在 XZ 平面（y=height）→ 地块/高亮全部是"竖着的纸片"，俯视相机几乎看不见 → 点起来"没反应"（其实高亮不可见）。
  - 修复：mesh 顶点改到 XZ 平面（y=0），法线 Vector3.up。
- 嫌疑2：MapView.Awake 里 _hexMesh 还没创建就传给 _selection.Init（null）→ 地块高亮 mesh=null 不可见。应先建共享 mesh 再 Init。
- 顺带：BuildTiles 每格 new 一个 Mesh+Material（400 份），应共享。
- 排查方法：点单位看 Console 是否打印"剩余移动力"——有=逻辑通（渲染问题）；无=GetClickHex 返回 null 或 _isMoving 卡 true。
## 问答记录：TerrainType→Color / TerrainType→Material 两个字典是否合适（2026-09-02）
- 结论：不合适——Material 的颜色派生自 tile color，两份数据存在"改一处不同步"风险，应单一数据源二选一。
- 方向1（代码生成材质）：只保留 tileColors（或 4 个 SerializeField），Init 时据此生成 4 个材质模板，不再单独存材质字典之外的颜色。
- 方向2（材质即资产）：只建 _hexMeshMaterials，颜色从材质读。
- 补充：连续枚举建议用数组 `Material[4]` 而非 Dictionary（无哈希开销、可序列化）；Unity 不能直接序列化 Dictionary。
## 问答记录：地形视觉的确定方案（2026-09-02，为将来美术素材预留）
- 确定做法：删除 Color 字典和 GetTerrainColor switch；表现层持有一个 `Material[4]`（下标=TerrainType），Inspector 直接拖 4 个材质，null 时代码生成占位 URP 材质。
- BuildTiles 用 `materials[(int)tile.Type]`。
- 换美术路径：只换 Inspector 里的材质（贴图/模型材质）；若换 3D 地块模型，把该表升级为 TerrainType→prefab 并让 BuildTiles 改 Instantiate——升级都从这个单一映射表出发，代码别处不动。
## 问答记录：不用枚举做键的确定写法（2026-09-02）
- 结论：地形固定且少（4 种）时，放弃字典/枚举下标数组，用"4 个 SerializeField + 一个 GetMaterial(TerrainType) switch 表达式"，零 (int) 强转、Inspector 直观。
- 未来地形变多/数据驱动时再升级为 List<TerrainMaterialEntry{Type,Material}>，Awake 构建运行时字典。
- 换美术=只换字段里的材质引用，逻辑不动。
## 问答记录：网格存在但不显示（2026-09-02）
- 根因：三角形绕序反了。HexMeshFactory 的 triangles = {0,2,1, 0,3,2, 0,4,3, 0,5,4}，叉积算出几何法线朝 -Y（朝下），而相机在 +Y 上方 → 看到的是背面，被背面剔除 → 不渲染。
- 修复：每三个索引反转 → {0,1,2, 0,2,3, 0,3,4, 0,4,5}。
- 验证：Scene 里开 Cull Off 或显示法线；修复后地块朝上即可见。
- 顺带：MapView 有多余的 `using System.Drawing;`（会和 UnityEngine.Color 冲突/编译风险），删掉。18. **架构方向定为"命令驱动的分层"（2026-09）**：详见 docs/architecture.md。要点：Model 唯一事实源；玩家操作=可序列化命令（单机本地执行=联机发主机执行，同一路径）；Model 系统产事件、表现层消费；EndTurn 是有序流水线；UI 状态（选中/相机）不进 Model；迁移增量进行，W3.2 起新功能直接按新模式落位。

## 问答记录：FoundCity 用途说明 + 文档损坏提醒（2026-09-03）
- FoundCity/FoundCityOrder（W3.2）：移民建城动作——校验目标格（无城市/无单位/可建城/城市数<上限），Model 执行 Cities.Add + 消耗移民，表现层生成城市视觉。
- 勘误（2026-09-03）：全量扫描后确认所有文档/代码均为合法 UTF-8（严格解码零失败），并无中文损坏；此前看到的乱码/孤立 "n" 是读取端未按 UTF-8 解码（终端 GBK）的显示假象。真实缺陷仅是 progress.md 里出现字面 "`n" 把多行挤在一起，已修复。
## 协作约定补充：统一 UTF-8（2026-09-03）
1. 所有文档/代码统一 UTF-8（.md 保留 BOM；.cs 无 BOM 的 UTF-8 亦可，勿写 GBK）。
2. 文档内禁止出现字面 "`n"——换行必须用真实换行符；写完自检 `字面反引号+n` 数量为 0。
3. 读写一律显式 UTF-8：PowerShell 5.1 默认按 GBK 解码，需用 `Get-Content -Encoding UTF8` 或 .NET `[IO.File]::ReadAllText/WriteAllText(..., UTF8Encoding)`，追加写入也要显式 UTF8Encoding。
## 问答记录：城市范围 List<HexCoord> 不需要存进 City（2026-09-03）
- 依据：gamedesign.md 明确"领地=距中心≤2，派生不存储"；城市不移动、半径固定 → 存储是冗余 + 脏数据风险 + 序列化负担；产出为统一固定值，无需遍历领地。
- 做法：City 只存 Position/Owner（+将来生产状态），提供 GetTerritory(MapData) 派生方法；半径常量放 GameRules.CityRadius。
- 例外：将来做扩建/买地/中心可变时才升级为存储列表，demo 不加。19. **表现层落地形态定为 MVC 风格 + EventBus（2026-09）**：Model=Model（纯 C#，不感知事件）；GameController=唯一 Controller（装配/输入/调 Model/编排）；MapView/UnitView/CityView/SelectionView=View（纯显示）；EventBus=表现层事件中心（静态服务，允许全局）。异步完成/跨组件反应走事件，单一接收方走方法调用；Model 不发布不订阅事件。
20. **网络定案（2026-09，Mirror）**：选 Mirror 作传输层（NetworkMessage 收发）；主机权威 + 整状态广播；游戏状态保持在纯 C# Model，不做成 Mirror 同步对象/变量；演示走本机/LAN；W4 AI 先建命令接缝（SubmitOrder），W5 网络复用。

21. **EventCenter 用单例（2026-09）**：轻量单例（`Instance` 静态属性 + 实例方法 + `Clear()`），不用场景 MonoBehaviour。理由：EventCenter 是"服务"（允许全局），且不需要 Unity 生命周期；`Clear()` 用于开新局/测试。Framework/Singleton 同时提供 `MonoSingleton<T>` 基类供以后需要场景生命周期的服务（如 AudioManager）使用。调用改为 `EventCenter.Instance.Subscribe/Publish`。

## 问答记录：seed 和 hexSize 的归属（2026-09-04，MVC 装配期）
- 结论：两者都作为 GameController 的 [SerializeField]（场景唯一配置点）。
  - seed：只用于生成地图（Model 创建参数），生成完即完成使命，不进 View。
  - hexSize：所有要摆位置的 View（Map/Unit/Selection/City/Camera）都用 → Controller 统一持有并在 Init 时注入，View 不再各自 SerializeField。
- 目标分工：GameController=装配+输入+调 Model+编排；MapView 只剩渲染（BuildTiles/材质/mesh）；state/输入/EndTurn/出生摆放等从 MapView 迁到 GameController。
- 现状：重构未完成——MapView 仍带 Controller 逻辑（state/Update/出生），GameController 骨架未接完（mapView 未赋值、缺 seed/hexSize/hexMesh 字段），另有 SelectionController.cs/HexPicker.cs 疑似旧文件待清理。
## 问答记录：静态 HexPicker 如何拿 state/hexSize（2026-09-04）
- 推荐：静态方法改为带参 `GetClickHex(GameState state, float hexSize)`，调用方（持有实例状态）传入；或职责再切细——HexPicker 只做"屏幕点→地面世界点"纯几何，PixelToHex/IsInMap 留在调用方。
- 不推荐在静态类里存静态全局 state/hexSize（隐藏全局可变依赖）。
- 现状：HexPicker.GetClickHex 引用了不存在的 state/hexSize，编译不过，需按上面修。
## 问答记录：事件中心会不会跨过控制层（2026-09-04）
- 结论：按本方案，"GameState 发布表现层事件"是不正常的——违背"Model 不发布不订阅事件"；View 订阅自动反应本身正常，但发布权归 Controller。
- 正确链：输入→Order→Model.Execute 改状态并返回事件数据（纯数据）→ GameController 决定表现反应（转 EventCenter.Publish 或直接调 View 方法）→ View 播动画。
- 事件中心用于多订阅者/解耦；单一接收方走方法调用。View 不得直接订阅 Model 状态变化自行动画（Controller 失编排、联机同路径被破坏）。
## 问答记录：FoundCity 走 GameController 驱动表现（2026-09-04）
- Execute(order)：架构目标里的统一命令入口（GameState.Execute(Order) 校验+分发+返回事件数据）；现状尚未实现，FoundCity/MoveUnit 仍是直调方法，可增量迁移。
- FoundCity 数据层缺口：1) 未 Units.Remove(settler)；2) 未 CityNum++；3) bug：GetUnitAt(settler.Position) 会返回移民自己 → 永远 OccupiedByUnit，应改为"同格存在非本单位的其他单位"才算被占。
- 控制层编排：GameController.TryFoundCity(settler)：result=state.FoundCity → 成功则 cityView.Spawn(city) + unitView.PlayDestroy(settler)（销毁动画后删 GameObject）+ selectionView.ClearSelection()。
- 演示触发：选中移民按 F（将来换 UI 按钮/FoundCityOrder）。
## 问答记录：EventCenter 委托类型冲突（2026-09-04）
- 报错根因：paramEvents 用字符串 eventName 作键，同一名字下挂了不同类型委托（"FoundCity" 同时被 UnityAction<Unit> 和 UnityAction<City> 注册），AddListener 里 Delegate.Remove(existing, action) 类型不同 → ArgumentException。
- 修复：键改成载荷类型 typeof(T)（推荐，事件=数据对象，去掉字符串名）；或保留名字用复合键 (eventName, typeof(T))。
- 另发现 bug：EventTrigger<T> 查的是 noParamEvents（错字典），带参事件实际永远触发不了。

## 问答记录：新造单位出生格全被占怎么办（2026-09-04，W3.2）
- 规则建议：出生格 = 城市中心优先；中心不可用则 BFS 由近及远找"可行走+无单位+非其他城市中心"的空格；全满 → 生产挂起（进度保留/继续累积），有空位再落地，不丢单位不挤占。
- 城市碰撞只算中心格（领地=视觉范围，不该挡通行/落位，除非 W4 做 ZOC）。
- 现状缺口：BuildUnit 只扣产出建 Unit 对象，未选出生格、未加入 Units；需补 BFS 定位 + 落位。
- 帮助方法可复用 FindSpawnPoint 的 BFS 模式。
## 问答记录：攻击判断放控制层还是数据层（2026-09-04）
- 结论：规则校验一律放数据层（与移动/建城一致）："能否攻击"取决于世界数据（射程/敌我/回合/是否已行动），Controller 不拥有数据，无法正确判断；联机时主机用 Core 校验，客户端不得自行裁定。
- 分层：数据层提供 查询 CanAttack(attacker,target)（UI 灰置用，只读） + 执行 TryAttack/Execute(AttackOrder)（校验+改状态+返回事件）；Controller 只做 输入→Order、结果→表现；"UI 处于什么模式/是否选中单位"才是表现层 UI 状态。
- 原因回顾：移动/建城判断在 GameState 正是"规则=Model 职责"的正确体现，攻击保持同模式。
## 问答记录：近战攻击是否拆成 MoveUnit+AttackUnit（2026-09-04）
- 结论：方向对但要分层——数据层攻击是"一个原子动作"（TryAttack/AttackOrder 一次校验+结算，返回 是否击杀/攻击者最终落点/目标移除/行动力消耗），不要把移动与攻击拆成两次数据调用（中间态+联机不一致）。
- "是否挺进目标格"由攻击结果决定并在数据层改好（击杀→Position=目标格），Controller 只按结果编排动画：走位→攻击→（击杀则）挺进，动画段用回调衔接。
- 若近战只打相邻：没有走位段，直接攻击结算。移动+攻击（打相邻需先走位）的寻路/移动力校验也在 Core。
- 一句话：判定在 Core 一次完成，动画在 Controller 分段播放。
## 问答记录：FindPath 路径不含起点 + 攻击越界崩溃（2026-09-05）
- 约定：Pathfinding 返回路径不含起点（含起点之后直到 goal 的所有格）。相邻攻击时 path.Count==1。
- 崩溃根因：GameState.AttackUnit 的 else 分支 `path[Count-2]`，相邻攻击 path[-1] 越界。
- 修复：defender 未死且 Count>1 才移动到 path[Count-2]（目标相邻格）；Count==1 原地不动。
## 问答记录：单位颜色区分与联机一致性（2026-09-05，仅了解）
- 颜色不进网络同步数据：同步 owner(玩家Id)，各端用同一张 owner→颜色 映射本地渲染 → 同一单位任何客户端颜色一致。
- 敌我判断 = `unit.own == myPlayerId`（本地知道自己是几号玩家），颜色只是辅助；两种配色哲学：A 阵营固定色（同一单位跨端同色，推荐 demo）/ B 自己绿敌红（跨端颜色不同，回放/观战乱）。
- 两个维度分开：形状=单位类型（勇士/移民），颜色=阵营(owner)。现状 UnitView 按 type 染红/蓝是"类型色"，敌我无法从颜色区分。
- 实现：每 owner 共享材质实例或 MaterialPropertyBlock；PlayerState.PlayerColor 用了 UnityEngine.Color，在 Model 层违背零 UnityEngine，应改存颜色索引/表现层建表。
## 问答记录：仅边缘/脚下颜色区分敌我（2026-09-05）
- 推荐方案A（文明式脚下彩色环）：主体保持中性色，单位脚下加一个扁平彩色圆环（子物体，随单位移动），颜色=GetOwnerColor(owner)；每 owner 一个共享材质或 MaterialPropertyBlock，避免每单位实例化材质。
- 备选方案B（模型自身边缘染色）：URP 自定义 shader/ShaderGraph 做 Fresnel rim 边缘光，_RimColor 按 owner 用 MaterialPropertyBlock 设置，主体保持中性。
- 单位类型区分继续用形状/大小，颜色只表阵营。22. **视觉语言定案（W4a）**：颜色=玩家归属，标识=实体信息。
    - 玩家色表放 View（Model 不存 Color）：owner1=红、owner2=蓝（占位可调）。
    - 每个单位/城市 GameObject 挂一个"头顶标识"子物体（小圆环/菱形，水平朝上，y 抬高），**跟随移动**（child 自动跟随）；标识颜色=玩家色。
    - 单位本体改中性色，类型用大小/形态区分（战士大、移民小——scale 已有）；城市本体保持灰。
    - 标识的"信息内容"（HP/类型/城市名）用 TextMeshPro 世界标签，放 UI 批次挂到标识下；现在先做归属色结构。

## 问答记录：Marker 做成预制体，代码只改颜色（2026-09-05）
- 做法：Prefabs/OwnerMarker.prefab（Plane/圆盘，法线朝上天然水平；去碰撞体，尺寸在预制体内调好）。
- 代码只做：Instantiate(prefab, host) 子物体挂载 + 设 localPosition(头顶) + 按 owner 换共享材质（per-owner 材质数组 or MaterialPropertyBlock），避免每单位实例化。
- 颜色表留 View；单位/城市生成时 AttachOwnerMarker(host, owner)。23. **战斗模型定案（W4a）**：攻击 = 目标在本回合可达即攻（移动+攻击合并），消耗全部移动力（MovementLeft=0）；双方结算：攻方伤害 max(1, 攻Atk−守Def)；**守方 Atk>0 才反击**，伤害 max(1, 守Atk−攻Def)；守方死亡→攻方进格，攻方死亡→移除；攻方若活着未杀→停在目标相邻格。单位属性统一 GameRules：战士 3/2/10/移2，移民 0/0/1/移3。
