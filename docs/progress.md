# 星火纪元（SparkAge）进度记录

> 本文件由"执行对话"维护：每完成一个任务更新一次（做了什么、结果、阻塞点）。
> 两个对话以本文件为最新事实。

## 协作约定
1. 执行对话开始任何任务前，必须先读取 docs/notes.md 和 docs/progress.md。
2. 问答对话的结论追加写入 docs/notes.md。
3. 执行对话每完成一个任务，更新 docs/progress.md（做了什么、结果、阻塞点）。
4. 两个对话看到对方更新的文件时，以文件内容为最新事实。

## 当前状态：W2.3 评审完成，待修复 3 项

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
- GetReachableTiles：带代价扩散（松弛），返回可达集合
- 单测：平原 19 格可达、山阻断
- 表现层重构：HandleClick 单入口 + SelectUnit / ClearSelection / ShowRange；范围对象预创建 64 个 + 全量重置（幂等）
- 评审修复：去掉可达范围缓存；出生点改用 Walkable

### W2.3（评审完成，待修复）
- ✅ A* 重写：PriorityQueue + PathResult(Found/Path/Cost)，正确性 OK
- ✅ MoveUnit（Core）：校验可达 + 占位 + 寻路 + 扣移动力 + 更新位置
- ✅ 表现层：_selectedUnit、Unit→GameObject 映射、右键移动、移动后刷新
- ⚠️ 待修复：
  1. GameState 混入 UnityEngine（Debug.Log），违反 Core 零依赖 → 改返回 MoveResult 结果对象，表现层反馈
  2. MapView.MoveUnit 忽略返回值（失败也刷新/更新），且 tmp 未使用 → 按结果处理
  3. 无新单测 → 补 Pathfinding + MoveUnit（移动力扣减）测试
- 📝 备注：右键移动与相机右键拖拽冲突（后续处理）；单位逐格动画留打磨期

### W3（待开始）
- 城市、生产、科技；回合系统；战斗与胜负；联机
