# 星火纪元（SparkAge）

基于 Unity 3D URP 与 Mirror 开发的简化版《文明6》风格 4X 回合制策略 Demo，支持真人玩家与 AI 混合联机。

## 技术栈

- Unity 2022.3 LTS、3D URP
- C#、UGUI、TextMeshPro
- Mirror
- ScriptableObject

## 已实现

- 程序化六边形地图与确定性 Seed
- A* 寻路、单位移动与战斗
- 城市建立、生产、占领与淘汰
- 回合流转、征服胜利、观战和结算
- 4 人房间、角色选择、就绪与 AI 填充
- Host 权威联机：Order 上行、服务端执行、状态下行
- 服务端 AI，与人类共用命令和广播链路
- 单位/城市血条、城市名字、领土边界和基础动画

## 架构

```text
Assets/Scripts
├── Framework/   事件中心、Hex 工具
├── Model/       纯 C# 游戏状态与规则
├── Controller/  回合流程、命令执行、网络、AI 驱动
├── View/        地图、单位、城市、UI 与表现
└── Config/      ScriptableObject 配置
```

Model 负责唯一游戏事实状态；客户端提交操作意图；服务端负责校验、AI 行动和状态广播；View 通过事件和 ID 映射更新表现。

## 当前状态

已完成可运行的单机与联机 Demo 核心流程。

## 待开发内容

地块改造、建筑、科技树、断线重连、完整存档等。