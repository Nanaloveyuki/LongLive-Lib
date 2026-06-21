using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fight;
using GUIPackage;
using System.Collections;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveFadeOptimizationRuntime
{
    private static readonly HashSet<int> AcceleratedAnimatorIds = new HashSet<int>();
    private static readonly HashSet<int> AcceleratedTweenIds = new HashSet<int>();
    private static readonly Dictionary<string, int> TranspilerPatchHitCounts = new Dictionary<string, int>(StringComparer.Ordinal);
    private static string? LastLoadedSceneName;

    public static bool IsEnabled => LongLivePlugin.Instance?.Options.EnableFadeOptimization.Value == true;

    public static float FadeDurationScale
    {
        get
        {
            var configured = LongLivePlugin.Instance?.Options.FadeDurationScale.Value ?? 0.5f;
            return Math.Max(0.05f, configured);
        }
    }

    public static float MapDoorTransitionSeconds
    {
        get
        {
            var configured = LongLivePlugin.Instance?.Options.MapDoorTransitionSeconds.Value ?? 0.35f;
            return Math.Max(0.05f, configured);
        }
    }

    public static float GetMapDoorTransitionDelay(float originalSeconds)
    {
        if (!IsEnabled)
        {
            return originalSeconds;
        }

        var scaled = Math.Max(0.05f, originalSeconds * FadeDurationScale);
        var resolved = Math.Min(scaled, MapDoorTransitionSeconds);
        LogVerbose($"resolved map-door transition delay: original={originalSeconds:0.###}, scaled={scaled:0.###}, final={resolved:0.###}");
        return resolved;
    }

    public static float GetBattleReadyDelay(float originalSeconds)
    {
        if (!IsEnabled)
        {
            return originalSeconds;
        }

        var scaled = originalSeconds * FadeDurationScale;
        var resolved = Math.Max(0.01f, Math.Min(scaled, 0.02f));
        LogVerbose($"resolved battle-ready delay: original={originalSeconds:0.###}, scaled={scaled:0.###}, final={resolved:0.###}");
        return resolved;
    }

    public static float GetLoadingSceneTipToDoorDelay(float originalSeconds)
    {
        if (!IsEnabled)
        {
            return originalSeconds;
        }

        var sceneName = LastLoadedSceneName ?? SceneManager.GetActiveScene().name;
        if (IsWorldOrSeaScene(sceneName))
        {
            var resolved = Math.Max(0.12f, Math.Min(originalSeconds * FadeDurationScale, 0.22f));
            LogVerbose($"resolved loading-scene tip-to-door delay: scene={sceneName}, original={originalSeconds:0.###}, final={resolved:0.###}");
            return resolved;
        }

        return ScaleDuration(originalSeconds);
    }

    public static float GetLoadingSceneOutroDelay(float originalSeconds, int scaleMode)
    {
        if (!IsEnabled)
        {
            return originalSeconds;
        }

        var sceneName = LastLoadedSceneName ?? SceneManager.GetActiveScene().name;
        if (IsWorldOrSeaScene(sceneName))
        {
            var resolved = Math.Max(0.15f, Math.Min(originalSeconds * FadeDurationScale, 0.26f));
            LogVerbose($"resolved loading-scene outro delay: scene={sceneName}, scaleMode={scaleMode}, original={originalSeconds:0.###}, final={resolved:0.###}");
            return resolved;
        }

        return ScaleDuration(originalSeconds);
    }

    public static float GetBlackMaskReplayDelay(string patchKey)
    {
        var resolved = GetMapDoorTransitionDelay(0.1f);
        var sceneName = LastLoadedSceneName ?? SceneManager.GetActiveScene().name;
        if (!IsWorldOrSeaScene(sceneName))
        {
            return resolved;
        }

        if (patchKey.IndexOf("asyncRedirect", StringComparison.Ordinal) >= 0
            || patchKey.IndexOf("UI_Manager.Update", StringComparison.Ordinal) >= 0)
        {
            var bridged = Math.Max(0.18f, Math.Min(ScaleDuration(0.45f), 0.3f));
            LogVerbose($"resolved black-mask replay bridge delay: scene={sceneName}, key={patchKey}, final={bridged:0.###}");
            return bridged;
        }

        return resolved;
    }

    public static bool TryCreateLoadingSceneEntryRoutine(Loading? instance, Camera? camera, int scaleMode, float delay, out IEnumerator? routine)
    {
        routine = null;
        if (!IsEnabled || instance == null || camera == null)
        {
            return false;
        }

        var sceneName = LastLoadedSceneName ?? SceneManager.GetActiveScene().name;
        if (!IsWorldOrSeaScene(sceneName))
        {
            return false;
        }

        routine = RunLoadingSceneEntry(instance, camera, scaleMode, delay, sceneName);
        LogVerbose($"created custom loading-scene entry routine: scene={sceneName}, scaleMode={scaleMode}, delay={delay:0.###}");
        return true;
    }

    public static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        LastLoadedSceneName = scene.name;

        if (AcceleratedAnimatorIds.Count == 0)
        {
            return;
        }

        var clearedCount = AcceleratedAnimatorIds.Count;
        AcceleratedAnimatorIds.Clear();
        AcceleratedTweenIds.Clear();
        LogVerbose($"cleared accelerated animator state on scene load: scene={scene.name}, mode={mode}, count={clearedCount}");
    }

    public static void OnPluginShutdown()
    {
        if (AcceleratedAnimatorIds.Count == 0)
        {
            AcceleratedTweenIds.Clear();
            TranspilerPatchHitCounts.Clear();
            return;
        }

        var clearedCount = AcceleratedAnimatorIds.Count;
        AcceleratedAnimatorIds.Clear();
        AcceleratedTweenIds.Clear();
        TranspilerPatchHitCounts.Clear();
        LogVerbose($"cleared accelerated animator state on plugin shutdown: count={clearedCount}");
    }

    public static void ReportTranspilerPatchResult(string patchKey, float targetValue, int hitCount)
    {
        TranspilerPatchHitCounts[patchKey] = hitCount;

        if (hitCount > 0)
        {
            LogVerbose($"fade transpiler patch matched: key={patchKey}, target={targetValue:0.###}, hits={hitCount}");
            return;
        }

        LogVerbose($"fade transpiler patch found no matching constants: key={patchKey}, target={targetValue:0.###}");
    }

    public static void ReconcileAnimatorSpeed(Animator? animator, string? stateName)
    {
        if (!IsEnabled || animator == null || string.IsNullOrWhiteSpace(stateName))
        {
            return;
        }

        var animatorId = animator.GetInstanceID();
        if (!IsSupportedFadeState(stateName!))
        {
            if (AcceleratedAnimatorIds.Remove(animatorId) && Math.Abs(animator.speed - 1f) > 0.001f)
            {
                animator.speed = 1f;
                LogVerbose($"restored animator speed for non-fade state: name={stateName}, speed={animator.speed:0.###}");
            }

            return;
        }

        animator.speed = 1f / FadeDurationScale;
        AcceleratedAnimatorIds.Add(animatorId);
        LogVerbose($"accelerated animator state: name={stateName}, speed={animator.speed:0.###}");
    }

    public static float ScaleDuration(float duration)
    {
        if (!IsEnabled)
        {
            return duration;
        }

        var scaled = Math.Max(0.05f, duration * FadeDurationScale);
        LogVerbose($"scaled duration: original={duration:0.###}, final={scaled:0.###}");
        return scaled;
    }

    public static bool TryHandleSameSceneMapReturn(string? finalScene)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(finalScene))
        {
            return false;
        }

        var activeScene = SceneManager.GetActiveScene().name;
        if (!string.Equals(activeScene, finalScene, StringComparison.Ordinal))
        {
            return false;
        }

        PanelMamager.CanOpenOrClose = true;
        Tools.canClickFlag = true;
        LogVerbose($"skipped same-scene map reload: scene={activeScene}");
        return true;
    }

    public static bool TryHandleImmediateFightSceneEntry(UI_Manager? uiManager)
    {
        if (!IsEnabled || uiManager == null || Tools.instance == null)
        {
            return false;
        }

        if (!Tools.instance.isNeedSetTalk || Tools.instance.loadSceneType != 0)
        {
            return false;
        }

        var targetScene = Tools.instance.ohtherSceneName;
        if (!IsImmediateFightScene(targetScene))
        {
            return false;
        }

        var activeScene = SceneManager.GetActiveScene().name;
        if (!string.Equals(activeScene, targetScene, StringComparison.Ordinal))
        {
            return false;
        }

        Tools.instance.isNeedSetTalk = false;
        PanelMamager.CanOpenOrClose = true;
        Tools.canClickFlag = true;

        var blackMask = PanelMamager.inst?.UIBlackMaskGameObject;
        if (blackMask != null && blackMask.activeSelf)
        {
            blackMask.SetActive(false);
        }

        if (ResManager.inst != null)
        {
            UnityEngine.Object.Instantiate(ResManager.inst.LoadPrefab("threeScreenUI"), uiManager.gameObject.transform);
            UnityEngine.Object.Instantiate(ResManager.inst.LoadPrefab("NPCView"), uiManager.gameObject.transform);
        }

        uiManager.checkTool?.Init();
        LogVerbose($"bypassed black-mask replay for immediate fight scene entry: scene={activeScene}");
        return true;
    }

    public static bool TryHandleAsyncMapSceneLoad(Tools? tools, string? sceneName, bool lastSceneIsValue)
    {
        if (!IsEnabled || tools == null || string.IsNullOrWhiteSpace(sceneName))
        {
            return false;
        }

        // The async NextScene bridge is fast, but battle-return quest settlement is sensitive
        // to the original loadMapScenes order. Keep the original return path for stability.
        if (string.Equals(sceneName, "AllMaps", StringComparison.Ordinal))
        {
            var currentScene = SceneManager.GetActiveScene().name;
            if (IsImmediateFightScene(currentScene))
            {
                LogVerbose($"kept original fight-return map load path for stability: current={currentScene}, target={sceneName}");
                return false;
            }
        }

        var activeSceneName = SceneManager.GetActiveScene().name;
        if (string.Equals(activeSceneName, "NextScene", StringComparison.Ordinal))
        {
            return false;
        }

        if (!ShouldUseAsyncMapSceneLoad(activeSceneName, sceneName!))
        {
            LogVerbose($"kept original map-scene load path: current={activeSceneName}, target={sceneName}");
            return false;
        }

        if (!string.Equals(sceneName, "LianDan", StringComparison.Ordinal) && lastSceneIsValue)
        {
            tools.getPlayer().lastScence = sceneName;
        }

        Tools.jumpToName = sceneName;
        tools.loadSceneType = 1;
        tools.isNeedSetTalk = true;

        var blackMask = PanelMamager.inst?.UIBlackMaskGameObject;
        if (blackMask == null && ResManager.inst != null)
        {
            UnityEngine.Object.Instantiate(ResManager.inst.LoadPrefab("BlackHide"));
            blackMask = PanelMamager.inst?.UIBlackMaskGameObject;
        }

        if (blackMask != null)
        {
            blackMask.SetActive(false);
            blackMask.SetActive(true);
            ReconcileBlackMaskReplay(blackMask, "Tools.loadMapScenes.asyncRedirect");
        }

        if (ThreeSceernUIFab.inst != null)
        {
            UnityEngine.Object.Destroy(ThreeSceernUIFab.inst.gameObject);
        }

        if (ThreeSceneMagFab.inst != null)
        {
            UnityEngine.Object.Destroy(ThreeSceneMagFab.inst.gameObject);
        }

        tools.CanOpenTab = true;
        SceneManager.LoadScene("NextScene");
        LogVerbose($"redirected map-scene load to async NextScene bridge: current={activeSceneName}, target={sceneName}");
        return true;
    }

    public static void ReconcileBlackMaskReplay(GameObject? target, string source)
    {
        if (!IsEnabled || target == null)
        {
            return;
        }

        TryAccelerateAnimatorOnObject(target, source + ".animator");
        TryShortCircuitBlackMaskReplay(target, source + ".replay");
    }

    public static void ReconcileUiBlackOverlay(GameObject? target, string source)
    {
        if (!IsEnabled || target == null)
        {
            return;
        }

        TryAccelerateAnimatorOnObject(target, source + ".animator");
        TryShortCircuitBlackOverlay(target, source + ".overlay");
    }

    public static void TryAccelerateTweenField(object? host, string fieldName, string patchKey)
    {
        if (!IsEnabled || host == null || string.IsNullOrWhiteSpace(fieldName))
        {
            return;
        }

        try
        {
            var field = host.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var tween = field?.GetValue(host);
            TryAccelerateTweenObject(tween, patchKey);
        }
        catch (Exception exception)
        {
            LogVerbose($"tween acceleration failed: key={patchKey}, reason={exception.GetType().Name}: {exception.Message}");
        }
    }

    public static void TryAccelerateAnimatorOnObject(GameObject? target, string patchKey)
    {
        if (!IsEnabled || target == null)
        {
            return;
        }

        try
        {
            var animator = target.GetComponent<Animator>();
            if (animator == null)
            {
                return;
            }

            var targetSpeed = 1f / FadeDurationScale;
            if (Math.Abs(animator.speed - targetSpeed) < 0.001f)
            {
                return;
            }

            animator.speed = targetSpeed;
            AcceleratedAnimatorIds.Add(animator.GetInstanceID());
            LogVerbose($"accelerated black-mask animator: key={patchKey}, speed={targetSpeed:0.###}, object={target.name}");
        }
        catch (Exception exception)
        {
            LogVerbose($"black-mask animator acceleration failed: key={patchKey}, reason={exception.GetType().Name}: {exception.Message}");
        }
    }

    public static void TryShortCircuitBlackMaskReplay(GameObject? target, string patchKey)
    {
        if (!IsEnabled || target == null || !target.activeSelf)
        {
            return;
        }

        var host = LongLivePlugin.Instance;
        if (host == null)
        {
            return;
        }

        host.StartCoroutine(DisableAfterDelay(target, GetBlackMaskReplayDelay(patchKey), patchKey));
    }

    public static void TryShortCircuitBlackOverlay(GameObject? target, string patchKey)
    {
        if (!IsEnabled || target == null)
        {
            return;
        }

        var host = LongLivePlugin.Instance;
        if (host == null)
        {
            return;
        }

        host.StartCoroutine(DisableAfterDelay(target, ScaleDuration(0.5f), patchKey));
    }

    private static void TryAccelerateTweenObject(object? tween, string patchKey)
    {
        if (tween == null)
        {
            return;
        }

        var tweenId = tween.GetHashCode();
        var tweenType = tween.GetType();
        var timeScaleProperty = tweenType.GetProperty("timeScale", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (timeScaleProperty == null || !timeScaleProperty.CanWrite || timeScaleProperty.PropertyType != typeof(float))
        {
            return;
        }

        var targetSpeed = 1f / FadeDurationScale;
        var currentSpeed = (float?)timeScaleProperty.GetValue(tween, null) ?? 1f;
        if (Math.Abs(currentSpeed - targetSpeed) < 0.001f)
        {
            return;
        }

        timeScaleProperty.SetValue(tween, targetSpeed, null);
        if (AcceleratedTweenIds.Add(tweenId))
        {
            LogVerbose($"accelerated tween: key={patchKey}, speed={targetSpeed:0.###}, tweenType={tweenType.FullName}");
        }
    }

    private static IEnumerator DisableAfterDelay(GameObject target, float delay, string patchKey)
    {
        yield return new WaitForSeconds(Math.Max(0.01f, delay));
        if (target != null && target.activeSelf)
        {
            target.SetActive(false);
            Tools.canClickFlag = true;
            LogVerbose($"short-circuited black-screen object after delay: key={patchKey}, delay={delay:0.###}, object={target.name}");
        }
    }

    private static IEnumerator RunLoadingSceneEntry(Loading instance, Camera camera, int scaleMode, float delay, string sceneName)
    {
        StagesParser.loadingTip = -1;
        var transform = instance.transform;
        var time = GetLoadingSceneOutroDelay(0.45f, scaleMode);

        if (scaleMode == 2)
        {
            transform.localScale = new Vector3(0.334f, 0.334f, 0.334f);
            transform.position = new Vector3(9f, -44.061f, -25.05859f);
            time = GetLoadingSceneOutroDelay(0.65f, scaleMode);
        }
        else if (scaleMode == 3)
        {
            transform.localScale = new Vector3(0.334f, 0.334f, 0.334f);
            transform.position = new Vector3(82.20029f, -40.65633f, -25.05859f);
            time = GetLoadingSceneOutroDelay(0.65f, scaleMode);
        }
        else if (scaleMode == 5)
        {
            transform.position = new Vector3(0f, 0f, -5f);
            var gateAnimator = transform.Find("Loading Animation Vrata")?.GetComponent<Animator>();
            if (gateAnimator != null)
            {
                gateAnimator.speed = Math.Max(2f, 1f / FadeDurationScale);
            }

            time = 0f;
        }
        else
        {
            transform.position = new Vector3(camera.transform.position.x, camera.transform.position.y, camera.transform.position.z + 5f);
        }

        var startDelay = ScaleDuration(delay);
        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        var tipAnimator = transform.Find("Loading Animation Tip-s")?.GetComponent<Animator>();
        tipAnimator?.Play("Loading Tip Odlazak");

        if (time > 0f)
        {
            yield return new WaitForSeconds(time);
        }

        var doorAnimator = transform.Find("Loading Animation Vrata")?.GetComponent<Animator>();
        doorAnimator?.Play("Loading Zidovi Odlazak");

        var destroyDelay = GetLoadingSceneTipToDoorDelay(1f);
        yield return new WaitForSeconds(destroyDelay);

        UnityEngine.Object.Destroy(instance.gameObject);
        if (scaleMode == 1)
        {
            var goScreen = GameObject.Find("GO screen");
            if (goScreen != null)
            {
                var collider = goScreen.GetComponent("Collider");
                if (collider != null)
                {
                    var enabledProperty = collider.GetType().GetProperty("enabled", BindingFlags.Instance | BindingFlags.Public);
                    if (enabledProperty != null && enabledProperty.CanWrite && enabledProperty.PropertyType == typeof(bool))
                    {
                        enabledProperty.SetValue(collider, true, null);
                    }
                }
            }
        }

        LogVerbose($"completed custom loading-scene entry routine: scene={sceneName}, scaleMode={scaleMode}, outro={time:0.###}, destroyDelay={destroyDelay:0.###}");
    }

    private static bool IsSupportedFadeState(string stateName)
    {
        return string.Equals(stateName, "Loading Zidovi Dolazak", StringComparison.Ordinal)
            || string.Equals(stateName, "Loading Zidovi Odlazak", StringComparison.Ordinal)
            || string.Equals(stateName, "Fader In", StringComparison.Ordinal);
    }

    private static bool IsWorldOrSeaScene(string? sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return false;
        }

        return string.Equals(sceneName, "AllMaps", StringComparison.Ordinal)
            || string.Equals(sceneName, "Sea", StringComparison.Ordinal)
            || sceneName.IndexOf("Sea", StringComparison.Ordinal) >= 0;
    }

    private static bool IsImmediateFightScene(string? sceneName)
    {
        return string.Equals(sceneName, "YSNewFight", StringComparison.Ordinal)
            || string.Equals(sceneName, "YSFight", StringComparison.Ordinal);
    }

    private static bool ShouldUseAsyncMapSceneLoad(string currentSceneName, string targetSceneName)
    {
        if (!string.Equals(targetSceneName, "AllMaps", StringComparison.Ordinal))
        {
            return false;
        }

        return IsImmediateFightScene(currentSceneName);
    }

    private static void LogVerbose(string message)
    {
        if (LongLivePlugin.Instance?.Options.EnableDebugLogging.Value == true)
        {
            LongLivePlugin.LogSource?.LogInfo("[FadeOptimization] " + message);
        }
    }
}
