using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YAPYAP;

namespace Yap_NativeSettingsUI
{
    [BepInPlugin("com.yapyap.nativesettingsui", "Yap Native Settings UI", "1.0.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        private static Plugin instance;
        private SystemLanguage lastLanguage;
        private int languagePollCountdown;

        private void Awake()
        {
            instance = this;
            Log = Logger;
            NativeSettingsUI.InternalInitialize(this);
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private void Update()
        {
            if (languagePollCountdown > 0)
            {
                languagePollCountdown--;
                return;
            }

            languagePollCountdown = 10;
            var lang = NativeSettingsUI.GetCurrentLanguage();
            if (lang != lastLanguage)
            {
                lastLanguage = lang;
                NativeSettingsUI.NotifyLanguageChanged(lang);
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartCoroutine(InjectWhenReady(isInGame: false));
            StartCoroutine(InjectWhenReady(isInGame: true));
        }

        private IEnumerator InjectWhenReady(bool isInGame)
        {
            UISettings uiSettings = null;

            if (!isInGame)
            {
                MenuController menuController = null;
                for (int i = 0; i < 60; i++)
                {
                    menuController = UnityEngine.Object.FindFirstObjectByType<MenuController>();
                    if (menuController != null)
                    {
                        break;
                    }
                    yield return null;
                }

                if (menuController == null)
                {
                    yield break;
                }

                uiSettings = NativeSettingsUI.TryGetUiSettings(menuController);
            }
            else
            {
                GameController gameController = null;
                for (int i = 0; i < 60; i++)
                {
                    gameController = UnityEngine.Object.FindFirstObjectByType<GameController>();
                    if (gameController != null)
                    {
                        break;
                    }
                    yield return null;
                }

                if (gameController == null)
                {
                    yield break;
                }

                uiSettings = NativeSettingsUI.TryGetUiSettings(gameController);
            }

            if (uiSettings == null)
            {
                yield break;
            }

            NativeSettingsUI.Inject(uiSettings, isInGame);
        }
    }

    public readonly struct LocalText
    {
        public readonly string Key;
        public readonly string Chinese;
        public readonly string English;

        public LocalText(string key, string chinese, string english)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (chinese == null) throw new ArgumentNullException(nameof(chinese));
            if (english == null) throw new ArgumentNullException(nameof(english));
            Key = key;
            Chinese = chinese;
            English = english;
        }
    }

    public sealed class UiRef<T> where T : UnityEngine.Object
    {
        public T Value { get; private set; }
        public bool IsReady => Value != null;

        public event Action<T> Ready;

        internal void SetValue(T value)
        {
            if (value == null)
            {
                return;
            }
            Value = value;
            Ready?.Invoke(value);
        }
    }

    public sealed class SettingsTab
    {
        internal readonly string Guid;
        internal readonly LocalText Title;
        internal readonly bool ShowInGame;
        private readonly List<IControlDefinition> controls = new List<IControlDefinition>();

        internal SettingsTab(string guid, LocalText title, bool showInGame)
        {
            Guid = guid;
            Title = title;
            ShowInGame = showInGame;
        }

        public UiRef<Button> CreateButton(string id, LocalText text, Action onClick, bool showInGame = true, Vector2? preferredSize = null, Vector2? anchoredPositionOffset = null)
        {
            var uiRef = new UiRef<Button>();
            controls.Add(new ButtonDefinition(id, text, onClick, showInGame, preferredSize, anchoredPositionOffset, uiRef));
            NativeSettingsUI.TryBuildTabIntoExistingContexts(this);
            return uiRef;
        }

        public UiRef<TMP_Text> CreateLabel(string id, LocalText text, bool showInGame = true, Vector2? preferredSize = null, Vector2? anchoredPositionOffset = null)
        {
            var uiRef = new UiRef<TMP_Text>();
            controls.Add(new LabelDefinition(id, text, showInGame, preferredSize, anchoredPositionOffset, uiRef));
            NativeSettingsUI.TryBuildTabIntoExistingContexts(this);
            return uiRef;
        }

        public UiRef<UISettingToggle> CreateToggle(string id, string settingKey, LocalText title, bool initialValue, Action<bool> onChanged, bool showInGame = true, Vector2? preferredSize = null, Vector2? anchoredPositionOffset = null)
        {
            var uiRef = new UiRef<UISettingToggle>();
            controls.Add(new ToggleDefinition(id, settingKey, title, initialValue, onChanged, showInGame, preferredSize, anchoredPositionOffset, uiRef));
            NativeSettingsUI.TryBuildTabIntoExistingContexts(this);
            return uiRef;
        }

        public UiRef<UISettingDropdownString> CreateDropdownString(string id, string settingKey, LocalText title, IList<string> options, string initialValue, Action<string> onChanged, bool showInGame = true, Vector2? preferredSize = null, Vector2? anchoredPositionOffset = null)
        {
            var uiRef = new UiRef<UISettingDropdownString>();
            controls.Add(new DropdownStringDefinition(id, settingKey, title, options, initialValue, onChanged, showInGame, preferredSize, anchoredPositionOffset, uiRef));
            NativeSettingsUI.TryBuildTabIntoExistingContexts(this);
            return uiRef;
        }

        public UiRef<UICustomSlider> CreateSliderInt(string id, string settingKey, LocalText title, int minValue, int maxValue, int initialValue, Action<int> onChanged, bool showInGame = true, Vector2? preferredSize = null, Vector2? anchoredPositionOffset = null)
        {
            var uiRef = new UiRef<UICustomSlider>();
            controls.Add(new SliderDefinition(id, settingKey, title, minValue, maxValue, initialValue, onChanged, showInGame, preferredSize, anchoredPositionOffset, uiRef));
            NativeSettingsUI.TryBuildTabIntoExistingContexts(this);
            return uiRef;
        }

        public UiRef<TMP_InputField> CreateInputString(string id, string settingKey, LocalText title, string initialValue, Action<string> onChanged, bool showInGame = true, Vector2? preferredSize = null, Vector2? anchoredPositionOffset = null)
        {
            var uiRef = new UiRef<TMP_InputField>();
            controls.Add(new InputStringDefinition(id, settingKey, title, initialValue, onChanged, showInGame, preferredSize, anchoredPositionOffset, uiRef));
            NativeSettingsUI.TryBuildTabIntoExistingContexts(this);
            return uiRef;
        }

        internal IReadOnlyList<IControlDefinition> GetControls() => controls;
    }

    public static class NativeSettingsUI
    {
        private sealed class UiSettingsContext
        {
            public readonly UISettings UiSettings;
            public readonly Transform ContentRoot;
            public readonly bool IsInGame;

            public UiSettingsContext(UISettings uiSettings, Transform contentRoot, bool isInGame)
            {
                UiSettings = uiSettings;
                ContentRoot = contentRoot;
                IsInGame = isInGame;
            }
        }

        private static Plugin plugin;
        private static readonly List<SettingsTab> tabs = new List<SettingsTab>();
        private static readonly List<UiSettingsContext> contexts = new List<UiSettingsContext>();
        internal static SystemLanguage CurrentLanguage { get; private set; } = Application.systemLanguage;
        internal static event Action<SystemLanguage> LanguageChanged;

        public static void BindText(TMP_Text tmp, LocalText text)
        {
            if (tmp == null)
            {
                return;
            }
            LocalTextBinder.Bind(tmp, text);
        }

        public static void UnbindText(TMP_Text tmp)
        {
            if (tmp == null)
            {
                return;
            }

            var binder = tmp.GetComponent<LocalTextBinder>();
            if (binder != null)
            {
                UnityEngine.Object.Destroy(binder);
            }
        }

        public static SettingsTab RegisterTab(string guid, LocalText title, bool showInGame)
        {
            if (string.IsNullOrWhiteSpace(guid))
            {
                guid = Guid.NewGuid().ToString("N");
            }

            for (int i = 0; i < tabs.Count; i++)
            {
                if (string.Equals(tabs[i].Guid, guid, StringComparison.Ordinal))
                {
                    return tabs[i];
                }
            }

            var tab = new SettingsTab(guid, title, showInGame);
            tabs.Add(tab);
            TryBuildTabIntoExistingContexts(tab);
            return tab;
        }

        internal static void InternalInitialize(Plugin hostPlugin)
        {
            plugin = hostPlugin;
            CurrentLanguage = GetCurrentLanguage();
            NotifyLanguageChanged(CurrentLanguage);
        }

        internal static SystemLanguage GetCurrentLanguage()
        {
            SystemLanguage language = Application.systemLanguage;
            LocalisationManager localisationManager;
            if (Service.Get<LocalisationManager>(out localisationManager) && localisationManager != null)
            {
                var translator = localisationManager.CurrentTranslator ?? localisationManager.DefaultTranslator;
                if (translator != null)
                {
                    language = translator.Language;
                }
            }
            return language;
        }

        internal static void NotifyLanguageChanged(SystemLanguage language)
        {
            CurrentLanguage = language;
            LanguageChanged?.Invoke(language);
        }

        internal static UISettings TryGetUiSettings(Component controller)
        {
            if (controller == null)
            {
                return null;
            }

            var uiSettingsField = controller.GetType().GetField("_uiSettings", BindingFlags.Instance | BindingFlags.NonPublic);
            var uiSettings = uiSettingsField != null
                ? uiSettingsField.GetValue(controller) as UISettings
                : null;
            if (uiSettings == null)
            {
                uiSettings = controller.GetComponentInChildren<UISettings>(true);
            }
            return uiSettings;
        }

        internal static void Inject(UISettings uiSettings, bool isInGame)
        {
            if (uiSettings == null)
            {
                return;
            }

            Transform content = uiSettings.transform.Find("Window/Content");
            if (content == null)
            {
                return;
            }

            if (!TryAddContext(uiSettings, content, isInGame))
            {
                return;
            }

            for (int i = 0; i < tabs.Count; i++)
            {
                TryBuildTab(uiSettings, content, isInGame, tabs[i]);
            }
        }

        private static bool TryAddContext(UISettings uiSettings, Transform content, bool isInGame)
        {
            for (int i = 0; i < contexts.Count; i++)
            {
                var ctx = contexts[i];
                if (ctx.UiSettings == uiSettings && ctx.IsInGame == isInGame)
                {
                    return false;
                }
            }

            contexts.Add(new UiSettingsContext(uiSettings, content, isInGame));
            return true;
        }

        internal static void TryBuildTabIntoExistingContexts(SettingsTab tab)
        {
            if (tab == null)
            {
                return;
            }

            for (int i = contexts.Count - 1; i >= 0; i--)
            {
                var ctx = contexts[i];
                if (ctx.UiSettings == null || ctx.ContentRoot == null)
                {
                    contexts.RemoveAt(i);
                    continue;
                }

                TryBuildTab(ctx.UiSettings, ctx.ContentRoot, ctx.IsInGame, tab);
            }
        }

        private static void TryBuildTab(UISettings uiSettings, Transform content, bool isInGame, SettingsTab tab)
        {
            if (uiSettings == null || content == null || tab == null)
            {
                return;
            }

            if (isInGame && !tab.ShowInGame)
            {
                return;
            }

            if (!Templates.TryResolve(content, out var templates))
            {
                Plugin.Log?.LogWarning("NativeSettingsUI: UI templates not found; cannot build settings tab.");
                return;
            }

            var tabsRoot = content.Find("Tabs");
            var sectionsRoot = content.Find("SettingsSection");
            if (tabsRoot == null || sectionsRoot == null)
            {
                return;
            }

            string tabName = "Tab_" + tab.Guid;
            string sectionName = "Sec_" + tab.Guid;

            var tabTransform = EnsureTab(tabsRoot, templates.TabTemplate, tabName);
            var sectionTransform = EnsureSection(sectionsRoot, templates.SectionTemplate, sectionName);

            if (tabTransform == null || sectionTransform == null)
            {
                return;
            }

            ApplyTitle(tabTransform.gameObject, tab.Title, templates.Language);

            EnsureControls(sectionTransform, templates, isInGame, tab);
            EnsureSectionBinding(content, tabTransform, sectionTransform);
        }

        private static Transform EnsureTab(Transform tabsRoot, Transform template, string tabName)
        {
            var existing = tabsRoot.Find(tabName);
            if (existing != null)
            {
                return existing;
            }

            if (template == null)
            {
                return null;
            }

            var instance = UnityEngine.Object.Instantiate(template.gameObject, tabsRoot).transform;
            instance.name = tabName;
            instance.SetAsLastSibling();
            return instance;
        }

        private static RectTransform EnsureSection(Transform sectionsRoot, RectTransform template, string sectionName)
        {
            var existing = sectionsRoot.Find(sectionName) as RectTransform;
            if (existing != null)
            {
                EnsureSectionHasPlaceholder(existing);
                CopyLayoutComponents(template, existing);
                return existing;
            }

            var obj = new GameObject(sectionName, typeof(RectTransform));
            var rect = obj.GetComponent<RectTransform>();

            if (template != null)
            {
                rect.anchorMin = template.anchorMin;
                rect.anchorMax = template.anchorMax;
                rect.pivot = template.pivot;
                rect.sizeDelta = template.sizeDelta;
                rect.localPosition = template.localPosition;
                rect.localRotation = template.localRotation;
                rect.localScale = template.localScale;
                CopyLayoutComponents(template, rect);
            }

            rect.SetParent(sectionsRoot, false);
            obj.SetActive(template != null && template.gameObject.activeSelf);
            EnsureSectionHasPlaceholder(rect);
            return rect;
        }

        private static void EnsureSectionHasPlaceholder(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            if (rect.childCount > 0)
            {
                return;
            }

            var placeholderObj = new GameObject("Placeholder", typeof(RectTransform));
            var placeholderRect = placeholderObj.GetComponent<RectTransform>();
            placeholderRect.SetParent(rect, false);
        }

        private static void CopyLayoutComponents(RectTransform from, RectTransform to)
        {
            if (from == null || to == null)
            {
                return;
            }

            var components = from.GetComponents<Component>();
            foreach (var src in components)
            {
                if (src is RectTransform || src.GetType().Name == "CanvasRenderer")
                {
                    continue;
                }

                var type = src.GetType();
                if (!typeof(UnityEngine.UI.LayoutGroup).IsAssignableFrom(type) && type != typeof(UnityEngine.UI.ContentSizeFitter))
                {
                    continue;
                }

                var dst = to.GetComponent(type) ?? to.gameObject.AddComponent(type);

                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var field in fields)
                {
                    if (field.IsInitOnly)
                    {
                        continue;
                    }
                    field.SetValue(dst, field.GetValue(src));
                }
            }
        }

        private static void EnsureControls(Transform section, Templates templates, bool isInGame, SettingsTab tab)
        {
            if (section == null)
            {
                return;
            }

            var placeholder = section.Find("Placeholder");
            if (placeholder != null)
            {
                UnityEngine.Object.Destroy(placeholder.gameObject);
            }

            var controls = tab.GetControls();
            bool anyCustomSize = false;
            for (int i = 0; i < controls.Count; i++)
            {
                var def = controls[i];
                if (def == null)
                {
                    continue;
                }
                if (def.PreferredSize != null)
                {
                    anyCustomSize = true;
                    break;
                }
            }

            if (!anyCustomSize)
            {
                for (int i = 0; i < controls.Count; i++)
                {
                    var def = controls[i];
                    if (def == null)
                    {
                        continue;
                    }
                    if (isInGame && !def.ShowInGame)
                    {
                        continue;
                    }
                    def.Build(section, section, templates);
                }
                return;
            }

            SectionLayout.DisableConflictingLayouts(section);
            Canvas.ForceUpdateCanvases();

            int rowPadLeft = templates.ContentInsetLeft > 0 ? templates.ContentInsetLeft : templates.GridPaddingLeft;
            int rowPadRight = templates.ContentInsetRight > 0 ? templates.ContentInsetRight : templates.GridPaddingRight;

            float sectionWidth = templates.GridCellSize.x * 2f + templates.GridSpacing.x;
            var sectionRect = section as RectTransform;
            if (sectionRect != null)
            {
                sectionRect.anchorMin = new Vector2(0f, sectionRect.anchorMin.y);
                sectionRect.anchorMax = new Vector2(1f, sectionRect.anchorMax.y);
                sectionRect.pivot = new Vector2(templates.SectionPivotX, sectionRect.pivot.y);
                sectionRect.offsetMin = new Vector2(templates.SectionOffsetMinX, sectionRect.offsetMin.y);
                sectionRect.offsetMax = new Vector2(templates.SectionOffsetMaxX, sectionRect.offsetMax.y);
                sectionRect.sizeDelta = new Vector2(0f, sectionRect.sizeDelta.y);
                Canvas.ForceUpdateCanvases();

                float w = sectionRect.rect.width;
                if (w > 0.01f)
                {
                    sectionWidth = w;
                }

                var parentRect = sectionRect.parent as RectTransform;
                if (parentRect != null)
                {
                    float pw = parentRect.rect.width;
                    if (pw > sectionWidth + 0.01f)
                    {
                        sectionWidth = pw;
                    }
                }
            }

            float availableContentWidth = sectionWidth - rowPadLeft - rowPadRight;
            if (availableContentWidth < templates.GridCellSize.x)
            {
                availableContentWidth = templates.GridCellSize.x * 2f + templates.GridSpacing.x;
            }

            float computedColumnWidth = (availableContentWidth - templates.GridSpacing.x) * 0.5f;
            if (computedColumnWidth < 1f)
            {
                computedColumnWidth = templates.GridCellSize.x;
            }

            int rowIndex = 0;
            float y = -templates.GridPaddingTop;

            bool hasOpenTwoColumnRow = false;
            Transform openRow = null;
            Transform openLeft = null;
            Transform openRight = null;
            float openRowHeight = templates.GridCellSize.y;
            bool openLeftUsed = false;
            bool openRightUsed = false;
            var deferredFullRows = new Queue<IControlDefinition>();

            void CloseOpenRow()
            {
                hasOpenTwoColumnRow = false;
                openRow = null;
                openLeft = null;
                openRight = null;
                openRowHeight = templates.GridCellSize.y;
                openLeftUsed = false;
                openRightUsed = false;
            }

            void FlushDeferredFullRows()
            {
                while (deferredFullRows.Count > 0)
                {
                    var deferred = deferredFullRows.Dequeue();
                    if (deferred == null)
                    {
                        continue;
                    }

                    float rowHeight = GetDesiredHeight(deferred);
                    var full = SectionLayout.EnsureFullRow(section, rowIndex++, templates, availableContentWidth, rowHeight, y);
                    y -= rowHeight + templates.GridSpacing.y;
                    deferred.Build(section, full, templates);
                }
            }

            void EnsureOpenRow(float requiredHeight)
            {
                if (!hasOpenTwoColumnRow)
                {
                    openRowHeight = Mathf.Max(templates.GridCellSize.y, requiredHeight);
                    var rowSlots = SectionLayout.EnsureTwoColumnRow(section, rowIndex++, templates, computedColumnWidth, openRowHeight, y);
                    openRow = rowSlots.Row;
                    openLeft = rowSlots.Left;
                    openRight = rowSlots.Right;
                    y -= openRowHeight + templates.GridSpacing.y;
                    hasOpenTwoColumnRow = true;
                    openLeftUsed = false;
                    openRightUsed = false;
                    return;
                }

                float desired = Mathf.Max(openRowHeight, requiredHeight);
                if (desired > openRowHeight + 0.01f)
                {
                    openRowHeight = desired;
                    SectionLayout.SetRowHeight(openRow, openRowHeight);
                    SectionLayout.SetSlotHeight(openLeft, openRowHeight);
                    SectionLayout.SetSlotHeight(openRight, openRowHeight);
                }
            }

            bool IsFullRow(IControlDefinition def)
            {
                if (def == null || def.PreferredSize == null)
                {
                    return false;
                }
                return def.PreferredSize.Value.x > computedColumnWidth + 0.01f;
            }

            float GetDesiredHeight(IControlDefinition def)
            {
                if (def == null || def.PreferredSize == null)
                {
                    return templates.GridCellSize.y;
                }
                var h = def.PreferredSize.Value.y;
                if (h <= 0f)
                {
                    return templates.GridCellSize.y;
                }
                return h;
            }

            for (int i = 0; i < controls.Count; i++)
            {
                var def = controls[i];
                if (def == null)
                {
                    continue;
                }
                if (isInGame && !def.ShowInGame)
                {
                    continue;
                }

                float desiredHeight = GetDesiredHeight(def);
                bool isFull = IsFullRow(def);
                if (isFull && !hasOpenTwoColumnRow)
                {
                    var full = SectionLayout.EnsureFullRow(section, rowIndex++, templates, availableContentWidth, desiredHeight, y);
                    y -= desiredHeight + templates.GridSpacing.y;
                    def.Build(section, full, templates);
                    continue;
                }

                EnsureOpenRow(desiredHeight);

                if (!openLeftUsed)
                {
                    def.Build(section, openLeft, templates);
                    openLeftUsed = true;
                    continue;
                }

                if (!openRightUsed)
                {
                    if (isFull)
                    {
                        deferredFullRows.Enqueue(def);
                        continue;
                    }

                    def.Build(section, openRight, templates);
                    openRightUsed = true;
                    CloseOpenRow();
                    FlushDeferredFullRows();
                }
            }

            if (hasOpenTwoColumnRow)
            {
                CloseOpenRow();
            }
            FlushDeferredFullRows();
            SectionLayout.DisableExtraRows(section, rowIndex);
        }

        private static void EnsureSectionBinding(Transform content, Transform tab, Transform section)
        {
            var uiSettings = content.GetComponentInParent<UISettings>();
            if (uiSettings == null)
            {
                return;
            }

            var sectionsField = typeof(UISettings).GetField("sections", BindingFlags.Instance | BindingFlags.NonPublic);
            if (sectionsField == null)
            {
                return;
            }

            var sectionsValue = sectionsField.GetValue(uiSettings);
            var list = sectionsValue as IList;
            if (list == null)
            {
                return;
            }

            var sectionType = typeof(UISettings).GetNestedType("SettingsSection", BindingFlags.Public);
            if (sectionType == null)
            {
                return;
            }

            var sectionObjField = sectionType.GetField("SectionObj", BindingFlags.Instance | BindingFlags.Public);
            var tabButtonField = sectionType.GetField("TabButton", BindingFlags.Instance | BindingFlags.Public);
            var indicatorField = sectionType.GetField("Indictor", BindingFlags.Instance | BindingFlags.Public);
            if (sectionObjField == null || tabButtonField == null || indicatorField == null)
            {
                return;
            }

            foreach (var item in list)
            {
                var sectionObj = sectionObjField.GetValue(item) as GameObject;
                if (sectionObj == section.gameObject)
                {
                    return;
                }
            }

            var newSection = Activator.CreateInstance(sectionType);
            sectionObjField.SetValue(newSection, section.gameObject);

            var button = tab.GetComponent<Button>();
            if (button == null)
            {
                return;
            }
            tabButtonField.SetValue(newSection, button);

            var indicator = tab.GetComponentInChildren<UIFader>(true);
            indicatorField.SetValue(newSection, indicator);

            if (list.IsFixedSize)
            {
                var array = sectionsValue as Array;
                if (array == null)
                {
                    return;
                }

                int length = array.Length;
                var newArray = Array.CreateInstance(sectionType, length + 1);
                for (int i = 0; i < length; i++)
                {
                    newArray.SetValue(array.GetValue(i), i);
                }
                newArray.SetValue(newSection, length);
                sectionsField.SetValue(uiSettings, newArray);
            }
            else
            {
                list.Add(newSection);
            }

            var initMethod = typeof(UISettings).GetMethod("InitSections", BindingFlags.Instance | BindingFlags.NonPublic);
            initMethod?.Invoke(uiSettings, null);
        }

        private static void ApplyTitle(GameObject root, LocalText title, SystemLanguage language)
        {
            if (root == null)
            {
                return;
            }

            var localised = root.GetComponentInChildren<LocalisedTMP>(true);
            if (localised != null)
            {
                UnityEngine.Object.Destroy(localised);
            }

            var text = root.GetComponentInChildren<TMP_Text>(true);
            if (text == null)
            {
                return;
            }

            LocalTextBinder.Bind(text, title);
        }
    }

    internal sealed class LocalTextBinder : MonoBehaviour
    {
        private TMP_Text tmp;
        private string zh;
        private string en;

        internal static void Bind(TMP_Text tmp, LocalText text)
        {
            if (tmp == null)
            {
                return;
            }

            var existing = tmp.GetComponent<LocalTextBinder>();
            if (existing == null)
            {
                existing = tmp.gameObject.AddComponent<LocalTextBinder>();
            }

            existing.tmp = tmp;
            existing.zh = text.Chinese;
            existing.en = text.English;
            existing.Apply(NativeSettingsUI.CurrentLanguage);
        }

        private void OnEnable()
        {
            NativeSettingsUI.LanguageChanged += OnLanguageChanged;
            Apply(NativeSettingsUI.CurrentLanguage);
        }

        private void OnDisable()
        {
            NativeSettingsUI.LanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged(SystemLanguage language)
        {
            Apply(language);
        }

        private void Apply(SystemLanguage language)
        {
            if (tmp == null)
            {
                tmp = GetComponent<TMP_Text>();
                if (tmp == null)
                {
                    return;
                }
            }

            bool isChinese =
                language == SystemLanguage.Chinese ||
                language == SystemLanguage.ChineseSimplified ||
                language == SystemLanguage.ChineseTraditional;

            if (isChinese)
            {
                tmp.text = !string.IsNullOrEmpty(zh) ? zh : en;
            }
            else
            {
                tmp.text = !string.IsNullOrEmpty(en) ? en : zh;
            }
        }
    }

    internal static class LocalTextUtil
    {
        internal static string Resolve(LocalText text, SystemLanguage language)
        {
            bool isChinese =
                language == SystemLanguage.Chinese ||
                language == SystemLanguage.ChineseSimplified ||
                language == SystemLanguage.ChineseTraditional;

            if (isChinese)
            {
                if (!string.IsNullOrEmpty(text.Chinese))
                {
                    return text.Chinese;
                }
                return text.English;
            }

            if (!string.IsNullOrEmpty(text.English))
            {
                return text.English;
            }
            return text.Chinese;
        }

        internal static void ApplyTo(GameObject root, LocalText text, SystemLanguage language)
        {
            if (root == null)
            {
                return;
            }

            var localised = root.GetComponent<LocalisedTMP>();
            if (localised != null)
            {
                UnityEngine.Object.Destroy(localised);
            }

            var tmp = root.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                LocalTextBinder.Bind(tmp, text);
            }
        }
    }

    internal readonly struct Templates
    {
        public readonly RectTransform SectionTemplate;
        public readonly Transform TabTemplate;
        public readonly Transform SliderTemplate;
        public readonly Transform ToggleTemplate;
        public readonly Transform DropdownTemplate;
        public readonly Transform ButtonTemplate;
        public readonly Transform LabelTemplate;
        public readonly SystemLanguage Language;
        public readonly Vector2 GridCellSize;
        public readonly Vector2 GridSpacing;
        public readonly int GridPaddingLeft;
        public readonly int GridPaddingRight;
        public readonly int GridPaddingTop;
        public readonly int GridPaddingBottom;
        public readonly GridLayoutGroup.Constraint GridConstraint;
        public readonly int GridConstraintCount;
        public readonly float SectionOffsetMinX;
        public readonly float SectionOffsetMaxX;
        public readonly float SectionPivotX;
        public readonly int ContentInsetLeft;
        public readonly int ContentInsetRight;

        private Templates(
            RectTransform sectionTemplate,
            Transform tabTemplate,
            Transform sliderTemplate,
            Transform toggleTemplate,
            Transform dropdownTemplate,
            Transform buttonTemplate,
            Transform labelTemplate,
            SystemLanguage language,
            Vector2 gridCellSize,
            Vector2 gridSpacing,
            int gridPaddingLeft,
            int gridPaddingRight,
            int gridPaddingTop,
            int gridPaddingBottom,
            GridLayoutGroup.Constraint gridConstraint,
            int gridConstraintCount,
            float sectionOffsetMinX,
            float sectionOffsetMaxX,
            float sectionPivotX,
            int contentInsetLeft,
            int contentInsetRight)
        {
            SectionTemplate = sectionTemplate;
            TabTemplate = tabTemplate;
            SliderTemplate = sliderTemplate;
            ToggleTemplate = toggleTemplate;
            DropdownTemplate = dropdownTemplate;
            ButtonTemplate = buttonTemplate;
            LabelTemplate = labelTemplate;
            Language = language;
            GridCellSize = gridCellSize;
            GridSpacing = gridSpacing;
            GridPaddingLeft = gridPaddingLeft;
            GridPaddingRight = gridPaddingRight;
            GridPaddingTop = gridPaddingTop;
            GridPaddingBottom = gridPaddingBottom;
            GridConstraint = gridConstraint;
            GridConstraintCount = gridConstraintCount;
            SectionOffsetMinX = sectionOffsetMinX;
            SectionOffsetMaxX = sectionOffsetMaxX;
            SectionPivotX = sectionPivotX;
            ContentInsetLeft = contentInsetLeft;
            ContentInsetRight = contentInsetRight;
        }

        public static bool TryResolve(Transform content, out Templates templates)
        {
            templates = default;
            if (content == null)
            {
                return false;
            }

            var tabsRoot = content.Find("Tabs");
            var sectionsRoot = content.Find("SettingsSection");
            if (tabsRoot == null || sectionsRoot == null)
            {
                return false;
            }

            Transform tabTemplate = tabsRoot.Find("Tab_General");

            var sectionTemplate = sectionsRoot.Find("Sec_Audio") as RectTransform;

            var audioSection = content.Find("SettingsSection/Sec_Audio");
            Transform sliderTemplate = audioSection != null ? audioSection.Find("MasterVolume") : null;
            Transform dropdownTemplate = audioSection != null ? audioSection.Find("Microphone") : null;
            Transform toggleTemplate = audioSection != null ? audioSection.Find("PushToTalk") : null;

            var uiRoot = content.root;
            Transform closeButton = null;
            if (uiRoot != null)
            {
                closeButton = uiRoot.Find("UISettings/Window/Content/ButtonContainer/Close");
            }

            Transform labelTemplate = null;
            if (sliderTemplate != null)
            {
                labelTemplate = sliderTemplate.Find("Title");
            }

            SystemLanguage language = NativeSettingsUI.GetCurrentLanguage();

            if (tabTemplate == null || sectionTemplate == null || sliderTemplate == null || dropdownTemplate == null || toggleTemplate == null || closeButton == null || labelTemplate == null)
            {
                return false;
            }

            float sectionOffsetMinX = sectionTemplate.offsetMin.x;
            float sectionOffsetMaxX = sectionTemplate.offsetMax.x;
            float sectionPivotX = sectionTemplate.pivot.x;

            Vector2 cellSize = new Vector2(300f, 60f);
            Vector2 spacing = new Vector2(10f, 10f);
            int padLeft = 0;
            int padRight = 0;
            int padTop = 0;
            int padBottom = 0;
            GridLayoutGroup.Constraint constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            int constraintCount = 2;

            var grid = sectionTemplate.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                cellSize = grid.cellSize;
                spacing = grid.spacing;
                if (grid.padding != null)
                {
                    padLeft = grid.padding.left;
                    padRight = grid.padding.right;
                    padTop = grid.padding.top;
                    padBottom = grid.padding.bottom;
                }
                constraint = grid.constraint;
                constraintCount = grid.constraintCount;
            }

            int contentInsetLeft = padLeft;
            int contentInsetRight = padRight;
            var sliderRect = sliderTemplate as RectTransform;
            if (sliderRect != null)
            {
                var sectionCorners = new Vector3[4];
                var childCorners = new Vector3[4];
                sectionTemplate.GetWorldCorners(sectionCorners);
                sliderRect.GetWorldCorners(childCorners);
                float sectionLeftLocal = sectionTemplate.InverseTransformPoint(sectionCorners[0]).x;
                float sectionRightLocal = sectionTemplate.InverseTransformPoint(sectionCorners[2]).x;
                float childLeftLocal = sectionTemplate.InverseTransformPoint(childCorners[0]).x;
                float childRightLocal = sectionTemplate.InverseTransformPoint(childCorners[2]).x;
                int derivedLeft = Mathf.Max(0, Mathf.RoundToInt(childLeftLocal - sectionLeftLocal));
                int derivedRight = Mathf.Max(0, Mathf.RoundToInt(sectionRightLocal - childRightLocal));
                if (derivedLeft > 0)
                {
                    contentInsetLeft = derivedLeft;
                }
                if (derivedRight > 0)
                {
                    contentInsetRight = derivedRight;
                }
            }

            contentInsetLeft = Mathf.Max(contentInsetLeft, 30);
            contentInsetRight = Mathf.Max(contentInsetRight, 30);

            templates = new Templates(
                sectionTemplate,
                tabTemplate,
                sliderTemplate,
                toggleTemplate,
                dropdownTemplate,
                closeButton,
                labelTemplate,
                language,
                cellSize,
                spacing,
                padLeft,
                padRight,
                padTop,
                padBottom,
                constraint,
                constraintCount,
                sectionOffsetMinX,
                sectionOffsetMaxX,
                sectionPivotX,
                contentInsetLeft,
                contentInsetRight);
            return true;
        }
    }

    internal interface IControlDefinition
    {
        string Id { get; }
        bool ShowInGame { get; }
        Vector2? PreferredSize { get; }
        void Build(Transform sectionRoot, Transform parent, Templates templates);
    }

    internal static class SectionLayout
    {
        private const string RowPrefix = "NSUI_Row_";
        private const string TwoLeftSlotName = "NSUI_Slot_Left";
        private const string TwoRightSlotName = "NSUI_Slot_Right";
        private const string FullSlotName = "NSUI_Slot_Full";

        internal readonly struct RowSlots
        {
            public readonly Transform Row;
            public readonly Transform Left;
            public readonly Transform Right;

            public RowSlots(Transform row, Transform left, Transform right)
            {
                Row = row;
                Left = left;
                Right = right;
            }
        }

        internal static void DisableConflictingLayouts(Transform section)
        {
            if (section == null)
            {
                return;
            }

            var groups = section.GetComponents<LayoutGroup>();
            for (int i = 0; i < groups.Length; i++)
            {
                groups[i].enabled = false;
            }
        }

        internal static RowSlots EnsureTwoColumnRow(Transform section, int rowIndex, Templates templates, float columnWidth, float rowHeight, float y)
        {
            var row = EnsureRowBase(section, rowIndex, rowHeight, y);

            var full = row.Find(FullSlotName);
            if (full != null)
            {
                full.gameObject.SetActive(false);
            }

            var left = EnsureSlot(row, TwoLeftSlotName, columnWidth, rowHeight);
            var right = EnsureSlot(row, TwoRightSlotName, columnWidth, rowHeight);

            EnsureRowSlotLayout(row, templates);
            return new RowSlots(row, left, right);
        }

        internal static Transform EnsureFullRow(Transform section, int rowIndex, Templates templates, float fullRowWidth, float rowHeight, float y)
        {
            var row = EnsureRowBase(section, rowIndex, rowHeight, y);

            var left = row.Find(TwoLeftSlotName);
            if (left != null)
            {
                left.gameObject.SetActive(false);
            }
            var right = row.Find(TwoRightSlotName);
            if (right != null)
            {
                right.gameObject.SetActive(false);
            }

            var slot = EnsureSlot(row, FullSlotName, fullRowWidth, rowHeight);

            EnsureRowSingleSlotLayout(row, templates);
            return slot;
        }

        internal static void SetRowHeight(Transform row, float rowHeight)
        {
            if (row == null)
            {
                return;
            }

            var rect = row as RectTransform;
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, rowHeight);
            }

            var layout = row.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.preferredHeight = rowHeight;
                layout.minHeight = rowHeight;
            }
        }

        internal static void SetSlotHeight(Transform slot, float rowHeight)
        {
            if (slot == null)
            {
                return;
            }

            var layout = slot.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.preferredHeight = rowHeight;
                layout.minHeight = rowHeight;
            }
        }

        private static RectTransform EnsureRowBase(Transform section, int rowIndex, float rowHeight, float y)
        {
            string rowName = RowPrefix + rowIndex;
            var existing = section.Find(rowName) as RectTransform;
            RectTransform row;
            if (existing != null)
            {
                row = existing;
                row.gameObject.SetActive(true);
            }
            else
            {
                var rowObj = new GameObject(rowName, typeof(RectTransform));
                row = rowObj.GetComponent<RectTransform>();
                row.SetParent(section, false);
            }

            var layoutElement = row.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = row.gameObject.AddComponent<LayoutElement>();
            }
            layoutElement.preferredHeight = rowHeight;
            layoutElement.minHeight = rowHeight;
            layoutElement.flexibleHeight = 0f;

            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.anchoredPosition = new Vector2(0f, y);
            row.sizeDelta = new Vector2(0f, rowHeight);

            return row;
        }

        private static Transform EnsureSlot(RectTransform row, string name, float width, float height)
        {
            var existing = row.Find(name) as RectTransform;
            RectTransform slot;
            if (existing != null)
            {
                slot = existing;
                slot.gameObject.SetActive(true);
            }
            else
            {
                var obj = new GameObject(name, typeof(RectTransform));
                slot = obj.GetComponent<RectTransform>();
                slot.SetParent(row, false);
            }

            var le = slot.GetComponent<LayoutElement>();
            if (le == null)
            {
                le = slot.gameObject.AddComponent<LayoutElement>();
            }
            le.preferredWidth = width;
            le.minWidth = width;
            le.flexibleWidth = 0f;
            le.preferredHeight = height;
            le.minHeight = height;
            le.flexibleHeight = 0f;

            var h = slot.GetComponent<HorizontalLayoutGroup>();
            if (h == null)
            {
                h = slot.gameObject.AddComponent<HorizontalLayoutGroup>();
            }
            h.spacing = 0f;
            h.padding = new RectOffset(0, 0, 0, 0);
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;

            return slot;
        }

        private static void EnsureRowSlotLayout(RectTransform row, Templates templates)
        {
            var h = row.GetComponent<HorizontalLayoutGroup>();
            if (h == null)
            {
                h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            }
            h.spacing = templates.GridSpacing.x;
            int left = templates.ContentInsetLeft > 0 ? templates.ContentInsetLeft : templates.GridPaddingLeft;
            int right = templates.ContentInsetRight > 0 ? templates.ContentInsetRight : templates.GridPaddingRight;
            h.padding = new RectOffset(left, right, 0, 0);
            h.childAlignment = TextAnchor.UpperLeft;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
        }

        private static void EnsureRowSingleSlotLayout(RectTransform row, Templates templates)
        {
            var h = row.GetComponent<HorizontalLayoutGroup>();
            if (h == null)
            {
                h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            }
            h.spacing = 0f;
            int left = templates.ContentInsetLeft > 0 ? templates.ContentInsetLeft : templates.GridPaddingLeft;
            int right = templates.ContentInsetRight > 0 ? templates.ContentInsetRight : templates.GridPaddingRight;
            h.padding = new RectOffset(left, right, 0, 0);
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = false;
        }

        internal static void DisableExtraRows(Transform section, int usedRowCount)
        {
            for (int i = usedRowCount; i < usedRowCount + 50; i++)
            {
                var row = section.Find(RowPrefix + i);
                if (row == null)
                {
                    break;
                }
                row.gameObject.SetActive(false);
            }
        }
    }

    internal static class ControlUtil
    {
        internal static Transform FindFirstByName(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (string.Equals(root.name, name, StringComparison.Ordinal))
            {
                return root;
            }

            var queue = new Queue<Transform>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                for (int i = 0; i < current.childCount; i++)
                {
                    var child = current.GetChild(i);
                    if (string.Equals(child.name, name, StringComparison.Ordinal))
                    {
                        return child;
                    }
                    queue.Enqueue(child);
                }
            }
            return null;
        }

        internal static void ApplyPreferredSize(GameObject root, Vector2? preferredSize)
        {
            if (root == null || preferredSize == null)
            {
                return;
            }

            var layout = root.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = root.AddComponent<LayoutElement>();
            }

            layout.preferredWidth = preferredSize.Value.x;
            layout.preferredHeight = preferredSize.Value.y;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;
        }

        internal static void ApplyAnchoredPositionOffset(GameObject root, Vector2? offset)
        {
            if (root == null || offset == null)
            {
                return;
            }

            var rect = root.transform as RectTransform;
            if (rect == null)
            {
                return;
            }

            rect.anchoredPosition += offset.Value;
        }

        internal static void ApplyTitle(Transform controlRoot, LocalText title, SystemLanguage language)
        {
            if (controlRoot == null)
            {
                return;
            }

            var titleTransform = controlRoot.Find("Title");
            if (titleTransform == null)
            {
                return;
            }

            var localised = titleTransform.GetComponent<LocalisedTMP>();
            if (localised != null)
            {
                UnityEngine.Object.Destroy(localised);
            }

            var tmp = titleTransform.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                LocalTextBinder.Bind(tmp, title);
            }
        }
    }

    internal sealed class ButtonDefinition : IControlDefinition
    {
        private readonly string id;
        private readonly LocalText text;
        private readonly Action onClick;
        private readonly Vector2? preferredSize;
        private readonly Vector2? anchoredPositionOffset;
        private readonly UiRef<Button> uiRef;
        public string Id => id;
        public bool ShowInGame { get; }
        public Vector2? PreferredSize => preferredSize;

        public ButtonDefinition(string id, LocalText text, Action onClick, bool showInGame, Vector2? preferredSize, Vector2? anchoredPositionOffset, UiRef<Button> uiRef)
        {
            this.id = string.IsNullOrWhiteSpace(id) ? "Button_" + Guid.NewGuid().ToString("N") : id;
            this.text = text;
            this.onClick = onClick;
            this.preferredSize = preferredSize;
            this.anchoredPositionOffset = anchoredPositionOffset;
            this.uiRef = uiRef;
            ShowInGame = showInGame;
        }

        public void Build(Transform sectionRoot, Transform parent, Templates templates)
        {
            var existing = ControlUtil.FindFirstByName(sectionRoot, id);
            Transform buttonTransform;
            if (existing != null)
            {
                buttonTransform = existing;
                if (buttonTransform.parent != parent)
                {
                    buttonTransform.SetParent(parent, false);
                }
            }
            else
            {
                buttonTransform = UnityEngine.Object.Instantiate(templates.ButtonTemplate.gameObject, parent).transform;
                buttonTransform.name = id;
                buttonTransform.localScale = Vector3.one;
            }

            var button = buttonTransform.GetComponent<Button>();
            if (button != null)
            {
                button.onClick = new Button.ButtonClickedEvent();
                if (onClick != null)
                {
                    button.onClick.AddListener(() => onClick());
                }
                uiRef?.SetValue(button);
            }

            var textTransform = buttonTransform.Find("Text");
            if (textTransform != null)
            {
                var localised = textTransform.GetComponent<LocalisedTMP>();
                if (localised != null)
                {
                    UnityEngine.Object.Destroy(localised);
                }

                var tmp = textTransform.GetComponentInChildren<TMP_Text>(true);
                if (tmp != null)
                {
                LocalTextBinder.Bind(tmp, text);
                }
            }

            ControlUtil.ApplyPreferredSize(buttonTransform.gameObject, preferredSize);
            ControlUtil.ApplyAnchoredPositionOffset(buttonTransform.gameObject, anchoredPositionOffset);
        }
    }

    internal sealed class LabelDefinition : IControlDefinition
    {
        private readonly string id;
        private readonly LocalText text;
        private readonly Vector2? preferredSize;
        private readonly Vector2? anchoredPositionOffset;
        private readonly UiRef<TMP_Text> uiRef;
        public string Id => id;
        public bool ShowInGame { get; }
        public Vector2? PreferredSize => preferredSize;

        public LabelDefinition(string id, LocalText text, bool showInGame, Vector2? preferredSize, Vector2? anchoredPositionOffset, UiRef<TMP_Text> uiRef)
        {
            this.id = string.IsNullOrWhiteSpace(id) ? "Label_" + Guid.NewGuid().ToString("N") : id;
            this.text = text;
            this.preferredSize = preferredSize;
            this.anchoredPositionOffset = anchoredPositionOffset;
            this.uiRef = uiRef;
            ShowInGame = showInGame;
        }

        public void Build(Transform sectionRoot, Transform parent, Templates templates)
        {
            var existing = ControlUtil.FindFirstByName(sectionRoot, id);
            Transform labelTransform;
            if (existing != null)
            {
                labelTransform = existing;
                if (labelTransform.parent != parent)
                {
                    labelTransform.SetParent(parent, false);
                }
            }
            else
            {
                labelTransform = UnityEngine.Object.Instantiate(templates.LabelTemplate.gameObject, parent).transform;
                labelTransform.name = id;
                labelTransform.localScale = Vector3.one;
            }

            var localised = labelTransform.GetComponent<LocalisedTMP>();
            if (localised != null)
            {
                UnityEngine.Object.Destroy(localised);
            }

            var tmp = labelTransform.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                LocalTextBinder.Bind(tmp, text);
                uiRef?.SetValue(tmp);
            }

            ControlUtil.ApplyPreferredSize(labelTransform.gameObject, preferredSize);
            ControlUtil.ApplyAnchoredPositionOffset(labelTransform.gameObject, anchoredPositionOffset);
        }
    }

    internal sealed class InputStringDefinition : IControlDefinition
    {
        private readonly string id;
        private readonly string settingKey;
        private readonly LocalText title;
        private readonly string initialValue;
        private readonly Action<string> onChanged;
        private readonly Vector2? preferredSize;
        private readonly Vector2? anchoredPositionOffset;
        private readonly UiRef<TMP_InputField> uiRef;
        public string Id => id;
        public bool ShowInGame { get; }
        public Vector2? PreferredSize => preferredSize;

        public InputStringDefinition(string id, string settingKey, LocalText title, string initialValue, Action<string> onChanged, bool showInGame, Vector2? preferredSize, Vector2? anchoredPositionOffset, UiRef<TMP_InputField> uiRef)
        {
            this.id = string.IsNullOrWhiteSpace(id) ? "Input_" + Guid.NewGuid().ToString("N") : id;
            if (settingKey == null) throw new ArgumentNullException(nameof(settingKey));
            this.settingKey = settingKey;
            this.title = title;
            if (initialValue == null) throw new ArgumentNullException(nameof(initialValue));
            this.initialValue = initialValue;
            this.onChanged = onChanged;
            this.preferredSize = preferredSize;
            this.anchoredPositionOffset = anchoredPositionOffset;
            this.uiRef = uiRef;
            ShowInGame = showInGame;
        }

        public void Build(Transform sectionRoot, Transform parent, Templates templates)
        {
            Transform root;
            var existing = ControlUtil.FindFirstByName(sectionRoot, id);
            if (existing != null)
            {
                root = existing;
                if (root.parent != parent)
                {
                    root.SetParent(parent, false);
                }
            }
            else
            {
                root = UnityEngine.Object.Instantiate(templates.SliderTemplate.gameObject, parent).transform;
                root.name = id;
                root.localScale = Vector3.one;
            }

            ControlUtil.ApplyTitle(root, title, templates.Language);

            var customSlider = root.GetComponent<UICustomSlider>();
            if (customSlider != null)
            {
                UnityEngine.Object.Destroy(customSlider);
            }

            var sliderNode = ControlUtil.FindFirstByName(root, "Slider");
            if (sliderNode != null)
            {
                sliderNode.gameObject.SetActive(true);
                var valueNode = sliderNode.Find("Value");
                if (valueNode != null)
                {
                    UnityEngine.Object.Destroy(valueNode.gameObject);
                }
                var fillAreaNode = sliderNode.Find("Fill Area");
                if (fillAreaNode != null)
                {
                    UnityEngine.Object.Destroy(fillAreaNode.gameObject);
                }
                var handleNode = sliderNode.Find("Handle");
                if (handleNode != null)
                {
                    UnityEngine.Object.Destroy(handleNode.gameObject);
                }

                sliderNode.name = "Input";
            }

            var input = root.GetComponentInChildren<TMP_InputField>(true);

            if (input == null) throw new InvalidOperationException("NativeSettingsUI: Input field template is missing TMP_InputField.");

            input.gameObject.SetActive(true);
            input.enabled = true;
            string value = initialValue;
            if (!string.IsNullOrEmpty(settingKey))
            {
                value = PlayerPrefs.GetString(settingKey, initialValue);
            }

            input.onValueChanged.RemoveAllListeners();
            input.onEndEdit.RemoveAllListeners();
            input.SetTextWithoutNotify(value);

            input.onEndEdit.AddListener(v =>
            {
                if (!string.IsNullOrEmpty(settingKey))
                {
                    PlayerPrefs.SetString(settingKey, v);
                }
                onChanged?.Invoke(v);
            });

            uiRef?.SetValue(input);

            ControlUtil.ApplyPreferredSize(root.gameObject, preferredSize);
            ControlUtil.ApplyAnchoredPositionOffset(root.gameObject, anchoredPositionOffset);
        }
    }

    internal sealed class ToggleDefinition : IControlDefinition
    {
        private readonly string id;
        private readonly string settingKey;
        private readonly LocalText title;
        private readonly bool initialValue;
        private readonly Action<bool> onChanged;
        private readonly Vector2? preferredSize;
        private readonly Vector2? anchoredPositionOffset;
        private readonly UiRef<UISettingToggle> uiRef;
        public string Id => id;
        public bool ShowInGame { get; }
        public Vector2? PreferredSize => preferredSize;

        public ToggleDefinition(string id, string settingKey, LocalText title, bool initialValue, Action<bool> onChanged, bool showInGame, Vector2? preferredSize, Vector2? anchoredPositionOffset, UiRef<UISettingToggle> uiRef)
        {
            this.id = string.IsNullOrWhiteSpace(id) ? "Toggle_" + Guid.NewGuid().ToString("N") : id;
            if (settingKey == null) throw new ArgumentNullException(nameof(settingKey));
            this.settingKey = settingKey;
            this.title = title;
            this.initialValue = initialValue;
            this.onChanged = onChanged;
            this.preferredSize = preferredSize;
            this.anchoredPositionOffset = anchoredPositionOffset;
            this.uiRef = uiRef;
            ShowInGame = showInGame;
        }

        public void Build(Transform sectionRoot, Transform parent, Templates templates)
        {
            Transform toggleTransform;
            var existing = ControlUtil.FindFirstByName(sectionRoot, id);
            if (existing != null)
            {
                toggleTransform = existing;
                if (toggleTransform.parent != parent)
                {
                    toggleTransform.SetParent(parent, false);
                }
            }
            else
            {
                toggleTransform = UnityEngine.Object.Instantiate(templates.ToggleTemplate.gameObject, parent).transform;
                toggleTransform.name = id;
                toggleTransform.localScale = Vector3.one;
            }

            ControlUtil.ApplyTitle(toggleTransform, title, templates.Language);

            var setting = toggleTransform.GetComponentInChildren<UISettingToggle>(true);
            if (setting == null) throw new InvalidOperationException("NativeSettingsUI: Toggle template is missing UISettingToggle.");

            setting.SetSettingKey(settingKey);
            ReflectionUtil.SetPrivateField(setting, "defaultValue", initialValue);
            setting.SetValueNoNotify(initialValue);

            if (onChanged != null)
            {
                setting.OnSettingChanged.AddListener(value => onChanged(value));
            }

            uiRef?.SetValue(setting);

            ControlUtil.ApplyPreferredSize(toggleTransform.gameObject, preferredSize);
            ControlUtil.ApplyAnchoredPositionOffset(toggleTransform.gameObject, anchoredPositionOffset);
        }
    }

    internal sealed class DropdownStringDefinition : IControlDefinition
    {
        private readonly string id;
        private readonly string settingKey;
        private readonly LocalText title;
        private readonly IList<string> options;
        private readonly string initialValue;
        private readonly Action<string> onChanged;
        private readonly Vector2? preferredSize;
        private readonly Vector2? anchoredPositionOffset;
        private readonly UiRef<UISettingDropdownString> uiRef;
        public string Id => id;
        public bool ShowInGame { get; }
        public Vector2? PreferredSize => preferredSize;

        public DropdownStringDefinition(string id, string settingKey, LocalText title, IList<string> options, string initialValue, Action<string> onChanged, bool showInGame, Vector2? preferredSize, Vector2? anchoredPositionOffset, UiRef<UISettingDropdownString> uiRef)
        {
            this.id = string.IsNullOrWhiteSpace(id) ? "Dropdown_" + Guid.NewGuid().ToString("N") : id;
            if (settingKey == null) throw new ArgumentNullException(nameof(settingKey));
            this.settingKey = settingKey;
            this.title = title;
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            if (initialValue == null) throw new ArgumentNullException(nameof(initialValue));
            this.initialValue = initialValue;
            this.onChanged = onChanged;
            this.preferredSize = preferredSize;
            this.anchoredPositionOffset = anchoredPositionOffset;
            this.uiRef = uiRef;
            ShowInGame = showInGame;
        }

        public void Build(Transform sectionRoot, Transform parent, Templates templates)
        {
            Transform dropdownTransform;
            var existing = ControlUtil.FindFirstByName(sectionRoot, id);
            if (existing != null)
            {
                dropdownTransform = existing;
                if (dropdownTransform.parent != parent)
                {
                    dropdownTransform.SetParent(parent, false);
                }
            }
            else
            {
                dropdownTransform = UnityEngine.Object.Instantiate(templates.DropdownTemplate.gameObject, parent).transform;
                dropdownTransform.name = id;
                dropdownTransform.localScale = Vector3.one;
            }

            ControlUtil.ApplyTitle(dropdownTransform, title, templates.Language);

            var setting = dropdownTransform.GetComponentInChildren<UISettingDropdownString>(true);
            if (setting == null) throw new InvalidOperationException("NativeSettingsUI: Dropdown template is missing UISettingDropdownString.");

            setting.SetSettingKey(settingKey);
            ReflectionUtil.SetPrivateField(setting, "defaultValue", initialValue);
            setting.SetValueNoNotify(initialValue);
            setting.PopulateOptions(new List<string>(options));

            if (onChanged != null)
            {
                setting.OnSettingChanged.AddListener(value => onChanged(value));
            }

            uiRef?.SetValue(setting);

            ControlUtil.ApplyPreferredSize(dropdownTransform.gameObject, preferredSize);
            ControlUtil.ApplyAnchoredPositionOffset(dropdownTransform.gameObject, anchoredPositionOffset);
        }
    }

    internal sealed class SliderDefinition : IControlDefinition
    {
        private readonly string id;
        private readonly string settingKey;
        private readonly LocalText title;
        private readonly int minValue;
        private readonly int maxValue;
        private readonly int initialValue;
        private readonly Action<int> onChanged;
        private readonly Vector2? preferredSize;
        private readonly Vector2? anchoredPositionOffset;
        private readonly UiRef<UICustomSlider> uiRef;
        public string Id => id;
        public bool ShowInGame { get; }
        public Vector2? PreferredSize => preferredSize;

        public SliderDefinition(string id, string settingKey, LocalText title, int minValue, int maxValue, int initialValue, Action<int> onChanged, bool showInGame, Vector2? preferredSize, Vector2? anchoredPositionOffset, UiRef<UICustomSlider> uiRef)
        {
            this.id = string.IsNullOrWhiteSpace(id) ? "Slider_" + Guid.NewGuid().ToString("N") : id;
            if (settingKey == null) throw new ArgumentNullException(nameof(settingKey));
            this.settingKey = settingKey;
            this.title = title;
            this.minValue = minValue;
            this.maxValue = maxValue;
            this.initialValue = initialValue;
            this.onChanged = onChanged;
            this.preferredSize = preferredSize;
            this.anchoredPositionOffset = anchoredPositionOffset;
            this.uiRef = uiRef;
            ShowInGame = showInGame;
        }

        public void Build(Transform sectionRoot, Transform parent, Templates templates)
        {
            Transform sliderTransform;
            var existing = ControlUtil.FindFirstByName(sectionRoot, id);
            if (existing != null)
            {
                sliderTransform = existing;
                if (sliderTransform.parent != parent)
                {
                    sliderTransform.SetParent(parent, false);
                }
            }
            else
            {
                sliderTransform = UnityEngine.Object.Instantiate(templates.SliderTemplate.gameObject, parent).transform;
                sliderTransform.name = id;
                sliderTransform.localScale = Vector3.one;
            }

            ControlUtil.ApplyTitle(sliderTransform, title, templates.Language);

            var slider = sliderTransform.GetComponentInChildren<UICustomSlider>(true);
            if (slider == null) throw new InvalidOperationException("NativeSettingsUI: Slider template is missing UICustomSlider.");

            slider.SetSettingKey(settingKey);
            ReflectionUtil.SetPrivateField(slider, "minValue", minValue);
            ReflectionUtil.SetPrivateField(slider, "maxValue", maxValue);

            int clampedInitial = Mathf.Clamp(initialValue, minValue, maxValue);
            ReflectionUtil.SetPrivateField(slider, "defaultValue", clampedInitial);
            slider.SetValueNoNotify(clampedInitial);
            slider.DisplayValue(clampedInitial);

            if (onChanged != null)
            {
                slider.OnSettingChanged.AddListener(value => onChanged(value));
            }

            uiRef?.SetValue(slider);

            ControlUtil.ApplyPreferredSize(sliderTransform.gameObject, preferredSize);
            ControlUtil.ApplyAnchoredPositionOffset(sliderTransform.gameObject, anchoredPositionOffset);
        }
    }

    internal static class ReflectionUtil
    {
        internal static void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null || string.IsNullOrEmpty(fieldName))
            {
                return;
            }

            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                return;
            }
            field.SetValue(target, value);
        }
    }
}
