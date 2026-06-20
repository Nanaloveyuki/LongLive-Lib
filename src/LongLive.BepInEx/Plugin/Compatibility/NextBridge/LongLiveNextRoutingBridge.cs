using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fungus;
using JSONClass;
using LongLive.Mods.SceneRouting;

namespace LongLive.BepInEx.Plugin;

internal static class LongLiveNextRoutingBridge
{
    public static LongLiveSceneAddress CreateAddress(string? logicalSceneId, string? sceneName, int? entryIndex = null, bool autoResolveEntryIndex = true)
    {
        return new LongLiveSceneAddress
        {
            LogicalSceneId = logicalSceneId ?? string.Empty,
            SceneName = sceneName ?? string.Empty,
            EntryIndex = entryIndex,
            AutoResolveEntryIndex = autoResolveEntryIndex,
            PreserveLastScene = true,
        };
    }

    public static string ResolveCurrentSceneName()
    {
        return LongLivePluginContext.SceneRouting.CaptureSnapshot().ActiveSceneName;
    }

    public static int ResolveCurrentMapIndex()
    {
        return LongLivePluginContext.SceneRouting.CaptureSnapshot().PlayerNowMapIndex ?? 0;
    }

    public static int ResolveCurrentFuBenIndex()
    {
        return LongLivePluginContext.SceneRouting.CaptureSnapshot().CurrentFuBenIndex ?? 0;
    }

    public static string ResolveCurrentPlaceName()
    {
        return LongLivePluginContext.SceneRouting.CaptureSnapshot().PlaceName;
    }

    public static int ResolveCurrentMapType()
    {
        try
        {
            var sceneName = ResolveCurrentSceneName();
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return 0;
            }

            if (SceneNameJsonData.DataDict.TryGetValue(sceneName, out var metadata) && metadata is not null)
            {
                return metadata.MapType;
            }
        }
        catch
        {
        }

        return 0;
    }

    public static LongLiveSceneRouteResult WarpPlayer(string? logicalSceneId, string? sceneName, int? entryIndex = null, bool autoResolveEntryIndex = true)
    {
        var address = CreateAddress(logicalSceneId, sceneName, entryIndex, autoResolveEntryIndex);
        return LongLivePluginContext.SceneRouting.WarpPlayer(address);
    }

    public static LongLiveSceneRouteResult WarpNpc(int npcId, string? logicalSceneId, string? sceneName, int? entryIndex = null, bool autoResolveEntryIndex = true)
    {
        var address = CreateAddress(logicalSceneId, sceneName, entryIndex, autoResolveEntryIndex);
        return LongLivePluginContext.SceneRouting.WarpNpc(npcId, address);
    }

    public static bool TryMovePlayerToSceneEntry(int index)
    {
        try
        {
            AvatarTransfer.Do(index);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryWalkPlayerOnWorldMap(int index)
    {
        try
        {
            if (!string.Equals(Tools.getScreenName(), "AllMaps", StringComparison.Ordinal))
            {
                return false;
            }

            AllMapManage.instance.mapIndex[index].movaAvatar();
            PlayerEx.Player.NowMapIndex = index;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string ResolveNpcName(int npcId)
    {
        try
        {
            var normalizedNpcId = NPCEx.NPCIDToNew(npcId);
            var oldNpcId = NPCEx.NPCIDToOld(normalizedNpcId);

            if (normalizedNpcId == 0)
            {
                return "旁白";
            }

            if (normalizedNpcId == 1)
            {
                return Tools.GetPlayerName();
            }

            if (jsonData.instance.AvatarRandomJsonData.HasField(normalizedNpcId.ToString()))
            {
                return ToolsEx.ToCN(jsonData.instance.AvatarRandomJsonData[normalizedNpcId.ToString()]["Name"].str);
            }

            if (NpcJieSuanManager.inst != null
                && NpcJieSuanManager.inst.npcDeath != null
                && NpcJieSuanManager.inst.npcDeath.npcDeathJson.HasField(normalizedNpcId.ToString()))
            {
                return ToolsEx.ToCN(NpcJieSuanManager.inst.npcDeath.npcDeathJson[normalizedNpcId.ToString()]["deathName"].str);
            }

            if (jsonData.instance.AvatarJsonData.HasField(oldNpcId.ToString()))
            {
                var npcData = jsonData.instance.AvatarJsonData[oldNpcId.ToString()];
                return ToolsEx.ToCN(npcData["FirstName"].str) + ToolsEx.ToCN(npcData["Name"].str);
            }
        }
        catch
        {
        }

        return "未知";
    }

    public static bool RollProbability(int threshold)
    {
        var clampedThreshold = Math.Max(0, Math.Min(100, threshold));
        var value = UnityEngine.Random.Range(0, 100);
        return value < clampedThreshold;
    }

    public static bool ResolveNearNpcContains(object? nativeEnvironment, object? argument, int probability, out int roleId, out string roleName, out int roleBindId)
    {
        roleId = 0;
        roleName = string.Empty;
        roleBindId = 0;

        try
        {
            var nearNpcList = TryReadNearNpcList(nativeEnvironment);
            if (nearNpcList is null || nearNpcList.Count == 0)
            {
                return false;
            }

            var candidateIds = NormalizeNpcArguments(argument);
            if (candidateIds.Count == 0)
            {
                return false;
            }

            var oldIds = candidateIds.Select(NPCEx.NPCIDToOld).ToArray();
            var match = nearNpcList.FirstOrDefault(x => candidateIds.Contains(x));
            if (match <= 0)
            {
                match = nearNpcList.FirstOrDefault(x => oldIds.Contains(x));
            }

            if (match <= 0 || !RollProbability(probability))
            {
                return false;
            }

            roleId = NPCEx.NPCIDToNew(match);
            roleBindId = NPCEx.NPCIDToOld(match);
            roleName = ResolveNpcName(match);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool ResolvePlayerHasDongFu(int dongFuId)
    {
        try
        {
            return DongFuManager.PlayerHasDongFu(dongFuId);
        }
        catch
        {
            return false;
        }
    }

    public static int ResolvePlayerShengWang(int shengWangId)
    {
        try
        {
            return PlayerEx.GetShengWang(shengWangId);
        }
        catch
        {
            return 0;
        }
    }

    public static void AddPlayerShengWang(int shengWangId, int amount, bool show = true)
    {
        try
        {
            PlayerEx.AddShengWang(shengWangId, amount, show);
        }
        catch
        {
        }
    }

    public static int ResolveSearchNpc(int type = 0, int liuPai = 0, int level = 0, int sex = 0, int zhengXie = 0)
    {
        try
        {
            var matches = jsonData.instance.AvatarJsonData.list
                .Where(static npc => npc["id"].I >= 20000 && !npc.HasField("IsFly"));

            if (type > 0)
            {
                matches = matches.Where(npc => npc["Type"].I == type);
            }

            if (liuPai > 0)
            {
                matches = matches.Where(npc => npc["LiuPai"].I == liuPai);
            }

            if (level > 0)
            {
                matches = matches.Where(npc => npc["Level"].I == level);
            }

            if (sex > 0)
            {
                matches = matches.Where(npc => npc["SexType"].I == sex);
            }

            if (zhengXie == 1)
            {
                matches = matches.Where(npc => npc["XingGe"].I < 10);
            }
            else if (zhengXie == 2)
            {
                matches = matches.Where(npc => npc["XingGe"].I > 10);
            }

            var candidateIds = matches.Select(static npc => npc["id"].I).ToList();
            if (candidateIds.Count == 0)
            {
                return 0;
            }

            var selectedIndex = ResolveRandomInt(candidateIds.Count);
            return selectedIndex >= 0 && selectedIndex < candidateIds.Count ? candidateIds[selectedIndex] : 0;
        }
        catch
        {
            return 0;
        }
    }

    public static int ResolveSearchNpcByFavor(int minimumFavor = -201)
    {
        try
        {
            var candidateIds = new List<int>();
            foreach (var key in jsonData.instance.AvatarRandomJsonData.keys)
            {
                if (!int.TryParse(key, out var npcId) || npcId < 20000)
                {
                    continue;
                }

                var npc = jsonData.instance.AvatarRandomJsonData[key];
                if (npc["HaoGanDu"].I >= minimumFavor)
                {
                    candidateIds.Add(npcId);
                }
            }

            if (candidateIds.Count == 0)
            {
                return 0;
            }

            var selectedIndex = ResolveRandomInt(candidateIds.Count);
            return selectedIndex >= 0 && selectedIndex < candidateIds.Count ? candidateIds[selectedIndex] : 0;
        }
        catch
        {
            return 0;
        }
    }

    public static int ResolveCreateNpc(int type = 0, int liuPai = 0, int level = 0, int sex = 0, int zhengXie = 0)
    {
        try
        {
            var candidates = jsonData.instance.NPCLeiXingDate.list.AsEnumerable();

            if (type > 0)
            {
                candidates = candidates.Where(npc => npc["Type"].I == type);
            }

            if (liuPai > 0)
            {
                candidates = candidates.Where(npc => npc["LiuPai"].I == liuPai);
            }

            if (level > 0)
            {
                candidates = candidates.Where(npc => npc["Level"].I == level);
            }

            var candidateList = candidates.ToList();
            if (candidateList.Count == 0 || FactoryManager.inst?.npcFactory is null)
            {
                return 0;
            }

            var selectedIndex = ResolveRandomInt(candidateList.Count);
            if (selectedIndex < 0 || selectedIndex >= candidateList.Count)
            {
                return 0;
            }

            var npcId = FactoryManager.inst.npcFactory.AfterCreateNpc(candidateList[selectedIndex], false, 0, false, null, sex);
            if ((zhengXie == 1 || zhengXie == 2) && jsonData.instance.AvatarJsonData.HasField(npcId.ToString()))
            {
                var xingGe = FactoryManager.inst.npcFactory.getRandomXingGe(zhengXie);
                jsonData.instance.AvatarJsonData[npcId.ToString()].SetField("XingGe", xingGe);
            }

            return npcId;
        }
        catch
        {
            return 0;
        }
    }

    public static string ResolveNpcSayName(int npcId)
    {
        return ResolveNpcAddressingPlayer(npcId, includePlayerFirstName: false);
    }

    public static string ResolveNpcSayNameFirstName(int npcId)
    {
        return ResolveNpcAddressingPlayer(npcId, includePlayerFirstName: true);
    }

    public static string ResolvePlayerSayNpcName(int npcId)
    {
        try
        {
            var player = PlayerEx.Player;
            var normalizedNpcId = NPCEx.NPCIDToNew(npcId);
            if (player is null || normalizedNpcId < 20000 || !TryGetNpcSocialData(normalizedNpcId, out var npcLevel, out var npcSex, out var npcAge, out var npcMenPai, out _))
            {
                return "道友";
            }

            var playerLevel = (int)player.level;
            var playerSex = player.Sex;
            var playerAge = (int)player.age;
            var playerMenPai = (int)player.menPai;
            var npcName = ResolveNpcName(normalizedNpcId);
            var npcFavor = NPCEx.GetFavor(normalizedNpcId);

            if (player.DaoLvId.HasItem(normalizedNpcId))
            {
                return ResolveDaoLvPlayerCall(npcName);
            }

            if (player.Brother.HasItem(normalizedNpcId))
            {
                return playerSex == 1 ? "兄弟" : "姑娘";
            }

            if (player.TeatherId.HasItem(normalizedNpcId))
            {
                return "徒儿";
            }

            if (player.TuDiId.HasItem(normalizedNpcId))
            {
                return "师傅";
            }

            if (npcFavor >= 140)
            {
                if (playerAge > npcAge)
                {
                    if (playerSex == 1)
                    {
                        return npcSex == 1 ? "兄弟" : "哥哥";
                    }

                    if (playerSex == 2)
                    {
                        return npcSex == 1 ? "姑娘" : "姐姐";
                    }
                }
                else
                {
                    if (playerSex == 1)
                    {
                        return npcSex == 1 ? "兄弟" : "弟弟";
                    }

                    if (playerSex == 2)
                    {
                        return npcSex == 1 ? "妹子" : "妹妹";
                    }
                }
            }

            if (playerMenPai == npcMenPai)
            {
                if (playerLevel > npcLevel)
                {
                    if (playerLevel > 12 && npcLevel < 13)
                    {
                        return "师祖";
                    }

                    if (playerLevel > 6 && npcLevel < 7)
                    {
                        return "师叔";
                    }

                    return playerSex == 1 ? "师兄" : "师姐";
                }

                if (playerLevel < npcLevel)
                {
                    if (npcLevel > 12 && playerLevel < 13)
                    {
                        return "师侄";
                    }

                    if (npcLevel > 6 && playerLevel < 7)
                    {
                        return "师侄";
                    }

                    return playerSex == 1 ? "师弟" : "师妹";
                }

                if (playerAge >= npcAge)
                {
                    return playerSex == 1 ? "师兄" : "师姐";
                }

                return playerSex == 1 ? "师弟" : "师妹";
            }

            if (npcFavor >= 40)
            {
                return npcName;
            }

            if (playerLevel / 3 > npcLevel / 3)
            {
                return "前辈";
            }

            if (playerLevel / 3 < npcLevel / 3)
            {
                return "小友";
            }

            return "道友";
        }
        catch
        {
            return "道友";
        }
    }

    public static int ResolveRandomInt(int exclusiveMax, int inclusiveMin = 0)
    {
        var min = inclusiveMin;
        var max = exclusiveMax;
        if (max <= min)
        {
            return min;
        }

        return UnityEngine.Random.Range(min, max);
    }

    public static int ResolveNpcNewId(int npcId)
    {
        try
        {
            return NPCEx.NPCIDToNew(npcId);
        }
        catch
        {
            return npcId;
        }
    }

    public static bool ResolveIsTaiMiaoYear()
    {
        try
        {
            var nowTime = Tools.instance.getPlayer().worldTimeMag.getNowTime();
            return nowTime.Year % 20 == 0 && nowTime.Month == 2;
        }
        catch
        {
            return false;
        }
    }

    public static bool ResolveHasItem(int itemId, bool isNpc, int npcId)
    {
        try
        {
            if (!isNpc)
            {
                return Tools.instance.getPlayer().hasItem(itemId);
            }

            var normalizedNpcId = NPCEx.NPCIDToNew(npcId);
            var npcStatus = jsonData.instance.AvatarBackpackJsonData[normalizedNpcId.ToString()];
            if (npcStatus is null || !npcStatus.HasField("Backpack"))
            {
                return false;
            }

            return npcStatus["Backpack"].list.Find(item => (int)item["ItemID"].n == itemId) is not null;
        }
        catch
        {
            return false;
        }
    }

    public static bool ResolveHasWorkshopMod(string modName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(modName))
            {
                return false;
            }

            foreach (var modDir in WorkshopTool.GetAllModDirectory())
            {
                if (modDir.FullName.Contains(modName))
                {
                    return !WorkshopTool.CheckModIsDisable(modName);
                }
            }
        }
        catch
        {
        }

        return false;
    }

    private static string ResolveNpcAddressingPlayer(int npcId, bool includePlayerFirstName)
    {
        var player = PlayerEx.Player;
        var playerFirstName = player?.firstName ?? string.Empty;
        var defaultAddress = includePlayerFirstName ? playerFirstName + "道友" : "道友";

        try
        {
            if (player is null)
            {
                return defaultAddress;
            }

            var normalizedNpcId = NPCEx.NPCIDToNew(npcId);
            if (normalizedNpcId < 20000 || !TryGetNpcSocialData(normalizedNpcId, out var npcLevel, out var npcSex, out var npcAge, out var npcMenPai, out _))
            {
                return defaultAddress;
            }

            var playerLevel = (int)player.level;
            var playerSex = player.Sex;
            var playerAge = (int)player.age;
            var playerMenPai = (int)player.menPai;
            var npcFavor = NPCEx.GetFavor(normalizedNpcId);

            if (player.DaoLvId.HasItem(normalizedNpcId))
            {
                return PlayerEx.GetDaoLvNickName(normalizedNpcId);
            }

            if (player.Brother.HasItem(normalizedNpcId))
            {
                return includePlayerFirstName
                    ? playerFirstName + (playerSex == 1 ? "兄弟" : "姑娘")
                    : playerSex == 1 ? "兄弟" : "姑娘";
            }

            if (player.TeatherId.HasItem(normalizedNpcId))
            {
                return "徒儿";
            }

            if (player.TuDiId.HasItem(normalizedNpcId))
            {
                return "师傅";
            }

            if (npcFavor >= 140)
            {
                if (playerAge > npcAge)
                {
                    if (playerSex == 1)
                    {
                        return includePlayerFirstName
                            ? playerFirstName + (npcSex == 1 ? "兄弟" : "哥哥")
                            : npcSex == 1 ? "兄弟" : "哥哥";
                    }

                    if (playerSex == 2)
                    {
                        return includePlayerFirstName
                            ? playerFirstName + (npcSex == 1 ? "姑娘" : "姐姐")
                            : npcSex == 1 ? "姑娘" : "姐姐";
                    }
                }
                else
                {
                    if (playerSex == 1)
                    {
                        return includePlayerFirstName
                            ? playerFirstName + (npcSex == 1 ? "兄弟" : "弟弟")
                            : npcSex == 1 ? "兄弟" : "弟弟";
                    }

                    if (playerSex == 2)
                    {
                        return includePlayerFirstName
                            ? playerFirstName + (npcSex == 1 ? "妹子" : "妹妹")
                            : npcSex == 1 ? "妹子" : "妹妹";
                    }
                }
            }

            if (playerMenPai != 0 && playerMenPai == npcMenPai)
            {
                if (playerLevel > npcLevel)
                {
                    if (playerLevel > 12 && npcLevel < 13)
                    {
                        return includePlayerFirstName ? playerFirstName + "师祖" : "师祖";
                    }

                    if (playerLevel > 6 && npcLevel < 7)
                    {
                        return includePlayerFirstName ? playerFirstName + "师叔" : "师叔";
                    }

                    return includePlayerFirstName
                        ? playerFirstName + (playerSex == 1 ? "师兄" : "师姐")
                        : playerSex == 1 ? "师兄" : "师姐";
                }

                if (playerLevel < npcLevel)
                {
                    if (npcLevel > 12 && playerLevel < 13)
                    {
                        return includePlayerFirstName ? playerFirstName + "师侄" : "师侄";
                    }

                    if (npcLevel > 6 && playerLevel < 7)
                    {
                        return includePlayerFirstName ? playerFirstName + "师侄" : "师侄";
                    }

                    return includePlayerFirstName
                        ? playerFirstName + (playerSex == 1 ? "师弟" : "师妹")
                        : playerSex == 1 ? "师弟" : "师妹";
                }

                if (playerAge >= npcAge)
                {
                    return includePlayerFirstName
                        ? playerFirstName + (playerSex == 1 ? "师兄" : "师姐")
                        : playerSex == 1 ? "师兄" : "师姐";
                }

                return includePlayerFirstName
                    ? playerFirstName + (playerSex == 1 ? "师弟" : "师妹")
                    : playerSex == 1 ? "师弟" : "师妹";
            }

            if (npcFavor >= 40)
            {
                return includePlayerFirstName ? player.firstName + player.lastName : player.lastName;
            }

            if (playerLevel / 3 > npcLevel / 3)
            {
                return includePlayerFirstName ? playerFirstName + "前辈" : "前辈";
            }

            if (playerLevel / 3 < npcLevel / 3)
            {
                return includePlayerFirstName ? playerFirstName + "小友" : "小友";
            }

            return defaultAddress;
        }
        catch
        {
            return defaultAddress;
        }
    }

    private static bool TryGetNpcSocialData(int npcId, out int level, out int sex, out int age, out int menPai, out string npcName)
    {
        level = 0;
        sex = 0;
        age = 0;
        menPai = 0;
        npcName = string.Empty;

        try
        {
            if (!jsonData.instance.AvatarJsonData.HasField(npcId.ToString()))
            {
                return false;
            }

            var npc = jsonData.instance.AvatarJsonData[npcId.ToString()];
            level = npc["Level"].I;
            sex = npc["SexType"].I;
            age = npc["age"].I;
            menPai = npc["MenPai"].I;
            npcName = npc["Name"].Str;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string ResolveDaoLvPlayerCall(string npcName)
    {
        if (string.IsNullOrEmpty(npcName))
        {
            return string.Empty;
        }

        var specialSuffixes = new[]
        {
            "道人",
            "上人",
            "仙子",
            "真人",
            "大圣",
            "剑仙",
            "老祖",
            "老魔",
            "散人",
            "子",
            "道长",
            "尊者",
            "长老",
        };

        foreach (var suffix in specialSuffixes)
        {
            if (npcName.EndsWith(suffix, StringComparison.Ordinal))
            {
                return npcName.Substring(0, npcName.Length - suffix.Length);
            }
        }

        return npcName.Substring(npcName.Length - 1) + "儿";
    }

    private static List<int>? TryReadNearNpcList(object? nativeEnvironment)
    {
        if (nativeEnvironment is null)
        {
            return null;
        }

        try
        {
            var customDataProperty = nativeEnvironment.GetType().GetProperty("customData");
            var customData = customDataProperty?.GetValue(nativeEnvironment, null);
            if (customData is IDictionary dictionary && dictionary.Contains("NearNpcList"))
            {
                return ConvertNpcList(dictionary["NearNpcList"]);
            }
        }
        catch
        {
        }

        return null;
    }

    private static List<int> NormalizeNpcArguments(object? argument)
    {
        var result = new List<int>();
        if (argument is null)
        {
            return result;
        }

        if (argument is int intNpcId)
        {
            result.Add(NPCEx.NPCIDToNew(intNpcId));
            return result;
        }

        if (argument is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item is int candidateId)
                {
                    result.Add(NPCEx.NPCIDToNew(candidateId));
                }
            }
        }

        return result;
    }

    private static List<int>? ConvertNpcList(object? value)
    {
        if (value is List<int> typedList)
        {
            return typedList;
        }

        if (value is IEnumerable enumerable)
        {
            var result = new List<int>();
            foreach (var item in enumerable)
            {
                if (item is int typedValue)
                {
                    result.Add(typedValue);
                }
            }

            return result;
        }

        return null;
    }
}
