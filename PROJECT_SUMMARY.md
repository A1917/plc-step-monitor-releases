# 欧姆龙流程步监控程序 — 工作总结备忘

> 用途：跨会话延续上下文。新会话里让 Hermes 读本文件即可接着推进。
> 更新时间：2026-08-29

## ⚠️ 2026-08-22 大事记：源码加密损坏 → 反编译恢复 → 优化重建
- 工作电脑发来的两个 zip（PLCStepMonitor.zip、stepRecord.zip）里 **6 个核心 .cs 文件被加密软件污染**：
  `Frm_Main.cs / Frm_Main.Designer.cs / Frm_StepInfo.cs / Frm_StepInfo.Designer.cs / PlcData.cs / Program.cs`
  文件头为 `%TSD-Header-###%`（透明加密容器格式），已非文本 → 这就是此前 CS2015「二进制文件而非文本」的根源
- **恢复途径**：zip 内 `bin/Debug/Test.exe` 完好（加密只针对 .cs），用 ilspycmd 反编译完整还原源码
  （工具：Windows dotnet.exe 装 `C:\pcm-tools\ilspycmd.exe` 8.2.0.7535，DOTNET_ROLL_FORWARD=LatestMajor 兼容 .NET9 运行）
- **新基线目录**：`/root/plc-step-monitor-v2/`（干净 git 仓库），与旧仓库 `/root/plc-step-monitor/`（四层版）并存
- 优化原则：**只修 bug 与代码质量，不新增功能**（用户 8-15 曾因「改太多」回滚，勿重蹈覆辙）

## 优化清单（2026-08-22，全部已编译验证 0 错误 0 警告）
| # | 优化点 | 原版问题 → 现版本 |
|---|---|---|
| 1 | 批量读步值 | 逐字 ReadInt16×50 次/周期 → `ReadInt16("D10002", 50)` 一次批量读，请求数 50→1 |
| 2 | changeList 泄漏 | 成员 List 只 Add 不清 → 局部变量收集变化项，随方法结束释放 |
| 3 | 数组长度统一 | 127/50/100 三处不一致（50~99 永远显示 0）→ 统一 `PlcData.StepCount = 50`，网格 10×10 → **5×10** |
| 4 | RowStyles 冗余 | 双重循环各加 100 个 → 行 5 个 + 列 10 个 |
| 5 | 心跳崩溃 | CheckHeartBeat 无 try-catch，PLC 掉线直读 .Content 崩 → try-catch + 判空 + 掉线红灯 |
| 6 | 状态字读取 | ShowStateEven_Run 首行读 D10 在 try 外、死赋值 num=100 → 移入 try，删除死代码 |
| 7 | UI 跨线程安全 | BeginInvoke 无 IsDisposed 保护（关窗体后线程仍刷 UI 会抛异常）→ SafeSetStatus/UpdateLabels 全面保护 |
| 8 | 线程退出 | 状态线程 while(true) 无法停止、FormClosing 直接 Dispose → CTS 协作取消 + Join(2000) + 安全释放 |
| 9 | 连接逻辑 | 断开时也先 ConnectServer() 再 Close（多一次无用连接）→ 连接/断开拆分支，互不干扰 |
| 10 | 死代码 | Frm_Main.GetStep 从未启动、_isReading 未用、_cts.Cancel() 无意义调用 → 全部删除 |
| 11 | namespace 语法 | 反编译输出 `namespace Test;`（C#10，.NET4.7.2 不支持）→ 传统块 |
| 12 | 引用路径 | csproj HintPath 指向 `D:\HMI\...` 绝对路径 → `lib\HslCommunication.dll` 相对引用 |
| 13 | 文本语义 | 标签「线程N」→「工位N」（一个寄存器=一个工位） |
| 14 | 编码 | 所有 .cs/resx 统一 UTF-8 BOM（VS 中文环境防乱码） |

## 一、项目概况
- 项目名：Test（C# WinForms，.NET Framework 4.7.2），源码在 `/root/plc-step-monitor-v2/Test/`
- 功能：连接欧姆龙 PLC（FINS-TCP，192.168.1.50:9600），MDI 主窗体 + 工位监控子窗体
- 数据点：步值 D10002 起 50 个字（每寄存器=一工位）、心跳 D10000.1、状态字 D10（1 Run/2 Maintain/3 Alert/4 Idle）
- 界面：主窗体顶栏（连接按钮/心跳灯/状态标签/线程个数输入框[未接线]）+ 子窗体 5×10 工位网格
- 编译：VS2022 MSBuild `-t:Rebuild` 0 错误 0 警告（见 wsl-msbuild-csharp 技能）

## 二、文件结构
| 文件 | 职责 |
|---|---|
| `Frm_Main.cs` / `.Designer.cs` | 主窗体：连接/断开、心跳、状态轮询（1s）、MDI 容器 |
| `Frm_StepInfo.cs` / `.Designer.cs` | 子窗体：5×10 工位网格，100ms 批量轮询 + 差异刷新 |
| `PlcData.cs` | 静态共享：omronFinsNet 客户端、ThdStep 步值、IsConnected、StepCount=50 |
| `Program.cs` | 入口 |
| `StepHistoryItem.cs` | 半成品用户控件（未接入主界面，保留不动） |
| `lib/HslCommunication.dll` | 通信库（相对引用） |

## 三、已知坑（勿再踩）
- **工作电脑源码会被加密软件污染**（%TSD-Header-###%）：从工作电脑拷 .cs 必须检查 `file` 是否为文本；已损坏的用 zip 内 exe 反编译恢复
- 轮询采样局限：PLC 在两次轮询（100ms）间跳步会漏显示；需精确需 PLC 侧记录
- CancellationTokenSource：先 Cancel() 等线程退出再 Dispose()
- 此版 HslCommunication.dll 的 Modbus TCP 类型名是 `ModbusTcpNet`（新版文档叫 ModbusTcpClient），本项目暂只用到 OmronFinsNet

## 四、待办 / 待确认
1. ~~工位数确认~~：✅ 2026-08-22 主界面「工位数:」输入框接线，连接时应用（默认 50，上限 200，动态重建网格），不再硬编码
2. ~~textBox1 未接线~~：✅ 已接线为工位数配置（原「线程个数:」标签改「工位数:」）
3. `StepHistoryItem` 半成品未接入（步描述/时长编辑功能，暂不做）
4. 命名：项目名 Test/窗体 Frm_Main 可后续语义化（涉及 sln/csproj 联动，本次未动）
5. 旧仓库 /root/plc-step-monitor（四层版+StepTimer）保留未动，可随时 git archive 取用
6. PLC IP 已开放主界面（txtPlcIp，默认 192.168.1.50），端口固定 9600；如需端口可配再加
7. **步耗时记录+趋势图（已规划 2026-08-22，待实施）**：
   - 需求：记录每工位每流程步耗时→本地；**周期判定=模式学习指纹法**（非"回到0"）：学习前2~3轮步号转移序列提取稳定起始指纹（如 0→10），检测到指纹重复即新周期；中间 0→1000 等不误切；起始步序不稳定时升级前缀聚类（接口预留可替换）
   - 设计：按工位独立 CycleDetector（可替换边界判定接口：指纹法默认→前缀聚类升级）；StepRecorder 挂轮询线程喂数据；CSV UTF-8 BOM 按天分文件（明细 steps_/汇总 cycles_）；Frm_Records 用内置 Chart 控件（无需 NuGet）**实时滚动+周期分色** + DataGridView TopN（默认10）
   - 交互（2026-08-22 确认）：工位格子**保持 Label + 双击**打开该工位实时趋势图（非模态 Show，不阻塞监控），视觉零回归；触摸屏场景将来可切无边框 Button 单击（代码预留切换开关）
   - 存储（2026-08-22 确认，采集/持久化解耦）：**只存步号变化事件**（非全量采样，量降 10~100 倍）；三级通路=内存环形缓冲(实时直读)→临时分段文件(10min/段滚动保留~2h)→正式记录(主界面开关ON按天CSV落盘)；统一格式统一加载，启动清理过期临时段；加载支持选日期/时间段回放，表格TopN基于加载数据重算
   - 实施顺序（✅ 2026-08-22 已完成第 1 步）：EventStore(✅)+Frm_Records 实时趋势图(✅)+工位双击(✅) 已交付 v0.3-trend → 下一步：CycleDetector(周期判定指纹法)→临时分段文件+记录开关落盘→表格TopN+加载回放→仿真联调→现场实测

## 五、版本管理（本次重建）
- 新仓库：`/root/plc-step-monitor-v2/`（git 已 init，干净历史），远程分支 `v2-optimized`（A1917/plc-step-monitor）
- 交付流程：编译 → **编写/更新相关测试并验证通过** → push v2-optimized → GitHub Release 上传运行包（浏览器下载解压即用，工作电脑零安装）
- **测试规则（2026-08-29 用户明确要求）**：每次改动后必须为该次改动编写或更新相关测试，验证通过后才推送；纯文档改动除外。后续 CycleDetector/EventStore/StepRecorder 等逻辑类改动需配单元测试（建议新建独立测试工程）
- Release 历史：v0.1-optimized（恢复+优化基线）、v0.2-config（IP+工位数配置）
