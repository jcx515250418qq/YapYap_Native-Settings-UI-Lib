using System;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using Yap_NativeSettingsUI;

namespace Yap_NativeSettingsUI_Demo
{
    [BepInPlugin("com.yapyap.nativesettingsui.demo", "Yap Native Settings UI Demo", "1.0.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        private SettingsTab tab;

        private void Awake()
        {
            Log = Logger;

            tab = NativeSettingsUI.RegisterTab(
                "NSUI_DEMO",
                new LocalText("NSUI_DEMO_TITLE", "UI 组件示例", "UI Component Demo"),
                showInGame: true);

            tab.CreateLabel(
                "label_intro",
                new LocalText("NSUI_DEMO_LABEL_INTRO", "下面是所有组件的演示", "All controls demo below"),
                showInGame: true);

            tab.CreateButton(
                "btn_ping",
                new LocalText("NSUI_DEMO_BTN_PING", "点我输出日志", "Click to log"),
                () => Log.LogInfo("Demo Button Clicked"),
                showInGame: true);

            tab.CreateToggle(
                "toggle_enabled",
                "Settings_NSUI_Demo_Enabled",
                new LocalText("NSUI_DEMO_TOGGLE_ENABLED", "启用示例开关", "Enable demo toggle"),
                initialValue: true,
                onChanged: v => Log.LogInfo("Demo Toggle Changed: " + v),
                showInGame: true);

            tab.CreateDropdownString(
                "dropdown_mode",
                "Settings_NSUI_Demo_Mode",
                new LocalText("NSUI_DEMO_DROPDOWN_MODE", "示例下拉", "Demo dropdown"),
                options: new[] { "A", "B", "C" },
                initialValue: "A",
                onChanged: v => Log.LogInfo("Demo Dropdown Changed: " + v),
                showInGame: true);

            tab.CreateSliderInt(
                "slider_power",
                "Settings_NSUI_Demo_Power",
                new LocalText("NSUI_DEMO_SLIDER_POWER", "示例滑条(整数)", "Demo slider (int)"),
                minValue: 0,
                maxValue: 100,
                initialValue: 50,
                onChanged: v => Log.LogInfo("Demo Slider Changed: " + v),
                showInGame: true);

            tab.CreateInputString(
                "input_text",
                "Settings_NSUI_Demo_Text",
                new LocalText("NSUI_DEMO_INPUT_TEXT", "示例输入框", "Demo input"),
                initialValue: "Hello",
                onChanged: v => Log.LogInfo("Demo Input Changed: " + v),
                showInGame: true);
        }
    }
}

