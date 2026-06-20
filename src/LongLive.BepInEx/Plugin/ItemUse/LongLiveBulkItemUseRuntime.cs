using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;
using Bag;
using BepInEx.Logging;
using GUIPackage;
using HarmonyLib;
using JSONClass;
using KBEngine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveBulkItemUseRuntime
{

    private static ManualLogSource? Logger => LongLivePlugin.LogSource;

    private static readonly Dictionary<Type, Action<object>> UseDelegateCache = new Dictionary<Type, Action<object>>();
    private static CoroutineHost? _coroutineHost;
    private static Coroutine? _activeCoroutine;
    private static BulkUseRequest? _activeRequest;
    private static SlotBase? _pressedSlot;
    private static float _pressedStartedTime;
    private static bool _longPressPopupOpen;
    private static bool _suppressNextPointerUpUse;
    private static bool _suppressAggregation;
    private static readonly List<AggregatedPopTip> AggregatedPopTips = new List<AggregatedPopTip>();
    private static int _activeBulkItemId;
    private static string? _activeBulkItemName;
    private static int _activeBulkRequestedCount;
    private static int _activeBulkCompletedCount;
    private static BulkUseIterationContext? _currentIterationContext;

    public static bool IsEnabled => LongLivePlugin.Instance?.Options.EnableBulkItemUseOptimization.Value == true;

    public static bool IsCapturingAggregationSession => IsAggregationSessionActive;

    public static int ChunkSize
    {
        get
        {
            var configured = LongLivePlugin.Instance?.Options.BulkItemUseChunkSize.Value ?? 24;
            return Math.Max(1, configured);
        }
    }

    public static double FrameBudgetMs
    {
        get
        {
            var configured = LongLivePlugin.Instance?.Options.BulkItemUseFrameBudgetMs.Value ?? 3.0f;
            return Math.Max(0.25d, configured);
        }
    }

    public static bool TryScheduleBulkUse(object? item, int count, object? slot)
    {
        if (!IsEnabled || item == null || count <= 0)
        {
            return false;
        }

        var useAction = ResolveUseAction(item.GetType());
        if (useAction == null)
        {
            return false;
        }

        EnsureCoroutineHost();
        CancelActiveRequest();
        FlushAggregatedPopTips();
        BeginAggregationSession(item, count);

        _activeRequest = new BulkUseRequest(item, slot, useAction, count);
        _activeCoroutine = _coroutineHost!.StartCoroutine(RunBulkUse(_activeRequest));
        Log($"bulk item use scheduled: itemType={item.GetType().FullName}, count={count}, chunkSize={ChunkSize}, frameBudgetMs={FrameBudgetMs.ToString(CultureInfo.InvariantCulture)}");
        return true;
    }

    public static void OnSceneLoaded(Scene scene)
    {
        CancelActiveRequest();
        ResetPointerState();
        ResetAggregatedPopTips();
        CleanupPopTips();
        LogVerbose($"bulk item-use scene cleanup: scene={scene.name}");
    }

    public static void OnPluginShutdown()
    {
        CancelActiveRequest();
        ResetPointerState();
        ResetAggregatedPopTips();
        CleanupPopTips();
        LongLivePopTipRuntimeAccess.ClearAllTimingSnapshots();
    }

    public static bool TryHandlePointerDown(SlotBase slot, object? eventData)
    {
        if (!IsEnabled || slot == null)
        {
            return false;
        }

        var button = TryReadPointerButton(eventData);
        var isDragging = TryReadPointerDragging(eventData);
        if (button != PointerEventData.InputButton.Right || isDragging)
        {
            return false;
        }

        if (!slot.CanUse || slot.Item == null || slot.IsNull())
        {
            return false;
        }

        var itemType = BaseItem.GetItemType(slot.Item.Type);
        if ((int)itemType == 1 || (int)itemType == 3)
        {
            return false;
        }

        _pressedSlot = slot;
        _pressedStartedTime = Time.time;
        _suppressNextPointerUpUse = false;
        return false;
    }

    public static bool TryHandlePointerUp(SlotBase slot, object? eventData)
    {
        if (!IsEnabled || slot == null)
        {
            return false;
        }

        if (TryReadPointerDragging(eventData))
        {
            ResetPointerState();
            return false;
        }

        var button = TryReadPointerButton(eventData);
        if (button == PointerEventData.InputButton.Middle)
        {
            return TryOpenSelector(slot, "middle-click");
        }

        if (button != PointerEventData.InputButton.Right)
        {
            return false;
        }

        var shouldSuppressOriginal = _suppressNextPointerUpUse && ReferenceEquals(_pressedSlot, slot);
        ResetPointerState();
        return shouldSuppressOriginal;
    }

    public static void Tick()
    {
        if (!IsEnabled || _pressedSlot == null || _longPressPopupOpen)
        {
            return;
        }

        if (_pressedSlot.Item == null || _pressedSlot.IsNull() || !_pressedSlot.CanUse)
        {
            ResetPointerState();
            return;
        }

        if (Time.time - _pressedStartedTime <= 0.8f)
        {
            return;
        }

        if (TryOpenSelector(_pressedSlot, "long-press"))
        {
            _suppressNextPointerUpUse = true;
        }
    }

    public static bool TryAggregatePopTip(string? msg, PopTipIconType iconType, string? sound)
    {
        if (!IsEnabled || _suppressAggregation || string.IsNullOrWhiteSpace(msg) || !IsAggregationSessionActive)
        {
            return false;
        }

        RecordAggregatedPopTip(msg!, iconType, sound);
        return true;
    }

    private static IEnumerator RunBulkUse(BulkUseRequest request)
    {
        try
        {
            while (!request.Cancelled && request.Remaining > 0)
            {
                var frameBudget = TimeSpan.FromMilliseconds(FrameBudgetMs);
                var frameStopwatch = Stopwatch.StartNew();
                var processedThisFrame = 0;
                var batchLimit = Math.Min(ChunkSize, request.Remaining);

                while (processedThisFrame < batchLimit && request.Remaining > 0)
                {
                    if (request.Cancelled)
                    {
                        yield break;
                    }

                    var beforeSnapshot = CapturePlayerSnapshot();
                    var iterationContext = BeginIterationContext();
                    try
                    {
                        request.UseAction(request.Item);

                        var afterSnapshot = CapturePlayerSnapshot();
                        RecordPlayerSnapshotDelta(beforeSnapshot, afterSnapshot, iterationContext);
                        request.Remaining--;
                        _activeBulkCompletedCount++;
                        processedThisFrame++;
                    }
                    catch (Exception exception)
                    {
                        request.Cancel();
                        LogVerbose($"bulk item-use iteration failed: completed={_activeBulkCompletedCount}, remaining={request.Remaining}, reason={exception.GetType().Name}: {exception.Message}");
                        break;
                    }
                    finally
                    {
                        EndIterationContext(iterationContext);
                    }

                    if (frameStopwatch.Elapsed >= frameBudget)
                    {
                        break;
                    }
                }

                TryUpdateSlotUi(request.Slot);

                if (request.Remaining > 0 && !request.Cancelled)
                {
                    yield return null;
                }
            }
        }
        finally
        {
            TryUpdateSlotUi(request.Slot);
            FlushAggregatedPopTips();
            Log($"bulk item use completed: remaining={request.Remaining}, completed={_activeBulkCompletedCount}, cancelled={request.Cancelled}");
            _activeCoroutine = null;
            _activeRequest = null;
        }
    }

    private static void TryUpdateSlotUi(object? slot)
    {
        if (slot == null)
        {
            return;
        }

        var updateMethod = AccessTools.Method(slot.GetType(), "UpdateUI", Type.EmptyTypes);
        updateMethod?.Invoke(slot, Array.Empty<object>());
    }

    private static void CleanupPopTips()
    {
        try
        {
            if (!LongLivePopTipRuntimeAccess.TryGetSingleton(out var popTipType, out var inst))
            {
                return;
            }

            LongLivePopTipRuntimeAccess.CaptureTimingSnapshotIfNeeded(popTipType, inst);

            var waitForShow = LongLivePopTipRuntimeAccess.GetWaitForShow(popTipType, inst);
            LongLivePopTipRuntimeAccess.ClearWaitForShow(waitForShow);
            LongLivePopTipRuntimeAccess.SetTimingFields(popTipType, inst, 0f, 0f, 0f);
            LongLivePopTipRuntimeAccess.ClearAddItemMergeDictionary(popTipType, inst);

            var existingTips = LongLivePopTipRuntimeAccess.CollectAndClearTips(popTipType, inst);
            LongLivePopTipRuntimeAccess.DestroyTipObjects(existingTips);
        }
        catch (Exception exception)
        {
            LogVerbose("bulk item-use pop-tip cleanup failed: " + exception.GetType().Name + ": " + exception.Message);
        }
        finally
        {
            try
            {
                if (LongLivePopTipRuntimeAccess.TryGetSingleton(out var popTipType, out var inst))
                {
                    RestoreDefaultPopTipTiming(popTipType, inst);
                }
            }
            catch (Exception exception)
            {
                LogVerbose("bulk item-use pop-tip timing restore failed: " + exception.GetType().Name + ": " + exception.Message);
            }
        }
    }

    private static void FlushAggregatedPopTips()
    {
        if (AggregatedPopTips.Count == 0)
        {
            TryPopGenericBulkUseSummary();
            return;
        }

        try
        {
            if (!LongLivePopTipRuntimeAccess.TryGetSingleton(out var popTipType, out var inst))
            {
                return;
            }

            _suppressAggregation = true;

            foreach (var entry in AggregatedPopTips)
            {
                var message = entry.BuildMessage();
                if (string.IsNullOrWhiteSpace(message))
                {
                    continue;
                }

                var popMethod = AccessTools.Method(popTipType, "Pop", new[] { typeof(string), typeof(string), typeof(PopTipIconType) });
                if (popMethod != null)
                {
                    popMethod.Invoke(inst, new object[] { message, entry.Sound ?? string.Empty, entry.IconType });
                    continue;
                }

                popMethod = AccessTools.Method(popTipType, "Pop", new[] { typeof(string), typeof(PopTipIconType) });
                popMethod?.Invoke(inst, new object[] { message, entry.IconType });
            }
        }
        catch (Exception exception)
        {
            LogVerbose("bulk item-use aggregated pop-tip flush failed: " + exception.GetType().Name + ": " + exception.Message);
        }
        finally
        {
            _suppressAggregation = false;
            ResetAggregatedPopTips();
        }
    }

    private static void ResetAggregatedPopTips()
    {
        AggregatedPopTips.Clear();
        _suppressAggregation = false;
        _activeBulkItemId = 0;
        _activeBulkItemName = null;
        _activeBulkRequestedCount = 0;
        _activeBulkCompletedCount = 0;
    }

    private static bool IsAggregationSessionActive => _activeBulkItemId != 0;

    private static void BeginAggregationSession(object item, int count)
    {
        ResetAggregatedPopTips();

        if (item is BaseItem baseItem)
        {
            _activeBulkItemId = baseItem.Id;
            _activeBulkItemName = baseItem.GetName();
            _activeBulkRequestedCount = count;
            _activeBulkCompletedCount = 0;
            return;
        }

        var idValue = AccessTools.Property(item.GetType(), "Id")?.GetValue(item, null);
        if (idValue is int itemId)
        {
            _activeBulkItemId = itemId;
        }

        _activeBulkItemName = item.ToString();
        _activeBulkRequestedCount = count;
        _activeBulkCompletedCount = 0;
    }

    private static bool TryOpenSelector(SlotBase slot, string source)
    {
        if (slot == null || slot.Item == null || slot.IsNull() || !slot.CanUse)
        {
            return false;
        }

        var item = slot.Item;
        var maxSelectableCount = GetMaxSelectableUseCount(item);
        if (maxSelectableCount <= 1)
        {
            return false;
        }

        _longPressPopupOpen = true;

        var itemName = item.GetName();
        USelectNum.Show(itemName + "x{num}", 1, maxSelectableCount, number =>
        {
            TryScheduleBulkUse(item, number, slot);
            _longPressPopupOpen = false;
            ResetPointerState();
        }, () =>
        {
            _longPressPopupOpen = false;
            ResetPointerState();
        });

        Log($"bulk item-use opened selector: source={source}, count={maxSelectableCount}, itemId={item.Id}");
        return true;
    }

    private static PlayerSnapshot? CapturePlayerSnapshot()
    {
        try
        {
            var player = Tools.instance?.getPlayer();
            if (player == null)
            {
                return null;
            }

            return new PlayerSnapshot(
                player.HP,
                player.HP_Max,
                player.shengShi,
                (int)player.shouYuan,
                player.xinjin,
                CapturePlayerExperience(player),
                (int)player.level,
                player.ZiZhi,
                (int)player.wuXin,
                player.dunSu,
                player.WuDaoDian,
                player.Dandu,
                CaptureNaiYaoCount(player.NaiYaoXin, _activeBulkItemId),
                player.GetLingGeng?.ToArray() ?? player.LingGeng.ToArray(),
                CaptureWuDaoExperience(player),
                CaptureTemporaryDanYaoBuffs(player),
                CaptureSkillIds(player.hasSkillList),
                CaptureSkillIds(player.hasStaticSkillList),
                CaptureUnlockedHerbIds(player.YaoCaiShuXin),
                CaptureDanFangIds(player.DanFang),
                CaptureYaoCaiChanDiIds(player.YaoCaiChanDi),
                CaptureSeaTanSuoDu(player.SeaTanSuoDu),
                CaptureItemBuffKeys(AccessTools.Field(player.GetType(), "ItemBuffList")?.GetValue(player)),
                player.IsCanSetFace);
        }
        catch (Exception exception)
        {
            LogVerbose("bulk item-use snapshot capture failed: " + exception.GetType().Name + ": " + exception.Message);
            return null;
        }
    }

    private static void RecordPlayerSnapshotDelta(PlayerSnapshot? beforeSnapshot, PlayerSnapshot? afterSnapshot, BulkUseIterationContext? iterationContext)
    {
        if (beforeSnapshot == null || afterSnapshot == null)
        {
            return;
        }

        var hpMaxDelta = afterSnapshot.HpMax - beforeSnapshot.HpMax;
        var hpDelta = afterSnapshot.Hp - beforeSnapshot.Hp;
        RecordSignedDelta("你的血量上限提升了", "你的血量上限降低了", hpMaxDelta, PopTipIconType.上箭头);

        var shouldSuppressHpDelta = hpMaxDelta > 0 && hpDelta == hpMaxDelta;
        if (!shouldSuppressHpDelta)
        {
            RecordSignedDelta("你的血量恢复了", "你的血量降低了", hpDelta, PopTipIconType.上箭头);
        }

        RecordSignedDelta("你的神识提升了", "你的神识降低了", afterSnapshot.ShenShi - beforeSnapshot.ShenShi, PopTipIconType.上箭头);
        RecordSignedDelta("你的寿元增加了", "你的寿元减少了", afterSnapshot.ShouYuan - beforeSnapshot.ShouYuan, PopTipIconType.上箭头);
        RecordSignedDelta("你的心境提升了", "你的心境降低了", afterSnapshot.XinJing - beforeSnapshot.XinJing, PopTipIconType.上箭头);
        RecordExperienceDelta(beforeSnapshot, afterSnapshot, iterationContext);
        RecordSignedDelta("你的资质提升了", "你的资质降低了", afterSnapshot.ZiZhi - beforeSnapshot.ZiZhi, PopTipIconType.上箭头);
        RecordSignedDelta("你的悟性提升了", "你的悟性降低了", afterSnapshot.WuXing - beforeSnapshot.WuXing, PopTipIconType.上箭头);
        RecordSignedDelta("你的遁速提升了", "你的遁速降低了", afterSnapshot.DunSu - beforeSnapshot.DunSu, PopTipIconType.上箭头);
        RecordSignedDelta("你的悟道点提升了", "你的悟道点降低了", afterSnapshot.WuDaoDian - beforeSnapshot.WuDaoDian, PopTipIconType.上箭头);
        RecordSignedDelta("你的丹毒增加了", "你的丹毒降低了", afterSnapshot.DanDu - beforeSnapshot.DanDu, PopTipIconType.上箭头);
        RecordSignedDelta("你的耐药次数增加了", "你的耐药次数减少了", afterSnapshot.NaiYaoCount - beforeSnapshot.NaiYaoCount, PopTipIconType.上箭头);
        RecordWuDaoExperienceDelta(beforeSnapshot.WuDaoExperience, afterSnapshot.WuDaoExperience);
        RecordTemporaryDanYaoBuffDelta(beforeSnapshot.TemporaryDanYaoBuffs, afterSnapshot.TemporaryDanYaoBuffs);
        RecordUnlockDelta(beforeSnapshot, afterSnapshot, iterationContext);

        var lingGengCount = Math.Min(beforeSnapshot.LingGeng.Length, afterSnapshot.LingGeng.Length);
        for (var index = 0; index < lingGengCount; index++)
        {
            var delta = afterSnapshot.LingGeng[index] - beforeSnapshot.LingGeng[index];
            RecordSignedDelta(
                "你的" + ResolveLingGenName(index) + "灵根提升了",
                "你的" + ResolveLingGenName(index) + "灵根降低了",
                delta,
                PopTipIconType.上箭头);
        }
    }

    private static HashSet<int> CaptureSkillIds(List<SkillItem>? skills)
    {
        var result = new HashSet<int>();
        if (skills == null)
        {
            return result;
        }

        foreach (var skill in skills)
        {
            if (skill?.itemId > 0)
            {
                result.Add(skill.itemId);
            }
        }

        return result;
    }

    private static HashSet<int> CaptureUnlockedHerbIds(JSONObject? yaoCaiShuXin)
    {
        var result = new HashSet<int>();
        if (yaoCaiShuXin?.keys == null)
        {
            return result;
        }

        foreach (var key in yaoCaiShuXin.keys)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var separatorIndex = key.IndexOf('_');
            var idText = separatorIndex > 0 ? key.Substring(0, separatorIndex) : key;
            if (int.TryParse(idText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var herbId) && herbId > 0)
            {
                result.Add(herbId);
            }
        }

        return result;
    }

    private static int CapturePlayerExperience(KBEngine.Avatar player)
    {
        try
        {
            var value = AccessTools.Field(player.GetType(), "exp")?.GetValue(player);
            if (value is ulong unsignedValue)
            {
                return unsignedValue > int.MaxValue ? int.MaxValue : (int)unsignedValue;
            }

            if (value is long signedLongValue)
            {
                return signedLongValue > int.MaxValue ? int.MaxValue : (signedLongValue < int.MinValue ? int.MinValue : (int)signedLongValue);
            }

            if (value is int signedIntValue)
            {
                return signedIntValue;
            }
        }
        catch (Exception exception)
        {
            LogVerbose("bulk item-use EXP snapshot failed: " + exception.GetType().Name + ": " + exception.Message);
        }

        return 0;
    }

    private static int CaptureNaiYaoCount(JSONObject? naiYaoXin, int itemId)
    {
        if (naiYaoXin == null || itemId <= 0)
        {
            return 0;
        }

        try
        {
            return Tools.getJsonobject(naiYaoXin, itemId.ToString(CultureInfo.InvariantCulture));
        }
        catch (Exception exception)
        {
            LogVerbose($"bulk item-use NaiYao snapshot failed: itemId={itemId}, reason={exception.GetType().Name}: {exception.Message}");
            return 0;
        }
    }

    private static Dictionary<int, int> CaptureTemporaryDanYaoBuffs(KBEngine.Avatar player)
    {
        var result = new Dictionary<int, int>();

        try
        {
            foreach (var pair in player.StreamData?.DanYaoBuFFDict ?? new Dictionary<int, int>())
            {
                result[pair.Key] = pair.Value;
            }
        }
        catch (Exception exception)
        {
            LogVerbose("bulk item-use DanYao temporary buff snapshot failed: " + exception.GetType().Name + ": " + exception.Message);
        }

        return result;
    }

    private static HashSet<int> CaptureDanFangIds(JSONObject? danFang)
    {
        var result = new HashSet<int>();
        if (danFang?.list == null)
        {
            return result;
        }

        foreach (var entry in danFang.list)
        {
            var id = entry?["ID"]?.I ?? 0;
            if (id > 0)
            {
                result.Add(id);
            }
        }

        return result;
    }

    private static HashSet<int> CaptureYaoCaiChanDiIds(JSONObject? yaoCaiChanDi)
    {
        var result = new HashSet<int>();
        if (yaoCaiChanDi?.list == null)
        {
            return result;
        }

        foreach (var entry in yaoCaiChanDi.list)
        {
            if (entry != null)
            {
                var id = entry.I;
                if (id > 0)
                {
                    result.Add(id);
                }
            }
        }

        return result;
    }

    private static Dictionary<int, int> CaptureSeaTanSuoDu(JSONObject? seaTanSuoDu)
    {
        var result = new Dictionary<int, int>();
        if (seaTanSuoDu?.keys == null)
        {
            return result;
        }

        foreach (var key in seaTanSuoDu.keys)
        {
            if (!int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seaId) || seaId <= 0)
            {
                continue;
            }

            result[seaId] = seaTanSuoDu[key].I;
        }

        return result;
    }

    private static HashSet<string> CaptureItemBuffKeys(object? itemBuffList)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        if (itemBuffList == null)
        {
            return result;
        }

        try
        {
            var propertiesMethod = AccessTools.Method(itemBuffList.GetType(), "Properties", Type.EmptyTypes);
            if (propertiesMethod?.Invoke(itemBuffList, Array.Empty<object>()) is not IEnumerable properties)
            {
                return result;
            }

            foreach (var property in properties)
            {
                var propertyType = property?.GetType();
                if (propertyType == null)
                {
                    continue;
                }

                var name = AccessTools.Property(propertyType, "Name")?.GetValue(property, null) as string;
                var value = AccessTools.Property(propertyType, "Value")?.GetValue(property, null);
                if (string.IsNullOrWhiteSpace(name) || value == null)
                {
                    continue;
                }

                var getItemMethod = AccessTools.Method(value.GetType(), "get_Item", new[] { typeof(object) })
                    ?? AccessTools.Method(value.GetType(), "get_Item", new[] { typeof(string) });
                var startToken = getItemMethod?.Invoke(value, new object[] { "start" });
                if (startToken == null)
                {
                    continue;
                }

                var startValue = AccessTools.Property(startToken.GetType(), "Value")?.GetValue(startToken, null);
                if (startValue is bool enabled && enabled)
                {
                    result.Add(name!);
                }
            }
        }
        catch (Exception exception)
        {
            LogVerbose("bulk item-use item buff snapshot failed: " + exception.GetType().Name + ": " + exception.Message);
        }

        return result;
    }

    private static void RecordExperienceDelta(PlayerSnapshot beforeSnapshot, PlayerSnapshot afterSnapshot, BulkUseIterationContext? iterationContext)
    {
        var levelDelta = afterSnapshot.Level - beforeSnapshot.Level;
        var experienceDelta = afterSnapshot.Experience - beforeSnapshot.Experience;
        var suppressCultivation = iterationContext?.HasMarker(LongLiveBulkUsePromptMarker.CultivationGain) == true;

        if (levelDelta == 0)
        {
            if (!suppressCultivation)
            {
                RecordSignedDelta("你的修为提升了", "你的修为降低了", experienceDelta, PopTipIconType.上箭头);
            }

            return;
        }

        if (levelDelta > 0)
        {
            RecordAggregatedPopTip("你的境界提升了" + levelDelta.ToString(CultureInfo.InvariantCulture) + "重", PopTipIconType.上箭头, null);
        }
        else
        {
            RecordAggregatedPopTip("你的境界降低了" + Math.Abs(levelDelta).ToString(CultureInfo.InvariantCulture) + "重", PopTipIconType.下箭头, null);
        }

        if (experienceDelta != 0 && !suppressCultivation)
        {
            RecordSignedDelta("你的修为提升了", "你的修为降低了", experienceDelta, PopTipIconType.上箭头);
        }
    }

    private static void RecordTemporaryDanYaoBuffDelta(Dictionary<int, int> beforeSnapshot, Dictionary<int, int> afterSnapshot)
    {
        foreach (var pair in afterSnapshot)
        {
            var beforeValue = beforeSnapshot.TryGetValue(pair.Key, out var resolvedBefore) ? resolvedBefore : 0;
            var delta = pair.Value - beforeValue;
            if (delta == 0)
            {
                continue;
            }

            var itemName = ResolveItemName(pair.Key);
            if (delta > 0)
            {
                RecordAggregatedPopTip("你获得了" + itemName + "药效" + delta.ToString(CultureInfo.InvariantCulture) + "层", PopTipIconType.包裹, null);
            }
            else
            {
                RecordAggregatedPopTip("你的" + itemName + "药效减少了" + Math.Abs(delta).ToString(CultureInfo.InvariantCulture) + "层", PopTipIconType.下箭头, null);
            }
        }
    }

    private static Dictionary<int, int> CaptureWuDaoExperience(KBEngine.Avatar player)
    {
        var result = new Dictionary<int, int>();
        var wuDaoMag = player.wuDaoMag;
        if (wuDaoMag == null || jsonData.instance?.WuDaoAllTypeJson?.list == null)
        {
            return result;
        }

        foreach (var typeEntry in jsonData.instance.WuDaoAllTypeJson.list)
        {
            var typeId = typeEntry?["id"]?.I ?? 0;
            if (typeId <= 0)
            {
                continue;
            }

            try
            {
                result[typeId] = wuDaoMag.getWuDaoEx(typeId).I;
            }
            catch (Exception exception)
            {
                LogVerbose($"bulk item-use WuDao snapshot skipped: typeId={typeId}, reason={exception.GetType().Name}: {exception.Message}");
            }
        }

        return result;
    }

    private static void RecordWuDaoExperienceDelta(Dictionary<int, int> beforeSnapshot, Dictionary<int, int> afterSnapshot)
    {
        if (afterSnapshot.Count == 0)
        {
            return;
        }

        foreach (var pair in afterSnapshot)
        {
            var beforeValue = beforeSnapshot.TryGetValue(pair.Key, out var resolvedBefore) ? resolvedBefore : 0;
            var delta = pair.Value - beforeValue;
            if (delta == 0)
            {
                continue;
            }

            var wuDaoName = ResolveWuDaoTypeName(pair.Key);
            RecordSignedDelta(
                "你对" + wuDaoName + "之道的感悟提升了",
                "你对" + wuDaoName + "之道的感悟降低了",
                delta,
                PopTipIconType.上箭头);
        }
    }

    private static void RecordUnlockDelta(PlayerSnapshot beforeSnapshot, PlayerSnapshot afterSnapshot, BulkUseIterationContext? iterationContext)
    {
        if (iterationContext?.HasMarker(LongLiveBulkUsePromptMarker.SkillUnlock) != true)
        {
            RecordNamedUnlockDelta(
                afterSnapshot.LearnedSkillIds.Except(beforeSnapshot.LearnedSkillIds),
                ResolveSkillName,
                "你学会了神通",
                "你学会了{0}门神通");
        }

        if (iterationContext?.HasMarker(LongLiveBulkUsePromptMarker.StaticSkillUnlock) != true)
        {
            RecordNamedUnlockDelta(
                afterSnapshot.LearnedStaticSkillIds.Except(beforeSnapshot.LearnedStaticSkillIds),
                ResolveStaticSkillName,
                "你学会了功法",
                "你学会了{0}门功法");
        }

        if (iterationContext?.HasMarker(LongLiveBulkUsePromptMarker.HerbEncyclopediaUnlock) != true)
        {
            RecordNamedUnlockDelta(
                afterSnapshot.UnlockedHerbIds.Except(beforeSnapshot.UnlockedHerbIds),
                ResolveItemName,
                "你解锁了草药图鉴：",
                "你解锁了{0}种草药图鉴");
        }

        if (iterationContext?.HasMarker(LongLiveBulkUsePromptMarker.DanFangUnlock) != true)
        {
            RecordNamedUnlockDelta(
                afterSnapshot.DanFangIds.Except(beforeSnapshot.DanFangIds),
                ResolveItemName,
                "你学会了丹方：",
                "你学会了{0}份丹方");
        }

        if (iterationContext?.HasMarker(LongLiveBulkUsePromptMarker.HerbOriginUnlock) != true)
        {
            RecordNamedUnlockDelta(
                afterSnapshot.YaoCaiChanDiIds.Except(beforeSnapshot.YaoCaiChanDiIds),
                ResolveSeaLocationName,
                "你掌握了草药产地：",
                "你掌握了{0}处草药产地");
        }

        if (iterationContext?.HasMarker(LongLiveBulkUsePromptMarker.TemporaryItemBuffUnlock) != true)
        {
            RecordNamedStringUnlockDelta(
                afterSnapshot.ItemBuffKeys.Except(beforeSnapshot.ItemBuffKeys),
                ResolveItemBuffName,
                "你获得了临时物品增益：",
                "你获得了{0}个临时物品增益");
        }

        RecordSeaTanSuoDuDelta(beforeSnapshot.SeaTanSuoDu, afterSnapshot.SeaTanSuoDu, iterationContext);

        if (!beforeSnapshot.CanSetFace && afterSnapshot.CanSetFace && iterationContext?.HasMarker(LongLiveBulkUsePromptMarker.FaceCustomizationUnlock) != true)
        {
            RecordAggregatedPopTip("你解锁了角色形象调整", PopTipIconType.包裹, null);
        }
    }

    private static void RecordSeaTanSuoDuDelta(Dictionary<int, int> beforeSnapshot, Dictionary<int, int> afterSnapshot, BulkUseIterationContext? iterationContext)
    {
        if (iterationContext?.HasMarker(LongLiveBulkUsePromptMarker.SeaExplorationGain) == true)
        {
            return;
        }

        foreach (var pair in afterSnapshot)
        {
            var beforeValue = beforeSnapshot.TryGetValue(pair.Key, out var resolvedBefore) ? resolvedBefore : 0;
            var delta = pair.Value - beforeValue;
            if (delta == 0)
            {
                continue;
            }

            var seaName = ResolveSeaLocationName(pair.Key);
            RecordSignedDelta(
                "你对" + seaName + "的探索度提升了",
                "你对" + seaName + "的探索度降低了",
                delta,
                PopTipIconType.上箭头);
        }
    }

    private static void RecordNamedUnlockDelta(IEnumerable<int> newIds, Func<int, string> resolveName, string singlePrefix, string summaryFormat)
    {
        var uniqueIds = newIds
            .Where(static id => id > 0)
            .Distinct()
            .ToArray();
        if (uniqueIds.Length == 0)
        {
            return;
        }

        if (uniqueIds.Length > 3)
        {
            RecordAggregatedPopTip(string.Format(CultureInfo.InvariantCulture, summaryFormat, uniqueIds.Length), PopTipIconType.包裹, null);
            return;
        }

        foreach (var id in uniqueIds)
        {
            var resolvedName = resolveName(id);
            if (string.IsNullOrWhiteSpace(resolvedName))
            {
                resolvedName = id.ToString(CultureInfo.InvariantCulture);
            }

            RecordAggregatedPopTip(singlePrefix + resolvedName, PopTipIconType.包裹, null);
        }
    }

    private static void RecordNamedStringUnlockDelta(IEnumerable<string> newKeys, Func<string, string> resolveName, string singlePrefix, string summaryFormat)
    {
        var uniqueKeys = newKeys
            .Where(static key => !string.IsNullOrWhiteSpace(key))
            .Select(static key => key.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (uniqueKeys.Length == 0)
        {
            return;
        }

        if (uniqueKeys.Length > 3)
        {
            RecordAggregatedPopTip(string.Format(CultureInfo.InvariantCulture, summaryFormat, uniqueKeys.Length), PopTipIconType.包裹, null);
            return;
        }

        foreach (var key in uniqueKeys)
        {
            var resolvedName = resolveName(key);
            if (string.IsNullOrWhiteSpace(resolvedName))
            {
                resolvedName = key;
            }

            RecordAggregatedPopTip(singlePrefix + resolvedName, PopTipIconType.包裹, null);
        }
    }

    private static void RecordSignedDelta(string positivePrefix, string negativePrefix, int delta, PopTipIconType iconType)
    {
        if (delta == 0)
        {
            return;
        }

        if (delta > 0)
        {
            RecordAggregatedPopTip(positivePrefix + delta.ToString(CultureInfo.InvariantCulture), iconType, null);
            return;
        }

        RecordAggregatedPopTip(negativePrefix + Math.Abs(delta).ToString(CultureInfo.InvariantCulture), iconType, null);
    }

    private static void RecordAggregatedPopTip(string message, PopTipIconType iconType, string? sound)
    {
        _currentIterationContext?.ObservePrompt(message);

        if (LongLiveNumericMessageParser.TryParseNumericToken(message, out var prefix, out var numericValue, out var suffix))
        {
            var numericEntry = AggregatedPopTips.FirstOrDefault(entry =>
                entry.IconType == iconType &&
                string.Equals(entry.Sound, sound, StringComparison.Ordinal) &&
                entry.HasNumericSuffix &&
                string.Equals(entry.Prefix, prefix, StringComparison.Ordinal) &&
                string.Equals(entry.Suffix, suffix, StringComparison.Ordinal));

            if (numericEntry != null)
            {
                numericEntry.NumericValue += numericValue;
                numericEntry.Count++;
            }
            else
            {
                AggregatedPopTips.Add(AggregatedPopTip.CreateNumeric(prefix, numericValue, suffix, iconType, sound));
            }

            return;
        }

        var exactEntry = AggregatedPopTips.FirstOrDefault(entry =>
            entry.IconType == iconType &&
            string.Equals(entry.Sound, sound, StringComparison.Ordinal) &&
            !entry.HasNumericSuffix &&
            string.Equals(entry.RawMessage, message, StringComparison.Ordinal));

        if (exactEntry != null)
        {
            exactEntry.Count++;
            return;
        }

        AggregatedPopTips.Add(AggregatedPopTip.CreateLiteral(message, iconType, sound));
    }

    private static void TryPopGenericBulkUseSummary()
    {
        var effectiveCount = ResolveEffectiveBulkSummaryCount();
        if (effectiveCount <= 0 || string.IsNullOrWhiteSpace(_activeBulkItemName))
        {
            return;
        }

        try
        {
            if (!LongLivePopTipRuntimeAccess.TryGetSingleton(out var popTipType, out var inst))
            {
                return;
            }

            _suppressAggregation = true;
            var message = string.Format(CultureInfo.InvariantCulture, "已批量使用{0}x{1}", _activeBulkItemName, effectiveCount);
            var popMethod = AccessTools.Method(popTipType, "Pop", new[] { typeof(string), typeof(string), typeof(PopTipIconType) });
            if (popMethod != null)
            {
                popMethod.Invoke(inst, new object[] { message, string.Empty, PopTipIconType.包裹 });
                return;
            }

            popMethod = AccessTools.Method(popTipType, "Pop", new[] { typeof(string), typeof(PopTipIconType) });
            popMethod?.Invoke(inst, new object[] { message, PopTipIconType.包裹 });
        }
        catch (Exception exception)
        {
            LogVerbose("bulk item-use generic pop-tip flush failed: " + exception.GetType().Name + ": " + exception.Message);
        }
        finally
        {
            _suppressAggregation = false;
            ResetAggregatedPopTips();
        }
    }

    private static string ResolveLingGenName(int index)
    {
        switch (index)
        {
            case 0:
                return "金";
            case 1:
                return "木";
            case 2:
                return "水";
            case 3:
                return "火";
            case 4:
                return "土";
            default:
                return "未知";
        }
    }

    private static string ResolveWuDaoTypeName(int typeId)
    {
        try
        {
            if (WuDaoAllTypeJson.DataDict.TryGetValue(typeId, out var wuDaoType) && !string.IsNullOrWhiteSpace(wuDaoType.name1))
            {
                return wuDaoType.name1;
            }
        }
        catch (Exception exception)
        {
            LogVerbose($"bulk item-use WuDao name resolution failed: typeId={typeId}, reason={exception.GetType().Name}: {exception.Message}");
        }

        return typeId.ToString(CultureInfo.InvariantCulture);
    }

    private static string ResolveSkillName(int skillId)
    {
        try
        {
            var resolved = Tools.instance?.getSkillName(skillId, false);
            if (!string.IsNullOrWhiteSpace(resolved))
            {
                return NormalizeDisplayText(resolved);
            }

            var fallback = jsonData.instance?.skillJsonData?[skillId.ToString(CultureInfo.InvariantCulture)]?["name"]?.Str;
            if (!string.IsNullOrWhiteSpace(fallback))
            {
                return NormalizeDisplayText(fallback);
            }
        }
        catch (Exception exception)
        {
            LogVerbose($"bulk item-use skill name resolution failed: skillId={skillId}, reason={exception.GetType().Name}: {exception.Message}");
        }

        return skillId.ToString(CultureInfo.InvariantCulture);
    }

    private static string ResolveStaticSkillName(int skillId)
    {
        try
        {
            var resolved = Tools.instance?.getStaticSkillName(skillId, false);
            if (!string.IsNullOrWhiteSpace(resolved))
            {
                return NormalizeDisplayText(resolved);
            }
        }
        catch (Exception exception)
        {
            LogVerbose($"bulk item-use static skill name resolution failed: skillId={skillId}, reason={exception.GetType().Name}: {exception.Message}");
        }

        return skillId.ToString(CultureInfo.InvariantCulture);
    }

    private static string ResolveItemName(int itemId)
    {
        try
        {
            if (_ItemJsonData.DataDict.TryGetValue(itemId, out var itemData) && !string.IsNullOrWhiteSpace(itemData.name))
            {
                return NormalizeDisplayText(itemData.name);
            }
        }
        catch (Exception exception)
        {
            LogVerbose($"bulk item-use item name resolution failed: itemId={itemId}, reason={exception.GetType().Name}: {exception.Message}");
        }

        return itemId.ToString(CultureInfo.InvariantCulture);
    }

    private static string ResolveSeaLocationName(int seaId)
    {
        try
        {
            if (SeaHaiYuTanSuo.DataDict.ContainsKey(seaId))
            {
                var sceneName = "Sea" + seaId.ToString(CultureInfo.InvariantCulture);
                if (SceneNameJsonData.DataDict.TryGetValue(sceneName, out var sceneData) && !string.IsNullOrWhiteSpace(sceneData.EventName))
                {
                    return NormalizeDisplayText(sceneData.EventName);
                }

            }
        }
        catch (Exception exception)
        {
            LogVerbose($"bulk item-use sea name resolution failed: seaId={seaId}, reason={exception.GetType().Name}: {exception.Message}");
        }

        return seaId.ToString(CultureInfo.InvariantCulture);
    }

    private static string ResolveItemBuffName(string buffKey)
    {
        if (string.IsNullOrWhiteSpace(buffKey))
        {
            return string.Empty;
        }

        var normalizedKey = buffKey.Trim();
        return normalizedKey == "27"
            ? "海上临时状态"
            : string.Empty;
    }

    private static BulkUseIterationContext BeginIterationContext()
    {
        var context = new BulkUseIterationContext();
        _currentIterationContext = context;
        return context;
    }

    private static void EndIterationContext(BulkUseIterationContext context)
    {
        if (ReferenceEquals(_currentIterationContext, context))
        {
            _currentIterationContext = null;
        }
    }

    private static string NormalizeDisplayText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = Regex.Replace(value, "<[^>]+>", string.Empty);
        return Regex.Unescape(normalized).Trim();
    }

    private static void RestoreDefaultPopTipTiming(Type popTipType, object inst)
    {
        LongLivePopTipRuntimeAccess.TryRestoreTimingSnapshot(popTipType, inst);
    }

    private static int ResolveEffectiveBulkSummaryCount()
    {
        if (_activeBulkCompletedCount > 0)
        {
            return _activeBulkCompletedCount;
        }

        return _activeBulkRequestedCount;
    }

    private static Action<object>? ResolveUseAction(Type itemType)
    {
        if (UseDelegateCache.TryGetValue(itemType, out var cachedAction))
        {
            return cachedAction;
        }

        var useMethod = AccessTools.Method(itemType, "Use", Type.EmptyTypes);
        if (useMethod == null)
        {
            return null;
        }

        var action = (Action<object>)typeof(LongLiveBulkItemUseRuntime)
            .GetMethod(nameof(BuildUseAction), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(itemType)
            .Invoke(null, new object[] { useMethod })!;

        UseDelegateCache[itemType] = action;
        return action;
    }

    private static Action<object> BuildUseAction<TItem>(MethodInfo useMethod)
    {
        var typedAction = (Action<TItem>)Delegate.CreateDelegate(typeof(Action<TItem>), null, useMethod);
        return item => typedAction((TItem)item);
    }

    private static void EnsureCoroutineHost()
    {
        if (_coroutineHost != null)
        {
            return;
        }

        var hostObject = new UnityEngine.GameObject("LongLiveBulkItemUseHost");
        UnityEngine.Object.DontDestroyOnLoad(hostObject);
        _coroutineHost = hostObject.AddComponent<CoroutineHost>();
    }

    private static PointerEventData.InputButton? TryReadPointerButton(object? eventData)
    {
        if (eventData == null)
        {
            return null;
        }

        if (eventData is PointerEventData typedEventData)
        {
            return typedEventData.button;
        }

        var buttonValue = AccessTools.Property(eventData.GetType(), "button")?.GetValue(eventData, null);
        if (buttonValue is PointerEventData.InputButton typedButton)
        {
            return typedButton;
        }

        return null;
    }

    private static bool TryReadPointerDragging(object? eventData)
    {
        if (eventData == null)
        {
            return false;
        }

        if (eventData is PointerEventData typedEventData)
        {
            return typedEventData.dragging;
        }

        var draggingValue = AccessTools.Property(eventData.GetType(), "dragging")?.GetValue(eventData, null);
        return draggingValue is bool dragging && dragging;
    }

    private static void CancelActiveRequest()
    {
        _activeRequest?.Cancel();
        if (_activeCoroutine != null && _coroutineHost != null)
        {
            _coroutineHost.StopCoroutine(_activeCoroutine);
        }

        _activeCoroutine = null;
        _activeRequest = null;
    }

    private static void ResetPointerState()
    {
        _pressedSlot = null;
        _pressedStartedTime = 0f;
        _suppressNextPointerUpUse = false;
    }

    private static void Log(string message)
    {
        Logger?.LogInfo("[BulkItemUse] " + message);
    }

    private static void LogVerbose(string message)
    {
        if (LongLivePlugin.Instance?.Options.EnableDebugLogging.Value == true)
        {
            Log(message);
        }
    }

    private static int GetMaxSelectableUseCount(BaseItem item)
    {
        var maxSelectable = item.Count;
        if (item.Type != 5)
        {
            return maxSelectable;
        }

        var itemLogic = new item(item.Id);
        if (TpUIMag.inst == null && jsonData.instance.ItemJsonData[itemLogic.itemID.ToString()]["seid"].ToList().Contains(31))
        {
            UIPopTip.Inst.Pop("需要在突破前服用", PopTipIconType.叹号);
            return 0;
        }

        var usedCount = Tools.getJsonobject(Tools.instance.getPlayer().NaiYaoXin, itemLogic.itemID.ToString());
        maxSelectable = global::GUIPackage.item.GetItemCanUseNum(itemLogic.itemID) - usedCount;
        if (maxSelectable <= 0)
        {
            UIPopTip.Inst.Pop("已到最大耐药性，无法服用", PopTipIconType.叹号);
            return 0;
        }

        var avatar = (KBEngine.Avatar)KBEngineApp.app.player();
        var currentDanDu = avatar.Dandu;
        var danDuPerUse = Math.Max((int)jsonData.instance.ItemJsonData[itemLogic.itemID.ToString()]["DanDu"].n - avatar.getStaticSkillAddSum(14), 0);
        if (danDuPerUse > 0)
        {
            if (avatar.TianFuID.HasField("18"))
            {
                danDuPerUse *= 2;
            }

            var remainingSafeUses = (120 - currentDanDu) / danDuPerUse;
            if ((120 - currentDanDu) % danDuPerUse == 0)
            {
                remainingSafeUses--;
            }

            maxSelectable = Math.Min(maxSelectable, remainingSafeUses);
        }

        maxSelectable = Math.Min(maxSelectable, item.Count);
        if (maxSelectable <= 0)
        {
            UIPopTip.Inst.Pop("已到最大丹毒值，无法服用", PopTipIconType.叹号);
            return 0;
        }

        return maxSelectable;
    }

    private sealed class BulkUseRequest
    {
        public BulkUseRequest(object item, object? slot, Action<object> useAction, int remaining)
        {
            Item = item;
            Slot = slot;
            UseAction = useAction;
            Remaining = remaining;
        }

        public object Item { get; }

        public object? Slot { get; }

        public Action<object> UseAction { get; }

        public int Remaining { get; set; }

        public bool Cancelled { get; private set; }

        public void Cancel()
        {
            Cancelled = true;
        }
    }

    private sealed class CoroutineHost : MonoBehaviour
    {
        private Coroutine? _flushCoroutine;

        private void Update()
        {
            Tick();
        }

        public void SchedulePopTipFlush()
        {
            if (_flushCoroutine != null)
            {
                StopCoroutine(_flushCoroutine);
            }

            _flushCoroutine = StartCoroutine(FlushNextFrame());
        }

        private IEnumerator FlushNextFrame()
        {
            yield return null;
            FlushAggregatedPopTips();
            _flushCoroutine = null;
        }
    }

    private sealed class AggregatedPopTip
    {
        private AggregatedPopTip(string rawMessage, string? prefix, int numericValue, string? suffix, bool hasNumericSuffix, PopTipIconType iconType, string? sound)
        {
            RawMessage = rawMessage;
            Prefix = prefix;
            NumericValue = numericValue;
            Suffix = suffix;
            HasNumericSuffix = hasNumericSuffix;
            IconType = iconType;
            Sound = sound;
            Count = 1;
        }

        public string RawMessage { get; }

        public string? Prefix { get; }

        public int NumericValue { get; set; }

        public string? Suffix { get; }

        public bool HasNumericSuffix { get; }

        public PopTipIconType IconType { get; }

        public string? Sound { get; }

        public int Count { get; set; }

        public static AggregatedPopTip CreateNumeric(string prefix, int numericValue, string? suffix, PopTipIconType iconType, string? sound)
        {
            var normalizedSuffix = suffix ?? string.Empty;
            return new AggregatedPopTip(prefix + numericValue.ToString(CultureInfo.InvariantCulture) + normalizedSuffix, prefix, numericValue, normalizedSuffix, true, iconType, sound);
        }

        public static AggregatedPopTip CreateLiteral(string rawMessage, PopTipIconType iconType, string? sound)
        {
            return new AggregatedPopTip(rawMessage, null, 0, null, false, iconType, sound);
        }

        public string BuildMessage()
        {
            if (HasNumericSuffix && Prefix != null)
            {
                return LongLiveNumericMessageParser.RebuildNumericMessage(Prefix, NumericValue, Suffix);
            }

            if (Count > 1)
            {
                return RawMessage + " x" + Count.ToString(CultureInfo.InvariantCulture);
            }

            return RawMessage;
        }
    }

    private sealed class BulkUseIterationContext
    {
        private readonly HashSet<LongLiveBulkUsePromptMarker> _markers = new HashSet<LongLiveBulkUsePromptMarker>();

        public void ObservePrompt(string? message)
        {
            LongLiveBulkItemUsePromptClassifier.Observe(message, _markers);
        }

        public bool HasMarker(LongLiveBulkUsePromptMarker marker)
        {
            return _markers.Contains(marker);
        }
    }

    private sealed class PlayerSnapshot
    {
        public PlayerSnapshot(int hp, int hpMax, int shenShi, int shouYuan, int xinJing, int experience, int level, int ziZhi, int wuXing, int dunSu, int wuDaoDian, int danDu, int naiYaoCount, int[] lingGeng, Dictionary<int, int> wuDaoExperience, Dictionary<int, int> temporaryDanYaoBuffs, HashSet<int> learnedSkillIds, HashSet<int> learnedStaticSkillIds, HashSet<int> unlockedHerbIds, HashSet<int> danFangIds, HashSet<int> yaoCaiChanDiIds, Dictionary<int, int> seaTanSuoDu, HashSet<string> itemBuffKeys, bool canSetFace)
        {
            Hp = hp;
            HpMax = hpMax;
            ShenShi = shenShi;
            ShouYuan = shouYuan;
            XinJing = xinJing;
            Experience = experience;
            Level = level;
            ZiZhi = ziZhi;
            WuXing = wuXing;
            DunSu = dunSu;
            WuDaoDian = wuDaoDian;
            DanDu = danDu;
            NaiYaoCount = naiYaoCount;
            LingGeng = lingGeng;
            WuDaoExperience = wuDaoExperience;
            TemporaryDanYaoBuffs = temporaryDanYaoBuffs;
            LearnedSkillIds = learnedSkillIds;
            LearnedStaticSkillIds = learnedStaticSkillIds;
            UnlockedHerbIds = unlockedHerbIds;
            DanFangIds = danFangIds;
            YaoCaiChanDiIds = yaoCaiChanDiIds;
            SeaTanSuoDu = seaTanSuoDu;
            ItemBuffKeys = itemBuffKeys;
            CanSetFace = canSetFace;
        }

        public int Hp { get; }

        public int HpMax { get; }

        public int ShenShi { get; }

        public int ShouYuan { get; }

        public int XinJing { get; }

        public int Experience { get; }

        public int Level { get; }

        public int ZiZhi { get; }

        public int WuXing { get; }

        public int DunSu { get; }

        public int WuDaoDian { get; }

        public int DanDu { get; }

        public int NaiYaoCount { get; }

        public int[] LingGeng { get; }

        public Dictionary<int, int> WuDaoExperience { get; }

        public Dictionary<int, int> TemporaryDanYaoBuffs { get; }

        public HashSet<int> LearnedSkillIds { get; }

        public HashSet<int> LearnedStaticSkillIds { get; }

        public HashSet<int> UnlockedHerbIds { get; }

        public HashSet<int> DanFangIds { get; }

        public HashSet<int> YaoCaiChanDiIds { get; }

        public Dictionary<int, int> SeaTanSuoDu { get; }

        public HashSet<string> ItemBuffKeys { get; }

        public bool CanSetFace { get; }
    }
}
