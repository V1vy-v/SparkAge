# 星火纪元（SparkAge）进度记录

> 本文件由"执行对话"维护：每完成一个任务更新一次（做了什么、结果、阻塞点）。
> 两个对话以本文件为最新事实。

## 协作约定
1. 执行对话开始任何任务前，必须先读取 docs/notes.md 和 docs/progress.md。
2. 问答对话的结论追加写入 docs/notes.md。
3. 执行对话每完成一个任务，更新 docs/progress.md（做了什么、结果、阻塞点）。
4. 两个对话看到对方更新的文件时，以文件内容为最新事实。

## 当前状态：W3.0b-1 完成 → W3.0b-2 进行中（3D 化主体）

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
- ✅ A* 重写（PriorityQueue + PathResult）、MoveUnit（Core）校验/扣减/更新
- ✅ 表现层：_selectedUnit、Unit→GameObject 映射、移动后刷新
- ✅ 评审修复：Core 去掉 UnityEngine（返回结果对象）、MoveUnit 按结果处理
- ⏳ 延后待办（W6 集中补）：
  1. Pathfinding / MoveUnit 单测
  2. 恢复 Plains_Movement2_ReachesAllWithinDistance2 的断言（当前"假绿"）
- 📝 备注：右键移动与相机拖拽冲突（后续处理）；单位逐格动画留打磨期

### W3.0（完成）
### W3.0b-1（完成）
- 切换到 3D URP 管线（URP3D + Universal Renderer）、Directional Light、2D 灯光清理`n- 任务：把 MapView 拆成 MapView（协调者+地图渲染）/ UnitView / SelectionController，纯搬代码、行为零变化`n`n### W3.1（待开始）
- 任务：回合系统（回合数、当前玩家、EndTurn 重置单位移动力、回合显示）
- 为 W3.2 城市/生产打地基

### W3.2+（待开始）
- 城市（移民建城、生产队列、每回合产出）；科技树；战斗与胜负；联机




