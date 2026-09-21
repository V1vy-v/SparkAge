# 星火纪元（SparkAge）进度记录

> 本文件由"执行对话"维护：每完成一个任务更新一次（做了什么、结果、阻塞点）。
> 两个对话以本文件为最新事实。

## 协作约定
1. 执行对话开始任何任务前，必须先读取 docs/notes.md 和 docs/progress.md。
2. 问答对话的结论追加写入 docs/notes.md。
3. 执行对话每完成一个任务，更新 docs/progress.md（做了什么、结果、阻塞点）。
4. 两个对话看到对方更新的文件时，以文件内容为最新事实。

## 当前状态：联机基础 + AI 重写完成 → 下一步 NET-7 状态同步/表现层收尾

## 里程碑
### W1（完成）
- Hex 坐标数学（轴向 + 像素互转 + 邻居 + 距离）+ 3 个单测
- 12×12 六边形地图 + 地形生成（值噪声 + seed + 边缘水）
- 摄像机缩放/拖拽/边界 + 抖动修复（抓取点模式）
- 点击地块高亮

### W2.1 / W2.2（完成）
- Unit 数据模型、GameState 封装、BFS 出生点、单位渲染 + 选中框
- GetReachableTiles 带代价扩散；表现层重构（HandleClick / SelectUnit / ClearSelection / ShowRange）；范围对象预创建 64 个
- 评审修复：去掉可达缓存；出生点用 Walkable

### W2.3（完成，单测延后）
- ✅ A* 重写（PriorityQueue + PathResult）、MoveUnit（Model）校验/扣减/更新
- ✅ 表现层：_selectedUnit、Unit→GameObject 映射、移动后刷新
- ✅ 评审修复：Model 去掉 UnityEngine（返回结果对象）、MoveUnit 按结果处理
- ⏳ 延后待办（W6 集中补）：
  1. Pathfinding / MoveUnit 单测
  2. 恢复 Plains_Movement2_ReachesAllWithinDistance2 的断言（当前"假绿"）
- 📝 备注：右键移动与相机拖拽冲突（后续处理）；单位逐格动画留打磨期

### W3.0（完成）
### W3.0b-1（完成）
### W3.0b-2（完成）
- 3D 化主体：HexMeshFactory、地形/单位/高亮 3D、透视相机、射线拾取、Sprites/Default 修复透明
- 延后到 W6：单位选中框环网格（ring mesh）
- 切换到 3D URP 管线（URP3D + Universal Renderer）、Directional Light、2D 灯光清理
- 任务：把 MapView 拆成 MapView（协调者+地图渲染）/ UnitView / SelectionController，纯搬代码、行为零变化

### W3.1（完成）
- EndTurn（Model）+ 空格触发 + 刷新选中
- UI 统一延后：用户熟悉 UGUI，回合数/面板等 UI 批量后做
- 任务：回合系统（回合数、当前玩家、EndTurn 重置单位移动力、回合显示）
- 为 W3.2 城市/生产打地基

### W3.2+（待开始）
- 城市（移民建城、生产队列、每回合产出）；科技树；战斗与胜负；联机




















## 2026-09-21 NET-8 AI 重写（代码完成）
- `Model/Ai/AiDecider.cs`：纯 C# 决策，负责城市造兵、移民建城、攻击、移动、结束回合。
- `Controller/Ai/AiDriver.cs`：服务端驱动循环，按 AI 玩家推进，带步数上限和节流；不依赖 View。
- `NetworkMgr.ExecuteAndBroadcast`：远程人类命令与 AI 命令共用执行、Tip 回执和状态广播入口。
- `GameController.TryStartAi/RunAiSequence`：`EndPhase` 后检测 AI，启动驱动并在结束后恢复 Host phase。
- Review 修复：AI 无攻击目标时不再提前占用 `usedUnits`；Host 只在本地玩家回合进入 `Animating`，避免远程玩家操作后 Host phase 卡死。
- 下一步：NET-7 状态同步/表现层收尾。
## 联机开发计划（NET-1 ~ NET-8）

详见 docs/networking.md。当前状态：UI 面板（Begin/Login/Connect/Room）已拼好；NetworkSession 与 RoomState 为空壳；GameController 仍从 GameCfg 读取开局配置。下一步从 NET-1（登录昵称）开始。
