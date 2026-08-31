# 星火纪元（SparkAge）进度记录

> 本文件由"执行对话"维护：每完成一个任务更新一次（做了什么、结果、阻塞点）。
> 两个对话以本文件为最新事实。

## 协作约定
1. 执行对话开始任何任务前，必须先读取 docs/notes.md 和 docs/progress.md。
2. 问答对话的结论追加写入 docs/notes.md。
3. 执行对话每完成一个任务，更新 docs/progress.md（做了什么、结果、阻塞点）。
4. 两个对话看到对方更新的文件时，以文件内容为最新事实。

## 当前状态：W2.3 进行中（任务卡已发）

## 里程碑
### W1（完成）
- Hex 坐标数学（轴向 + 像素互转 + 邻居 + 距离）+ 3 个单测全绿
- 12×12 六边形地图 + 地形生成（值噪声 + seed + 边缘水）
- 摄像机缩放/拖拽/边界 + 抖动修复（抓取点模式）
- 点击地块高亮

### W2.1（完成）
- Unit 数据模型（Position / Owner / MaxMovement / MovementLeft）
- GameState 封装（Map + Units + GetUnitAt + FindSpawnPoint BFS）
- 单位渲染 + 选中框（独立对象 + SetActive）

### W2.2（完成）
- GetReachableTiles：带代价扩散（松弛），返回 Dictionary<HexCoord, int>（剩余移动力）
- 单测：平原 19 格可达、山阻断
- 表现层重构：HandleClick 单入口 + SelectUnit / ClearSelection / ShowRange；范围对象预创建 64 个 + 全量重置（幂等）
- 评审修复：去掉 unitReachableHex 缓存（每次现算）；出生点改用 Walkable（避免落在山上）

### W2.3（进行中）
- 任务：A* 路径移动 + 移动力消耗 + 数据/表现关联（Unit ↔ GameObject）
- 关键点：GameState.MoveUnit 校验与扣减；MapView 加 _selectedUnit 与 Unit→GameObject 映射；移动后刷新范围
- 待办：移动逻辑 + 单测（可选）

### W3+（待开始）
- 城市、生产、科技；回合系统；战斗与胜负；联机
