# 欧姆龙流程步监控上位机 — 产品设计文档

> 版本：v1.5.27（2026-08-31）
> 用途：功能规格、交互规则、技术架构、数据格式、发布流程的权威说明。
> 与 `PROJECT_SUMMARY.md`（工作日志/坑记录）互补：本文档管"现在是什么样"，备忘管"怎么走到这里的"。

## 1. 项目概述

C# WinForms 上位机（.NET Framework 4.7.2），通过 FINS-TCP 连接欧姆龙 PLC，
实时监控最多 50 个工位的流程步号，提供实时/历史趋势图、多工位总览图、
周期判定、事件记录、自动更新等能力。

- 数据点：步值 D10002 起 N 个字（每寄存器 = 一工位，N 可配，默认 50）
- 心跳：D10000.1；状态字：D10（1 Run / 2 Maintain / 3 Alert / 4 Idle）
- 单文件交付：Test.exe（HslCommunication.dll 嵌入），解压即用，免安装
- 仓库：`A1917/plc-step-monitor`（私有，源码）+ `A1917/plc-step-monitor-releases`（公开，更新检查用）

## 2. 功能清单

### 2.1 主界面（Frm_Main）
| 控件 | 功能 |
|---|---|
| 连接/断开 | FINS-TCP 连接 PLC（IP 可配，端口 9600） |
| 心跳灯/状态标签 | 1s 轮询心跳与状态字，掉线红灯 |
| 工位数 | 连接时应用（默认 50，上限 200，重建 5×10 网格） |
| 模拟运行 | 无 PLC 时模拟步号变化，便于调试趋势图 |
| 记录开关 + 记录名 | 事件落盘（CSV），文件名 = `{前缀}_日期_小时.csv` |
| 加载历史 | 多选 CSV 文件合并加载到全局缓存 |
| 总览趋势图 | 打开多工位总览图 |
| 实时/历史切换 | 主界面级数据源切换 |
| 检查更新 | 对比公开仓库最新 Release，下载更新自动重启 |
| 工位网格 | 5×10 步号显示，双击打开该工位实时趋势图 |

### 2.2 单工位趋势图（Frm_Records）
| 功能 | 说明 |
|---|---|
| 实时滚动 | 跟随模式：窗口右端贴当前时间，保持当前窗口宽度 |
| 缩放 | 滚轮锚点缩放（鼠标中心），X 下限 0.5ms |
| 拖拽平移 | 绝对位移模型，仅绘图区，X/Y 双轴 |
| 边界保护 | ClampToNow（X 右端贴数据末尾/当前时间）+ ClampYAxis（±20%） |
| 游标 | 勾选显示，竖线 + 步号标签（含该步耗时）+ 信息栏 |
| 区域 | 双竖线 + 半透明高亮 + 时长标签（ms）+ 锁定宽度联动 |
| Alt 吸附 | 按住 Alt 拖区域线吸附到步边界（阈值 2% 窗口） |
| 分页 | ◀▶ 翻页 + 每页时长（默认 60s） |
| 刷新率 | 60/30/10fps |
| 周期判定 | 开关：每周期一色 + 点击周期看各步耗时（见 §3.4） |
| 加载历史 | 多选 CSV 文件合并 |
| 0 基准刻度 | Y 轴 nice 步进（1/2/5×10ⁿ）+ IntervalOffset 使 0 恒为刻度线 |
| 步号标签自绘 | 游标/区域线旁标签，分层防重叠，Y 限绘图区 |

### 2.3 多工位总览图（Frm_MultiTrend）
| 功能 | 说明 |
|---|---|
| 工位勾选 | 左侧 CheckedListBox，默认不勾选；勾选即重建曲线（保持视图） |
| 实时/历史 | 实时从 EventStore 增量拉取；历史来自全局缓存或文件 |
| 图例交互 | 单击图例高亮（仅加粗不淡化）；双击改色（持久化） |
| 游标/区域 | 同单工位（勾选归位、Alt 吸附、耗时显示） |
| 分页/刷新率/适应 | 同单工位 |
| 配置持久化 | 勾选工位/颜色/窗口位置存 `records/multi_config.cfg`，下次自动恢复 |
| 黄金角配色 | 相邻工位色相 137.5° 间隔，颜色区分度大 |

### 2.4 事件记录（RecordStore）
- 存储：`records/{前缀}/{前缀}_日期_小时.csv`（按小时自动切分）
- 格式：`时间,工位,步号`（UTF-8 BOM），每行一条步号变化事件
- 触发：步号变化时生成（含首值事件：启动后第一步也记录）
- 加载：主界面/趋势图多选文件合并，按时间排序

### 2.5 自动更新（UpdateChecker）
- 检查：公开仓库 `A1917/plc-step-monitor-releases` 最新 Release API（无需认证）
- 对比：`v{AssemblyVersion}` vs Release tag
- 流程：确认 → 进度窗口（Marquee + 已下载大小）→ 后台下载 → 解压 → bat 脚本替换 exe → 重启
- 版本相同不弹窗

## 3. 交互规则定稿

### 3.1 拖拽（绝对位移模型）
- **基准在 MouseDown 固定，MouseMove 期间绝不更新**（基准同步会导致增量累积、视图自走——v1.3.5 曾回滚）
- X 轴内容跟随（右拖 → 曲线右移看更早），Y 轴视野跟随
- 拖拽仅限绘图区；`_panPlotHeight` 用绘图区实际像素高度（Y 灵敏度）

### 3.2 缩放
- 滚轮锚点缩放：鼠标中心不动，X/Y 同步缩放
- X 窗口上限 = 每页时长，下限 0.5ms；clamp 后按实际比例重算 Position 保持鼠标中心

### 3.3 跟随模式
- **仅实时数据活跃（PLC 连接中或模拟运行中）** 且拖到最右端（当前时间）时进入跟随
- 跟随 = 右端贴当前时间，**保持当前窗口宽度**（放大也跟随，不重置宽度防视野跳变）
- 历史/断开数据拖拽不触发跟随，松手保持当前位置

### 3.4 周期判定
- **周期边界 = 最小步号（流程起点）再次出现**（数据中途接入也自然）
- 着色：每周期一色（6 色循环：红/蓝/绿/橙/紫/深青）；选中周期亮金高亮
- 点击周期曲线 → 右侧面板：周期序号 + 总时长 + 各步耗时列表
- 排序：按钮「从大到小/按步数顺序」；列头点击（步号列升序/耗时列降序）
- 关闭开关 → 恢复原色，面板隐藏

### 3.5 游标/区域
- 默认不勾选；勾选后在当前视野中央显示
- 标签偏移 136px，靠近顶部（y>140）翻转到下方；Y 限定绘图区内
- 锁定区域 = 长度固定整体平移（拖动一根线另一根跟随）
- Alt + 拖动区域线 → 吸附到最近步边界（阈值 = 窗口宽度 2%）

### 3.6 Y 轴刻度（v1.3.4 定稿）
- 自动 nice 步进（1/2/5×10ⁿ）+ 动态 IntervalOffset 使 0 恒为刻度线
- 自由上下拖拽：数据范围 ±10% 留白，clamp ±20%
- 历经三次反复：FixedCount=6（嫌密）→ 0 中轴对称（锁死拖拽）→ 当前方案

## 4. 技术架构

### 4.1 文件结构
| 文件 | 职责 |
|---|---|
| Frm_Main.cs | 主界面：连接/模拟/记录/加载/总览/更新 |
| Frm_StepInfo.cs | 工位网格（5×10 步号，100ms 批量轮询） |
| Frm_Records.cs | 单工位趋势图（~1230 行，核心交互） |
| Frm_MultiTrend.cs | 多工位总览图（~700 行） |
| Frm_UpdateProgress.cs | 更新进度窗口 |
| EventStore.cs | 静态环形缓冲（50000 条），实时数据源 |
| RecordStore.cs | CSV 按小时落盘/加载 |
| CycleDetector.cs | 周期判定分析（纯静态无副作用） |
| MultiConfig.cs | 总览配置持久化（行格式） |
| UpdateChecker.cs | 公开仓库 API 检查 + 后台下载 |
| PlcData.cs | 静态共享：FINS 客户端/ThdStep/StepCount |
| Simulator.cs | 模拟数据源 |
| Program.cs | 入口 + 嵌入 DLL 加载（HslCommunication + native 释放） |

### 4.2 数据流
```
PLC/模拟器 → PlcData.ThdStep（100ms 批量读）
           → Frm_StepInfo 网格显示
           → EventStore.Feed(工位, 步号)（首值也记录）
                ├→ Frm_Records 实时（增量 GetSince + 虚拟点延伸）
                ├→ Frm_MultiTrend 实时（增量拉取）
                └→ RecordStore.Write（开关 ON 时落盘 CSV）
加载历史 → RecordStore.Load → EventStore.LoadedHistory → 趋势图/总览图
```

### 4.3 关键实现约束
- C# 7.3：无 `new()`/`using var`/switch 表达式
- Chart API：无 ScaleView.Scrollable；SuspendLayout/ResumeLayout 必须 try-finally
- TextAnnotation 无 AutoSize；PlotAreaPosition 不存在（用 Position+InnerPlotPosition 百分比）
- `SetValueXY` 更新虚拟点（不删不加，防重算）；跟随滚动段在 ResumeLayout 外
- OnChartPaint 自绘标签（GDI+，_labelFont 缓存 + FormClosing Dispose）

## 5. 发布流程

1. 编译（MSBuild Rebuild 0 错误 0 警告）
2. **运行单元测试**：`dotnet run --project tests`（Windows dotnet，从 WSL 调 `/mnt/c/Program Files/dotnet/dotnet.exe`），退出码 0 = 通过
3. 单文件打包：`Test.exe` → zip（`PLCStepMonitor/Test.exe`）
4. 推源码：`main` → 私有仓库 `v2-optimized` 分支 + 公开仓库 `main`
5. 打 tag（vX.Y.Z）+ 双边 GitHub Release + 上传 zip
6. **AssemblyInfo 版本号随发版递增**（自动更新对比 tag，不升会永远提示更新）

### 5.1 测试（tests/）

- **工程**：`tests/tests.csproj`（.NET 9 console，自写断言零依赖），链接主项目逻辑类源码
- **覆盖**：CycleDetector（周期检测/最小步号起点/空数据）、EventStore（首值/增量/环形上限）、RecordStore（CSV 解析容错/多文件合并排序）
- **规则**：每次改动后必须为该改动编写/更新测试并验证通过后才推送；纯文档改动除外
- **注意**：EventStore 依赖 PlcData.StepCount，测试工程用 `tests/PlcDataStub.cs` 桩替代（无 HslCommunication 依赖）

## 6. 未实现/规划中
- 断点续传（更新下载中断恢复）
- 表格 TopN（工位耗时排行，基于加载数据重算）
- 多设备支持（SQLite 分支预留 device 维度，feature/sqlite-records 保留）
- 周期判定指纹法升级（起始步序不稳定时前缀聚类）
- StepHistoryItem 半成品（步描述/时长编辑）
