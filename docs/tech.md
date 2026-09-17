# 星火纪元（SparkAge）技术文档

> 本文档为**当前权威技术文档**（2026-09 重组），主要规划程序技术层面细节。整合并取代早期 architecture.md 中过时内容；策划语义见 docs/design.md。

## 1. 分层与依赖规则
```
Model（纯 C#，零 UnityEngine）  ← 唯一事实源：GameState + 实体 + 规则方法（返回结果对象）
Controller（GameController 唯一）← 装配/输入/调 Model/编排 View
View（MapView/UnitView/CityView/SelectionView/Camera）← 纯显示
Framework（EventCenter/Hex/工具）← 基础设施
```
- 依赖方向：Model 零依赖；View 读 Model；**唯一能改 Model 状态的是 Controller**；View↔View 不直接引用。
- 通信：命令 = Controller 直调 Model 方法（返回结果）；通知 = `EventCenter.Instance`（单例，只发"发生了什么"）；**Model 不发布/不订阅事件**。

## 2. 目录结构
```
Scripts/
├─ Framework/   EventCenter/(EventCenter, EventDefine) · Hex/(HexLayout, HexMeshFactory)
├─ Model/       GameState · GameRules · Hex/ · Map/ · Units/(Unit, UnitType) · Cities/(City) · Players/(PlayerState)
├─ View/        MapView · UnitView · CityView · SelectionView · CameraController · ViewTools · (UI/ 以后)
└─ Controller/  GameController · (未来 AIController · NetworkSession)
```
- 命名空间 = 目录。文档统一 UTF-8。

## 3. 实体三维度约定（新增实体照此办理）
| 维度 | 内容 | 位置 |
|---|---|---|
| **Def（静态定义）** | 类型天生属性（规则数值） | Model 数据表 |
| **State（运行状态）** | 实例当前值（Hp/位置/移动力/Owner/Production） | Model 实例，构造时从 Def 初始化 |
| **View（表现映射）** | 颜色/模型/名字 | View（字典/工厂） |

- Unit：`UnitStats`（`Dictionary<UnitType, UnitStat>`）取代"构造 switch + 常量堆"（**待做**）；`new Unit(...)` 从 Def 抄数值。
- City：基础数值集中在 `GameRules`（当前即 Def）；未来出"城市类型"再照 Unit 加表。
- Player：无规则差异 → 不预造字段；接缝 = 未来 `PlayerDef`（Id→加成），PlayerState 只放 IsAlive 等运行状态。

## 4. 移动/攻击统一规则（核心，先做）
Model 提供两个函数，**所有"能不能走"都调它们**（MoveUnit / GetReachableTiles / Pathfinding / 攻击/攻城寻路）：
```csharp
bool CanPass(HexCoord hex, Unit mover);   // 穿过：地形 + 敌方城格 + 敌方单位（己方单位可穿）
bool CanStand(HexCoord hex, Unit mover);  // 停留：CanPass + 无任何单位 + 非敌方城格
```
- 攻击目标格**豁免障碍**（目标是可打击对象，不是障碍）：代价函数里 `hex == 目标格` 时放行。
- 攻击统一 = 走到目标相邻 → 打击 → 目标死/城破才进目标格（Controller 收敛为右键 Intent）。

## 5. GameController 状态机
用 `enum GamePhase` 取代散布尔（isMoving/gameOver）：
```csharp
enum GamePhase { PlayerTurn, Animating, GameOver, /* 预留: AiTurn, WaitingPeer */ }
```
- PlayerTurn --发起移动/攻击--> Animating --动画完成事件--> PlayerTurn
- Model 查询 `IsGameOver()`（某玩家城市数 0）→ GameOver
- 结束回合回 PlayerTurn（单机）；未来切 AiTurn / WaitingPeer
- "是否结束"是 Model 事实；"能否点鼠标"是 Controller 的 phase，两者分开。

## 6. 事件清单（Framework/EventCenter/EventDefine）
BuildUnitEvent · UnitMoveEvent · FoundCityEvent · AttackUnitEvent · AttackCityEvent
（通知只走这些；新增事件按需加，不放字符串无参事件）

## 7. W4 收尾技术规格（对照现状）
| 项 | 状态 |
|---|---|
| CanPass / CanStand 统一规则 | 待做（当前 MoveUnit/可达/寻路各写各的） |
| 攻击目标格豁免 + 停城旁/破城才进 | 待做（守军/进城语义 bug 根源） |
| CityNum 手工维护 → `CityCount(owner)` 派生 | 待做 |
| `GetPlayerState` → `TryGetPlayer(id)`（字典封装） | 待做（改名/统一） |
| UnitStats 数据表取代构造 switch | 待做 |
| 移动动画三协程合一 `PlayStepMove(obj, path, walkIntoLast)` | 待做 |
| UnitFactory 收敛 Resources.Load | 待做（材质缓存一并） |
| GamePhase 状态机 | 待做 |
| 右键 Intent（移动/攻击/攻城统一） | AI 前做 |
| 预制体/玩家色标识/本体中性色 | 已完成 |
| AttackCity（易主+重置+进城+出局） | 已完成（语义待按上表微调） |

## 8. 已知缺陷与待办
- 守军：杀守军后攻方"进城/占领"边界规则（配合 §4 统一后修）。
- UI 批次：回合数/生产力/HP/城市面板、结束画面、生产/建城按钮（替代临时按键）。
- 材质缓存、事件退订（OnDestroy）、编解码统一 UTF-8。
- 单测：移动/战斗/建城规则（W6 集中补）。

## 9. 网络（W5，位置与选型）
- 选型 **Mirror**，仅作传输层（NetworkMessage 收发）。禁止把游戏状态做成 Mirror 同步对象/变量。
- 模式：主机权威 + 命令中继 + 整状态广播（瘦客户端）；Model/Serialization（LitJson）+ Controller/NetworkSession（封装 Mirror，可替换）。
- 接缝：AI 先抽出 GameController 的"命令接缝"（SubmitOrder/Intent），W5 网络复用（客户端 = 命令发主机）。
- 演示：本机双客户端 / 局域网直连。
## 10. ID 与引用：形态纪律（W5 联机定案，2026-09-14）

### 10.1 每层固定一种形态

| 层 | 形态 | 原因 |
|---|---|---|
| 线上格式：Order / 网络 DTO / 存档 | **只带 ID + 纯数据** | 对象引用不可序列化、无法跨端 |
| 运行时：Model 规则方法、GameState 内部、AI 决策 | **对象引用** | 规则要读 unit.Atk / city.Hp 这类实例数据 |
| 表现：Result、EventCenter 事件、View、协程参数 | **对象引用** | Unity 对象、字典键（Unit→GameObject）、协程局部变量 |
| 转换点 | **只有两个** | ① 收到 Order：ID→引用；② 广播：引用→ID（客户端收到后再解析一次） |

纪律：**每端只解析一次**——入口解析出引用后，整条内部链路只传引用，禁止二次转换（这是之前"反复来回换形态"的根因）。

### 10.2 被移除实体的规则（关键）

- **新建实体**（城市、造出的单位）：Result 带 ID 或引用都可以——它还在 Units/Cities 里，按 ID 能查到；
- **被移除实体**（建城消耗的移民、阵亡的单位）：**必须靠"操作前捕获的引用"**，或让 Result / 事件显式携带引用；此时只给 ID 必然解析失败。
- 因此会移除实体的入口统一写法：**先解析引用 → 再执行 → 用该引用构造表现事件**。
  - 例：TryFoundCity(unitID) 先取 `Unit settler = state.TryGetUnit(unitID)`，再调 `FoundCity(settler)`（内部会 Units.Remove(settler)），最后用 settler 触发 FoundCityEvent 播销毁动画。

### 10.3 Result 与广播 DTO 的定位

- **Result = 本地表现数据**（允许带引用），不承担广播职责；
- 广播 = **整状态快照（ID + 纯数据）+ 少量"表现提示 DTO"**（如 unitID + path），供客户端知道"播什么动画"；
- 客户端：ApplySnapshot(snapshot) + PlayHint(hint)；解析一次 ID→引用后调用**同一套 View 方法**（表现代码只有一份，主机/客户端复用）。

### 10.4 Order 提交入口

- 统一入口 `SubmitOrder(order)`（UI / 网络）+ 内部引用重载（AI、已解析场景），两条路最终调用同一个 Model 方法；
- **PlayerId 由入口打标**（本地 = 当前玩家；网络 = 按连接身份填），Order 构造函数不再接收 playerId；网络/回放收到的命令**不得覆盖**其自带的 PlayerId。

### 10.5 ID 分配与查找（实现约束）

- 所有 Unit/City 创建统一走 Model 工厂，**在创建处分配自增 ID**（当前开局两个单位未分配 ID，均为 0，必须修）；
- `TryGetUnit` / `TryGetCity` 必须**按 ID 线性查找**，**不得用列表下标**（单位阵亡、移民被消耗后下标错位，会取到错误对象）。


---

## 附：联机执行卡（NET-1 ~ NET-8）

联机部分的逐卡执行计划（含每步在游戏流程与架构中的意义）见 **docs/networking.md**。本节 §9 只保留选型与总体模式，具体步骤以该文档为准。
