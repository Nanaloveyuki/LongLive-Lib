using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using LongLive.Next.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveMapOverviewCustomPageRuntime
{
    private const string TabButtonNamePrefix = "LongLiveCustomMapTab_";
    private const string TabHighlightNamePrefix = "LongLiveCustomMapTabHighlight_";
    private const string PageRootNamePrefix = "LongLiveCustomMapPage_";
    private const string PageNodeNamePrefix = "LongLiveCustomMapPageNode_";
    private const string HeaderBandName = "HeaderBand";
    private const string SummaryLabelName = "Summary";
    private const string MetaLabelName = "Meta";
    private const string RegionsRootName = "RegionsRoot";
    private const string RegionBadgeNamePrefix = "RegionBadge_";

    private static readonly Dictionary<string, Sprite?> BackgroundSpriteCache = new Dictionary<string, Sprite?>(StringComparer.Ordinal);
    private static readonly Dictionary<string, Sprite> GeneratedBackgroundSpriteCache = new Dictionary<string, Sprite>(StringComparer.Ordinal);

    private static ManualLogSource? _logger;
    private static LongLiveHostOptions? _options;
    private static string _activePageId = string.Empty;
    private static UIMapPanel? _cachedPanel;
    private static Sprite? _cachedDefaultBackground;

    public static void Initialize(ManualLogSource logger, LongLiveHostOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsEnabled())
        {
            return;
        }

        _logger?.LogInfo($"[MapOverviewCustomPage] sceneLoaded: scene={scene.name}, mode={mode}, activePage={(string.IsNullOrWhiteSpace(_activePageId) ? "n/a" : _activePageId)}");
    }

    public static LongLiveMapOverviewCustomPageRuntimeSnapshot CaptureSnapshot(int sampleLimit = 8)
    {
        var panel = UIMapPanel.Inst;
        var targetByPageId = ResolveCustomTargets().ToDictionary(static target => target.PageId, StringComparer.Ordinal);
        var pageRoots = panel is null ? new List<Transform>() : EnumeratePageRoots(panel).ToList();
        var mountedPageIds = pageRoots
            .Select(static root => root.name)
            .Where(static name => name.StartsWith(PageRootNamePrefix, StringComparison.Ordinal))
            .Select(static name => name.Substring(PageRootNamePrefix.Length))
            .Take(sampleLimit)
            .ToList();

        var activePageRoot = panel is null || string.IsNullOrWhiteSpace(_activePageId)
            ? null
            : panel.PanelObj.transform.Find(BuildPageRootName(_activePageId)) as RectTransform;
        var nodesRoot = activePageRoot?.Find("NodesRoot");
        var regionsRoot = activePageRoot?.Find(RegionsRootName);

        return new LongLiveMapOverviewCustomPageRuntimeSnapshot
        {
            ActiveSceneName = SceneManager.GetActiveScene().name,
            HasPanelInstance = panel is not null,
            HasCachedPanel = _cachedPanel is not null,
            HasTabRoot = panel?.TabRoot is not null,
            HasPanelRoot = panel?.PanelObj is not null,
            IsCustomPageActive = !string.IsNullOrWhiteSpace(_activePageId),
            ActivePageId = _activePageId,
            ActivePageDisplayName = targetByPageId.TryGetValue(_activePageId, out var activeTarget) ? activeTarget.DisplayName : string.Empty,
            CustomPageTargetCount = targetByPageId.Count,
            MountedTabButtonCount = CountChildrenByPrefix(panel?.TabRoot?.transform, TabButtonNamePrefix),
            MountedTabHighlightCount = CountChildrenByPrefix(panel?.TabRoot?.transform, TabHighlightNamePrefix),
            MountedPageRootCount = pageRoots.Count,
            ActivePageRenderedNodeCount = nodesRoot?.childCount ?? 0,
            ActivePageRegionOverlayCount = regionsRoot?.childCount ?? 0,
            MountedPageIds = mountedPageIds,
        };
    }

    public static void EnsureCustomPages(UIMapPanel? panel, string source)
    {
        if (!ShouldRun() || panel is null || panel.TabRoot is null)
        {
            return;
        }

        if (_cachedPanel != panel)
        {
            _cachedPanel = panel;
            _cachedDefaultBackground = panel.MapBG != null ? panel.MapBG.sprite : null;
            _activePageId = string.Empty;
        }

        var customTargets = ResolveCustomTargets();
        var tabIndex = 0;
        foreach (var target in customTargets)
        {
            EnsureTabButton(panel, target, tabIndex);
            EnsurePageRoot(panel, target);
            tabIndex++;
        }

        if (IsVerbose())
        {
            _logger?.LogInfo($"[MapOverviewCustomPage] ensure: source={source}, targets={customTargets.Count}, activePage={(string.IsNullOrWhiteSpace(_activePageId) ? "n/a" : _activePageId)}");
        }
    }

    public static void OnPanelShow(UIMapPanel? panel)
    {
        if (!ShouldRun() || panel is null)
        {
            return;
        }

        EnsureCustomPages(panel, "UIMapPanel.ShowPanel");
    }

    public static void PrepareForBuiltInPage(UIMapPanel? panel, string source)
    {
        if (!ShouldRun() || panel is null)
        {
            return;
        }

        if (IsVerbose())
        {
            _logger?.LogInfo($"[MapOverviewCustomPage] exit-custom-page: source={source}, activePage={(string.IsNullOrWhiteSpace(_activePageId) ? "n/a" : _activePageId)}");
        }

        HideCustomPages(panel, clearActivePage: true, restoreBackground: true);
    }

    public static void OnPanelHide(UIMapPanel? panel)
    {
        if (!ShouldRun() || panel is null)
        {
            return;
        }

        HideCustomPages(panel, clearActivePage: true, restoreBackground: true);
    }

    private static void ShowCustomPage(UIMapPanel panel, LongLiveMapOverviewPageInstallTarget target)
    {
        EnsureCustomPages(panel, "custom-tab-click");

        _activePageId = target.PageId;
        panel.NingZhou.Hide();
        panel.Sea.Hide();

        if (panel.MapBG != null)
        {
            panel.MapBG.sprite = ResolveBackgroundSprite(panel, target);
            panel.MapBG.color = Color.white;
        }

        foreach (var pageRoot in EnumeratePageRoots(panel))
        {
            pageRoot.gameObject.SetActive(string.Equals(pageRoot.name, BuildPageRootName(target.PageId), StringComparison.Ordinal));
        }

        ApplyTabHighlights(panel, target.PageId);

        LongLivePluginContext.GetRuntime().SetString(LongLiveMapDemoStateKeys.CustomPageStatus, $"active=true, page={target.PageId}, title={target.DisplayName}");

        if (IsEnabled())
        {
            _logger?.LogInfo($"[MapOverviewCustomPage] show: page={target.PageId}, title={target.DisplayName}");
        }
    }

    private static void HideCustomPages(UIMapPanel panel, bool clearActivePage, bool restoreBackground)
    {
        foreach (var pageRoot in EnumeratePageRoots(panel))
        {
            pageRoot.gameObject.SetActive(false);
        }

        ApplyTabHighlights(panel, string.Empty);

        if (restoreBackground && panel.MapBG != null && _cachedDefaultBackground != null)
        {
            panel.MapBG.sprite = _cachedDefaultBackground;
            panel.MapBG.color = Color.white;
        }

        if (clearActivePage)
        {
            _activePageId = string.Empty;
            LongLivePluginContext.GetRuntime().SetString(LongLiveMapDemoStateKeys.CustomPageStatus, "active=false");
        }
    }

    private static List<LongLiveMapOverviewPageInstallTarget> ResolveCustomTargets()
    {
        return LongLivePluginContext.GetMapOverviewInstallPlan()
            .PageTargets
            .Where(static target => target.RequiresHostInjection)
            .OrderBy(static target => target.OrderHint ?? int.MaxValue)
            .ThenBy(static target => target.PageId, StringComparer.Ordinal)
            .ToList();
    }

    private static void EnsureTabButton(UIMapPanel panel, LongLiveMapOverviewPageInstallTarget target, int tabIndex)
    {
        var existingButton = panel.TabRoot.transform.Find(BuildTabButtonName(target.PageId));
        var existingHighlight = panel.TabRoot.transform.Find(BuildTabHighlightName(target.PageId));
        if (existingButton is not null && existingHighlight is not null)
        {
            UpdateTabTransform(panel, existingButton as RectTransform, existingHighlight as RectTransform, tabIndex);
            UpdateText(existingButton, target.DisplayName);
            UpdateText(existingHighlight, target.DisplayName);
            return;
        }

        var buttonTemplate = panel.SeaTab != null ? panel.SeaTab.gameObject : panel.NingZhouTab.gameObject;
        var highlightTemplate = panel.SeaTabHighlight != null ? panel.SeaTabHighlight : panel.NingZhouTabHighlight;
        var buttonClone = UnityEngine.Object.Instantiate(buttonTemplate, panel.TabRoot.transform, false);
        buttonClone.name = BuildTabButtonName(target.PageId);
        var highlightClone = UnityEngine.Object.Instantiate(highlightTemplate, panel.TabRoot.transform, false);
        highlightClone.name = BuildTabHighlightName(target.PageId);

        var fpBtn = buttonClone.GetComponent<FpBtn>();
        if (fpBtn is not null)
        {
            fpBtn.mouseUpEvent = new UnityEvent();
            fpBtn.mouseUpEvent.AddListener(new UnityAction(() => ShowCustomPage(panel, target)));
        }

        UpdateTabTransform(panel, buttonClone.transform as RectTransform, highlightClone.transform as RectTransform, tabIndex);
        UpdateText(buttonClone.transform, target.DisplayName);
        UpdateText(highlightClone.transform, target.DisplayName);
        highlightClone.SetActive(false);
    }

    private static void UpdateTabTransform(UIMapPanel panel, RectTransform? buttonRect, RectTransform? highlightRect, int tabIndex)
    {
        if (buttonRect is null || highlightRect is null)
        {
            return;
        }

        var templateRect = panel.SeaTab != null ? panel.SeaTab.transform as RectTransform : panel.NingZhouTab.transform as RectTransform;
        var highlightTemplateRect = panel.SeaTabHighlight != null ? panel.SeaTabHighlight.transform as RectTransform : panel.NingZhouTabHighlight.transform as RectTransform;
        if (templateRect is null || highlightTemplateRect is null)
        {
            return;
        }

        var step = templateRect.sizeDelta.x + 18f;
        var buttonAnchor = ResolveLeftmostTabRect(panel);
        var highlightAnchor = ResolveLeftmostHighlightRect(panel);

        CopyRectTransform(buttonAnchor, buttonRect, new Vector2(-step * (tabIndex + 1), 0f));
        CopyRectTransform(highlightAnchor, highlightRect, new Vector2(-step * (tabIndex + 1), 0f));
    }

    private static void EnsurePageRoot(UIMapPanel panel, LongLiveMapOverviewPageInstallTarget target)
    {
        var existing = panel.PanelObj.transform.Find(BuildPageRootName(target.PageId)) as RectTransform;
        if (existing is null)
        {
            existing = CreatePageRoot(panel, target);
        }

        RefreshPagePresentation(panel, existing, target);
        BuildPageNodes(existing, target);
    }

    private static RectTransform CreatePageRoot(UIMapPanel panel, LongLiveMapOverviewPageInstallTarget target)
    {
        var root = new GameObject(BuildPageRootName(target.PageId), typeof(RectTransform), typeof(Image));
        root.transform.SetParent(panel.PanelObj.transform, false);

        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        var rootImage = root.GetComponent<Image>();
        rootImage.color = Color.white;

        var headerBand = new GameObject(HeaderBandName, typeof(RectTransform), typeof(Image));
        headerBand.transform.SetParent(rootRect, false);
        var headerBandRect = headerBand.GetComponent<RectTransform>();
        headerBandRect.anchorMin = new Vector2(0f, 1f);
        headerBandRect.anchorMax = new Vector2(1f, 1f);
        headerBandRect.pivot = new Vector2(0.5f, 1f);
        headerBandRect.offsetMin = new Vector2(28f, -150f);
        headerBandRect.offsetMax = new Vector2(-28f, -24f);

        var headerBandImage = headerBand.GetComponent<Image>();
        headerBandImage.color = new Color(0.05f, 0.08f, 0.12f, 0.58f);

        var title = CreateLabel(rootRect, "Title", target.DisplayName, 30, FontStyle.Bold, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(520f, 40f));
        title.color = new Color(0.93f, 0.95f, 0.98f, 1f);

        var subtitle = CreateLabel(rootRect, "Subtitle", "LongLive custom overview page", 18, FontStyle.Normal, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(520f, 28f));
        subtitle.color = new Color(0.73f, 0.82f, 0.94f, 1f);

        var summary = CreateLabel(rootRect, SummaryLabelName, string.Empty, 16, FontStyle.Normal, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(760f, 52f));
        summary.color = new Color(0.87f, 0.91f, 0.96f, 0.96f);

        var meta = CreateLabel(rootRect, MetaLabelName, string.Empty, 14, FontStyle.Normal, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -134f), new Vector2(860f, 24f));
        meta.color = new Color(0.71f, 0.78f, 0.88f, 0.92f);

        var regionsRoot = new GameObject(RegionsRootName, typeof(RectTransform));
        regionsRoot.transform.SetParent(rootRect, false);
        var regionsRect = regionsRoot.GetComponent<RectTransform>();
        regionsRect.anchorMin = new Vector2(0f, 1f);
        regionsRect.anchorMax = new Vector2(0f, 1f);
        regionsRect.pivot = new Vector2(0f, 1f);
        regionsRect.anchoredPosition = new Vector2(40f, -34f);
        regionsRect.sizeDelta = new Vector2(360f, 88f);

        var nodesRoot = new GameObject("NodesRoot", typeof(RectTransform));
        nodesRoot.transform.SetParent(rootRect, false);
        var nodesRect = nodesRoot.GetComponent<RectTransform>();
        nodesRect.anchorMin = new Vector2(0f, 0f);
        nodesRect.anchorMax = new Vector2(1f, 1f);
        nodesRect.offsetMin = new Vector2(52f, 56f);
        nodesRect.offsetMax = new Vector2(-52f, -172f);

        root.SetActive(false);
        return rootRect;
    }

    private static void RefreshPagePresentation(UIMapPanel panel, RectTransform root, LongLiveMapOverviewPageInstallTarget target)
    {
        var background = root.GetComponent<Image>();
        if (background != null)
        {
            background.sprite = ResolveBackgroundSprite(panel, target);
            background.color = Color.white;
            background.type = Image.Type.Simple;
            background.preserveAspect = false;
        }

        if (TryGetPageDescriptor(target.PageId, out var descriptor) && descriptor is not null)
        {
            UpdateLabel(root, "Title", descriptor.DisplayName);
        }
        else
        {
            UpdateLabel(root, "Title", target.DisplayName);
        }

        UpdateLabel(root, "Subtitle", BuildPageSubtitle(target));
        UpdateLabel(root, SummaryLabelName, BuildPageSummary(target));
        UpdateLabel(root, MetaLabelName, BuildPageMeta(target));
        RefreshHeaderBand(root, target.PageId);
        BuildRegionBadges(root, target);
    }

    private static void BuildPageNodes(RectTransform root, LongLiveMapOverviewPageInstallTarget target)
    {
        var nodesRoot = root.Find("NodesRoot") as RectTransform;
        if (nodesRoot is null)
        {
            return;
        }

        foreach (Transform child in nodesRoot)
        {
            UnityEngine.Object.Destroy(child.gameObject);
        }

        var template = ResolveTemplateNode();
        if (template is null)
        {
            return;
        }

        var projections = LongLivePluginContext.MapOverview.Routing.GetByPageId(target.PageId);
        foreach (var projection in projections)
        {
            var clone = UnityEngine.Object.Instantiate(template.gameObject, nodesRoot, false);
            clone.name = PageNodeNamePrefix + projection.NodeLogicalId;
            clone.SetActive(true);

            var node = clone.GetComponent<UIMapNingZhouNode>();
            if (node is null)
            {
                UnityEngine.Object.Destroy(clone);
                continue;
            }

            node.Init();
            node.SetNodeName(string.IsNullOrWhiteSpace(projection.NodeDisplayName) ? projection.SceneDisplayName : projection.NodeDisplayName);
            node.WarpSceneName = projection.SceneName;
            node.SetCanJiaoHu(true);
            node.SetNodeAlpha(false);

            var marker = clone.GetComponent<LongLiveMapOverviewUiNodeMarker>() ?? clone.AddComponent<LongLiveMapOverviewUiNodeMarker>();
            marker.NodeLogicalId = projection.NodeLogicalId;
            marker.PageId = projection.PageId;
            marker.SceneLogicalId = projection.SceneLogicalId;
            marker.SceneName = projection.SceneName;

            var rect = clone.transform as RectTransform;
            if (rect is not null)
            {
                rect.anchoredPosition = ResolvePageNodePosition(projection, nodesRoot);
                rect.localScale = Vector3.one;
            }
        }
    }

    private static UIMapNingZhouNode? ResolveTemplateNode()
    {
        var panel = UIMapPanel.Inst;
        if (panel?.NingZhou?.NodesRoot is null)
        {
            return null;
        }

        return panel.NingZhou.NodesRoot.GetComponentsInChildren<UIMapNingZhouNode>(true)
            .FirstOrDefault(static node => node.GetComponent<LongLiveMapOverviewUiNodeMarker>() is null);
    }

    private static Vector2 ResolvePageNodePosition(LongLive.Mods.Maps.LongLiveMapOverviewRouteProjection projection, RectTransform nodesRoot)
    {
        if (LongLivePluginContext.MapOverview.Catalog.TryGetNode(projection.NodeLogicalId, out var descriptor) && descriptor is not null)
        {
            var maxX = Math.Max(160f, nodesRoot.rect.width * 0.45f);
            var maxY = Math.Max(120f, nodesRoot.rect.height * 0.40f);
            return new Vector2(
                Mathf.Clamp(descriptor.Position.X, -maxX, maxX),
                Mathf.Clamp(descriptor.Position.Y, -maxY, maxY));
        }

        return Vector2.zero;
    }

    private static void BuildRegionBadges(RectTransform root, LongLiveMapOverviewPageInstallTarget target)
    {
        var regionsRoot = root.Find(RegionsRootName) as RectTransform;
        if (regionsRoot is null)
        {
            return;
        }

        foreach (Transform child in regionsRoot)
        {
            UnityEngine.Object.Destroy(child.gameObject);
        }

        var pageRegions = LongLivePluginContext.MapOverview.Catalog
            .GetRegionsForPage(target.PageId)
            .OrderBy(static region => region.DisplayName, StringComparer.Ordinal)
            .ToArray();
        if (pageRegions.Length == 0)
        {
            return;
        }

        var activeRegionId = LongLivePluginContext.GetMapOverviewRuntimeSnapshot().ActiveRegionId;
        for (var index = 0; index < pageRegions.Length; index++)
        {
            var region = pageRegions[index];
            var badge = CreateRegionBadge(regionsRoot, region, target, index, string.Equals(region.LogicalId, activeRegionId, StringComparison.Ordinal));
            badge.name = RegionBadgeNamePrefix + region.LogicalId;
        }
    }

    private static GameObject CreateRegionBadge(RectTransform parent, LongLive.Mods.Maps.LongLiveHighlightRegionDescriptor region, LongLiveMapOverviewPageInstallTarget target, int index, bool isActive)
    {
        var badge = new GameObject("RegionBadge", typeof(RectTransform), typeof(Image));
        badge.transform.SetParent(parent, false);

        var rect = badge.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(0f, -index * 28f);
        rect.sizeDelta = new Vector2(340f, 24f);

        var accent = ResolveAccentColor(region.LogicalId);
        var image = badge.GetComponent<Image>();
        image.color = isActive
            ? new Color(accent.r, accent.g, accent.b, 0.54f)
            : new Color(0.07f, 0.10f, 0.15f, 0.34f);

        var label = CreateLabel(rect, "Label", BuildRegionBadgeText(region, target), 13, FontStyle.Normal, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        label.color = isActive
            ? new Color(0.98f, 0.99f, 1f, 1f)
            : new Color(0.84f, 0.89f, 0.96f, 0.92f);
        label.rectTransform.offsetMin = new Vector2(8f, 0f);
        label.rectTransform.offsetMax = new Vector2(-8f, 0f);
        return badge;
    }

    private static string BuildRegionBadgeText(LongLive.Mods.Maps.LongLiveHighlightRegionDescriptor region, LongLiveMapOverviewPageInstallTarget target)
    {
        var nodeCount = LongLivePluginContext.MapOverview.Routing.GetByRegionId(region.LogicalId).Count;
        if (nodeCount == 0)
        {
            nodeCount = LongLivePluginContext.MapOverview.Catalog.GetNodesForRegion(region.LogicalId).Count;
        }

        var regionIndex = 1;
        for (var index = 0; index < target.RegionIds.Count; index++)
        {
            if (string.Equals(target.RegionIds[index], region.LogicalId, StringComparison.Ordinal))
            {
                regionIndex = index + 1;
                break;
            }
        }

        var totalRegions = Math.Max(1, target.RegionCount);
        return region.DisplayName + "  |  region " + regionIndex + "/" + totalRegions + "  |  nodes " + nodeCount;
    }

    private static IEnumerable<Transform> EnumeratePageRoots(UIMapPanel panel)
    {
        foreach (Transform child in panel.PanelObj.transform)
        {
            if (child.name.StartsWith(PageRootNamePrefix, StringComparison.Ordinal))
            {
                yield return child;
            }
        }
    }

    private static int CountChildrenByPrefix(Transform? root, string prefix)
    {
        if (root is null)
        {
            return 0;
        }

        var count = 0;
        foreach (Transform child in root)
        {
            if (child.name.StartsWith(prefix, StringComparison.Ordinal))
            {
                count++;
            }
        }

        return count;
    }

    private static void ApplyTabHighlights(UIMapPanel panel, string activePageId)
    {
        if (panel.NingZhouTabHighlight != null)
        {
            panel.NingZhouTabHighlight.SetActive(false);
        }

        if (panel.SeaTabHighlight != null)
        {
            panel.SeaTabHighlight.SetActive(false);
        }

        foreach (Transform child in panel.TabRoot.transform)
        {
            if (!child.name.StartsWith(TabHighlightNamePrefix, StringComparison.Ordinal))
            {
                continue;
            }

            child.gameObject.SetActive(string.Equals(child.name, BuildTabHighlightName(activePageId), StringComparison.Ordinal));
        }
    }

    private static Sprite? ResolveBackgroundSprite(UIMapPanel panel, LongLiveMapOverviewPageInstallTarget target)
    {
        var assetSprite = TryResolveConfiguredBackgroundSprite(target);
        if (assetSprite != null)
        {
            return assetSprite;
        }

        var hostBackground = panel.NingZhou != null && panel.NingZhou.BGSprite != null
            ? panel.NingZhou.BGSprite
            : _cachedDefaultBackground;
        if (hostBackground != null)
        {
            return hostBackground;
        }

        return ResolveGeneratedBackgroundSprite(target.PageId);
    }

    private static Text CreateLabel(RectTransform parent, string name, string textValue, int fontSize, FontStyle fontStyle, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        var label = new GameObject(name, typeof(RectTransform), typeof(Text));
        label.transform.SetParent(parent, false);
        var rect = label.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var text = label.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.text = textValue;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.supportRichText = false;
        text.raycastTarget = false;
        return text;
    }

    private static void UpdateText(Transform target, string value)
    {
        var text = target.GetComponentInChildren<Text>(true);
        if (text != null)
        {
            text.text = value;
        }
    }

    private static void UpdateLabel(RectTransform root, string labelName, string value)
    {
        var label = root.Find(labelName);
        if (label != null)
        {
            UpdateText(label, value);
        }
    }

    private static void CopyRectTransform(RectTransform source, RectTransform target, Vector2 offset)
    {
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.sizeDelta = source.sizeDelta;
        target.anchoredPosition = source.anchoredPosition + offset;
        target.localScale = source.localScale;
    }

    private static RectTransform ResolveLeftmostTabRect(UIMapPanel panel)
    {
        var ningZhouRect = panel.NingZhouTab.transform as RectTransform;
        var seaRect = panel.SeaTab.transform as RectTransform;
        if (ningZhouRect is null)
        {
            return seaRect!;
        }

        if (seaRect is null)
        {
            return ningZhouRect;
        }

        return ningZhouRect.anchoredPosition.x <= seaRect.anchoredPosition.x ? ningZhouRect : seaRect;
    }

    private static RectTransform ResolveLeftmostHighlightRect(UIMapPanel panel)
    {
        var ningZhouRect = panel.NingZhouTabHighlight.transform as RectTransform;
        var seaRect = panel.SeaTabHighlight.transform as RectTransform;
        if (ningZhouRect is null)
        {
            return seaRect!;
        }

        if (seaRect is null)
        {
            return ningZhouRect;
        }

        return ningZhouRect.anchoredPosition.x <= seaRect.anchoredPosition.x ? ningZhouRect : seaRect;
    }

    private static string BuildTabButtonName(string pageId)
    {
        return TabButtonNamePrefix + pageId;
    }

    private static string BuildTabHighlightName(string pageId)
    {
        return TabHighlightNamePrefix + pageId;
    }

    private static string BuildPageRootName(string pageId)
    {
        return PageRootNamePrefix + pageId;
    }

    private static bool TryGetPageDescriptor(string pageId, out LongLive.Mods.Maps.LongLiveWorldMapPageDescriptor? descriptor)
    {
        return LongLivePluginContext.MapOverview.Catalog.TryGetPage(pageId, out descriptor);
    }

    private static string BuildPageSubtitle(LongLiveMapOverviewPageInstallTarget target)
    {
        var routeKinds = LongLivePluginContext.MapOverview.Routing
            .GetByPageId(target.PageId)
            .Select(static projection => projection.RouteKind.ToString())
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
        var routeSummary = routeKinds.Length == 0 ? "custom route" : string.Join(" / ", routeKinds);
        return target.DisplayName + "  |  " + routeSummary;
    }

    private static string BuildPageSummary(LongLiveMapOverviewPageInstallTarget target)
    {
        var regionNames = LongLivePluginContext.MapOverview.Catalog
            .GetRegionsForPage(target.PageId)
            .Select(static region => region.DisplayName)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var regionSummary = regionNames.Length == 0 ? "No region registered" : string.Join(" / ", regionNames);
        return $"Regions: {regionSummary}    Nodes: {target.NodeCount}    Projections: {target.ProjectionCount}";
    }

    private static string BuildPageMeta(LongLiveMapOverviewPageInstallTarget target)
    {
        return $"PageId={target.PageId}    Mod={target.OwningModId}    Order={target.OrderHint?.ToString() ?? "auto"}";
    }

    private static void RefreshHeaderBand(RectTransform root, string pageId)
    {
        var headerBand = root.Find(HeaderBandName);
        var image = headerBand?.GetComponent<Image>();
        if (image is null)
        {
            return;
        }

        var accent = ResolveAccentColor(pageId);
        image.color = new Color(accent.r, accent.g, accent.b, 0.28f);
    }

    private static Sprite? TryResolveConfiguredBackgroundSprite(LongLiveMapOverviewPageInstallTarget target)
    {
        if (string.IsNullOrWhiteSpace(target.BackgroundAssetId))
        {
            return null;
        }

        if (BackgroundSpriteCache.TryGetValue(target.BackgroundAssetId, out var cachedSprite))
        {
            return cachedSprite;
        }

        var sprite = TryLoadSpriteFromDemoAsset(target.BackgroundAssetId);
        BackgroundSpriteCache[target.BackgroundAssetId] = sprite;
        return sprite;
    }

    private static Sprite? TryLoadSpriteFromDemoAsset(string assetId)
    {
        var assetRoot = Path.Combine(Path.GetDirectoryName(typeof(LongLivePlugin).Assembly.Location) ?? string.Empty, "LongLiveAssets", "Demo");
        foreach (var candidate in BuildBackgroundAssetCandidates(assetRoot, assetId))
        {
            var sprite = LoadSprite(candidate);
            if (sprite != null)
            {
                if (IsEnabled())
                {
                    _logger?.LogInfo($"[MapOverviewCustomPage] background sprite loaded: assetId={assetId}, file={candidate}");
                }

                return sprite;
            }
        }

        return null;
    }

    private static IEnumerable<string> BuildBackgroundAssetCandidates(string assetRoot, string assetId)
    {
        if (string.IsNullOrWhiteSpace(assetRoot) || string.IsNullOrWhiteSpace(assetId))
        {
            yield break;
        }

        var lastSegment = assetId.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? assetId;
        var secondLastSegment = assetId.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Reverse().Skip(1).FirstOrDefault();
        var fileNames = new List<string>
        {
            assetId + ".png",
            assetId.Replace('.', '_') + ".png",
            lastSegment + ".png",
        };

        if (!string.IsNullOrWhiteSpace(secondLastSegment))
        {
            fileNames.Add(secondLastSegment + "_" + lastSegment + ".png");
        }

        fileNames.Add("background.png");

        foreach (var fileName in fileNames.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            yield return Path.Combine(assetRoot, fileName);
        }
    }

    private static Sprite ResolveGeneratedBackgroundSprite(string pageId)
    {
        if (GeneratedBackgroundSpriteCache.TryGetValue(pageId, out var cachedSprite))
        {
            return cachedSprite;
        }

        var accent = ResolveAccentColor(pageId);
        var secondary = Color.Lerp(accent, new Color(0.08f, 0.10f, 0.14f, 1f), 0.62f);
        var highlight = Color.Lerp(accent, Color.white, 0.38f);

        var texture = new Texture2D(2, 2, TextureFormat.ARGB32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp,
        };
        texture.SetPixels(new[]
        {
            secondary,
            accent,
            new Color(0.06f, 0.08f, 0.12f, 1f),
            highlight,
        });
        texture.Apply(false, false);

        var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        sprite.name = "LongLiveGeneratedMapPageBg_" + pageId;
        GeneratedBackgroundSpriteCache[pageId] = sprite;
        return sprite;
    }

    private static Color ResolveAccentColor(string key)
    {
        var hash = string.IsNullOrWhiteSpace(key) ? 0 : key.Aggregate(17, static (current, ch) => current * 31 + ch);
        var hue = Mathf.Abs(hash % 360) / 360f;
        return Color.HSVToRGB(hue, 0.46f, 0.82f);
    }

    private static Sprite? LoadSprite(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        var bytes = File.ReadAllBytes(filePath);
        var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
        };

        if (!texture.LoadImage(bytes))
        {
            UnityEngine.Object.Destroy(texture);
            return null;
        }

        var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        sprite.name = Path.GetFileNameWithoutExtension(filePath);
        return sprite;
    }

    private static bool ShouldRun()
    {
        return LongLivePlugin.Instance is not null
            && LongLivePlugin.Instance.Options.EnableDemoMapRegistration.Value;
    }

    private static bool IsEnabled()
    {
        return _logger is not null
            && _options?.EnableDebugLogging.Value == true
            && _options.EnableMapOverviewRuntimeLogging.Value;
    }

    private static bool IsVerbose()
    {
        return IsEnabled() && _options?.EnableMapOverviewRuntimeVerbose.Value == true;
    }
}
