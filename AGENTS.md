# Project_CyberSousa Agent Guide

本文件给进入本仓库协作的 AI / 自动化代理使用。

目标有两个：

1. 约束修改方式，尽量沿用项目现有结构与框架。
2. 快速说明当前项目内容、目录结构和关键入口，降低二次摸索成本。

## 1. 协作总原则

- 优先复用项目内已有框架、基类、工具类、目录组织和资源路径，不要在没有必要的情况下额外起一套新架构。
- 尤其是 UI、资源加载、事件监听、输入控制、对象池、表格导出、循环列表，这些能力项目里已经有现成实现，默认先沿用。
- 如果一个需求可以通过少量修改现有代码完成，就不要为了“兼容旧结构”或“预留未来”堆很多标志位、兜底分支和回退逻辑。
- 遇到可能出错的情况，优先打印明确日志帮助排查，不要静默 `return`。
- 如果需求不涉及中文，不要改动项目里的中文文本和中文注释。

## 2. 编码与文件处理约束

- 所有文本文件读取都要显式指定编码，默认先按 UTF-8 读取。
- 所有文本文件写入都要显式指定 UTF-8。
- 不要使用 PowerShell 默认编码直接改源码、Markdown、XML、Gradle、Kotlin、Java、YAML、JSON、Properties。
- 终端里看到 `????` 时，不要直接判断文件损坏，应先确认真实编码和实际内容。
- 如果文件不是 UTF-8，先确认后再修改，不要盲目覆盖。

推荐写法：

```python
from pathlib import Path

text = Path(path).read_text(encoding="utf-8")
Path(path).write_text(text, encoding="utf-8")
```

## 3. 现有框架复用优先级

### 3.1 UI

- 当前运行时 UI 主线是 `Unity UGUI + Addressables + DOTween + SCFrame 自定义 UI 框架`。
- 默认沿用 `Node / Panel / Mono` 三层结构，不要直接把页面逻辑全塞进一个 `MonoBehaviour`。
- 普通页面优先使用 `UINodeCommon<TMONO, TPANEL>`。
- 只有页面生命周期或栈行为明显特殊时，再单独写专用 `UINodeXxx`。
- 新 UI prefab 默认放在 `Assets/GameRes/UI/`，并配置到 Addressables。
- 如果通过编辑器代码生成 UI prefab，生成逻辑里要同步自动注册到 Addressables 的 `UI` 组，并写好对应 address，避免 prefab 已生成但运行时 key 找不到。

推荐做法：

1. 建 prefab。
2. 写 `UIMonoXxx` 挂引用。
3. 写 `UIPanelXxx` 处理交互。
4. 确保 prefab 已加入 Addressables `UI` 组，且 address 与运行时加载 key 一致。
5. 用 `UINodeCommon<TMONO, TPANEL>` 打开。

### 3.2 资源加载

- 现有资源加载统一走 `Assets/_Scripts/SCFrame/Util/Resources/ResourcesHelper.cs`。
- Addressable 资源优先沿用现有 key、分组和加载入口，不要随手再写一套重复封装。

### 3.3 事件与输入

- 按钮和指针事件优先沿用 `SCEventListener` / `SCEventListenerExtension`。
- 全局输入控制优先沿用 `SCInputListener`。

### 3.4 对象池与任务

- 通用对象池优先复用 `SCPoolMgr`。
- 协程/异步任务优先复用 `SCTaskHelper`。
- 无限滚动列表内部已经有自己的列表私有池，不要强行改接全局池。

### 3.5 表格与配置数据

- 表格导出与 RefData 相关逻辑优先复用 `SCFrame/Editor/Export`、`SCRefDataMgr`、`SCRefDataCore`、`SCRefDataList` 这套现有链路。
- 当前 `Resources/RefData` 下已经有导出文本和 Excel 示例，新增配置优先沿着这条路径扩展。

## 4. 项目当前概况

- Unity 版本：`2022.3.30f1c1`
- 渲染管线：`URP`
- 当前 UI 相关包：`UGUI`、`TextMeshPro`、`Addressables`
- 项目内有 `DOTween` 插件代码和 `vHierarchy` 编辑器插件
- 当前主场景资源：
  - `Assets/Scenes/SampleScene.unity`
  - `Assets/Scenes/TestScenes/UIListTest.unity`
- `ProjectSettings/EditorBuildSettings.asset` 目前没有正式配置构建场景列表

## 5. 当前目录结构速览

```text
Assets/
  _Scripts/
    GameCore/
      RefData/
      Test/
      UI/
        Common/
        LoopListDemo/
        Start/
    SCFrame/
      Core/
      Editor/
      Export/
      Plugin/
      UI/
      Util/
  AddressableAssetsData/
  GameRes/
    UI/
  Plugin/
    vHierarchy/
  Resources/
    RefData/
      Excel/
      ExportTxt/
  Scenes/
    TestScenes/
  Settings/
    Render/
Docs/
Packages/
ProjectSettings/
```

## 6. 关键目录说明

### 6.1 `Assets/_Scripts/GameCore`

业务层代码，主要放项目自己的初始化、UI 页面、配置数据定义、测试入口。

- `GameInitializer.cs`
  - 游戏启动入口。
  - 当前会初始化消息、任务、对象池、输入、RefData、UI 节点管理器。
  - 然后默认打开开始页 `panel_start`。
- `UI/Start/`
  - 开始页示例。
- `UI/Common/`
  - 通用 UI 节点模板、通用容器、循环列表业务桥接层。
- `UI/LoopListDemo/`
  - 无限滚动列表示例页。
- `RefData/`
  - 业务配置数据结构。
- `Test/`
  - 当前有 `UIListInit.cs` 之类的测试相关代码。

### 6.2 `Assets/_Scripts/SCFrame`

项目自定义基础框架，很多需求先看这里有没有现成能力。

- `Core/`
  - 单例、生命周期接口、扩展方法、公共枚举。
- `UI/`
  - UI 框架核心。
  - 包含 `UINodeMgr`、循环列表底层、UI 基类。
- `Util/`
  - 动画、协程、调试、事件监听、输入、消息中心、对象池、资源加载、状态机、任务、Tween、UI 工具。
- `Editor/Export/`
  - Excel 导出与编辑器导出工具。
- `Export/`
  - RefData 导出基础类。
- `Plugin/DOTween/`
  - 项目内置 DOTween 相关代码。

### 6.3 `Assets/GameRes`

运行时资源目录。

- 当前已确认 `Assets/GameRes/UI/` 下有：
  - `panel_start.prefab`
  - `panel_loop_list_demo.prefab`

### 6.4 `Assets/AddressableAssetsData`

Addressables 配置目录。

- 当前已经有 `UI` 资源组。
- UI prefab 默认应优先走这里管理，而不是随意散落在 `Resources.Load` 路线里。

### 6.5 `Assets/Resources/RefData`

当前配置表示例数据目录。

- `Excel/` 下有 Excel 示例。
- `ExportTxt/` 下有导出的文本示例。

### 6.6 `Docs`

当前已有两份项目内文档：

- `Docs/UI框架分析.md`
- `Docs/无限滚动列表使用与原理.md`

后续涉及 UI 或循环列表，建议先看这两份文档再改代码。

## 7. UI 框架现状摘要

当前 UI 不是 UI Toolkit，主线是 UGUI。

核心结构：

- `UINodeMgr`：UI 节点栈与显示调度
- `_ASCUINodeBase`：页面节点基类
- `_ASCUIPanelBase<T>`：页面逻辑与显示控制基类
- `_ASCUIMonoBase`：Prefab 引用层基类
- `UINodeCommon<TMONO, TPANEL>`：通用页面节点模板

场景内全局 UI 挂点由 `SCGameMono` 持有，主要包括：

- `mainCanvas`
- `fullLayerRoot`
- `additionLayerRoot`
- `topLayerRoot`
- `poolRoot`
- `bgmRoot`
- `sfxRoot`

AI 在新增 UI 时，默认应遵守以下路径：

1. 复用现有层级和根节点。
2. 复用现有 `Node / Panel / Mono` 结构。
3. 复用现有事件扩展与淡入淡出机制。
4. 资源接入优先走 Addressables。

## 8. 无限滚动列表现状摘要

项目已经有一套可复用的循环列表框架，不要重复造轮子。

关键类：

- `SCLoopListView`
- `SCLoopListViewItem`
- `SCLoopListItemPool`
- `SCLoopListDefine`
- `SCLoopListItemBase`
- `UIPanelLoopListBase<...>`
- `UIMonoLoopListContainer`

业务接入时优先复用：

1. `UIMonoLoopItemBase` 子类挂 item 引用。
2. `UIPanelLoopItemBase<T>` 子类写 item 逻辑。
3. `UIPanelLoopListBase<...>` 写列表业务绑定。

参考实现：

- `Assets/_Scripts/GameCore/UI/LoopListDemo/`
- `Docs/无限滚动列表使用与原理.md`

## 9. 当前已知入口与示例

- 启动入口：`Assets/_Scripts/GameCore/GameInitializer.cs`
- 当前默认首页：
  - `panel_start`
  - 通过 `UINodeCommon<UIMonoStart, UIPanelStart>` 打开
- 循环列表示例页代码仍在仓库中，但默认启动入口目前已注释掉

## 10. 修改时的建议顺序

收到需求后，建议按这个顺序判断：

1. 先看 `Docs/` 有没有现成说明。
2. 再看 `GameCore` 是否已有接近的业务实现。
3. 再看 `SCFrame` 是否已有可复用基类或工具。
4. 能复用就复用，确实不合适再补新实现。
5. 如果新增结构，优先与现有目录风格保持一致。

## 11. 需要特别注意的点

- 不要因为图省事，把业务直接绕开 `SCFrame` 现有 UI 体系。
- 不要在没有明确理由的情况下新引入一套事件系统、资源加载器、对象池或 UI 架构。
- 不要随意修改 `ProjectSettings`、Addressables 配置、Prefab/YAML，除非需求确实需要且知道影响范围。
- 如果改 YAML / prefab 成本很高，但写编辑器脚本自动生成更稳，可以优先考虑补编辑器生成代码。
- 如果发现文件编码异常，先确认编码再改，不要直接覆盖导致中文损坏。

## 12. 给后续 AI 的一句话总结

这是一个基于 Unity 2022.3 的项目，核心运行时框架已经围绕 `SCFrame` 建好了；后续开发默认策略不是“重新设计一套”，而是“先沿着现有 UI、资源、输入、事件、列表、RefData 体系扩展”，只在现有结构明显不适合时才新增抽象。
