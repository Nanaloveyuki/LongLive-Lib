using System;
using BepInEx.Logging;
using LongLive.Next.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LongLive.BepInEx.Plugin;

internal sealed class LongLiveMainMenuPanel : IESCClose
{
    private enum PageKind
    {
        Overview,
        Compatibility,
        Diagnostics,
    }

    private readonly ManualLogSource _logger;
    private readonly NextRuntimeFacade _runtime;
    private readonly LongLiveHostOptions _options;
    private readonly LongLiveTextLocalizer _localizer;

    private GameObject? _root;
    private Text? _titleText;
    private Text? _bodyText;
    private RectTransform? _contentRect;
    private FpBtn? _overviewButton;
    private FpBtn? _compatibilityButton;
    private FpBtn? _diagnosticsButton;

    public bool IsVisible => _root is not null && _root.activeSelf;

    public bool IsUsable => _root != null;

    public LongLiveMainMenuPanel(ManualLogSource logger, NextRuntimeFacade runtime, LongLiveHostOptions options, LongLiveTextLocalizer localizer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    public bool TryShow(Transform uiRoot)
    {
        if (uiRoot is null)
        {
            return false;
        }

        EnsurePanel(uiRoot);
        if (_root is null)
        {
            return false;
        }

        _root.SetActive(true);
        ESCCloseManager.Inst.RegisterClose(this);
        ShowPage(PageKind.Overview);
        return true;
    }

    public bool TryEscClose()
    {
        Hide();
        return true;
    }

    public void Hide()
    {
        if (_root == null)
        {
            return;
        }

        _root.SetActive(false);
        ESCCloseManager.Inst.UnRegisterClose(this);
    }

    private void EnsurePanel(Transform uiRoot)
    {
        if (_root == null)
        {
            _root = new GameObject("LongLivePanel", typeof(RectTransform), typeof(Image));
            _root.transform.SetParent(uiRoot, false);
            _root.transform.SetAsLastSibling();

            var rootRect = _root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(1240f, 760f);
            rootRect.anchoredPosition = Vector2.zero;

            var rootImage = _root.GetComponent<Image>();
            rootImage.color = new Color(0.12f, 0.14f, 0.17f, 0.96f);

            CreateHeader(rootRect);
            CreateNavigation(rootRect);
            CreateContent(rootRect);
            CreateFooter(rootRect);

            _root.SetActive(false);
            return;
        }

        if (!_root)
        {
            ResetDestroyedState();
            EnsurePanel(uiRoot);
            return;
        }

        if (_root.transform.parent != uiRoot)
        {
            _root.transform.SetParent(uiRoot, false);
        }

        _root.transform.SetAsLastSibling();
    }

    private void ResetDestroyedState()
    {
        _root = null;
        _titleText = null;
        _bodyText = null;
        _contentRect = null;
        _overviewButton = null;
        _compatibilityButton = null;
        _diagnosticsButton = null;
    }

    private void CreateHeader(RectTransform parent)
    {
        var header = CreatePanel("Header", parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(1180f, 54f), new Color(0.15f, 0.17f, 0.21f, 1f));
        _titleText = CreateText("Title", header, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1040f, 34f), 24, TextAnchor.MiddleCenter, FontStyle.Bold);
        _titleText.text = "LongLive";

        var closeButton = CreateButton("Close", header, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(84f, 30f), _localizer.Get("panel.action.close"), 16);
        closeButton.mouseUpEvent.AddListener(new UnityAction(Hide));
    }

    private void CreateNavigation(RectTransform parent)
    {
        var nav = CreatePanel("Navigation", parent, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, -10f), new Vector2(180f, 620f), new Color(0.16f, 0.18f, 0.22f, 1f));
        _overviewButton = CreateNavButton(nav, _localizer.Get("panel.nav.overview"), new Vector2(0f, -16f), () => ShowPage(PageKind.Overview));
        _compatibilityButton = CreateNavButton(nav, _localizer.Get("panel.nav.compatibility"), new Vector2(0f, -72f), () => ShowPage(PageKind.Compatibility));
        _diagnosticsButton = CreateNavButton(nav, _localizer.Get("panel.nav.diagnostics"), new Vector2(0f, -128f), () => ShowPage(PageKind.Diagnostics));
    }

    private void CreateContent(RectTransform parent)
    {
        var content = CreatePanel("Content", parent, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, -10f), new Vector2(980f, 620f), new Color(0.15f, 0.17f, 0.21f, 1f));

        var scrollRect = content.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 24f;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(content, false);
        var viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = new Vector2(0f, 0f);
        viewportRect.anchorMax = new Vector2(1f, 1f);
        viewportRect.offsetMin = new Vector2(16f, 16f);
        viewportRect.offsetMax = new Vector2(-16f, -16f);
        var viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.05f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        _bodyText = CreateText("Body", viewportRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(0f, 0f), 18, TextAnchor.UpperLeft, FontStyle.Normal);
        _bodyText.resizeTextForBestFit = false;
        _bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _bodyText.verticalOverflow = VerticalWrapMode.Overflow;
        _bodyText.lineSpacing = 1.2f;
        _bodyText.supportRichText = true;
        _bodyText.raycastTarget = false;

        _contentRect = _bodyText.rectTransform;
        _contentRect.offsetMin = new Vector2(0f, 0f);
        _contentRect.offsetMax = new Vector2(0f, 0f);

        var bodyFitter = _bodyText.gameObject.AddComponent<ContentSizeFitter>();
        bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        bodyFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect.viewport = viewportRect;
        scrollRect.content = _contentRect;
    }

    private void CreateFooter(RectTransform parent)
    {
        var footer = CreatePanel("Footer", parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(1180f, 48f), new Color(0.15f, 0.17f, 0.21f, 1f));

        var toggleEasyBatch = CreateButton("ToggleEasyBatch", footer, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(152f, 28f), _localizer.Get("panel.action.toggle_easybatch"), 15);
        toggleEasyBatch.mouseUpEvent.AddListener(new UnityAction(() =>
        {
            LongLiveCompatibilitySettingsPresenter.CycleEasyBatch(_options);
            ShowPage(PageKind.Compatibility);
        }));

        var toggleWhiteZe = CreateButton("ToggleWhiteZe", footer, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(180f, 0f), new Vector2(152f, 28f), _localizer.Get("panel.action.toggle_whiteze"), 15);
        toggleWhiteZe.mouseUpEvent.AddListener(new UnityAction(() =>
        {
            LongLiveCompatibilitySettingsPresenter.CycleWhiteZe(_options);
            ShowPage(PageKind.Compatibility);
        }));

        var toggleVTools = CreateButton("ToggleVTools", footer, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(344f, 0f), new Vector2(152f, 28f), _localizer.Get("panel.action.toggle_vtools"), 15);
        toggleVTools.mouseUpEvent.AddListener(new UnityAction(() =>
        {
            LongLiveCompatibilitySettingsPresenter.CycleVTools(_options);
            ShowPage(PageKind.Compatibility);
        }));
    }

    private void ShowPage(PageKind page)
    {
        if (_bodyText is null || _titleText is null)
        {
            return;
        }

        switch (page)
        {
            case PageKind.Overview:
                _titleText.text = _localizer.Get("panel.nav.overview");
                _bodyText.text = LongLiveMainMenuPanelContentBuilder.BuildOverviewText(_localizer, _runtime, _options);
                break;
            case PageKind.Compatibility:
                _titleText.text = _localizer.Get("panel.nav.compatibility");
                _bodyText.text = LongLiveMainMenuPanelContentBuilder.BuildCompatibilityText(_localizer, _runtime, _options);
                break;
            case PageKind.Diagnostics:
                _titleText.text = _localizer.Get("panel.nav.diagnostics");
                _bodyText.text = LongLiveMainMenuPanelContentBuilder.BuildDiagnosticsText(_localizer, _runtime, _options);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(page), page, null);
        }

        ApplyNavState(_overviewButton, page == PageKind.Overview);
        ApplyNavState(_compatibilityButton, page == PageKind.Compatibility);
        ApplyNavState(_diagnosticsButton, page == PageKind.Diagnostics);
    }

    private static RectTransform CreatePanel(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        go.GetComponent<Image>().color = color;
        return rect;
    }

    private static Text CreateText(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, int fontSize, TextAnchor alignment, FontStyle fontStyle)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.fontStyle = fontStyle;
        text.color = new Color(0.96f, 0.96f, 0.96f, 1f);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.supportRichText = false;
        text.raycastTarget = false;
        return text;
    }

    private static FpBtn CreateButton(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, string label, int fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(FpBtn));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var image = go.GetComponent<Image>();
        image.color = new Color(0.24f, 0.27f, 0.34f, 1f);

        var button = go.GetComponent<FpBtn>();
        button.targetImage = image;
        button.nomalSprite = image.sprite;
        button.mouseEnterSprite = image.sprite;
        button.mouseDownSprite = image.sprite;
        button.mouseUpSprite = image.sprite;
        button.mouseUpEvent = new UnityEvent();
        button.mouseDownEvent = new UnityEvent();
        button.mouseEnterEvent = new UnityEvent();
        button.mouseOutEvent = new UnityEvent();

        var labelText = CreateText("Label", rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, sizeDelta - new Vector2(12f, 8f), fontSize, TextAnchor.MiddleCenter, FontStyle.Bold);
        labelText.text = label;
        return button;
    }

    private static FpBtn CreateNavButton(RectTransform parent, string label, Vector2 anchoredPosition, Action onClick)
    {
        var button = CreateButton(label, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), anchoredPosition, new Vector2(156f, 36f), label, 16);
        button.mouseUpEvent.AddListener(new UnityAction(() => onClick()));
        return button;
    }

    private static void ApplyNavState(FpBtn? button, bool selected)
    {
        if (button?.targetImage is null)
        {
            return;
        }

        button.targetImage.color = selected
            ? new Color(0.34f, 0.49f, 0.66f, 1f)
            : new Color(0.24f, 0.27f, 0.34f, 1f);
    }
}
