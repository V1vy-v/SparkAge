# 星火纪元（SparkAge）策划案

> 本文档为**当前权威策划文档**（2026-09 重组）。整合并取代早期 gamedesign.md 中过时内容；技术细节见 docs/tech.md，决策记录见 docs/notes.md。

## 1. 定位与目标
- 简化版《文明6》求职 demo，卖点：**回合制架构 + 联机链路**。
- 目标：一局 10~20 分钟、每个系统都露脸、可演示可讲清。
- 对手：先做最简 AI（单机可演示），再接入联机（架构展示）。

## 2. 玩家
- 2 名玩家，owner = 1 / 2，回合轮流制。
- **玩家间无规则差异**（只有颜色区分）。将来若做差异化（如生产加成/特殊单位），规则差异进 `PlayerDef`（静态表），**PlayerState 只放运行状态**（IsAlive）。

## 3. 一局流程
- 开局：20×20 程序化地图（边缘水域）；双方出生点各 1 移民（未来 AI 自带首都）。
- 每回合（玩家轮流）：
  1. 操作阶段：单位移动/攻击、移民建城、选中城市造兵；
  2. 结束回合；
  3. 结算（EndTurn）：各城生产力 +5（**只累积，不自动造**）→ 单位恢复移动力 → 回合数 +1 → 胜负判定；
  4. 轮到对手。
- 一局推进：建城 → 扩张+造兵 → 攻城 → 终局。

## 4. 实体语义（定案）
### 单位
- 类型：战士（打仗）、移民（建城）。
- 单位**可自由进出/穿过己方城市格**；**不可进入敌方城市格**（敌城格 = 障碍）。

### 城市
- 结构：**中心单格 + 领地半径 2**（领地 = 距中心 ≤2 的格子，派生不存储）。
- 己方城格可进出；敌方城格不可进；**只有攻破占领的瞬间攻方才自动进入**。
- 城市本身是"占据一格"的实体：城市格上有己方单位 = 驻防。

## 5. 移动与阻挡语义（定案）
| 情形 | 能否穿过 | 能否停留 |
|---|---|---|
| 不可走地形（山/水） | 否 | 否 |
| 敌方城市格 | 否 | 否 |
| **敌方单位格** | **否** | 否 |
| **己方单位格** | **是（不挡，与原版一致）** | 否（不堆叠） |
| 己方城市格 | 是 | 是 |

- 唯一进入敌方单位格/敌方城格的方式 = **攻击胜利 / 占领**。

## 6. 战斗语义（定案）
### 单位 vs 单位
- 攻击 = 攻方先**走到目标相邻的可行格** → 结算；目标死亡才进目标格，未死停在旁。
- 攻方伤害 `max(1, 攻Atk − 守Def)`；**守方 Atk>0 才反击**（移民不反击），反击伤害 `max(1, 守Atk − 攻Def)`；死亡移除。
- 攻击消耗全部移动力（本回合不能再动）。

### 攻城 / 守军 / 占领
- 攻城 = 攻击城市：伤害 `max(1, 攻Atk − 城Def)`，城不反击；攻方停在城旁，**城破才进城**。
- 守军语义：站在敌方城格上的敌方单位会被优先攻击（右键先判单位）；**守军死亡不影响城血**，城血独立结算。
- 破城 = **易主（Owner=攻方）+ 重置满血 + 攻方进城**。
- 某玩家城市数归零 = 出局 → 对方获胜（征服胜利唯一）。

## 7. 生产力模型（定案）
- 每座城生产力**可存储累积**（+5/回合，`EndTurn` 结算）。
- **玩家决定何时花、造什么**：选中城市 → 花生产力造战士/移民（无自动队列）。

## 8. 科技（预留，可压缩）
- 单线 ≤6 节点，只解锁 1 个更强单位；未实现前不影响"打完一局"。

## 9. 胜利 / 失败
- 胜：对方所有城市被占领（城市数归零）。
- 败：自己城市全没。
- 砍掉：首都规则、多种胜利、外交/贸易/宗教。

## 10. 视觉语言（定案）
- **颜色 = 玩家归属**：owner1 红 / owner2 蓝（View 侧映射，Model 不存颜色）。
- **头顶标识 = 实体信息**（HP/类型/城市名，UI 批次用 TextMeshPro 挂到标识下）；标识颜色 = 玩家色。
- 单位本体中性色，类型用大小/形态区分；玩家色只上标识。

## 11. 数值表（现行草案，以 Model/GameRules 为准）
| 项 | 值 |
|---|---|
| 战士 | Atk4 / Def1 / HP10 / 移动2 / 造价5 |
| 移民 | Atk0 / Def0 / HP1 / 移动3 / 造价10 |
| 城市 | MaxHp30 / Def5 / 半径2 / 每回合生产力5 |
| 城市数量上限 | 无（已移除） |
| 初始单位 | 每玩家 1 移民 |

## 12. AI（最简，预留）
- 目标驱动：战士朝最近敌城/敌单位移动并攻击；移民找空地建城。
- AI = 一个自动产生命令的输入源（验证"命令 → Model"架构），细节在实现卡中定。

## 13. 范围外（明确不做）
- 地形产出/市民工作地块、多种胜利、外交/贸易/宗教、完整科技树分支。

---

## 任务卡：角色差异化 + 单位/城市血条（GAMEPLAY-1）

**目标**：在不增加区域、人口、科技树的前提下，用角色数值特性让单位与城市产生差异；用 UGUI 显示单位和城市血条。

### 一、角色特性

`CharacterCfg` 和 `CharacterInfo` 增加以下字段：

```text
单位特性：
int WarriorAtkBonus;
int WarriorDefBonus;
int WarriorHpBonus;
int SettlerMoveBonus;

城市特性：
int CityHpBonus;
int CityDefBonus;
int CityProductionBonus;
```

作用范围：

- 角色特性只影响该角色本局拥有的单位和城市；
- 单位创建时应用角色加成；
- 城市建立时应用角色加成；
- 客户端创建单位/城市副本时也应用同一套角色加成。

建议先做 5 个角色的示例数值：

| 类型 | 单位 | 城市 |
|---|---|---|
| 均衡 | 无加成 | 无加成 |
| 好战 | 战士 Atk +1 | 城市 Def +1 |
| 建设 | 移民 Move +1 | 城市 Production +1 |
| 守成 | 战士 Def +1 | 城市 MaxHp +5 |
| 扩张 | 移民 Move +1 | 城市 Production +1 |

具体数值先写在 ScriptableObject 中，不引入特性接口或策略类。

### 二、Model 与同步

不新增静态属性的网络字段。单位与城市的基础属性按以下规则在两端本地推导：

```text
Unit 静态属性：
UnitInfo(Type) + CharacterInfo(Owner)

City 静态属性：
CityInfo + CharacterInfo(Owner)
```

`UnitData` 继续只同步动态状态：

```text
Id / Owner / Type / Position / Hp / MovementLeft / IsDead
```

`CityData` 继续只同步动态状态：

```text
Id / Owner / Name / Position / Production / Hp
```

规则：

- 单位创建时，服务端和客户端都按 `Type + Owner` 应用同一份角色加成；
- 城市建立时，服务端和客户端都按 `Owner` 应用同一份角色加成；
- 城市易主时，按新拥有者的 `CharacterInfo` 重算静态城防属性；
- 后续若出现临时 buff、建筑改造等运行中变化，再引入单独的 modifier 列表，不修改当前基础字段。

`City` 增加：

```text
int ProductionPerTurn;
```

`GameState.EndTurn()` 不再统一写死：

```csharp
city.Production += GameRules.CityProductionPerTurn;
```

改为：

```csharp
city.Production += city.ProductionPerTurn;
```

制造单位和建立城市时，通过对应玩家的 `CharacterInfo` 应用属性加成。

### 三、单位血条

新增：

```text
Assets/Scripts/View/UI/HealthBarUI.cs
Assets/Resources/Prefabs/UI/HealthBar.prefab
```

`HealthBarUI` 使用 UGUI：

- `Image` 背景；
- `Image` 填充条；
- 可选 `TextMeshProUGUI` 显示 `当前值 / 最大值`；
- 提供 `SetValue(int current, int max)`。

新增管理脚本：

```text
Assets/Scripts/View/HealthBarManager.cs
```

职责：

- 使用 `UnitView.UnitObjs` 找到单位世界对象；
- 使用 `Camera.main.WorldToScreenPoint(...)` 转换为屏幕坐标；
- 为每个单位维护一个 `HealthBarUI`；
- 单位移动时血条跟随；
- 单位死亡或 View 被销毁时移除血条；
- 只在目标位于摄像机前方时显示。

不要在 `UnitView` 里直接创建 UI 元素。血条是表现层独立组件，由 `HealthBarManager` 统一管理。

### 四、城市血条

复用同一个 `HealthBarUI` 和 `HealthBarManager`：

- 通过 `CityView.CityObjs` 找到城市世界对象；
- 使用相同的世界坐标转屏幕坐标逻辑；
- 城市易主后刷新颜色和血条；
- 城市被攻占回满血后，血条立即显示满值。

### 五、验收

- [ ] 不同角色的战士攻击、防御、移动或生命值有差异。
- [ ] 不同角色建立的城市 MaxHp、Def 或 ProductionPerTurn 有差异。
- [ ] 主机和非主机看到的单位属性一致。
- [ ] 单位受伤后血条立即更新。
- [ ] 单位死亡后血条和单位 View 一起消失。
- [ ] 城市受伤后血条立即更新。
- [ ] 城市易主后血条颜色和数值正确。
- [ ] 血条不显示在摄像机背后。
- [ ] 连续移动、攻击、攻城时血条不会丢失或指错对象。

### 本卡不做

- 区域、地块产出、人口、科技树；
- 特性接口、策略模式、复杂建筑系统；
- 血条美术打磨和复杂动画。

---

## 任务卡：单位/城市血条（GAMEPLAY-2）

**目标**：用 UGUI 显示每个单位和城市的当前血量、最大血量和填充比例。

**实现方式**：屏幕空间 Overlay，不用 3D 血条 Mesh，不把 `MaxHp` 放进网络消息。`MaxHp` 由本机根据配置和角色属性计算；`Hp` 只在攻击动画完成事件触发后刷新，不能在每帧直接覆盖，否则动画还没播完血量就先变了。

### 一、UIManager 暴露 Canvas\n\n> UI 面板和 HUD 统一通过 `UIManager.Instance` 获取和操控；事件中心只用于游戏表现层（UnitView / CityView / HealthBarMgr 等）的多播，不为面板调度强行套事件。

`Assets/Scripts/View/UI/UIManager.cs`：

```csharp
public RectTransform CanvasRect => canvas as RectTransform;
```

### 二、HealthBarUI

新增：

```text
Assets/Scripts/View/UI/HealthBarUI.cs
Assets/Resources/Prefabs/UI/HealthBar.prefab
```

`HealthBarUI` 组件字段：

```csharp
[SerializeField] Image fill;
[SerializeField] TextMeshProUGUI txtValue;
```

提供方法：

```csharp
public void SetValue(int current, int max)
{
    fill.fillAmount = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
    txtValue.SetText("{0}/{1}", current, max);
}

public void SetColor(Color color)
{
    fill.color = color;
}
```

Prefab 结构：

```text
HealthBar
├─ Background Image
├─ Fill Image        Type = Filled, Horizontal
└─ TextMeshProUGUI   显示 当前值/最大值
```

### 三、HealthBarManager

新增：

```text
Assets/Scripts/View/HealthBarManager.cs
```

字段：

```csharp
GameState state;
UnitView unitView;
CityView cityView;
Camera worldCamera;
RectTransform canvasRoot;
HealthBarUI healthBarPrefab;

Dictionary<Unit, HealthBarUI> unitBars = new();
Dictionary<City, HealthBarUI> cityBars = new();
```

需要的公开/内部方法：

```text
Init(...)
LateUpdate()
UpdateUnitBars()
UpdateCityBars()
GetOrCreateUnitBar(Unit)
GetOrCreateCityBar(City)
UpdateBarPosition(bar, worldPosition)
RefreshUnitHealth(Unit)
RefreshCityHealth(City)
RemoveMissingBars()
OnAttackUnit(AttackUnitEvent)
OnAttackCity(AttackCityEvent)
```

职责：

- `LateUpdate` 只负责创建缺失血条、更新位置、处理摄像机背后隐藏和清理；
- 不要在 `LateUpdate` 里直接读取 `unit.Hp / city.Hp` 覆盖显示，否则攻击动画还没播完血条就会先变化；
- 血条初始创建时用当前 `Hp / MaxHp` 初始化；
- 订阅 `AttackUnitEvent`，动画完成后调用 `RefreshUnitHealth(unit)`；
- 订阅 `AttackCityEvent`，动画完成后调用 `RefreshCityHealth(city)`；
- 单位移动只更新血条位置，不改血量；
- 城市易主由 `AttackCityEvent` 触发后刷新血条颜色和血量；
- View 对象消失时销毁对应血条；
- 屏幕坐标 `z <= 0` 时隐藏血条；
- 使用 `ViewTools.GetPlayerColor(owner)` 设置血条颜色。

建议偏移：

```text
单位：世界坐标 + Vector3.up * 1.2f
城市：世界坐标 + Vector3.up * 1.5f
```

### 四、初始化

`GameController.BuildModelAndView()` 在 `mapView`、`unitView`、`selectionView`、`cityView` 初始化完成后：

```csharp
HealthBarManager healthBarManager =
    gameObject.AddComponent<HealthBarManager>();

healthBarManager.Init(
    state,
    unitView,
    cityView,
    Camera.main,
    UIManager.Instance.Canvas);
```

不要在 `UnitView` 和 `CityView` 里直接创建 UI 对象。血条是独立的 View/UI 管理组件。

### 五、验收

- [ ] 每个单位头顶显示血条和 `当前值/最大值`。
- [ ] 每座城市上方显示血条和 `当前值/最大值`。
- [ ] 单位受伤后数值和填充比例立即更新。
- [ ] 城市受伤后数值和填充比例立即更新。
- [ ] 城市易主后血条颜色更新，数值和血量保持一致。
- [ ] 单位死亡后血条与单位 View 同步消失。
- [ ] 单位移动、攻击、攻城动画期间血条跟随。
- [ ] 摄像机背后的单位或城市不显示血条。
- [ ] 主机和非主机显示同一套血量结果。

### 本卡不做

- 血条动画、掉血飘字、buff 图标、护盾条；
- 世界空间 Canvas；
- 复杂对象池和血条性能优化；
- 把 `MaxHp` 放入 `UnitData / CityData`。

---

## 任务卡：Tile / City 美术资源接入

**目标**：地图生成后使用现有 Tile Prefab 渲染地形；城市建立和易主时使用对应玩家颜色的 City Prefab。

### 一、地图 Tile 接入

资源目录：

```text
Assets/Resources/Prefabs/Tiles/
├─ Grass
├─ Forest
├─ Mountain
└─ Water
```

`MapView` 不再为每个地块创建 MeshFilter/MeshRenderer，改为按 `TileData.Type` 实例化对应 Prefab。

推荐映射：

```text
Plain    → Grass/*
Forest   → Forest/*
Mountain → Mountain/*
Water    → Water/*
```

在 `MapView.Init()` 中加载：

```csharp
Resources.LoadAll<GameObject>("Prefabs/Tiles/Grass")
Resources.LoadAll<GameObject>("Prefabs/Tiles/Forest")
Resources.LoadAll<GameObject>("Prefabs/Tiles/Mountain")
Resources.LoadAll<GameObject>("Prefabs/Tiles/Water")
```

`BuildTiles()`：

- 按 `Map.Tiles` 遍历；
- 根据 `tile.Type` 选择 Prefab 集合；
- 使用 `tile.Coord` 和地图 Seed 做确定性变体选择；
- 实例化到 `MapRoot`；
- 位置仍使用：

```csharp
HexLayout.HexToPixel(tile.Coord, hexSize, 0)
```

变体不能直接用 `UnityEngine.Random`，否则不同客户端可能显示不同地块：

```csharp
int hash = tile.Coord.Q * 73856093
         ^ tile.Coord.R * 19349663
         ^ mapSeed * 83492791;

int idx = Mathf.Abs(hash) % prefabs.Length;
```

`SelectionView` 的高亮仍使用 `HexMeshFactory.CreateHexMesh()`，不要把高亮依附到 Tile Prefab 的 Mesh 上。

### 二、城市 Prefab 接入

资源目录：

```text
Assets/Resources/Prefabs/Cities/
├─ Red
├─ Blue
├─ Green
└─ Yellow
```

按 `city.Owner` 选择颜色：

```text
1 → Red
2 → Blue
3 → Green
4 → Yellow
```

每个颜色目录下取一个城市 Prefab。

`CityView.BuildCity()`：

- 根据 `city.Owner` 取颜色目录；
- 使用 `city.ID` 或坐标做确定性变体选择；
- 实例化 Prefab；
- 设置到对应 Hex 位置；
- `cityObjs[city.ID] = obj`。

### 三、城市易主

城市易主时有两个选择：

1. 继续使用颜色专属 Prefab：销毁旧对象，按新 Owner 重新实例化；
2. 使用通用城市模型：只改 Renderer/Material 颜色。

当前资源已经是颜色目录，第一版建议使用第 1 种：

```text
AttackCityCompletedEvent
→ city.Owner 已变化
→ RebuildCityObject(city)
```

如果后续决定改成通用模型，再走“直接改实体颜色”的路线。

### 四、尺寸与定位

- Tile Prefab 必须与 `hexSize` 的比例一致；
- 如果 Prefab 模型尺寸不是 1，统一在 Prefab 或 `MapView` 中设置缩放；
- 城市 Prefab 的位置 y 偏移独立于 Tile，不要直接复用地块高度逻辑；
- 城市根节点的原点要统一，否则选中、血条和射线拾取会错位。

### 五、验收

- [ ] 25×25 地图能使用 Tile Prefab 正常显示。
- [ ] Host 和 Client 的地形变体一致。
- [ ] 地形修改后只需重新选择 Prefab，不改 Model。
- [ ] 不同玩家建立的城市使用对应颜色 Prefab。
- [ ] 城市易主后视觉颜色更新。
- [ ] 城市位置、选中和血条位置一致。
- [ ] 切换 Prefab 后不影响 Pathfinding 和 TileData。

### 不做

- 河流、道路、海岸自动连接；
- 高低差地形；
- 地块旋转系统；
- 地块美术 LOD 和批处理优化。