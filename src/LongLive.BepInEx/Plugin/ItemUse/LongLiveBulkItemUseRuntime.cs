using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Bag;
using BepInEx.Logging;
using GUIPackage;
using HarmonyLib;
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
    private static int _aggregatedExpGain;
    private static string? _aggregatedExpSound;
    private static bool _suppressAggregation;

    public static bool IsEnabled => LongLivePlugin.Instance?.Options.EnableBulkItemUseOptimization.Value == true;

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

        var button = TryReadPointerButton(eventData);
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

        var item = _pressedSlot.Item;
        var maxSelectableCount = GetMaxSelectableUseCount(item);
        _suppressNextPointerUpUse = true;

        if (maxSelectableCount <= 1)
        {
            return;
        }

        _longPressPopupOpen = true;

        var itemName = item.GetName();
        USelectNum.Show(itemName + "x{num}", 1, maxSelectableCount, number =>
        {
            TryScheduleBulkUse(item, number, _pressedSlot);
            _longPressPopupOpen = false;
            ResetPointerState();
        }, () =>
        {
            _longPressPopupOpen = false;
            ResetPointerState();
        });

        Log($"bulk item-use opened LongLive long-press selector: count={maxSelectableCount}, itemId={item.Id}");
    }

    public static bool TryAggregatePopTip(string? msg, PopTipIconType iconType, string? sound)
    {
        if (!IsEnabled || _suppressAggregation || string.IsNullOrWhiteSpace(msg))
        {
            return false;
        }

        if (iconType != PopTipIconType.上箭头)
        {
            return false;
        }

        const string prefix = "你的修为提升了";
        if (!msg!.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var suffix = msg.Substring(prefix.Length).Trim();
        if (!int.TryParse(suffix, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return false;
        }

        _aggregatedExpGain += value;
        if (!string.IsNullOrWhiteSpace(sound))
        {
            _aggregatedExpSound = sound;
        }

        if (_activeRequest == null)
        {
            EnsureCoroutineHost();
            _coroutineHost!.SchedulePopTipFlush();
        }

        return true;
    }

    private static IEnumerator RunBulkUse(BulkUseRequest request)
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

                request.UseAction(request.Item);
                request.Remaining--;
                processedThisFrame++;

                if (frameStopwatch.Elapsed >= frameBudget)
                {
                    break;
                }
            }

            TryUpdateSlotUi(request.Slot);

            if (request.Remaining > 0)
            {
                yield return null;
            }
        }

        TryUpdateSlotUi(request.Slot);
        FlushAggregatedPopTips();
        Log($"bulk item use completed: remaining={request.Remaining}");
        _activeCoroutine = null;
        _activeRequest = null;
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
            if (GetPopTipSingleton(out var popTipType, out var inst) == false)
            {
                return;
            }

            var waitForShow = AccessTools.Field(popTipType, "WaitForShow")?.GetValue(inst);
            AccessTools.Method(waitForShow?.GetType(), "Clear", Type.EmptyTypes)?.Invoke(waitForShow, Array.Empty<object>());

            AccessTools.Field(popTipType, "minCD")?.SetValue(inst, 0f);
            AccessTools.Field(popTipType, "tweenDestoryCD")?.SetValue(inst, 0f);
            AccessTools.Field(popTipType, "addItemMergeCD")?.SetValue(inst, 0f);

            var addItemMergeDict = AccessTools.Field(popTipType, "addItemMergeMsgDict")?.GetValue(inst) as IDictionary;
            addItemMergeDict?.Clear();

            var tips = AccessTools.Field(popTipType, "Tips")?.GetValue(inst) as IList;

            var existingTips = new List<object>();
            if (tips != null)
            {
                foreach (var entry in tips)
                {
                    if (entry != null)
                    {
                        existingTips.Add(entry);
                    }
                }

                tips.Clear();
            }

            foreach (var entry in existingTips)
            {
                if (entry is Component tipComponent)
                {
                    UnityEngine.Object.Destroy(tipComponent.gameObject);
                }
            }
        }
        catch (Exception exception)
        {
            LogVerbose("bulk item-use pop-tip cleanup failed: " + exception.GetType().Name + ": " + exception.Message);
        }
    }

    private static void FlushAggregatedPopTips()
    {
        if (_aggregatedExpGain <= 0)
        {
            return;
        }

        try
        {
            if (GetPopTipSingleton(out var popTipType, out var inst) == false)
            {
                return;
            }

            _suppressAggregation = true;

            var popMethod = AccessTools.Method(popTipType, "Pop", new[] { typeof(string), typeof(string), typeof(PopTipIconType) });
            if (popMethod != null)
            {
                popMethod.Invoke(inst, new object[]
                {
                    "你的修为提升了" + _aggregatedExpGain.ToString(CultureInfo.InvariantCulture),
                    _aggregatedExpSound ?? string.Empty,
                    PopTipIconType.上箭头
                });
            }
            else
            {
                popMethod = AccessTools.Method(popTipType, "Pop", new[] { typeof(string), typeof(PopTipIconType) });
                popMethod?.Invoke(inst, new object[]
                {
                    "你的修为提升了" + _aggregatedExpGain.ToString(CultureInfo.InvariantCulture),
                    PopTipIconType.上箭头
                });
            }
        }
        catch (Exception exception)
        {
            LogVerbose("bulk item-use exp pop-tip flush failed: " + exception.GetType().Name + ": " + exception.Message);
        }
        finally
        {
            _suppressAggregation = false;
            _aggregatedExpGain = 0;
            _aggregatedExpSound = null;
        }
    }

    private static void ResetAggregatedPopTips()
    {
        _aggregatedExpGain = 0;
        _aggregatedExpSound = null;
        _suppressAggregation = false;
    }

    private static bool GetPopTipSingleton(out Type popTipType, out object inst)
    {
        popTipType = AccessTools.TypeByName("UIPopTip")!;
        inst = null!;
        if (popTipType == null)
        {
            return false;
        }

        var candidate = AccessTools.Field(popTipType, "Inst")?.GetValue(null)
            ?? AccessTools.Property(popTipType, "Inst")?.GetValue(null, null);
        if (candidate == null)
        {
            return false;
        }

        inst = candidate;
        return true;
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

        var avatar = (Avatar)KBEngineApp.app.player();
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
}
