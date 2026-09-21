# 星火纪元 · 联机开发卡（NET-1 ~ NET-9）

> **状态同步与表现层的重新设计见 [docs/sync-design.md](sync-design.md)**（一份消息 = 事实快照 + 演出指令；View/UI 全部改为订阅事件中心，Controller 不再直接调度 View）。

> 本文档是联机部分的**执行卡集合**，每张卡都说明它在「游戏流程」和「代码架构」中的意义。
> 关联：docs/tech.md §9（网络方案）、docs/design.md（策划）、docs/notes.md（决策记录）。

---

## 0. 总体流程（唯一一条路径，没有"单机/联机"开关）

```
BeginScene
  ① BeginPanel（开始界面：显示昵称）
  ② LoginPanel（输入昵称 → 本地保存）
  ③ 创建房间 → StartHost        ／ 加入房间 → ConnectPanel（输入 address）→ StartClient
  ④ RoomPanel（固定 4 槽：昵称 / 角色 Dropdown / 就绪；房主可配 AI 填充）
  ⑤ 服务端判定「所有人类槽位就绪」→ 广播 GameStart（seed + 最终槽位表）
  ⑥ 加载 GameScene → GameController.StartGame(session, setupInfo)
GameScene
  ⑦ 对局中：命令上行（客户端 → 服务端）→ 权威执行 → 状态下行（服务端 → 客户端）
```

**核心原则**（贯穿所有卡）：
- 服务端（Host）是唯一权威：跑 Model、校验命令、跑 AI、判定胜负；
- 客户端是瘦客户端：发意图（Order）、收事实（状态快照）、只负责显示；
- 单机 = Host + 4 槽中的其余槽位填 AI —— **同一条代码路径**。

---

## NET-0（取消）：联机实现风格定为"教程式"

**决定**：联机部分采用 **Mirror 教程的直白写法**，不做适配层。

- 连接：UI 直接调 `NetworkManager.singleton.StartHost()` / `.StartClient(address)`；
- 不写 `NetworkSession` 适配层、不写 `NetworkBootstrap`、不拆 UI 接口（这些留给 NET-6/NET-7 需要"命令拦截、来源校验、状态广播"时再做）；
- 跨场景：给 `NetworkManager` 勾 **Don't Destroy On Load**（挂在常驻对象上）即可；
- 需要 Mirror 回调（如玩家连接、自定义消息）时，按教程做法：**子类化 `NetworkManager`** 或用一个 MonoBehaviour 调 `NetworkClient/NetworkServer.RegisterHandler`。

> 理由：首次学网络，优先"和教程一致、能搜到答案"；架构层的封装等联机跑通、需求明确后再加。

---
## NET-1：本地玩家资料（登录/昵称）

> **实现方式（已定，2026-09）**：昵称**仅存内存、不持久化**；每次启动都弹登录让玩家输入（本地多开测试天然不冲突，也不需要 PlayerPrefs）。以后若要记住昵称再引入持久化。

**目标**：每台机器先知道自己"是谁"（昵称），供房间显示与识别。

- **流程意义**：房间里 4 个槽位要靠昵称区分"哪个是我"；没有它，玩家无法确认自己的槽位/角色。
- **架构意义**：昵称是**客户端本地资料**，不属于游戏世界（不进 Model）。放在会话层（`Controller/Network/LocalPlayerProfile`）并持久化（PlayerPrefs）。
- **做什么**：
  - 新增 `LocalPlayerProfile`（昵称 + 读写 PlayerPrefs 的方法）；
  - `LoginPanel` 的"完成"按钮：保存昵称 → `HideMe()`；
  - `Main.cs` 判断"是否首次"：首次显示 `LoginPanel`，否则直接 `BeginPanel`；`BeginPanel` 显示当前昵称。
- **验收**：输入昵称 → 保存 → 重启仍是该昵称；BeginPanel 正确显示。
- **不做**：账号系统、密码、服务端账号校验（昵称只用于房间显示）。

---

## NET-2：装 Mirror + 连接打通（教程式）

**目标**：Editor 跑 Host，另一个实例 Join 成功，两端能看到"已连接"。

- **流程意义**：这是"创建房间 / 加入房间"的底层能力，NET-3~NET-5 都建立在它之上。
- **架构意义**：这一卡只碰"传输"，不碰任何游戏逻辑；Mirror 的用法与教程一致（`NetworkManager.singleton`）。
- **做什么**：
  1. 装 Mirror（Package Manager → Add package from git URL）；
  2. 场景（BeginScene）加一个常驻对象，挂 `NetworkManager`（Transport 用默认 KCP），勾上 **Don't Destroy On Load**；
  3. `BeginPanel` 的"创建房间"按钮 → `NetworkManager.singleton.StartHost()`；
  4. `ConnectPanel` 的 Join 按钮 → `NetworkManager.singleton.networkAddress = 地址; NetworkManager.singleton.StartClient();`
  5. 连接状态/切面板：**必须用 NetworkManager 的虚方法**（见下方"坑 3"），不要订阅 `NetworkClient.OnConnectedEvent` 之类静态事件；
  6. 连接成功后 `ShowPanel<RoomPanel>()`（房间内容 NET-3 做）。
- **验收**：Editor Host + ParrelSync/打包实例 Join(127.0.0.1) → 两端显示已连接；断开回退不崩；不打任何 NetworkObject。
- **不做**：房间数据、开局、命令、状态同步。

### NET-2 常见坑（实测补充）

**坑 1：Player Prefab 为空会报错**
Mirror 的 `NetworkManager` 默认勾选 `Auto Create Player`，勾着但 `Player Prefab` 为空时，StartHost/StartClient 会报错。
→ 修法：**取消勾选 `Auto Create Player`**（我们的"玩家"是 Model 里的概念，不使用 Mirror 的 player 对象；因此 Player Prefab 保持空是对的）。


**坑 3：不要订阅 `NetworkClient.OnConnectedEvent` 等静态事件（会失效）**
源码位置：`Assets/Mirror/Core/NetworkClient.cs`
- L102-104：`OnConnectedEvent / OnDisconnectedEvent / OnErrorEvent` 是**静态字段**；
- L2053-2055：`StartClient/StartHost` 过程中会把它们**直接 `= null` 清空** → 你在点按钮之前订阅的回调全被抹掉，UI 永远收不到"已连接"。

正确做法：**子类化 NetworkManager，override 虚方法**（`Assets/Mirror/Core/NetworkManager.cs` 提供）：
- `OnClientConnect()` —— 连上服务端（Host 自己也会触发）→ 切到 RoomPanel
- `OnClientDisconnect()` —— 断开 → 切回 BeginPanel
- `OnClientError(TransportError error, string reason)` —— 连接失败 → 显示错误
- `OnServerConnect(NetworkConnectionToClient conn)` —— 服务端侧有新客户端接入（NET-3 分配 PlayerID 用）

参考代码：

```csharp
using Mirror;
using System;

public class SparkAgeNetworkManager : NetworkManager
{
    public event Action Connected;                       // 连上（含 Host 自己）
    public event Action Disconnected;
    public event Action<string> ConnectFailed;

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Connected?.Invoke();
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        Disconnected?.Invoke();
    }

    public override void OnClientError(TransportError error, string reason)
    {
        base.OnClientError(error, reason);
        ConnectFailed?.Invoke(reason);
    }
}
```

UI 侧（在面板 `Init()` 里订阅一次；面板是常驻的，Init 只跑一次）：

```csharp
var nm = (SparkAgeNetworkManager)NetworkManager.singleton;   // 场景里换成我们的子类
nm.Connected    += () => { UIManager.Instance.ShowPanel<RoomPanel>(); };
nm.Disconnected += () => { UIManager.Instance.ShowPanel<BeginPanel>(); };
nm.ConnectFailed += msg => Debug.LogError($"连接失败：{msg}");
```
**调用链（源码实证，解释了"为什么必须 override 虚方法"）**
1. `NetworkManager.RegisterClientMessages()`（`Assets/Mirror/Core/NetworkManager.cs` L757-760）：
   `NetworkClient.OnConnectedEvent = OnClientConnectInternal;` —— **NetworkManager 用 `=` 把自己的处理器写进这些静态字段**，所以你提前订阅的回调被覆盖；
2. 连接成功后 `NetworkClient` 触发 `OnConnectedEvent?.Invoke()`（`NetworkClient.cs` L280）；
3. 于是进入 `NetworkManager.OnClientConnectInternal()`（`NetworkManager.cs` L1211）；
4. 它内部调用 **virtual** 的 `OnClientConnect()`（L1248）；
5. 你 override 的 `NetworkMgr.OnClientConnect()` 被执行 ✅。

> 一句话：**NetworkClient / NetworkServer 是引擎，NetworkManager 是引擎的遥控器 + 回调出口；那些静态事件是它们之间的内部线，不是留给你的公开接口。**
**坑 2：同机多开时 PlayerPrefs 是共享的**
`PlayerPrefs` 按「公司名 + 产品名」存在操作系统里 → **同一台电脑上的多个客户端实例共享同一份**，所以两个实例会读到同一个昵称、`IsFirst` 也一起为 false。
→ 三种处理：
1. **接受同名**（最省事，仅供本地测试；真机联机时每台机器各自独立，不存在这问题）；
2. **命令行覆盖**：启动参数带 `-nickname=Alice` 时优先用它，否则读 PlayerPrefs（ParrelSync 支持给克隆实例传启动参数）——推荐，既保留"记住昵称"又能在本机测出两个不同名字；
3. **昵称不做持久化、每次启动都弹登录** —— 已采用此项：每个实例各自输入昵称，同机多开不再冲突。
---

## NET-3：房间状态与槽位（RoomState 权威 + 同步）

**目标**：4 个槽位（人类/AI、昵称、角色、就绪）在所有端看到的是**同一份权威数据**。

- **流程意义**：房间阶段的核心——谁在房间里、谁是几号、哪个槽是我、哪些槽将来是 AI。开局的一切（玩家数、出生点、回合顺序）都由这份槽位表决定。
- **架构意义**：`RoomState` 是**会话层权威数据**（服务端持有、广播），**不进 Model**；各端 UI 订阅消息刷新，不自己推断。
- **做什么**：
  - `RoomState` 完成：固定 4 槽；Host 占 PlayerID 1；客户端连上后由服务端分配最小空闲 PlayerID；空槽标记为"待加入 / AI"；
  - 消息：`RoomStateMsg`（服务端→所有：完整槽位表）与"你是 PlayerID X"（可含在消息里）；
  - 客户端收到后写入 `GameSession.MyPlayerId`，并用昵称标记"本机玩家"；
  - `RoomPanel` 绑定显示 4 行（昵称 / 角色 / 就绪 / "我"标记）。
- **验收**：A 建房间、B/C 加入 → 三端 RoomPanel 显示一致的槽位表，各自标出"我"。
- **不做**：就绪与开局（NET-4/5）、掉线处理。

---


### NET-3 补充一：通用交互回路（所有房间控件都按这个走）

**UI 不直接改状态、也不做"本地先改"**，一律走下面这条回路：

1. UI 控件按下 → 组一条"意图消息"（C→S）→ `NetworkClient.Send(msg)`；
2. 服务端 handler：用 `conn.connectionId` 找到该连接的槽位 → **校验合法性**（这是你的槽位吗？该状态允许改吗？）→ 修改服务端权威槽位表；
3. 服务端 `NetworkServer.SendToAll(RoomStateMsg)` 广播**整张槽位表**；
4. 所有端（**包括操作者自己**）收到 `RoomStateMsg` → 刷新 UI。

> 为什么要"等广播"而不是本地直接改：UI 只有一份事实来源（服务端），否则客户端会和服务端不一致（例如两个玩家同时抢同一个角色/槽位）。

**NET-3 涉及到的控件**：
- `btnClose`（离开房间）：客户端断开（Host 用 `StopHost`，Client 用 `StopClient`）→ 服务端 `OnServerDisconnect` 释放槽位并广播 → 各端回 BeginPanel；
- `btnReady` / `btnCancel`：属于 NET-4（就绪），本卡可以先禁用/隐藏，但**回路与上面完全一样**（`SetReadyMsg`）。

### NET-3 补充二：房主初始槽位时序（房主不是特例）

```
房主点[创建房间] → NetworkManager.StartHost()
   → OnStartServer（注册服务端 handler：PlayerNameMsg）
   → OnStartClient（注册客户端 handler：PlayerIdMsg / RoomStateMsg）
   → Host 自身也作为客户端触发 OnClientConnect
   → 客户端发 PlayerNameMsg（昵称）
   → 服务端分配 1 号槽 → 单发 PlayerIdMsg + 广播 RoomStateMsg
   → 房主 RoomPanel 显示"玩家1：<昵称>（我）"
```

**要点**：房主不需要单独的分配代码，它和普通客户端走**同一条**"上报昵称 → 分配 → 广播"链路（前提是 `OnStartServer/OnStartClient` 里已注册 handler，再在 `OnClientConnect` 里发昵称）。

### NET-3 补充三：断开必须释放槽位

`OnServerDisconnect(conn)` 里：按 `conn.connectionId` 找到对应槽位 → 清空（名字清空、`slotConns = -1`）→ `NetworkServer.SendToAll(RoomStateMsg)`。
否则槽位会泄漏（玩家退出后房间永远显示占位，且新玩家无法加入）。
---


## NET-3.5：配置统一由 ConfigManager 单例提供

**决定**：新建一个**只读**配置单例 `ConfigManager`，统一提供 ScriptableObject 配置（角色 / 单位 / 城市 / 局参数），替代"配置只在 GameController 里注入"的做法。

- **放哪**：`Config/ConfigManager.cs`（与 SO 同层，属于基础设施/服务层）。它是"服务"，允许全局单例访问。
- **提供什么**（只读查询）：
  - 角色列表（给房间 Dropdown 用：id + 名称 + 描述）
  - 单位 / 城市配置（给启动装配用）
  - 局参数（seed / 地图宽高）
- **SO 引用怎么来**：在 BeginScene 的常驻对象上挂一个序列化引用指向 `GameCfg`，在 `Awake` 里 `ConfigManager.Instance.Init(gameCfg)`（避免用 `Resources.Load` 把资源路径写死）。
- **红线**：**Model 不允许通过 ConfigManager 拿配置**。启动时仍由 Controller 把 SO 转成纯 C# 的 `UnitInfo / CityInfo / CharacterInfo / GameSetUpInfo` 再注入 `GameInfo`；ConfigManager 只服务 ①启动装配 ②UI 显示。
- **谁在用**：Controller（启动装配）、UI（Dropdown 选项、名字显示）；Model 不引用它。

---

## NET-4：角色与就绪（含角色配置注入）

**目标**：玩家在房间用 Dropdown 选角色（默认随机），所有人点就绪。

- **流程意义**：这是"开局前最后一步配置"——角色（文明/领袖）与就绪状态都由这里确定。全人类就绪是开局的触发条件。
- **架构意义**：角色属于**开局配置数据**：`CharacterCfg`（Config 层，ScriptableObject）→ `CharacterInfo`（Model 层，纯 C#）→ 注入 `GameInfo`；UI 的 Dropdown 数据来自 **Controller 暴露的选项列表**（UI 不碰 Config/Model）。就绪是会话状态，不进 Model。
- **做什么**：
  - 新增 `CharacterCfg`（Id / Name / Description，特性字段预留）+ `GameCfg.characterCfgs`；
  - 新增 `CharacterInfo` + `GameInfo.CharacterInfos`；`InitGameInfo()` 里完成注入；
  - 服务端为每个槽位**随机分配未占用的角色**；`SetCharacterMsg` 改角色（服务端校验唯一性）；`SetReadyMsg` 切换就绪；
  - `RoomPanel`：Dropdown（角色）+ 就绪按钮 + 状态刷新；只有人类槽位参与就绪判定。
- **验收**：改角色/就绪在两端实时同步；选已被占用的角色被拒；AI 槽位不需要就绪。
- **不做**：角色特性效果（文明加成）——放联机完成之后。

---


### NET-4 执行要点（补）

**消息（都走 NET-3 的通用交互回路）**
- `SetCharacterMsg { int characterId }`：C→S，请求改角色；
- `SetReadyMsg { bool ready }`：C→S，切换就绪；
- 服务端改完权威槽位表后，统一用 `RoomStateMsg` 广播整表（不新增下行消息）；
- 可选 `RoomErrorMsg { string reason }`：服务端拒绝时回给该客户端（如"角色已被选择"）。

**服务端规则**
1. 分配槽位时**随机挑一个未被占用的角色**（默认随机），写入该槽；
2. 收到 `SetCharacterMsg`：校验①是自己这条连接的槽位②该角色未被别的槽位占用③该玩家未就绪；通过则改、否则回 `RoomErrorMsg`；
3. 收到 `SetReadyMsg`：只允许人类槽位切换；
4. 每次改动后 `NetworkServer.SendToAll(RoomStateMsg)`；
5. **全人类就绪判定**：只统计人类槽位（AI 槽位不参与）——满足则记一个"可开局"状态（真正的开局/场景加载在 NET-5）。

**客户端 / UI**
1. 4 行 UI 各自有：名字文本、角色 Dropdown、就绪图标（RoomPanel 已有这些控件）；
2. Dropdown 选项来自 `ConfigManager` 的角色列表（初始化时填一次）；
3. **只有自己那行可交互**：自己 = `MyPlayerId` 对应槽位；其他行 Dropdown/按钮禁用（显示别人选的）；
4. Dropdown 改变 → `NetworkClient.Send(SetCharacterMsg)`；收到 `RoomStateMsg` 后**以服务端数据回填**（不做本地乐观更新）；
5. `btnReady` / `btnCancel` → `SetReadyMsg(true/false)`；就绪状态由广播回来的 `RoomStateMsg.Ready` 决定图标颜色；
6. 收到 `RoomErrorMsg` → 弹提示（简单 `Debug.Log` 或面板文字），并把 Dropdown 回退到服务端当前值。

**与 SlotInfo 的关系（你之前的判断是对的）**
`SlotInfo`（Model）应当在**开局时**由服务端从房间槽位表（玩家 id + 昵称 + 选中的角色）组装成 `GameSetUpInfo.Slots`，随 `GameStart` 下发后注入 Model —— **不再来自配置文件**。
---

## NET-5：开局（GameStart 广播 + 场景加载）

**目标**：全人类就绪 → 服务端广播开局 → 所有端从**同一个起点**开始对局。

- **流程意义**：房间到对局的转折点。所有客户端必须拿到同一份开局数据（seed、尺寸、最终槽位表、角色分配），否则后续没法同步。
- **架构意义**：开局数据 = `GameSetUpInfo`（Model 的输入，纯数据）；**地图不传**，因为 `MapGenerator` 由 seed 确定性生成，各端本地生成结果一致。场景加载与 `GameController.StartGame()` 属于 Controller 职责。
- **做什么**：
  - 服务端：空槽填 AI、AI 随机角色、生成 seed、组装最终 `GameSetUpInfo` → 广播 `GameStartMsg`；
  - 各端：写入 `GameSession`（IsServer / MyPlayerId / 槽位控制者表）→ 加载 GameScene → `StartGame(setupInfo)`；
  - **替换现有路径**：现在 `GameController` 从 `gameCfg.gameSetUpCfg` 读开局配置；改为从"会话/开局数据"读（调试单机 = Host + 全 AI 槽，仍走同一条路）。
- **验收**：两端进入游戏后，地图、初始单位、城市、回合数完全一致；HUD 显示自己的昵称/回合。
- **不做**：命令同步（NET-6）。

---


### NET-5 执行要点（补）

**第 0 步（先修编译）**：`GameSession` 已删掉 `isServer / myPlayerId`，但 `GameController` 里还在用它们（L68/L69/L318）→ 改为：
- `MyPlayerId => NetworkMgr.Instance.MyPlayerId`
- `IsMyTurn => NetworkMgr.Instance.MyPlayerId == state.CurrentPlayer`
- AI 判定：`NetworkServer.active && session.GetControllerType(state.CurrentPlayer) == ControllerType.AI`

**开局数据（一条消息带齐）**
把现有 `StartGameMsg` 扩展为携带开局数据（或新增 `GameSetupMsg`）：
- `seed`、`mapWidth`、`mapHeight`
- 最终槽位表：`playerId / characterId / name`（**给 Model 的 SlotInfo 用**）
- 控制者类型表：`playerId → Human / AI`（**给 GameSession 用，不进 Model**）

**服务端流程**
1. 判定"所有人类槽位 Ready"（复用/改造现有 `StartGameMsg{AllReady}` 逻辑）；
2. 空槽填 AI：`SlotData.Reset()` 已提供 AI 默认值；为 AI 随机分配**未被占用**的角色；
3. 服务端生成 `seed`（本卡先用服务端随机；以后可做成"房主可填"）；
4. 组装 `GameSetUpInfo`（纯数据：seed/宽高 + `List<SlotInfo>`）；
5. 组装控制者映射（客观的 Human/AI，不含 Local/Remote）；
6. `NetworkServer.SendToAll(GameStartMsg{...})`；
7. `ServerChangeScene("GameScene")`（Mirror 统一切场景；**GameScene 必须加入 Build Settings**）。

**各端流程（含服务端自己）**
1. 收到 `GameStartMsg` → **缓存到 NetworkMgr**（例如 `StartData` 字段）；
2. 场景切换完成后，`GameController` 用缓存数据开局：
   - `GameInfo.GameSetUpInfo = new GameSetUpInfo(seed, w, h, slotInfos)`（**不再读本地 GameSetUpCfg/ConfigMgr**）
   - 填 `GameSession.PlayerType`：**自己 → HumanLocal**，其他人类 → HumanRemote，AI → AI（视角相关的转换只在本地做）
   - 然后 `StartGame()`（建 Model + 装配 View，沿用现有流程）
3. **时序兜底**：若场景已加载但缓存还没到（消息晚到），GameController 在 Update 里等 `HasStartData` 再开局，避免空数据开局。

**关键点**
- `seed` 由服务端唯一决定 → 各端地图一致，**不传地图数据**；
- `ControllerType` **绝不进 Model**（Model 只拿 SlotInfo：playerId/characterId/name）；
- AI 只在服务端跑（`NetworkServer.active` 守卫）；客户端的映射仅用于显示与锁定自己的输入。
---

## NET-6：命令中继（行动由权威执行）

**目标**：玩家操作只发"意图"给服务端，由服务端校验并执行。

- **流程意义**：没有这一步，两个客户端各自执行自己的操作 → **世界立刻分叉**（这是当前联机状态的必经阶段）。有了它，行动才有唯一结果。
- **架构意义**：`IOrderSink.RequestXxx` 已经是唯一入口（UI 不构造 Order）；联机只是把"本地执行"换成"发服务端执行"。校验点：`order.PlayerId == 该连接绑定的玩家`（防冒名）+ `== state.CurrentPlayer`（防抢回合）。AI 仍在服务端跑（已加 `IsServer` 守卫）。
- **做什么**：
  - Order → 网络消息（ID 化已完成，可直接序列化）；
  - 客户端：`RequestXxx` → 发消息；服务端：收到 → `SubmitOrder` → 执行 → 回执（可选）；
  - 拒绝时回执错误原因（供 UI 提示）。
- **验收**：客户端移动/攻击/建城/造兵均由服务端执行；伪造 PlayerId 的命令被拒；两端状态一致（依赖 NET-7 显示）。
- **不做**：状态下行（NET-7）。

---

## NET-7：状态同步与表现（别人的行动你能看到）

**目标**：服务端执行后，把结果同步给所有客户端并正确播放表现。

- **流程意义**：多人对局的可见性——A 的移动/战斗/建城，B 必须立刻看到；回合推进也要同步。
- **架构意义**：服务端广播**状态快照 + 提示 DTO**（docs/tech.md §10 约定）；客户端 `ApplySnapshot`（覆盖本地 Model 状态）+ `PlayHint`（复用现有 View 的动画/刷新逻辑）。**客户端不执行规则**。
- **做什么**：
  - 序列化：GameState（地图 seed/尺寸、单位、城市、玩家、回合、当前玩家）→ 广播；客户端反序列化后刷新 View；
  - 广播时机：每个命令执行后（简单）或回合末批量（更省）；
  - 提示 DTO：把"发生了什么"（移动路径 / 战斗结果 / 建城 / 造兵）发给客户端用于播放动画——客户端不重算规则。
- **验收**：A 动单位 → B 立刻看到（含动画）；战斗/城市易主/回合数同步；HUD 一致。
- **不做**：断线重连、增量同步优化（可后补）。

---


## NET-6 + NET-7 最小版：局内同步（必须一起做）

> 只做上行（NET-6）会看不到结果；只做下行（NET-7）没有数据源。所以这两步合成一次实现，先做"能用"的最小版，动画/增量优化留后面。

### 现状（已确认）
- 各端用同一个 `MapId/seed` 进入 GameScene，各自 `StartGame()` 建了**自己的一份 Model**；
- `GameController.SubmitOrder` 目前**在本地直接执行** → 每个客户端各跑各的，一操作就分叉。


### NET-6 前置概念：Order 类 与 网络消息 是两回事

- **Order 类**（`Model/Orders/*Order`）：进程内命令对象（class + 继承 `BaseOrder`），UI/Controller/AI 用它表达"我想做什么"。**保留不变**。
- **网络消息**（Mirror `NetworkMessage`）：必须是 **struct** 且类型在编译期静态已知，**不支持多态 class** → 所以 Order 类**不能直接发**。
- **翻译层**（本卡要写）：
  - 客户端：`Order 类 → OrderMsg(struct，type 枚举 + 扁平字段) → NetworkClient.Send`
  - 服务端：收到 `OrderMsg` → 按 type **译回对应 Order 类** → 交给权威 GameState 执行
- 两种译法：①**一条扁平 OrderMsg**（推荐，注册一次 handler）；②每种 Order 一条消息（类型清晰但要注册 N 个 handler）。
> **修正（2026-09-18）**：`Model/Orders/*Order` **保留并继续使用** —— 它们是进程内命令对象（UI/AI/单机 Host 共用 `SubmitOrder` 统一入口，类型安全、便于回放）。需要补的只是**网络边界的一层翻译**：
> - 客户端：`Order 类 → OrderMsg(struct)` → `NetworkClient.Send`（不本地执行）；
> - 服务端：`OrderMsg → 译回 Order 类` → 交给权威 GameState 执行；
> - 推荐"**一条扁平 OrderMsg + 一个集中翻译器**"（而不是每种 Order 各写一条消息）。
> 三个注意点不变：①来源校验用 `conn` 反查槽位 playerId；②字段有效性按 type 约定并校验；③失败单发回执、成功广播全量快照。


### NET-7 落地现状（客户端表现层）与剩余项

**已实现（核对过代码）**
- `NetworkMgr.OnGameStateDeltaMsg`：`if (NetworkServer.active) return;`（Host 不应用自己的快照）✓；`networkInput` 未就绪时入队 ✓
- `GameController.ApplySnapShot`：`state.ApplySnapshot(msg)` → `AppliedDelta` → HUD / `UnitView`(Build/Update/Destroy) / `CityView`(Build/Update) ✓
- `UnitView.UpdateUnit` = 直接改坐标（**瞬移**）；`CityView.UpadateCity` = 按 owner 重新上色（**城市易主会换色**）✓
- `TipMsg` = 服务端拒绝命令时的提示 ✓

**剩余 3 项（功能相关，建议补）**
1. **选中状态未清理/刷新**：快照应用后若选中的单位已被销毁/移动，选中框与范围高亮会残留或指错位置。
   → 应用快照后：选中的单位/城市若已不在 Model → `ClearSelection()`；若还在但位置变了 → 重新 `SelectUnit/SelectCity` 刷新。
2. **发命令后没有锁输入**：客户端点完命令后 phase 仍是 PlayerTurn，玩家可以连点 → 连发多条命令（后发的可能被服务端拒绝）。
   → 发送后切到"等待"状态（如 `OtherPhase` 或新增 `WaitingServer`），收到快照后再按 `CurrentPlayer == MyPlayerId` 解锁。
3. **`UnitView.UpdateUnit` 直接索引 `unitObjs[unit]`**：若某单位只在 Updated 列表里而从未 Build（delta 顺序/边界情况），会 KeyNotFound。
   → 改成 `TryGetValue` 兜底（没有就先 Build）。

**动画（v2）：Hint 队列**
- 新增 `HintMsg`（表现数据）：`type`（Move/AttackUnit/AttackCity/FoundCity/BuildUnit/TurnChange）+ `unitId` + `pathHex[]` + `targetId` + 标志位；
- 服务端在**执行成功时**同时组 `Hint` 与 `Snapshot` 一起广播（Hint 的数据直接取自 `MoveResult.Path` / `AttackUnitResult.AttackerIsDead|DefenderIsDead|CanEnter` / `AttackCityResult.CityIsCaptured` 等已有返回值）；
- 客户端：Hint 入队 → 逐条播放（**复用现有** `UnitView.MoveUnit / AttackUnit / AttackCity`）→ 每条播完应用最新快照 → 下一条；Host 收到 Hint 直接 return（本地已播）；
- 最小版只做 **Move + AttackUnit**（最影响观感），其余动作保持"快照瞬变"。
---

### NET-7 补充：动画（表现）如何同步

**核心结论**：**快照只能保证"最终一致"，不能表达"过程"**。
单位从 (2,3) 变成 (5,4)，客户端从快照里看不到：它走了哪条路、路上有没有打仗、有没有人死、城市是不是易主了。所以动画需要**另一类数据：提示（Hint）**。

**做法 = 事件（Hint）+ 快照，两条一起发**

```
服务端执行 Order 成功
  ├─ ① 生成 Hint（表现数据）：做了什么 + 过程信息（路径 / 双方是否死亡 / 是否占领…）
  ├─ ② 生成 Snapshot（最终状态）：用于最终一致
  └─ 广播 { Hint, Snapshot }

客户端收到
  ├─ Hint 入队 → 播放动画（复用已有 View 事件/方法）
  └─ 动画结束后 → 应用 Snapshot（覆盖本地副本 Model）→ 刷新 View
```

**时序：三选一，推荐第 2 种**

| 方案 | 做法 | 评价 |
|---|---|---|
| 1. 先应用快照，再播动画 | Model 已是终态，动画只是表演（要把视觉对象临时挪回起点） | 容易闪烁/穿帮 |
| **2. 先播动画，播完再应用快照** | 动画期间 Model 仍是旧状态，结束时一次覆盖 | **推荐**，实现最简单，视觉与 Model 不矛盾 |
| 3. 立即应用 Model，View 用独立"视觉副本" | 复杂度最高 | 以后要丝滑再做 |

**必须做"动画队列"**：一回合内可能有多条 Hint（尤其 AI 回合一次做很多动作）。不排队会互相打断/重叠。
→ 维护一个 `Queue<Hint>`，同一时间只播一条，播完出队再播下一条（Host 不需要队列，它本地执行时已经播过了）。

**各动作的 Hint 内容（复用现有 View 事件即可）**

| 动作 | Hint 带什么 | 客户端播放（对应现有 View 逻辑） |
|---|---|---|
| 移动 | 单位 id + 路径 | `UnitView.MoveUnit(unit, path)` |
| 攻击单位 | 攻方/守方 id + 双方死亡标志 + 是否进格 + 路径 | `UnitView.AttackUnit(...)` |
| 攻城 | 攻方 id + 城市 id + 是否占领 + 是否进城 | `UnitView.AttackCity(...)` |
| 建城 | 城市数据 + 被消耗的移民 id | 移民消失 + `CityView` 建城 |
| 造兵 | 新单位数据 | 单位出现 |
| 回合切换 | turnNumber + currentPlayer | HUD 刷新 + "轮到 X" 提示 |

**要点**
- Hint 是**表现层数据**，不要塞进 Model；它和 `EventDefine` 里那些 View 事件是同构的（可以理解为"网络版的 UnitMoveEvent / AttackUnitEvent"）；
- 服务端要把**本地执行时已有的结果信息**打包进 Hint（例如 Move 的 `result.Path`、战斗的 `AttackerIsDead/DefenderIsDead` —— 这些现在只在本机 View 用到）；
- Host 收到自己广播的 Hint/Snapshot 都直接 `return`（本地已经播过、状态已最新）。

**最小可用三步（时间紧就按这个来）**
1. **第 1 版**：不做动画同步 —— 客户端收到快照直接重建 View（瞬移）。先把"状态一致"跑通；
2. **第 2 版**：只给**移动**和**攻击**加 Hint 队列（最影响观感的两件事）；
3. **第 3 版**：建城 / 造兵 / 城市易主也走 Hint。
---

### NET-6：命令上行（客户端只发意图）

**消息**：`OrderMsg`（扁平结构，一条消息承载所有命令类型）
- `type`（Move / AttackUnit / AttackCity / FoundCity / BuildUnit / EndPhase）
- `playerId`、`unitId`、`cityId`、`targetQ`、`targetR`、`unitType`
- 说明：Order 有子类，网络消息用**扁平字段**最省事；坐标用两个 int，避免额外序列化不确定性。

**客户端侧**：`SubmitOrder` 加分支
- `NetworkServer.active`（Host/服务端）→ 本地执行（原逻辑）+ 广播快照；
- 否则（Client）→ **只发 `OrderMsg`，不本地执行**（关键：一执行就分叉）。

**服务端侧**：`NetworkMgr.OnStartServer` 注册 `OrderMsg` handler
- 先校验来源：`msg.playerId` 必须等于**该连接所在槽位的 PlayerId**（防冒名）；
- 再把 `OrderMsg` 转成对应 Order 对象 → 交给 GameController 执行（需要一个服务端执行入口，如 `ExecuteOrderFromServer`）；
- 执行成功 → 广播快照（NET-7）；失败 → 可回执错误（可选）。

**AI**：仍在服务端直接走本地执行路径（AiOrders 已经在 GameController 内部），执行后同样广播快照。

### NET-7 最小版：状态下行（快照覆盖）

**消息**：`GameSnapshotMsg`
- `turnNumber`、`currentPlayer`
- 单位数组：`id / owner / type / q / r / hp / movementLeft`
- 城市数组：`id / owner / q / r / hp / production`

**服务端**：每次命令执行成功（或回合末）→ 组快照 → `NetworkServer.SendToAll`。

**客户端**：收到快照 → `ApplySnapshot`：
1. 用快照**覆盖本地 Model**（清空 Units/Cities → 按快照重建；回合/当前玩家一并覆盖）；
2. **全量重建 View**（先销毁现有单位/城市 GameObject，再按 Model 重建）——单位数量少，demo 够用；增量/动画以后再做；
3. 快照应用后，HUD/选择状态按新 Model 刷新（选中单位若已不存在则清空选择）。

**进阶（本卡不做）**：`HintMsg`（移动路径/战斗结果）用于播放动画；增量同步。

### 验收
- [ ] A 移动单位 → B 端立刻看到位置变化（先不做动画也可以）
- [ ] 战斗、建城、造兵、城市易主、回合数都能同步
- [ ] B 自己操作 → A 端也能看到
- [ ] AI（服务端）行动 → 所有客户端都能看到
- [ ] 客户端无法操作非自己回合/非自己的单位（服务端校验兜底）
- [ ] 两台客户端跑同一局，长时间操作后状态不漂移
- [ ] 提交：`feat: order relay and snapshot sync`

### 附带风险（已用约定解决）
`GameController.InitGameInfo` 对每个槽位查 `StaticInfo.CharacterInfos[slot.CharacterId]`；空槽/AI 槽的 CharacterId = 0。
**约定**：`CharacterCfg.Id` 从 0 开始且连续，**0 号 = 随机/默认角色** → 查表不会 KeyNotFound。
待办（独立功能，非本卡）：给 AI 槽**随机分配且不与人类重复**的角色（当前都落 0 号）。
---

## NET-9（可选）：掉线接管与重连

**目标**：远端玩家掉线时由 AI 接管该槽位，避免对局中断。

- **流程意义**：稳定性与演示体验（掉线不炸局）。
- **架构意义**：只改服务端的"槽位→控制者"映射（HumanRemote → AI），Model 与命令通道不变——正是"控制者映射独立于 Model"的红利。
- **做什么**：监听连接断开 → 该槽位控制者改为 AI → 广播 RoomState/提示；若该玩家回到房间可选择归还控制权（可选）。
- **验收**：强制关闭一个客户端 → 其余端看到"该玩家由 AI 接管"，对局继续。
- **不做**：断线续传（重连后恢复自己控制权可作为增强）。

---

## 附：每张卡与架构层的对应

| 卡 | 主要落点 | 是否碰 Model |
|---|---|---|
| NET-1 登录昵称 | Controller/Network（本地资料） | 否 |
| NET-2 连接 | UI 直调 NetworkManager.singleton（教程式） | 否 |
| NET-3 房间槽位 | Controller/Network（RoomState）+ UI | 否 |
| NET-4 角色/就绪 | Config → Model（CharacterInfo 注入）+ Controller/UI | 仅注入配置数据 |
| NET-5 开局 | Controller（场景加载/StartGame）+ Model 输入（GameSetUpInfo） | 是（只读输入） |
| NET-6 命令中继 | Controller（SubmitOrder 入口） | 否（走既有 Order） |
| NET-7 状态同步 | Model/Serialization + Controller + View | 是（序列化快照） |
| NET-9 掉线接管 | Controller/Network（控制者映射） | 否 |

---

## NET-7A 任务卡：客户端表现层收尾（3 个小项）

**目标**：让客户端在收到快照后，选中状态、输入门控、边界情况都正确。

**意义**：数据同步已经通了，但"选中框指错位置""连点发出多条命令""字典 KeyNotFound"这类问题会让联机体验看起来像 bug；这三项补完后，客户端表现才"干净可用"。

**改动点与逻辑**
1. **选中状态刷新**（`GameController.ApplySnapShot` 末尾）
   - 若 `selectionView.SelectedUnit` 已不在 Model（被销毁）→ `ClearSelection()`；
   - 若仍存在但位置/移动力变化 → 重新 `SelectUnit` 刷新选中框与范围高亮；
   - 城市同理（选中城市被易主/不存在时清理）。
2. **发命令后锁输入**
   - 客户端 `SubmitOrder` 的"发送分支"里，把 phase 切到等待态（复用 `OtherPhase` 或新增 `WaitingServer`）；
   - 收到快照后按 `state.CurrentPlayer == MyPlayerId` 决定 `PlayerTurn` / `OtherPhase`（这段已在 `ApplySnapShot` 里，只需保证发送后不会继续保持 PlayerTurn）。
3. **`UnitView.UpdateUnit` 兜底**
   - `unitObjs[unit]` 改为 `TryGetValue`：没有视觉对象就先 `BuildUnit`，避免 delta 边界情况抛异常。

**验收**
- [ ] 客户端：选中单位 → 对方让它移动/死亡 → 本端选中框与范围正确（不残留、不指错）
- [ ] 客户端：连续点击目标格只发出一次命令（等待期内不再发）
- [ ] 收到只含 Updated 不含 Added 的单位时不会抛异常
- [ ] 提交：`fix: client selection refresh, input lock after order, safe unit view update`

**不做**：动画（NET-7B）。

---

## NET-7B 任务卡：动画同步最小版（Hint 队列，只做移动 + 攻击）

**目标**：客户端能看到单位"走过去"和"打起来"，而不是瞬移。

**意义**：快照只能表达终态，动画需要过程信息。做法是把服务端执行时已知的过程数据（路径/死亡标志）作为 Hint 一起下发，客户端复用现有 View 动画方法播放 —— 这是"表现层同步"与"状态同步"的分工点。

**关键 API / 数据结构**
- 新增 `HintMsg : NetworkMessage`：`type`（Move / AttackUnit）、`unitId`、`targetUnitId`、`path`（HexCoord 数组）、`attackerIsDead`、`defenderIsDead`、`canEnter`；`SlotData[]` 已验证自定义 struct 数组可序列化，HexCoord 同理；
- 服务端发送：`NetworkServer.SendToAll(hintMsg)`（与快照同一次执行里发出）；
- 客户端接收：`NetworkClient.ReplaceHandler<HintMsg>(...)`，**Host 直接 return**；
- 客户端播放：复用 `UnitView.MoveUnit(unit, path)` / `UnitView.AttackUnit(attacker, defender, attackerIsDead, defenderIsDead, canEnter, path)`。

**服务端改动**
- 在 `GameController.ExecuteOrder` 成功分支里，用已有返回值构造 Hint：
  - Move → `result.Path`
  - AttackUnit → `result.Path` / `AttackerIsDead` / `DefenderIsDead` / `CanEnter`
- `ExecuteResult` 扩展为可同时携带 Hint 与 Delta（例如加 `bool HasHint; HintMsg Hint;`），`NetworkMgr.OnOrderMsg` 广播时两者一起发。

**客户端改动（动画队列）**
- 收到 Hint → 入队 `Queue<HintMsg>`；
- **Hint 播放期间把快照缓存起来（只保留最新一条）**，不要立刻应用，否则动画会被瞬移打断；
- Update 里逐条播放：调 View 动画 → 等动画完成事件（`UnitMoveEvent` / `AttackUnitEvent`）→ 应用最新缓存快照 → 下一条；
- Host 既不收 Hint 也不入队（`if (NetworkServer.active) return;`）。

**验收**
- [ ] A 端移动单位 → B 端**看到沿路径行走**（不是瞬移）
- [ ] A 端攻击单位 → B 端看到攻击过程与死亡消失
- [ ] AI（服务端）行动在客户端同样可见，且多条行动按顺序播放、不重叠
- [ ] 动画期间不会因快照覆盖而瞬移/闪烁；播完状态与快照一致
- [ ] 提交：`feat: hint-based animation sync (move/attack)`

**不做**：建城/造兵/易主的 Hint（先走快照瞬变，后续按同一模式补）。

### Hint 与 GameStateDelta：分还是合？

**概念上必须分开（不要塞进同一个结构体）**，因为两者性质完全不同：

| 维度 | GameStateDelta（状态） | Hint（表现） |
|---|---|---|
| 回答的问题 | "现在是什么" | "刚刚是怎么变成这样的" |
| 内容 | 单位/城市的最终位置、血量、归属、回合、当前玩家 | 移动路径、双方是否死亡、是否占领、是否进格 |
| 生命周期 | **持久**（存档、重连补偿、校验、将来回放的基础） | **一次性**（播完即可丢；新加入/重连者不需要历史动画） |
| 与命令的数量关系 | 一条命令 = 一条终态 | 一条命令可能对应**多条**表现步骤（靠近→打击→死亡→进格） |
| 可否丢弃 | 丢一次可能造成显示错误/跳变 | 丢了只影响观感 |
| 是否可跳过 | 必须应用 | 可跳过（低配/加速/跳过动画模式） |

把 path、死亡顺序这类"过程信息"塞进状态消息，会让状态里混进**表现层字段**（状态本身并不需要路径），存档/回放/校验时都是噪声。

**传输上可以合并（推荐做法）**：用一条 `GameUpdateMsg` 包装两者

```
GameUpdateMsg { GameStateDeltaMsg Delta; HintMsg Hint; }   // Hint 可为 None
```

- 好处：**只发一次**（省一次 SendToAll 与一次打包），但逻辑上仍分两层：客户端先按 Hint 播动画，播完再应用 Delta；
- 若不合并（分两条消息）：**两条都要走默认 Reliable 通道**才保序（先 Hint 后 Delta）；若用了 Unreliable 通道，顺序不保证 → 还是合并成一条更省心。

**结论**：数据结构分开、传输合并成一条消息；`Hint` 允许为空（None），表示"这次没有需要播放的动画，直接应用 Delta"。

---

## NET-8 任务卡：AI 重写（服务端输入源 + 统一命令广播）

> 执行前置：当前未提交的同步/表现改动先单独提交，保证 AI 重写是一个可回滚的独立提交。本卡不改同步协议，只重排 AI 驱动链路。
> 卡号说明：原“掉线接管与重连”后移为 NET-9，避免与 AI 重写同名。

### 为什么现有 AI 不满足联网

| # | 现状 | 后果 |
|---|---|---|
| 1 | `HandleAiOrders/AiOrders` 是 `GameController` 内的旧协程，且入口已失效 | AI 依赖 Unity 帧循环和表现层，不是服务端输入源 |
| 2 | `ai.DecideOrders()` 返回命令后没有被执行，`WaitUntil` 也没有被 `yield return` | AI 实际不会完整行动 |
| 3 | 只有 `NetworkMgr.OnOrderMsg` 里的远程人类命令会广播 | AI 即使内部执行，客户端也收不到 |
| 4 | `TryEndPhase` 发现下一位是 AI 时直接 `state.EndTurn()` | 把 AI 回合当成跳过；`EndTurn` 是轮末结算，不是跳过玩家 |
| 5 | AI 决策写在 `Controller/AIOrders.cs`，与协程/表现耦合 | 不能单测，也不能复用到掉线接管 |

**结论**：删除旧 AI 协程，不修旧 `HandleAiOrders`。改成“AiDecider 产出命令 → AiDriver 在服务端驱动 → NetworkMgr 统一执行并广播”。

### 与现有代码的对应关系

| 现有代码 | 新代码 | 负责什么 | 关键区别 |
|---|---|---|---|
| `Controller/AIOrders.cs` 中的 `AiOrders.DecideOrders()` | `Model/Ai/AiDecider.cs` 中的 `AiDecider.Decide()` | **决策**：只回答“AI 下一步应该提交哪一条 Order” | 不引用 Unity；可以单测；不负责执行和广播 |
| `GameController.AiOrders()` 协程 | `Controller/Ai/AiDriver.cs` 中的 `Run()` | **驱动**：轮到 AI 时循环调用决策，执行命令，按间隔继续下一条 | 只在服务端运行；调用统一执行广播入口；不依赖 View 动画 |
| `GameController.HandleAiOrders()` | `GameController` 在 EndPhase 后启动 AiDriver | **入口**：判断当前玩家是不是 AI，并启动驱动 | 由回合推进触发，不再由失效的旧入口触发 |
| `NetworkMgr.OnOrderMsg` 中“执行 + 广播”的代码 | `NetworkMgr.ExecuteAndBroadcast()` | **权威执行与广播**：人类命令和 AI 命令共用 | 不再让 AI 走内部私有调用 |

一句话：**AiDecider 是原来的“军师”，AiDriver 是原来的“协程循环”；NetworkMgr 是新增的“传令与广播口”。**

### 统一链路

```text
远程人类：OrderMsg → NetworkMgr.OnOrderMsg → NetworkMgr.ExecuteAndBroadcast(order, conn)
AI：      AiDecider.Decide → AiDriver → NetworkMgr.ExecuteAndBroadcast(order, null)
```

### 为什么 AiDriver 不放在网络层

`AiDriver` 的职责是“在服务端驱动 AI 回合”：判断当前 AI 要执行哪一条命令、是否继续下一条、何时停止。它属于 Controller 层的运行时流程，不属于网络传输层。

它只是需要依赖一个服务端命令出口：

```text
GameController（回合推进）
    → AiDriver（AI 回合驱动，Controller/Ai）
        → AiDecider（AI 决策，Model/Ai）
        → NetworkMgr.ExecuteAndBroadcast（服务端执行 + 广播，Controller/Network）
```

因此：

- `AiDecider` 放 `Model/Ai`：纯决策，不依赖 Unity；
- `AiDriver` 放 `Controller/Ai`：只负责服务端驱动循环；
- `NetworkMgr` 放 `Controller/Network`：只负责网络边界、执行入口和广播；
- `GameController` 负责在回合推进后启动 AiDriver。

第一版可以让 AiDriver 直接调用 `NetworkMgr.Instance.ExecuteAndBroadcast`，不额外抽接口；目录归类仍保持职责清晰。
`ExecuteAndBroadcast` 放在 `NetworkMgr`（网络边界）：内部调用 `networkInput.ExecuteOrder(order)`，Tip 单发给发起者，GameUpdate 广播给所有客户端。`GameController.ExecuteOrder` 仍只负责游戏规则分发，不负责网络发送。

### 落地步骤

1. **`Model/Ai/AiDecider.cs`（纯 C#）**
   - `Decide(GameState state, int playerId) → BaseOrder`。
   - 迁移现有优先级：城市造兵 → 移民建城 → 战士攻击 → 向最近敌人移动 → `EndPhaseOrder`。
   - 只使用 ID 生成 Order；不使用 UnityEngine；AiDecider 每局创建一个；每次轮到 AI 调用 `BeginTurn(playerId)` 清空本回合尝试记录，不要每回合 new。
   - 这次只要求行为正确，不要求提高 AI 智力。

2. **`Controller/Ai/AiDriver.cs`（服务端驱动）**
   - 只允许在 `NetworkServer.active` 时运行。
   - `while (当前玩家是 AI)`：`Decide` → `ExecuteAndBroadcast` → 非 EndPhase 时按 `aiStepDelay` 等待。
   - 每回合设置最大步数，防止失败命令导致死循环。
   - 不等待客户端动画，不依赖 View；驱动结束时按当前玩家恢复 Host 本地 phase。
   - 由 `GameController` 创建并 `StartCoroutine`，不新增场景挂载脚本。

3. **`NetworkMgr` 增加统一服务端入口**
   - `OnOrderMsg` 和 `AiDriver` 都调用同一个 `ExecuteAndBroadcast`。
   - 统一处理 `ExecuteResultType.Tip` 与 `ExecuteResultType.GameUpdate`。
   - 本卡不顺手改来源校验，来源校验仍在后续权威边界任务中处理。

4. **`GameController` 清理旧 AI**
   - 删除 `AiOrders` 字段、`HandleAiOrders()`、`AiOrders()` 协程和旧 `Controller/AIOrders.cs`。
   - `TryEndPhase()` 删除“下一位是 AI 就 EndTurn”的分支，只执行 `state.EndPhase()`。
   - `EndPhase` 后若服务端发现当前玩家是 AI：设置 `phase = GamePhase.AiPhase`，启动 `AiDriver`。
   - AI 回合结束后：Host 根据当前玩家恢复 `PlayerTurn` 或 `OtherPhase`；非 Host 不启动 AiDriver，只按 GameUpdate 走 `OtherPhase`。

### 验收

- [ ] Host 中 1 个真人 + 3 个 AI：真人结束回合并轮到 AI 后，AI 会建城、造兵、移动、攻击并按节流逐步发生。
- [ ] AI 执行到下一个真人玩家后停止，不会继续偷偷操作。
- [ ] AI 的每次成功操作都通过同一广播链到达所有客户端。
- [ ] `EndPhaseOrder` 不再直接调用 `EndTurn`；轮末结算仍由 `GameState.EndPhase` 的轮次边界触发。
- [ ] 多个 AI 连续回合可正常跑完；最大步数保护生效。
- [ ] `AiDecider` 不引用 UnityEngine；非 Host 不启动 AiDriver。
- [ ] 提交：`feat: rewrite AI as server-side command source`

### 本卡不做

- AI 高级策略、难度分级、寻路优化；
- NET-7 状态结构重构；
- 掉线接管与重连（NET-9）；
- 人类命令来源校验与完整反作弊。
