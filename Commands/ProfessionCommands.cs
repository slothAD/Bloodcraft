using Bloodcraft.Interfaces;
using Bloodcraft.Services;
using Bloodcraft.Systems.Professions;
using VampireCommandFramework;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;
using static Bloodcraft.Utilities.Progression;
using static Bloodcraft.Utilities.EnumLocalization.EnumLocalizationLookup;

namespace Bloodcraft.Commands;

[CommandGroup(name: "profession", "prof")]
internal static class ProfessionCommands
{
    const int MAX_PROFESSION_LEVEL = 100;

    [Command(name: "log", adminOnly: false, usage: ".prof log", description: "Toggles profession progress logging.")]
    public static void LogProgessionCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.ProfessionSystem)
        {
            LocalizationService.HandleReply(ctx, "職業系統尚未啟用。");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        TogglePlayerBool(steamId, PROFESSION_LOG_KEY);
        LocalizationService.HandleReply(ctx, $"職業紀錄現在為 {(GetPlayerBool(steamId, PROFESSION_LOG_KEY) ? "<color=green>已啟用</color>" : "<color=red>已停用</color>")}.");
    }

    [Command(name: "get", adminOnly: false, usage: ".prof get [Profession]", description: "Display your current profession progress.")]
    public static void GetProfessionCommand(ChatCommandContext ctx, string profession)
    {
        if (!ConfigService.ProfessionSystem)
        {
            LocalizationService.HandleReply(ctx, "職業系統尚未啟用。");
            return;
        }

        if (!Enum.TryParse(profession, true, out ProfessionType professionType) &&
            !EnumLocalization.ZhToProfessionType.TryGetValue(profession, out professionType))
        {
            LocalizationService.HandleReply(ctx, $"有效職業：{ProfessionFactory.GetProfessionNames()}");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        IProfession professionHandler = ProfessionFactory.GetProfession(professionType);
        if (professionHandler == null)
        {
            LocalizationService.HandleReply(ctx, "無效的職業。");
            return;
        }

        KeyValuePair<int, float> data = professionHandler.GetProfessionData(steamId);
        if (data.Key > 0)
        {
            int progress = (int)(data.Value - ConvertLevelToXp(data.Key));
            LocalizationService.HandleReply(ctx, $"你目前在 <color=#c0c0c0>{GetProfessionZh(professionHandler.GetProfessionType())}</color> 的等級為 [<color=white>{data.Key}</color>]，擁有 <color=yellow>{progress}</color> 點<color=#FFC0CB>熟練度</color>（<color=white>{ProfessionSystem.GetLevelProgress(steamId, professionHandler)}%</color>）");
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"尚未在 <color=#c0c0c0>{GetProfessionZh(professionHandler.GetProfessionType())}</color> 開始進度！");
        }
    }

    [Command(name: "set", adminOnly: true, usage: ".prof set [Name] [Profession] [Level]", description: "Sets player profession level.")]
    public static void SetProfessionCommand(ChatCommandContext ctx, string name, string profession, int level)
    {
        if (!ConfigService.ProfessionSystem)
        {
            LocalizationService.HandleReply(ctx, "職業系統尚未啟用。");
            return;
        }

        PlayerInfo playerInfo = GetPlayerInfo(name);
        if (!playerInfo.UserEntity.Exists())
        {
            ctx.Reply($"找不到玩家。");
            return;
        }

        if (level < 0 || level > MAX_PROFESSION_LEVEL)
        {
            LocalizationService.HandleReply(ctx, $"等級必須介於 0 到 {MAX_PROFESSION_LEVEL} 之間。");
            return;
        }

        if (!Enum.TryParse(profession, true, out ProfessionType professionType) &&
            !EnumLocalization.ZhToProfessionType.TryGetValue(profession, out professionType))
        {
            LocalizationService.HandleReply(ctx, $"有效職業：{ProfessionFactory.GetProfessionNames()}");
            return;
        }

        IProfession professionHandler = ProfessionFactory.GetProfession(professionType);
        if (professionHandler == null)
        {
            LocalizationService.HandleReply(ctx, "無效的職業。");
            return;
        }

        ulong steamId = playerInfo.User.PlatformId;

        float xp = ConvertLevelToXp(level);
        professionHandler.SetProfessionData(steamId, new KeyValuePair<int, float>(level, xp));

        LocalizationService.HandleReply(ctx, $"<color=#c0c0c0>{GetProfessionZh(professionHandler.GetProfessionType())}</color> 等級設為 [<color=white>{level}</color>]，目標玩家：<color=green>{playerInfo.User.CharacterName.Value}</color>");
    }

    [Command(name: "list", shortHand: "l", adminOnly: false, usage: ".prof l", description: "Lists professions available.")]
    public static void ListProfessionsCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.ProfessionSystem)
        {
            LocalizationService.HandleReply(ctx, "職業系統尚未啟用。");
            return;
        }

        LocalizationService.HandleReply(ctx, $"可選擇的職業：{ProfessionFactory.GetProfessionNames()}");
    }
}