# 星火纪元（SparkAge）进度记录

> 本文件由"执行对话"维护：每完成一个任务更新一次（做了什么、结果、阻塞点）。
> 两个对话以本文件为最新事实。

## 协作约定
1. 执行对话开始任何任务前，必须先读取 docs/notes.md 和 docs/progress.md。
2. 问答对话的结论追加写入 docs/notes.md。
3. 执行对话每完成一个任务，更新 docs/progress.md（做了什么、结果、阻塞点）。
4. 两个对话看到对方更新的文件时，以文件内容为最新事实。

## 当前状态：W2.2 评审完成，表现层待重构

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
- 遗留待办已修复：地形颜色区分；GetMapBounds 使用 hexSize

### W2.2（评审完成，表现层待重构）
- ✅ GetReachableTiles：带代价扩散（松弛），返回 Dictionary<HexCoord, int>（剩余移动力）
- ✅ 单测：平原 19 格可达、山阻断
- ✅ 范围高亮显示/隐藏（初版）
- ⚠️ 评审发现：选择状态散落（preClickUnit/isRemoved/reachableHex）→ 切换单位旧范围不隐藏、连续点同单位重复创建对象
- 🔧 待重构：收敛为 SelectUnit/ClearSelection/HandleClick；范围对象预创建 64 个 + 开关，去掉补丁变量

### W2.3（待开始）
- A* 路径移动 + 移动力消耗 + 数据/表现关联（Unit ↔ GameObject）

### W3+（待开始）
- 城市、生产、科技；战斗与胜负；联机
