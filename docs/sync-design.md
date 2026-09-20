# 星火纪元 · 状态同步与表现层设计（Sync & Presentation Design）

> 目的：重新设计"服务端 → 客户端"的同步消息与表现层驱动方式，做到**一份消息同时完成：同步 GameState + 驱动 UI + 驱动 View/动画**，
> 并用**事件中心**取代 Controller 直接调度 View 的做法。
> 关联：docs/networking.md（联机执行卡）、docs/tech.md（技术约定）、docs/notes.md（决策记录）。

---

## 1. 现状问题（通读代码后的诊断）

| # | 问题 | 证据 |
|---|---|---|
| 1 | **两条并行表现路径**：同一动作，主机走"直接调 View"，客户端走"事件中心" | 主机：`GameController.TryMoveUnit` 直接 `unitView.MoveUnit(...)`（L560）、`TryFoundCity` 直接 `unitView.DestroyUnit + cityView.BuildCity`（L598-600）、`TryAttackUnit/AttackCity` 直接调（L668/L711）；客户端：`ApplyHint` 里 `EventCenter.EventTrigger<MoveUnitEvent/AttackUnitEvent/AttackCityEvent>`（L443/454/465） |
| 2 | **事件发了没人听**：全项目 View/UI **没有任何** `EventCenter.AddListener<T>`（只有 UGUI 的 `onClick.AddListener`） | 搜索 `AddListener<` 只命中 EventCenter 自身定义；UnitView/CityView/HUD 均未订阅 → 客户端 Hint 事件进入空气，动画不会播 |
| 3 | **`AnimationCompleted` 无定义** | UnitView L123/L153/L182 使用，但全项目无 `class/struct AnimationCompleted` → 编译期就会报错（需确认当前是否已修） |
| 4 | **`GameStateDeltaMsg` 名不符实** | 它是"本次涉及对象的零散列表"（Move 只带 1 个 UnitData），客户端靠 `TryGet != null` 判断"更新 or 创建"，**无法表达删除**；且缺 `MaxHp / MaxMovement / Radius / 玩家名 / 角色` 等 UI 需要的字段 |
| 5 | **Hint 信息不足** | `UnitHintData` 只有 `Path / TargetId / CanEnter / CityIsCaptured`，**没有 attackerDied / defenderDied**；`EventDefine.AttackUnitEvent` 也没有 → 客户端攻击动画无法正确表现死亡 |
| 6 | **死亡用 IsDead 标记、从不同步移除** | `GameState` 攻击后置 `IsDead = true`（L609/L621），但 `ApplyGameDelta` 只"更新或创建"，永不删除 → Model 里累积死单位，规则查询到处要判 IsDead |
| 7 | **UI 被 Controller 直接调度** | `UIManager.Instance.GetPanel<HUD>().UpdateHUD(...)` 在 GameController 出现 4+ 处；`SelUnitPanel/SelCityPanel.UpdatePanel()` 由输入处理直接调（L308/L316）；`UpdateTips` 直接调 |
| 8 | **主机不消费自己广播的消息** | `NetworkMgr.OnGameStateDeltaMsg` 里 `if (NetworkServer.active) return;` → 主机 UI/View 永远走"直接调用"这条路，**两端刷新逻辑不同源**，长期必然出现不一致 |

**结论**：消息不是"缺一条"，而是**事实（状态）与演出（动画）没有分层**、**表现层没有统一入口**、**两端走了两套刷新逻辑**。

---

## 2. 设计目标

1. **一条消息同时满足三件事**：同步 `GameState`（事实）、驱动 UI（回合/提示/面板）、驱动 View 动画（演出）；
2. **两端同一条应用管线**：主机执行完也走"应用消息 → 发事件"这条路，不再直接调 View（消除双路径）；
3. **Controller 只做两件事**：翻译输入→命令、把消息→事件；**不再调度具体 View/UI**；
4. **演出与事实解耦**：动画可跳过、可排队、可延迟，不影响状态正确性。

---

## 3. 消息设计（建议）

### 3.1 一条上行：`OrderMsg`（保持现状即可）
`Type + PlayerId + 参与方 id + HexCoord Target`（现已够用）。

### 3.2 一条下行：`GameUpdateMsg`

```
GameUpdateMsg {
    int Seq;                 // 递增序号：丢弃过期/重复消息，便于调试
    WorldSnapshot Snapshot;  // 事实：完整世界状态（全量）
    Effect[] Effects;        // 演出：有序表现指令（可为空）
}
```

**WorldSnapshot（全量，覆盖式）**

| 字段 | 说明 |
|---|---|
| `int TurnNumber / int CurrentPlayer` | 回合与当前玩家（UI 用） |
| `UnitSnap[] Units` | 全量单位：`Id, Owner, Type, Pos, Hp, MaxHp, MoveLeft, MaxMove` |
| `CitySnap[] Cities` | 全量城市：`Id, Owner, Name, Pos, Hp, MaxHp, Production, Radius` |
| `PlayerSnap[] Players` | 全量玩家：`Id, Name, CharacterId, IsAlive` |

> 全量的理由：客户端**覆盖式同步**最简单、最不会漂移；demo 的状态很小（几十个单位/城市），带宽不是问题。
> **死者不出现在 Units 里**（替代现在的 IsDead 标记），这样 Model 只保留活着的实体，规则查询不用到处判 IsDead。

**Effect（演出指令，自包含）**

| 字段 | 说明 |
|---|---|
| `EffectKind Kind` | Move / AttackUnit / AttackCity / FoundCity / BuildUnit / TurnChanged |
| `int ActorId` / `int TargetId` | 主动方 / 被动方（单位或城市 id） |
| `HexCoord From` / `HexCoord To` | **动画起点/终点**（关键：让"先应用状态、再播动画"成立） |
| `HexCoord[] Path` | 路径（含终点） |
| `bool AttackerDied / DefenderDied / CanEnter` | 单位战结果（显式，不再靠猜） |
| `bool CityCaptured / EnteredCity` | 攻城结果 |
| `int NewOwner / UnitType NewUnitType` | 易主 / 造兵类型 |

**为什么 Effect 要自包含 + 要带 From**：
客户端**先应用 Snapshot**（数据立刻正确）**再播动画**（纯视觉演出）——动画开始时把视觉对象摆到 `From`，沿 `Path` 走，结束正好落在 `To`（与 Snapshot 一致）。
这样既不需要"先播动画后应用状态"的排队复杂度，也不会出现"实体已被删除、动画查不到对象"的问题；代价只是动画期间 Model 已是终态（选中框会提前到位，可接受）。

---

## 4. 表现事件设计（EventCenter 承接）

**原则**：Controller 把消息翻译成事件后 `EventTrigger`，**View/UI 自己订阅**，Controller 不再认识 View。

### 4.1 状态类事件（由 Snapshot 差分产生）

| 事件 | 载荷 | 订阅者 | 用途 |
|---|---|---|---|
| `UnitAdded` | UnitSnap（或 Unit） | UnitView | 创建单位视觉 |
| `UnitUpdated` | UnitSnap | UnitView | 更新位置/血量 |
| `UnitRemoved` | unitId | UnitView | 销毁视觉 |
| `CityAdded` | CitySnap | CityView | 创建城市 |
| `CityUpdated` | CitySnap | CityView | 更新归属/血量（易主换色） |
| `TurnChanged` | turn, currentPlayer, playerName | HUD | 刷新回合与当前玩家 |
| `PlayerStateChanged` | PlayerSnap | HUD/GameOverPanel | 出局判定、胜负提示 |

> 差分来源：`GameState.ApplySnapshot(...)` 返回的 `AppliedDelta`（已存在，改造为"全量覆盖 + 输出增删改"）。

### 4.2 演出类事件（由 Effects 产生，按顺序发布）

| 事件 | 载荷 | 订阅者 |
|---|---|---|
| `UnitMoveAnim` | unitId, from, path | UnitView |
| `UnitAttackAnim` | attackerId, defenderId, from, path, attackerDied, defenderDied, canEnter, attackerFinal | UnitView |
| `CityAttackAnim` | attackerId, cityId, from, path, captured, newOwner | UnitView + CityView |
| `CityFoundedAnim` | settlerId, cityId, cityPos | UnitView + CityView |
| `UnitBuiltAnim` | cityId, unitId, unitType, unitPos | UnitView |
| `TurnChangedAnim` | turnNumber, currentPlayer | HUD |
| `AnimationCompleted` | 无参 | Controller（动画队列推进、解锁输入） |

### 4.3 交互/UI 类事件（视图之间解耦）

| 事件 | 载荷 | 订阅者 |
|---|---|---|
| `UnitSelected` / `CitySelected` / `SelectionCleared` | id | SelUnitPanel / SelCityPanel |
| `TipsChanged` | string | HUD |
| `InputLockChanged` | bool | HUD（显示"等待服务端/演出中"） |

---

## 5. 统一应用管线（两端同一条）

```
【服务端/Host】订单执行成功
  1. Model 执行（GameState 已更新）
  2. 组 GameUpdateMsg：Snapshot（全量导出）+ Effects（本次演出指令）
  3. 本地 apply(GameUpdateMsg)        ← 与客户端同一个函数
  4. NetworkServer.SendToAll(msg)     ← 主机回包跳过：用 Seq 或"本地已应用"标记

【客户端】收到 GameUpdateMsg
  apply(msg):
    ① Seq 过滤：<= LastAppliedSeq → 丢弃
    ② GameState.ApplySnapshot(Snapshot) → AppliedDelta
       → 发布状态类事件（UnitAdded/Updated/Removed、CityAdded/Updated、TurnChanged）
    ③ Effects 入队 → 依次发布演出事件；等 AnimationCompleted 再下一条
    ④ 队列清空 → 若 CurrentPlayer == MyPlayerId → InputLockChanged(false)
```

要点：
- **主机也走这套**（只是不经过网络），从此两端只有一条刷新逻辑；
- **动画队列**保证多条演出不重叠（AI 回合尤其）；
- **输入锁**：发命令后锁、动画播完 + 快照应用完再解锁（替代现在"发完命令仍可点"的问题）；
- **可跳过动画**：直接丢弃 Effects 队列，仍保证状态一致。

---

## 6. 迁移步骤（每步可编译、可运行，不建议一次大改）

| 步骤 | 做什么 | 验收 |
|---|---|---|
| **步骤 1** | 修 `AnimationCompleted` 未定义；补齐 `HintMsg` 死亡/占领标志与 `From` 字段 | 项目能编译；单机动画正常 |
| **步骤 2** | 建立表现事件层：EventDefine 增加 §4 的事件类型 + 一个 `PresentationDispatcher`（把 Snapshot/Effects 翻译成事件） | 事件能发布（先加日志验证） |
| **步骤 3** | 让 View/UI **订阅**事件：UnitView（Unit* + 演出）、CityView（City*）、HUD（TurnChanged/Tips/InputLock）、Sel*Panel（Selection*） | 客户端能看到动画；此时可先保留主机直调 |
| **步骤 4** | 主机改走统一管线：`TryXxx` 不再直接调 View，只产出 Snapshot+Effects → `apply(msg)` → 再广播 | 主机动画仍正常，且与客户端同源 |
| **步骤 5** | 删除 Controller 里所有直接 View/UI 调用（除 Init 装配）；清理旧事件（MoveUnitEvent 等）与旧 Hint 结构 | 全项目只有"事件 → View/UI"一条路 |

---

## 7. 回归清单（改造后逐项验证）

- [ ] 单机（1 人 + AI）全程正常：移动/攻击/建城/造兵/回合
- [ ] 主机端动画、UI、回合提示与改造前一致
- [ ] 客户端能看到移动与攻击动画（非瞬移）
- [ ] 客户端选中状态在快照后正确（不残留/不指错）
- [ ] 死亡单位从两端 Model 与视觉中都被移除
- [ ] 城市易主后颜色立即更新（两端）
- [ ] 动画期间不可操作；播完自动解锁（轮到自己时）
- [ ] 连续快速操作不会导致状态漂移（两台客户端长时间互操作）