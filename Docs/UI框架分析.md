# Project_CyberSousa UI 框架分析

## 1. 文档目的

本文基于当前仓库代码快照，对项目中的 UI 技术栈、运行时架构、资源组织、事件体系和现状问题做一次系统梳理，方便后续继续扩展页面、重构框架或统一规范。

重点回答 4 个问题：

1. 这个项目到底在用什么 UI 框架。
2. UI 是怎么被创建、显示、隐藏和销毁的。
3. 当前这套设计的优点和隐患分别是什么。
4. 如果后续继续做页面，建议沿着什么方向演进。

---

## 2. 结论摘要

先说结论：

- 项目的运行时 UI 框架不是 `UI Toolkit`，而是 **Unity UGUI + Addressables + DOTween + 一套自定义的 SCFrame UI 封装**。
- 项目已经安装了 `TextMeshPro`，但**当前实际 UI 页面仍然使用的是传统 UGUI 的 `Text/Button/Image`**。
- UI 的核心抽象分成三层：
  - `Node`：负责页面生命周期、层级归属、打开关闭规则。
  - `Panel`：负责页面表现、按钮事件绑定、显示隐藏动画。
  - `Mono`：负责把 prefab 上的组件引用暴露给代码。
- 当前启动页面不是通过专用的 `UINodeStart` 打开的，而是通过更通用的 `UINodeCommon<UIMonoStart, UIPanelStart>` 打开的。
- 这套架构的总体思路是清晰的，适合中小型游戏 UI；但在实现上有几个明显问题：
  - 隐藏时立刻 `SetActive(false)`，会让淡出动画几乎没有机会真正表现出来。
  - UI 资源加载大量依赖 `Addressables.WaitForCompletion()`，属于同步阻塞式加载。
  - 旧写法和新写法并存，框架入口有一点“半收敛”状态。
  - 事件与输入控制是全局布尔开关，简单但比较粗。

---

## 3. 实际使用的 UI 技术栈

## 3.1 Unity 与包依赖

从 `ProjectSettings/ProjectVersion.txt` 可以确认：

- Unity 版本：`2022.3.30f1c1`

从 `Packages/manifest.json` 可以确认，和 UI 直接相关的包主要有：

- `com.unity.ugui`: `1.0.0`
- `com.unity.textmeshpro`: `3.0.6`
- `com.unity.addressables`: `1.21.21`
- `com.unity.render-pipelines.universal`: `14.0.11`

其中真正参与当前 UI 的主要是：

- `UGUI`：按钮、图片、Canvas、EventSystem 等基础 UI 能力。
- `Addressables`：UI prefab 的加载和实例化。
- `DOTween`：UI 显示/隐藏时的淡入淡出动画。

## 3.2 当前没有在用的 UI 路线

虽然工程里有：

- `com.unity.modules.uielements`
- `TextMeshPro`

但从当前代码和资源来看：

- 没有发现 `UIDocument`、`VisualElement`、`UI Toolkit` 的实际使用。
- 没有发现 `TMP_Text`、`TextMeshProUGUI` 等脚本引用。
- 当前 prefab 中显示的是 `Text (Legacy)`。

所以可以明确判断：

> 项目当前的运行时 UI 主路线是 **UGUI**，不是 **UI Toolkit**。

## 3.3 第三方/附加框架

### DOTween

工程内置了 `Assets/_Scripts/SCFrame/Plugin/DOTween`，并且 `_ASCUIPanelBase` 里直接使用了 `CanvasGroup.DOFade(...)`。

这意味着：

- UI 动画当前依赖 DOTween。
- 主要用于面板级别的淡入淡出。
- 还没有看到更复杂的时间轴式 UI 动画组织。

### vHierarchy

`Assets/Plugin/vHierarchy` 是一个 **编辑器层级增强工具**，它帮助整理 Unity Hierarchy 面板体验，但**不是运行时 UI 框架的一部分**。

---

## 4. UI 架构总览

当前项目的 UI 可以理解为：

`Unity UGUI 容器` + `SCFrame 生命周期框架` + `Addressables 资源加载` + `自定义事件封装`

运行链路大致如下：

```text
SampleScene
  -> Gameinitializer(GameObject)
  -> GameInitializer.Start()
  -> 初始化 SCFrame 各类单例
  -> UINodeMgr.AddNode(...)
  -> UINodeCommon / UINodeStart 进入节点
  -> Addressables 实例化 UI prefab
  -> 获取 UIMonoXxx
  -> 构造 UIPanelXxx
  -> Panel.Initialize()
  -> Panel.ShowPanel()
  -> 绑定按钮事件 / 播放淡入动画
```

这套结构本质上是在 Unity 原生 UI 之上，自己搭了一个轻量页面系统。

---

## 5. 场景中的 UI 根节点组织

从 `Assets/Scenes/SampleScene.unity` 可以看到，场景里已经预先放好了 UI 容器：

- `Canvas`
- `Full`
- `Addition`
- `Top`
- `EventSystem`

同时，`SCGameMono` 被挂在场景对象 `Gameinitializer` 上，并通过 Inspector 持有这些引用：

- `mainCanvas`
- `fullLayerRoot`
- `additionLayerRoot`
- `topLayerRoot`
- `gameCamera`
- `poolRoot`
- `bgmRoot`
- `sfxRoot`

这说明当前 UI 框架不是“页面自己找 Canvas”，而是：

1. 场景提供统一的主 Canvas 和层级根节点。
2. `SCGameMono` 作为全局入口保存这些引用。
3. `Node` 根据自己的 `SCUIShowType` 决定挂到哪一层。

### 5.1 分层语义

`SCUIShowType` 定义了 4 种显示层级：

- `FULL`：全屏页面
- `ADDITION`：叠加页面
- `TOP`：顶层页面
- `INTERNAL`：页面内部子块

这个分层设计很常见，也比较合理：

- `FULL` 适合主界面、战斗主界面、背包主界面
- `ADDITION` 适合弹窗、二级面板
- `TOP` 适合引导、全局遮罩、提示层
- `INTERNAL` 适合某个页面内部的组件化子面板

---

## 6. 三层抽象：Node / Panel / Mono

这是项目 UI 框架最核心的部分。

## 6.1 Node：页面状态机与调度层

基类：`Assets/_Scripts/SCFrame/UI/Base/_ASCUINodeBase.cs`

`Node` 负责：

- 定义页面名字 `GetNodeName()`
- 定义资源名 `GetResName()`
- 定义显示层 `SCUIShowType`
- 管理生命周期：
  - `EnterNode`
  - `ShowNode`
  - `HideNode`
  - `QuitNode`
- 声明页面规则：
  - 新同层页面进入时是否隐藏自己
  - 新同层页面退出时是否重新显示自己
  - 是否允许 `Esc` 关闭
  - 是否允许鼠标右键关闭
  - 是否忽略 UI 栈
  - 隐藏时是否沉到底部

可以把 `Node` 理解成“页面控制器”或“页面状态外壳”。

它不直接关心按钮点击逻辑细节，而是更关注：

- 这个页面应该挂在哪一层
- 它和别的页面如何共存
- 它何时被创建、显示、隐藏、销毁

## 6.2 Panel：页面表现层

基类：`Assets/_Scripts/SCFrame/UI/Base/_ASCUIPanelBase.cs`

`Panel` 负责：

- 持有 `Mono`
- 在 `Initialize/Discard` 中组织自己的生命周期
- 处理显示/隐藏动画
- 在显示时注册按钮事件
- 在隐藏时移除按钮事件
- 通过 `CanvasGroup` 控制淡入淡出

典型职责很像 MVC/MVP 中的 ViewController 或 Presenter。

它比 `Node` 更贴近页面行为本身，比如：

- 页面显示时绑定 `btnStart`
- 页面隐藏时解绑 `btnStart`
- 页面显示时播放淡入

## 6.3 Mono：Prefab 引用层

基类：`Assets/_Scripts/SCFrame/UI/Base/_ASCUIMonoBase.cs`

`Mono` 非常轻，只负责两类信息：

- prefab 上拖拽的组件引用
- 配置参数，例如 `fadeInDuration` / `fadeOutDuration`

例如 `UIMonoStart` 里只暴露了：

- `btnStart`
- `btnSetting`
- `btnExit`

这层的意义是把：

- “场景/Prefab 上的可视对象引用”
- “纯代码逻辑”

明确拆开，避免在 `Panel` 里到处 `transform.Find(...)`。

### 6.4 这种三层拆分的优点

- 代码职责边界比较清楚。
- 便于美术和程序协作。
- 页面显示规则和页面交互逻辑被拆开了。
- 以后做通用页面模板时，复用成本较低。

---

## 7. UINodeMgr：页面栈管理器

核心类：`Assets/_Scripts/SCFrame/UI/UINodeMgr.cs`

`UINodeMgr` 是整个 UI 调度中心，内部维护一个 `_m_nodeList`。

它的主要职责：

- `AddNode`：添加页面
- `CloseTopNode`：关闭当前最上层页面
- `CloseTopAdditionNode`：优先关闭最上层叠加页
- `CloseNodeByEsc`：Esc 关闭
- `CloseNodeByMouseRight`：右键关闭
- `HideNode` / `ShowNode`
- `RemoveNode` / `RemoveAllNodes`
- `GetTopNode`

### 7.1 它在做什么

从设计上看，它相当于一个“轻量 UI 栈管理器”：

- 新页面打开时，加入栈顶。
- 某些同类型旧页面会自动隐藏。
- 关闭当前页面后，可按规则恢复上一页。

### 7.2 它的价值

这让 UI 不需要每个页面都自己管理“打开谁、关闭谁、恢复谁”。

对于游戏项目来说，这种集中式管理很常见，而且是有价值的，因为：

- 页面的互斥关系更统一。
- `Esc`、右键返回等行为能收口。
- 后面接入引导、全局遮罩也更方便。

### 7.3 目前的限制

这套管理器是“规则驱动”的，而不是“显式导航驱动”的。

优点是简单，缺点是：

- 页面跳转关系不会特别直观。
- 当页面越来越多时，依赖布尔规则可能会变得难维护。

---

## 8. 启动链路与当前实际入口

## 8.1 初始化顺序

`GameInitializer` 在 `Start()` 中调用 `Initialize()`，随后依次初始化：

- `SCMsgCenter`
- `SCTaskHelper`
- `SCPoolMgr`
- `SCInputListener`
- `SCRefDataMgr`
- `UINodeMgr`

然后调用 `startGame()` 打开首个 UI。

## 8.2 当前首屏不是走专用 Node，而是走通用 Node

这里有个很值得注意的点：

项目里同时存在两种“开始界面”节点写法：

### 专用写法

- `UINodeStart`

### 通用写法

- `UINodeCommon<UIMonoStart, UIPanelStart>`

但当前启动实际使用的是：

```csharp
UINodeMgr.instance.AddNode(
    new UINodeCommon<UIMonoStart, UIPanelStart>(
        SCFrame.UI.SCUIShowType.FULL,
        "panel_start",
        "UINodeStart",
        true,
        true,
        false,
        false
    )
);
```

这说明项目已经开始从“每个页面都写一个专用 Node”过渡到“用通用模板 Node 生成页面”。

这个方向是对的，因为：

- 绝大多数普通页面的生命周期逻辑高度相似。
- 专门写 `UINodeXxx` 很容易重复。
- `UINodeCommon` 能显著减少样板代码。

但当前仓库中 **新旧两套方式并存**，意味着框架还没有完全收口。

---

## 9. 当前 UI 资源组织方式

## 9.1 UI prefab 存放位置

当前可见 UI prefab 在：

- `Assets/GameRes/UI/panel_start.prefab`

## 9.2 Addressables 组织

`Assets/AddressableAssetsData/AssetGroups/UI.asset` 中可以看到：

- Addressable Group：`UI`
- 条目地址：`panel_start`

也就是说当前 UI 页面资源不是直接拖场景，也不是 `Resources.Load`，而是：

> 通过 `Addressables` 使用地址名 `panel_start` 动态实例化。

## 9.3 资源加载封装

`ResourcesHelper` 封装了 UI/资源加载逻辑，核心用法包括：

- `LoadAsset<T>()`
- `LoadGameObject(...)`
- `LoadGameObjectAsync(...)`
- `LoadAssets<T>()`

其中页面最常用的是：

```csharp
ResourcesHelper.LoadGameObject(GetResName(), GetRootTransform(), true);
```

这会：

1. 用 Addressables 实例化 prefab。
2. 挂到指定 UI 层级根节点下。
3. 给实例绑定“销毁时自动 release”的监听。

---

## 10. 当前页面样例：Start 面板

当前仓库只有一个很明确的业务 UI 样例，就是开始界面。

## 10.1 Prefab 结构

`panel_start.prefab` 中可以确认：

- 根节点名称：`panel_start`
- 根节点挂有 `CanvasGroup`
- 根节点挂有 `UIMonoStart`
- 子节点包含 3 个按钮：
  - `btn_start`
  - `btn_setting`
  - `btn_exit`
- 文本节点是 `Text (Legacy)`

从结构上看，这是一个非常标准的 UGUI 页面 prefab。

## 10.2 代码分层对应

### UIMonoStart

负责暴露按钮引用：

- `btnStart`
- `btnSetting`
- `btnExit`

### UIPanelStart

负责交互逻辑：

- `OnShowPanel()` 中绑定按钮事件
- `OnHidePanel()` 中移除按钮事件
- 点击后暂时只是输出日志：
  - 开始游戏
  - 设置
  - 退出游戏

### UINodeStart

负责页面创建/显示/隐藏/退出。

不过要注意：

> 这个类虽然存在，但当前启动流程实际上没有直接使用它，而是使用了 `UINodeCommon`。

---

## 11. 事件系统与输入封装

项目没有直接使用 Unity Button 的 `onClick.AddListener(...)` 作为唯一入口，而是又封了一层自己的事件系统。

核心类有：

- `SCEventListener`
- `SCEventListenerExtension`
- `SCInputListener`

## 11.1 SCEventListener 的作用

它挂在具体组件或物体上，统一转发：

- 鼠标进入/离开
- 左键点击、按下、抬起
- 右键点击、按下、抬起
- 中键事件
- 拖拽事件
- 碰撞/触发器事件
- Addressable 释放事件

这相当于一个统一的“事件代理层”。

## 11.2 Extension 的作用

`SCEventListenerExtension` 提供了更好写的扩展方法，例如：

- `AddMouseLeftClickDown`
- `RemoveMouseLeftClickDown`
- `AddMouseEnter`
- `AddDrag`

所以 `UIPanelStart` 中按钮绑定写法是：

```csharp
mono.btnStart.AddMouseLeftClickDown(onBtnStartClicked);
```

而不是 Unity 常见的：

```csharp
btn.onClick.AddListener(...)
```

## 11.3 SCInputListener 的作用

`SCInputListener` 是更高层的全局输入开关：

- 统一监听 `Esc`
- 统一监听鼠标右键
- 当页面淡入淡出时，通过 `SetCanInput(false)` 暂时阻止输入

它的目标是：

- 让 UI 动画期间不误触
- 让返回逻辑统一由 `UINodeMgr` 处理

这个思路没问题，但属于比较“全局化、粗粒度”的输入治理方式。

---

## 12. 动画机制

当前页面显示/隐藏动画由 `_ASCUIPanelBase` 统一处理。

主要机制：

- 所有 UI 页面要求带 `CanvasGroup`
- `ShowPanel()` 时把 alpha 从 `0 -> 1`
- `HidePanel()` 时把 alpha 从 `1 -> 0`
- 通过 DOTween 执行 `DOFade`

这是一种成本很低、统一性很高的方案，适合项目早期。

### 优点

- 新页面几乎零成本接入基础动画
- 动画风格统一
- 代码集中，便于后续替换

### 限制

- 目前只适合简单淡入淡出
- 如果页面需要更复杂的入场、退场、遮罩、弹性动画，后面可能还要扩展

---

## 13. 这套 UI 框架的优点

综合来看，当前架构有以下明显优点。

## 13.1 结构清晰

`Node / Panel / Mono` 分层明确，阅读成本不高。

尤其对 Unity 项目来说，这比“所有逻辑都堆在 MonoBehaviour 里”要健康很多。

## 13.2 页面层级有统一入口

`UINodeMgr` 把页面显示栈统一了，后续做：

- 弹窗
- 页面返回
- 顶层引导
- 页面遮挡规则

都会更容易收敛。

## 13.3 已经具备资源化思路

UI prefab 通过 Addressables 管理，而不是都塞在场景里。

这意味着：

- 后面扩 UI 不会让场景越来越重
- 更适合后期拆资源、热更、预加载

## 13.4 已有一定的组件化倾向

`UINodeCommon`、`UIPanelContainerBase`、`UIMonoCommonContainer` 说明作者已经在往“通用模板”和“容器型 UI”方向考虑。

这比完全页面式硬编码更有扩展性。

## 13.5 输入与动画已经初步统一

虽然实现还可以继续打磨，但至少：

- 输入封口
- 页面显示隐藏动画
- 按钮事件扩展

都已经形成了“框架层约束”，不是完全散的。

---

## 14. 当前实现中的问题与风险

这一节是最值得后续关注的部分。

## 14.1 隐藏时立即禁用对象，淡出动画很可能看不到

这是当前最明显的问题之一。

在 `UINodeCommon.OnHideNode()` 和 `UINodeStart.OnHideNode()` 里，逻辑都是：

1. `panel.HidePanel()`
2. 立刻 `SCCommon.SetGameObjectEnable(_m_panelGO, false)`

但 `HidePanel()` 本身已经在播放 DOTween 的淡出动画，并且 `_ASCUIPanelBase.OnHideOver()` 里也会在动画结束后再 `SetActive(false)`。

这会导致一个结果：

> 节点层在动画刚开始时就把物体禁用了，Panel 层的淡出动画等于被抢跑打断。

影响：

- 淡出表现不稳定
- 动画设计失效
- 框架职责边界被破坏

建议：

- `Node.OnHideNode()` 只调用 `HidePanel()`，不要再立即 `SetActive(false)`。
- 真正的隐藏时机交给 `Panel` 在动画完成后处理。

## 14.2 UI 加载使用同步阻塞式 Addressables

`ResourcesHelper.LoadGameObject()`、`LoadAsset<T>()` 都用了：

- `WaitForCompletion()`

这意味着：

- UI 打开时可能阻塞主线程
- 资源稍大时容易造成卡顿
- 如果未来 UI 页面复杂、贴图多、依赖链长，问题会更明显

早期项目这样做是能跑的，但不太适合长期扩展。

建议：

- 对常用首页、主 HUD 可以预加载。
- 中后期逐步迁移为真正的异步打开流程。

## 14.3 新旧 Node 写法并存，框架入口未完全统一

当前同时存在：

- `UINodeStart`
- `UINodeCommon<TMONO, TPANEL>`

而实际启动走的是后者。

这带来的问题是：

- 新人看代码会疑惑“到底该写哪种”
- 两套入口长期并存，规范会越来越模糊
- 旧代码可能被误继续复制

建议：

- 明确规定“普通页面默认用 `UINodeCommon`”
- 只有确实需要特殊节点行为时，才单独写 `UINodeXxx`

## 14.4 旧的 UINodeStart 存在重复销毁风险

`UINodeStart.OnQuitNode()` 中：

- 调用了 `_m_startPanel.Discard()`
- 又手动 `SCCommon.DestoryGameObject(_m_panelGO)`

但 `_ASCUIPanelBase.AfterDiscard()` 本身已经会销毁 `GetGameObject()`。

虽然 Unity 对重复 `Destroy` 通常不会立刻炸，但这仍然属于职责重复、容易埋坑的实现。

相对来说，`UINodeCommon.OnQuitNode()` 的写法更干净。

## 14.5 事件系统比 Unity 原生更统一，但也更重

自定义 `SCEventListener` 有好处，但代价也有：

- 学习成本高于原生 `Button.onClick`
- 页面多了以后，每个交互对象都可能挂一个监听组件
- 问题排查时需要先理解框架，而不是直接看 Unity 事件

如果团队规模不大，这个代价是可接受的；但需要有文档和规范支撑。

## 14.6 输入控制是全局布尔开关，粒度偏粗

`SCInputListener` 用一个 `canInput` 控制很多东西：

- `Esc`
- 右键
- 一部分 UI 指针事件

这很简单，但问题是：

- 某些时候你可能只想禁用点击，不想禁用全部输入
- 某些页面动画期间可能还想允许顶层 UI 接收事件
- 复杂场景下会出现“谁把输入关了”的追踪问题

短期可用，长期可能需要更细粒度的输入域控制。

## 14.7 指针事件的拦截规则不完全一致

在 `SCEventListener` 中：

- `OnPointerClick/Down/Up/Drag` 等会检查 `SCInputListener.canInput`
- 但 `OnPointerEnter/Exit` 仍然会直接派发

这不一定是错的，但需要明确是“设计如此”还是“暂时遗漏”。

否则后面很容易出现：

- 不能点按钮，但 hover 逻辑还在执行
- 动画期间仍触发某些进入/离开回调

## 14.8 当前文本体系没有统一到 TMP

虽然项目已经安装了 `TextMeshPro` 包，但当前 UI 仍然是 `Text (Legacy)`。

这会带来几个潜在问题：

- 文本显示能力和排版能力有限
- 多语言、富文本、字形资源管理后续不够现代化
- 新页面到底用 `Text` 还是 `TMP`，规范不统一

如果项目后续 UI 会持续增长，建议尽早统一。

## 14.9 部分源码注释存在编码/乱码现象

当前若用 UTF-8 视角查看，有不少注释显示为乱码。

这不影响运行，但会影响：

- 阅读体验
- 新人接手效率
- 文档生成与 AI 辅助分析质量

建议后续统一源码编码格式。

---

## 15. 对“这是不是一个成熟 UI 框架”的判断

如果用一句话评价：

> 这是一套“方向正确、结构清晰、已可用于项目早中期，但尚未完全打磨成熟”的 Unity 游戏 UI 框架。

它的成熟点在于：

- 已经有页面栈
- 已有层级概念
- 已有资源化加载
- 已有通用基类
- 已有事件封装

它还不够成熟的地方在于：

- 细节实现有一些职责重叠
- 新旧写法并存
- 动画/输入控制还偏粗
- 资源加载方式偏同步

所以它不是“临时拼的 UI 脚本”，但也还没到“强约束、高自动化、适合大规模团队协作”的程度。

---

## 16. 后续演进建议

下面按优先级给出建议。

## 16.1 第一优先级：修掉隐藏逻辑与动画冲突

建议直接统一规则：

- `Node` 只负责调用 `Panel.ShowPanel()` / `Panel.HidePanel()`
- 页面显隐的最终 `SetActive` 由 `Panel` 自己控制

这样最符合当前分层设计。

## 16.2 第二优先级：统一页面创建规范

建议确定一条团队规范：

- 普通页面：默认使用 `UINodeCommon<TMono, TPanel>`
- 特殊页面：只有需要自定义生命周期时才写 `UINodeXxx`

同时可以把 `UINodeStart` 这种示例型旧类保留为参考，或者逐步移除，避免误导。

## 16.3 第三优先级：规划异步加载策略

建议按场景拆分策略：

- 启动常驻页：可预加载
- 高频页：可缓存
- 低频页：异步加载

这样既能保留 Addressables 的优势，也能避免同步阻塞放大。

## 16.4 第四优先级：统一文本技术栈

建议尽快明确：

- 后续新 UI 是否统一改用 `TextMeshPro`
- 老页面是否需要逐步迁移

如果项目以后要做正式美术 UI、多语言或更细的排版，TMP 会更稳。

## 16.5 第五优先级：补一份“新页面接入指南”

当前框架已经值得写一份简短规范文档，内容可以包括：

1. 新建 prefab 放哪。
2. Addressables 地址怎么配。
3. `UIMonoXxx` 怎么写。
4. `UIPanelXxx` 里哪些方法该做什么。
5. 默认用 `UINodeCommon` 还是专用 `Node`。

这会显著降低后续接入成本。

---

## 17. 如果按当前框架继续新增 UI，推荐姿势

推荐流程如下：

1. 在 `Assets/GameRes/UI/` 下创建页面 prefab。
2. 给 prefab 挂 `CanvasGroup`。
3. 写 `UIMonoXxx`，只负责组件引用和配置。
4. 写 `UIPanelXxx`，负责事件绑定、交互逻辑、显示隐藏行为。
5. 在 Addressables 的 `UI` 组里配置地址名。
6. 用 `UINodeCommon<UIMonoXxx, UIPanelXxx>` 打开页面。
7. 只有在确实有特殊栈行为时，才额外写专用 `UINodeXxx`。

这是当前仓库里最一致、重复最少的一条路线。

---

## 18. 关键文件索引

### UI 框架核心

- `Assets/_Scripts/SCFrame/UI/UINodeMgr.cs`
- `Assets/_Scripts/SCFrame/UI/SCUIEnum.cs`
- `Assets/_Scripts/SCFrame/UI/Base/_ASCUINodeBase.cs`
- `Assets/_Scripts/SCFrame/UI/Base/_ASCUIPanelBase.cs`
- `Assets/_Scripts/SCFrame/UI/Base/_ASCUIMonoBase.cs`
- `Assets/_Scripts/SCFrame/UI/Base/_ASCUILifeObjBase.cs`

### 全局入口与场景承载

- `Assets/_Scripts/SCFrame/SCGameMono.cs`
- `Assets/_Scripts/GameCore/GameInitializer.cs`
- `Assets/Scenes/SampleScene.unity`

### 资源加载

- `Assets/_Scripts/SCFrame/Util/Resources/ResourcesHelper.cs`
- `Assets/AddressableAssetsData/AssetGroups/UI.asset`

### 事件与输入

- `Assets/_Scripts/SCFrame/Util/EventListener/SCEventListener.cs`
- `Assets/_Scripts/SCFrame/Util/EventListener/SCEventListenerExtension.cs`
- `Assets/_Scripts/SCFrame/Util/Input/SCInputListener.cs`

### 当前页面样例

- `Assets/GameRes/UI/panel_start.prefab`
- `Assets/_Scripts/GameCore/UI/Start/UIMonoStart.cs`
- `Assets/_Scripts/GameCore/UI/Start/UIPanelStart.cs`
- `Assets/_Scripts/GameCore/UI/Start/UINodeStart.cs`
- `Assets/_Scripts/GameCore/UI/Common/UINodeCommon.cs`

---

## 19. 最终判断

如果后续继续做这个项目的 UI，我会把它定义成：

> 一个基于 Unity UGUI 的自定义页面框架，已经具备“可持续扩展”的基础，但还需要做一轮工程化收敛。

换句话说：

- **不是没有框架**，而且框架思路其实不错。
- **也不是现成成熟方案**，目前仍然需要你们自己继续定规范、补细节、修边角。

对当前项目阶段来说，这套 UI 框架是能继续往前用的；前提是后续新增页面时尽量遵守统一路径，不要再让写法继续分叉。
