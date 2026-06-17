using LongLive.Mods.Models;

namespace LongLive.Mods.Installation;

public interface ILongLiveContentRegistry
{
    LongLiveContentInstallEntry InstallItem(LongLiveContentInstallRequest<LongLiveItemDefinition> request);

    LongLiveContentInstallEntry InstallSkill(LongLiveContentInstallRequest<LongLiveSkillDefinition> request);

    LongLiveContentInstallEntry InstallBuff(LongLiveContentInstallRequest<LongLiveBuffDefinition> request);

    LongLiveContentInstallEntry InstallAsset(LongLiveContentInstallRequest<LongLiveAssetMappingDefinition> request);
}
