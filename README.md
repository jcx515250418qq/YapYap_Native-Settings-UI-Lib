# Yap Native Settings UI（Yap_NativeSettingsUI）

[简体中文](#简体中文) | [English](#english)

---

Thunderstore: https://thunderstore.io/c/yapyap/p/XiaohaiMod/Native_Settings_UI_Lib/
GitHub: https://github.com/jcx515250418qq/YapYap_Native-Settings-UI-Lib

## 仓库结构

- 库源码：`YapYap_Native-Settings-UI-Lib/`
- 使用例程（包含所有组件）：`YapYap_Native-Settings-UI-Demo/`

## 更新日志

- 2026-02-15
  - 修复 UI 注入路径：适配新 UI 结构与对象名，增加多路径回退
  - 更新 DLL 引用 HintPath：解决编译引用路径问题
  - 新增示例项目：YapYap_Native-Settings-UI-Demo 覆盖所有常用控件

## 简体中文

一个用于 **YapYap（BepInEx）** 的设置界面注入库：在游戏自带 `UISettings` 界面中，自动创建自定义 Tab 与 Section，并在其中生成按钮、开关、下拉、滑条、标签、编辑框等控件。

本 README 重点介绍 **公开接口** 与参数含义。

---

## 快速开始



典型用法：

```csharp
var tab = NativeSettingsUI.RegisterTab(
    guid: "com.yourmod.settings.tab",
    title: new LocalText("TAB_TITLE", "我的设置", "My Settings"),
    showInGame: true
);

tab.CreateToggle(
    id: "Toggle_Enabled",
    settingKey: "com.yourmod.Enabled",
    title: new LocalText("ENABLED", "启用", "Enabled"),
    initialValue: true,
    onChanged: v => enabled = v,
    showInGame: true
);
```

---

## 核心概念

### 1) Tab / Section 注入

`RegisterTab(...)` 会在 `UISettings` 可用时，自动：
- 在左侧 Tab 列表创建一个按钮（Tab）
- 在右侧 Section 区创建一个面板（Section）
- 点击 Tab 时切换到该 Section（通过绑定到 `UISettings.sections` 实现）

### 2) LocalText（简易本地化文本）

`LocalText` 用来提供“中文/英文”两套文本：

```csharp
new LocalText(key: "EXAMPLE", chinese: "中文", english: "English")
```
- 如果你不打算制作多语言 UI,可以把 `chinese`或者 `english` 留空,但是至少要提供一个非空字符串 
- 当前语言为中文（`Chinese/ChineseSimplified/ChineseTraditional`）时显示 `chinese`
- 否则显示 `english`
- 该库会监听语言变化并自动刷新绑定过的文本

### 3) UiRef<T>（延迟拿到真实 UI 引用）

UISettings 的对象不是在 `Awake` 立刻存在，控件会“稍后”才被克隆/创建出来。

`CreateXxx()` 返回 `UiRef<T>`：
- `IsReady`：是否已经拿到了真实 UI 组件引用
- `Value`：真实引用（未就绪时为 `null`）
- `Ready`：当真实引用出现时触发

```csharp
UiRef<TMP_Text> labelRef = tab.CreateLabel(...);
labelRef.Ready += tmp =>
{
    // tmp 就是真实的 TMP_Text
};
```

---

## 公共接口：NativeSettingsUI

命名空间：`Yap_NativeSettingsUI`

### RegisterTab

```csharp
public static SettingsTab RegisterTab(string guid, LocalText title, bool showInGame)
```

- `guid`：Tab 的唯一标识（建议用你的 ModId 前缀），重复注册同 guid 会返回同一个 Tab
- `title`：Tab 标题（支持中/英切换）
- `showInGame`：是否在“游戏内打开的设置界面”也显示该 Tab（Tab 自身不开启则控件再开也不会显示）

### BindText / UnbindText

用于给某个 `TMP_Text` 绑定/解绑 `LocalText` 的自动刷新逻辑。

```csharp
public static void BindText(TMP_Text tmp, LocalText text)
public static void UnbindText(TMP_Text tmp)
```

适用场景：
- 你想让某个文本跟随语言变化：用 `BindText`
- 你想把某个标签改成“运行时动态文本”，不再被本地化刷新覆盖：先 `UnbindText` 再 `tmp.text = ...`

---

## 公共接口：SettingsTab（创建控件）

`RegisterTab(...)` 返回 `SettingsTab`，所有控件都在 Tab 上创建。

### 通用参数说明

以下参数在多数控件中都有：

- `id`
  - 控件对象名（用于查找/避免重复创建）
  - 建议保持唯一，例如：`"Toggle_Enabled"`、`"Button_Save"`
- `showInGame`
  - 该控件是否在“游戏内设置界面”显示
  - 前提：Tab 的 `showInGame=true`
- `preferredSize`（可选）
  - 期望尺寸（宽高）
  - 不传则使用游戏原生模板的默认尺寸/布局
  - 当你给某些控件设置了较大的宽度时，库会启用“自定义两列流式布局”，以支持跨列/换行
- `anchoredPositionOffset`（可选）
  - 在最终位置基础上做少量偏移（用于微调）
  - 注意：如果父级布局组件会重排，偏移可能会被布局覆盖或产生不可预期效果

### CreateButton

```csharp
public UiRef<Button> CreateButton(
    string id,
    LocalText text,
    Action onClick,
    bool showInGame = true,
    Vector2? preferredSize = null,
    Vector2? anchoredPositionOffset = null)
```

- `text`：按钮显示文本（中/英）
- `onClick`：点击回调

### CreateLabel

```csharp
public UiRef<TMP_Text> CreateLabel(
    string id,
    LocalText text,
    bool showInGame = true,
    Vector2? preferredSize = null,
    Vector2? anchoredPositionOffset = null)
```

- `text`：标签文本（中/英）

### CreateToggle

```csharp
public UiRef<UISettingToggle> CreateToggle(
    string id,
    string settingKey,
    LocalText title,
    bool initialValue,
    Action<bool> onChanged,
    bool showInGame = true,
    Vector2? preferredSize = null,
    Vector2? anchoredPositionOffset = null)
```

- `title`：控件标题（中/英）
- `initialValue`：初始值
- `onChanged`：值变化回调
- `settingKey`：**持久化键名**（见下文“settingKey 是什么”）

### CreateDropdownString

```csharp
public UiRef<UISettingDropdownString> CreateDropdownString(
    string id,
    string settingKey,
    LocalText title,
    IList<string> options,
    string initialValue,
    Action<string> onChanged,
    bool showInGame = true,
    Vector2? preferredSize = null,
    Vector2? anchoredPositionOffset = null)
```

- `options`：下拉可选项（字符串列表）
- `initialValue`：初始选中项
- `onChanged`：选择变化回调
- `settingKey`：持久化键名

### CreateSliderInt

```csharp
public UiRef<UICustomSlider> CreateSliderInt(
    string id,
    string settingKey,
    LocalText title,
    int minValue,
    int maxValue,
    int initialValue,
    Action<int> onChanged,
    bool showInGame = true,
    Vector2? preferredSize = null,
    Vector2? anchoredPositionOffset = null)
```

- `minValue / maxValue`：整数范围
- `initialValue`：初始值（会被 clamp 到范围内）
- `onChanged`：值变化回调
- `settingKey`：持久化键名

### CreateInputString（编辑框）

```csharp
public UiRef<TMP_InputField> CreateInputString(
    string id,
    string settingKey,
    LocalText title,
    string initialValue,
    Action<string> onChanged,
    bool showInGame = true,
    Vector2? preferredSize = null,
    Vector2? anchoredPositionOffset = null)
```

实现说明（与示例一致）：
- 基于 Slider 模板克隆
- 删除 `Slider` 子节点下的 `Value`、`Fill Area`、`Handle`
- 将 `Slider` 子节点重命名为 `Input`
- 销毁根对象上的 `YAPYAP.UICustomSlider` 组件
- 确保 `InputField` 对象被激活（模板里它可能默认是未激活）

数据行为：
- 若 `settingKey` 非空：启动时 `PlayerPrefs.GetString(settingKey, initialValue)`
- 在 `onEndEdit` 时保存：`PlayerPrefs.SetString(settingKey, v)` 并回调 `onChanged(v)`

---

## settingKey 是什么？

`settingKey` 用于 **游戏原生设置系统的持久化键名**（本质是 `PlayerPrefs` 的 key）。

推荐规则：
- 每个控件一个唯一 key
- 使用 ModId 做前缀，避免与别的 Mod 或游戏本体冲突

例如：
- `com.yourmod.Toggle.Enabled`
- `com.yourmod.Dropdown.Mode`
- `com.yourmod.Input.Note`

---

## 常见用法示例

### 运行时修改 Label 文本（不被本地化刷新覆盖）

```csharp
UiRef<TMP_Text> labelRef = tab.CreateLabel(
    id: "Label_Runtime",
    text: new LocalText("RUNTIME", "等待加载", "Waiting for UI")
);

labelRef.Ready += tmp =>
{
    NativeSettingsUI.UnbindText(tmp);
    tmp.text = "Runtime label ready.";
};
```

### 下拉只在主菜单显示，不在游戏内显示

```csharp
tab.CreateDropdownString(
    id: "Dropdown_Mode",
    settingKey: "com.yourmod.Mode",
    title: new LocalText("MODE", "模式", "Mode"),
    options: new [] { "Normal", "Hard" },
    initialValue: "Normal",
    onChanged: v =>  gameMode = v ,
    showInGame: false
);
```

---

## 作者 / Author

- 作者: 小海 (XiaoHai)
- Bilibili: https://space.bilibili.com/2055787437
- 邮箱: 515250418@qq.com

---

## English

An injection library MOD for **YapYap (BepInEx)**. It hooks into the built-in `UISettings` screen, automatically creates a custom Tab + Section, and provides helper APIs to generate common controls such as buttons, toggles, dropdowns, sliders, labels, and input fields.



---

## Quick Start



Typical usage:

```csharp
var tab = NativeSettingsUI.RegisterTab(
    guid: "com.yourmod.settings.tab",
    title: new LocalText("TAB_TITLE", "我的设置", "My Settings"),
    showInGame: true
);

tab.CreateToggle(
    id: "Toggle_Enabled",
    settingKey: "com.yourmod.Enabled",
    title: new LocalText("ENABLED", "启用", "Enabled"),
    initialValue: true,
     onChanged: v => enabled = v,
    showInGame: true
);
```

---

## Core Concepts

### 1) Tab / Section Injection

`RegisterTab(...)` automatically does the following when `UISettings` becomes available:
- Creates a Tab button in the left-side Tab list
- Creates a corresponding Section panel in the right-side content area
- Wires Tab switching to show the Section (by binding into `UISettings.sections`)

### 2) LocalText (Lightweight Localization)

`LocalText` holds two versions of a string (Chinese / English):

```csharp
new LocalText(key: "EXAMPLE", chinese: "中文", english: "English")
```

- If you don't plan to build a multilingual UI, you can leave the Chinese text empty.
- When the current language is Chinese (`Chinese/ChineseSimplified/ChineseTraditional`), the library shows `chinese`
- Otherwise it shows `english`
- The library listens for language changes and refreshes bound texts automatically

### 3) UiRef<T> (Deferred UI Reference)

`UISettings` objects do not necessarily exist at `Awake`, and controls are created later.

Each `CreateXxx()` returns `UiRef<T>`:
- `IsReady`: whether the real component instance is available
- `Value`: the actual UI component (null before ready)
- `Ready`: fired once the actual component instance becomes available

```csharp
UiRef<TMP_Text> labelRef = tab.CreateLabel(...);
labelRef.Ready += tmp =>
{
    // tmp is the real TMP_Text instance
};
```

---

## Public API: NativeSettingsUI

Namespace: `Yap_NativeSettingsUI`

### RegisterTab

```csharp
public static SettingsTab RegisterTab(string guid, LocalText title, bool showInGame)
```

- `guid`: Unique tab id. Registering the same guid multiple times returns the same `SettingsTab` instance. Recommended to prefix with your ModId.
- `title`: Tab title (Chinese/English switching supported)
- `showInGame`: Whether this tab should also appear in the in-game settings UI. If the tab itself is not shown in-game, controls won't be shown in-game either.

### BindText / UnbindText

Bind or unbind the automatic `LocalText` refresh behavior for a `TMP_Text`.

```csharp
public static void BindText(TMP_Text tmp, LocalText text)
public static void UnbindText(TMP_Text tmp)
```

Use cases:
- You want a text to follow language switching: use `BindText`
- You want a label to become a runtime-only dynamic string (not overridden by localization refresh): call `UnbindText` and then set `tmp.text = ...`

---

## Public API: SettingsTab (Creating Controls)

`RegisterTab(...)` returns a `SettingsTab`. All controls are created from the tab instance.

### Common Parameters

Most control creation methods share these parameters:

- `id`
  - The control object name (used for lookup and to avoid duplicating controls)
  - Should be unique, e.g. `"Toggle_Enabled"`, `"Button_Save"`
- `showInGame`
  - Whether the control is shown in the in-game settings UI
  - Requires the tab itself to be created with `showInGame=true`
- `preferredSize` (optional)
  - Desired size (width/height)
  - If omitted, the game template’s default size/layout is used
  - When some controls become “too wide”, the library enables a custom two-column flow layout to support wrapping and full-row controls
- `anchoredPositionOffset` (optional)
  - Small offset applied on top of the final position (for fine tuning)
  - Note: If a parent layout component drives positioning, offsets may be overridden or become unpredictable

### CreateButton

```csharp
public UiRef<Button> CreateButton(
    string id,
    LocalText text,
    Action onClick,
    bool showInGame = true,
    Vector2? preferredSize = null,
    Vector2? anchoredPositionOffset = null)
```

- `text`: Button text (Chinese/English)
- `onClick`: Click callback

### CreateLabel

```csharp
public UiRef<TMP_Text> CreateLabel(
    string id,
    LocalText text,
    bool showInGame = true,
    Vector2? preferredSize = null,
    Vector2? anchoredPositionOffset = null)
```

- `text`: Label text (Chinese/English)

### CreateToggle

```csharp
public UiRef<UISettingToggle> CreateToggle(
    string id,
    string settingKey,
    LocalText title,
    bool initialValue,
    Action<bool> onChanged,
    bool showInGame = true,
    Vector2? preferredSize = null,
    Vector2? anchoredPositionOffset = null)
```

- `title`: Control title (Chinese/English)
- `initialValue`: Initial value
- `onChanged`: Callback when the value changes
- `settingKey`: Persistent key (see “What is settingKey?” below)

### CreateDropdownString

```csharp
public UiRef<UISettingDropdownString> CreateDropdownString(
    string id,
    string settingKey,
    LocalText title,
    IList<string> options,
    string initialValue,
    Action<string> onChanged,
    bool showInGame = true,
    Vector2? preferredSize = null,
    Vector2? anchoredPositionOffset = null)
```

- `options`: Dropdown options (string list)
- `initialValue`: Initially selected item
- `onChanged`: Callback when selection changes
- `settingKey`: Persistent key

### CreateSliderInt

```csharp
public UiRef<UICustomSlider> CreateSliderInt(
    string id,
    string settingKey,
    LocalText title,
    int minValue,
    int maxValue,
    int initialValue,
    Action<int> onChanged,
    bool showInGame = true,
    Vector2? preferredSize = null,
    Vector2? anchoredPositionOffset = null)
```

- `minValue / maxValue`: Integer range
- `initialValue`: Initial value (clamped into the range)
- `onChanged`: Callback when value changes
- `settingKey`: Persistent key

### CreateInputString (Input Field)

```csharp
public UiRef<TMP_InputField> CreateInputString(
    string id,
    string settingKey,
    LocalText title,
    string initialValue,
    Action<string> onChanged,
    bool showInGame = true,
    Vector2? preferredSize = null,
    Vector2? anchoredPositionOffset = null)
```

Implementation notes (as in the example):
- Clones from the Slider template
- Deletes `Value`, `Fill Area`, `Handle` under the `Slider` child
- Renames the `Slider` child to `Input`
- Destroys the `YAPYAP.UICustomSlider` component on the root
- Ensures the `InputField` object is active (the template may keep it disabled by default)

Data behavior:
- If `settingKey` is non-empty: initial text uses `PlayerPrefs.GetString(settingKey, initialValue)`
- On `onEndEdit`: saves `PlayerPrefs.SetString(settingKey, v)` and calls `onChanged(v)`

---

## What is settingKey?

`settingKey` is the **persistent key name** used by the game’s settings system (effectively a `PlayerPrefs` key).

Recommended practice:
- Use a unique key per control
- Prefix with your ModId to avoid collisions with other mods or the base game

Examples:
- `com.yourmod.Toggle.Enabled`
- `com.yourmod.Dropdown.Mode`
- `com.yourmod.Input.Note`

---

## Common Usage Examples

### Update a label at runtime (and prevent localization from overriding it)

```csharp
UiRef<TMP_Text> labelRef = tab.CreateLabel(
    id: "Label_Runtime",
    text: new LocalText("RUNTIME", "等待加载", "Waiting for UI")
);

labelRef.Ready += tmp =>
{
    NativeSettingsUI.UnbindText(tmp);
    tmp.text = "Runtime label ready.";
};
```

### Show a dropdown only in the main menu (not in-game)

```csharp
tab.CreateDropdownString(
    id: "Dropdown_Mode",
    settingKey: "com.yourmod.Mode",
    title: new LocalText("MODE", "模式", "Mode"),
    options: new [] { "Normal", "Hard" },
    initialValue: "Normal",
    onChanged: v => { },
    showInGame: false
);
```

---

## Author

- Author: XiaoHai (小海)
- Bilibili: https://space.bilibili.com/2055787437
- Email: 515250418@qq.com
