using Bloodcraft.Interfaces;
using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Systems.Leveling.PrestigeManager;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;

namespace Bloodcraft.Commands;

[CommandGroup(name: "prestige")]
internal static class PrestigeCommands
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    const int EXO_PRESTIGES = 100;

    static readonly PrefabGUID _shroudBuff = new(1504279833);
    static readonly PrefabGUID _shroudCloak = new(1063517722);

    [Command(name: "self", shortHand: "me", adminOnly: false, usage: ".prestige me [PrestigeType]", description: "Handles player prestiging.")]
    public static void PrestigeCommand(ChatCommandContext ctx, string prestigeType)
    {
        if (!ConfigService.PrestigeSystem)
        {
            LocalizationService.HandleReply(ctx, "進階系統尚未啟用。");
            return;
        }

        if (!TryParsePrestigeType(prestigeType, out var parsedPrestigeType))
        {
            LocalizationService.HandleReply(ctx, "無效的進階類型，請使用 <color=white>'.prestige l'</color> 查看選項。");
            return;
        }

        Entity playerCharacter = ctx.Event.SenderCharacterEntity;
        ulong steamId = ctx.Event.User.PlatformId;

        if (ConfigService.ExoPrestiging && parsedPrestigeType.Equals(PrestigeType.Exo))
        {
            if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(PrestigeType.Experience, out var xpPrestige) && xpPrestige == ConfigService.MaxLevelingPrestiges)
            {
                if (steamId.TryGetPlayerExperience(out var expData) && expData.Key < ConfigService.MaxLevel)
                {
                    LocalizationService.HandleReply(ctx, "你必須先達到最高等級，方可再次進行 <color=#90EE90>Exo</color> 進階。");
                    return;
                }
                else if (prestigeData[PrestigeType.Exo] >= EXO_PRESTIGES)
                {
                    LocalizationService.HandleReply(ctx, $"你已達到 <color=#90EE90>Exo</color> 進階次數上限（<color=white>{EXO_PRESTIGES}</color>）");
                    return;
                }

                if (ConfigService.RestedXPSystem) LevelingSystem.UpdateMaxRestedXP(steamId, expData);

                expData = new KeyValuePair<int, float>(0, 0);
                steamId.SetPlayerExperience(expData);

                LevelingSystem.SetLevel(playerCharacter);

                int exoPrestiges = ++prestigeData[PrestigeType.Exo];

                KeyValuePair<DateTime, float> timeEnergyPair = new(DateTime.UtcNow, Shapeshifts.CalculateFormDuration(exoPrestiges));
                steamId.SetPlayerExoFormData(timeEnergyPair);

                prestigeData[PrestigeType.Exo] = exoPrestiges;
                steamId.SetPlayerPrestiges(prestigeData);

                PrefabGUID exoReward = PrefabGUID.Empty;

                if (ConfigService.ExoPrestigeReward != 0) exoReward = new(ConfigService.ExoPrestigeReward);
                else if (ConfigService.ExoPrestigeReward == 0)
                {
                    LocalizationService.HandleReply(ctx, $"<color=#90EE90>{parsedPrestigeType}</color>[<color=white>{exoPrestiges}</color>] 進階完成！");
                    return;
                }

                if (ServerGameManager.TryAddInventoryItem(playerCharacter, exoReward, ConfigService.ExoPrestigeRewardQuantity))
                {
                    LocalizationService.HandleReply(ctx, $"<color=#90EE90>{parsedPrestigeType}</color>[<color=white>{exoPrestiges}</color>] 進階完成！你獲得了 <color=#ffd9eb>{exoReward.GetLocalizedName()}</color>x<color=white>{ConfigService.ExoPrestigeRewardQuantity}</color> 的獎勵！");
                    return;
                }
                else
                {
                    InventoryUtilitiesServer.CreateDropItem(EntityManager, playerCharacter, exoReward, ConfigService.ExoPrestigeRewardQuantity, new Entity());
                    LocalizationService.HandleReply(ctx, $"<color=#90EE90>{parsedPrestigeType}</color>[<color=white>{exoPrestiges}</color>] 進階完成！你獲得了 <color=#ffd9eb>{exoReward.GetLocalizedName()}</color>x<color=white>{ConfigService.ExoPrestigeRewardQuantity}</color> 的獎勵！但由於你的背包已滿，物品掉落在地上。");
                    return;
                }
            }
            else
            {
                LocalizationService.HandleReply(ctx, "你必須先完成 <color=#90EE90>經驗</color> 進階並達到滿等，方可進行 <color=#90EE90>Exo</color> 進階。");
                return;
            }
        }
        else if (!ConfigService.ExoPrestiging && parsedPrestigeType.Equals(PrestigeType.Exo))
        {
            LocalizationService.HandleReply(ctx, "<color=#90EE90>Exo</color> 進階尚未啟用。");
            return;
        }

        var handler = PrestigeFactory.GetPrestige(parsedPrestigeType);

        if (handler == null)
        {
            LocalizationService.HandleReply(ctx, "無效的進階類型，請使用 <color=white>'.prestige l'</color> 查看可用選項。");
            return;
        }

        var xpData = handler.GetPrestigeData(steamId);
        if (CanPrestige(steamId, parsedPrestigeType, xpData.Key))
        {
            PerformPrestige(ctx, steamId, parsedPrestigeType, handler, xpData);
            Buffs.RefreshStats(playerCharacter);
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"你尚未達到 <color=#90EE90>{parsedPrestigeType}</color> 進階所需等級，或已達最高進階等級。");
        }
    }

    [Command(name: "get", adminOnly: false, usage: ".prestige get [PrestigeType]", description: "Shows information about player's prestige status.")]
    public static void GetPrestigeCommand(ChatCommandContext ctx, string prestigeType)
    {
        if (!ConfigService.PrestigeSystem)
        {
            LocalizationService.HandleReply(ctx, "進階系統尚未啟用。");
            return;
        }

        if (!TryParsePrestigeType(prestigeType, out var parsedPrestigeType))
        {
            LocalizationService.HandleReply(ctx, "無效的進階類型，請使用 <color=white>'.prestige l'</color> 查看可用選項。");
            return;
        }

        User user = ctx.Event.User;
        ulong steamId = user.PlatformId;

        if (parsedPrestigeType == PrestigeType.Exo && steamId.TryGetPlayerPrestiges(out var exoData) && exoData.TryGetValue(parsedPrestigeType, out var exoLevel) && exoLevel > 0)
        {
            LocalizationService.HandleReply(ctx, $"目前 <color=#90EE90>Exo</color> 進階等級：<color=yellow>{exoLevel}</color>/{PrestigeTypeToMaxPrestiges[parsedPrestigeType]} | 最大變身持續時間：<color=green>{(int)Shapeshifts.CalculateFormDuration(exoLevel)}</color> 秒");
            Shapeshifts.UpdateExoFormChargeStored(steamId);

            if (steamId.TryGetPlayerExoFormData(out var exoFormData))
            {
                if (exoFormData.Value < Shapeshifts.BASE_DURATION)
                {
                    Shapeshifts.ReplyNotEnoughCharge(user, steamId, exoFormData.Value);
                }
                else if (exoFormData.Value >= Shapeshifts.BASE_DURATION)
                {
                    LocalizationService.HandleReply(ctx, $"目前能量足以維持形態 <color=white>{(int)exoFormData.Value}</color> 秒");
                }

                /*
                var exoFormSkills = Buffs.EvolvedVampireUnlocks
                    .Where(pair => pair.Value != 0)
                    .Select(pair =>
                    {
                        string abilityName = Buffs.DraculaFormAbilityMap[pair.Key].GetPrefabName();
                        int prefabIndex = abilityName.IndexOf("Prefab");
                        if (prefabIndex != -1)
                        {
                            abilityName = abilityName[..prefabIndex].TrimEnd();
                        }

                        return $"<color=yellow>{pair.Value}</color>| <color=white>{abilityName}</color>";
                    })
                    .ToList();

                for (int i = 0; i < exoFormSkills.Count; i += 4)
                {
                    var batch = exoFormSkills.Skip(i).Take(4);
                    string replyMessage = string.Join(", ", batch);
                    LocalizationService.HandleReply(ctx, replyMessage);
                }
                */
            }

            return;
        }
        else if (parsedPrestigeType == PrestigeType.Exo)
        {
            LocalizationService.HandleReply(ctx, "你尚未在 <color=#90EE90>Exo</color> 中進行進階。");
            return;
        }

        IPrestige handler = PrestigeFactory.GetPrestige(parsedPrestigeType);
        if (handler == null)
        {
            LocalizationService.HandleReply(ctx, "無效的進階類型，請使用 <color=white>'.prestige l'</color> 查看可用選項。");
            return;
        }

        int maxPrestigeLevel = PrestigeTypeToMaxPrestiges[parsedPrestigeType];
        if (steamId.TryGetPlayerPrestiges(out var prestigeData) &&
            prestigeData.TryGetValue(parsedPrestigeType, out var prestigeLevel) && prestigeLevel > 0)
        {
            DisplayPrestigeInfo(ctx, steamId, parsedPrestigeType, prestigeLevel, maxPrestigeLevel);
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"你尚未在 <color=#90EE90>{parsedPrestigeType}</color> 進行過進階。");
        }
    }

    [Command(name: "set", adminOnly: true, usage: ".prestige set [Player] [PrestigeType] [Level]", description: "Sets the specified player to a certain level of prestige in a certain type of prestige.")]
    public static void SetPlayerPrestigeCommand(ChatCommandContext ctx, string name, string prestigeType, int level)
    {
        if (!ConfigService.PrestigeSystem)
        {
            LocalizationService.HandleReply(ctx, "進階系統尚未啟用。");
            return;
        }

        if (!TryParsePrestigeType(prestigeType, out var parsedPrestigeType))
        {
            LocalizationService.HandleReply(ctx, "無效的進階類型，請使用 <color=white>'.prestige l'</color> 查看可用選項。");
            return;
        }

        PlayerInfo playerInfo = GetPlayerInfo(name);
        if (!playerInfo.UserEntity.Exists())
        {
            ctx.Reply($"找不到玩家。");
            return;
        }

        Entity character = playerInfo.CharEntity;
        ulong steamId = playerInfo.User.PlatformId;

        if (parsedPrestigeType == PrestigeType.Exo)
        {
            if (!ConfigService.ExoPrestiging)
            {
                LocalizationService.HandleReply(ctx, $"<color=#90EE90>{parsedPrestigeType}</color> 進階尚未啟用。");
                return;
            }

            if (level > PrestigeTypeToMaxPrestiges[parsedPrestigeType] || level < 1)
            {
                LocalizationService.HandleReply(ctx, $"<color=#90EE90>{parsedPrestigeType}</color> 進階的最高等級為 {PrestigeTypeToMaxPrestiges[parsedPrestigeType]}。");
                return;
            }

            if (steamId.TryGetPlayerPrestiges(out var exoData) && exoData.TryGetValue(PrestigeType.Exo, out int exoPrestige))
            {
                exoPrestige = level;

                exoData[PrestigeType.Exo] = exoPrestige;
                steamId.SetPlayerPrestiges(exoData);

                KeyValuePair<DateTime, float> timeEnergyPair = new(DateTime.UtcNow, Shapeshifts.CalculateFormDuration(exoPrestige));
                steamId.SetPlayerExoFormData(timeEnergyPair);

                LocalizationService.HandleReply(ctx, $"玩家 <color=green>{playerInfo.User.CharacterName.Value}</color> 的 <color=#90EE90>{parsedPrestigeType}</color> 進階等級已設為 <color=white>{level}</color>。");
                return;
            }
        }

        IPrestige handler = PrestigeFactory.GetPrestige(parsedPrestigeType);

        if (handler == null)
        {
            LocalizationService.HandleReply(ctx, "無效的進階類型，請使用 <color=white>'.prestige l'</color> 查看可用選項。");
            return;
        }

        if (!steamId.TryGetPlayerPrestiges(out var prestigeData))
        {
            prestigeData = [];
            steamId.SetPlayerPrestiges(prestigeData);
        }

        if (!prestigeData.ContainsKey(parsedPrestigeType))
        {
            prestigeData[parsedPrestigeType] = 0;
        }

        if (level > PrestigeTypeToMaxPrestiges[parsedPrestigeType])
        {
            LocalizationService.HandleReply(ctx, $"<color=#90EE90>{parsedPrestigeType}</color> 進階的最高等級為 {PrestigeTypeToMaxPrestiges[parsedPrestigeType]}。");
            return;
        }

        prestigeData[parsedPrestigeType] = level;
        steamId.SetPlayerPrestiges(prestigeData);

        // Apply effects based on the prestige type
        if (parsedPrestigeType == PrestigeType.Experience)
        {
            ApplyPrestigeBuffs(character, level);
            ReplyExperiencePrestigeEffects(playerInfo.User, level);
            Progression.PlayerProgressionCacheManager.UpdatePlayerProgressionPrestige(steamId, true);
        }
        else
        {
            ReplyOtherPrestigeEffects(playerInfo.User, steamId, parsedPrestigeType, level);
        }

        LocalizationService.HandleReply(ctx, $"玩家 <color=green>{playerInfo.User.CharacterName.Value}</color> 的 <color=#90EE90>{parsedPrestigeType}</color> 進階等級已設為 <color=white>{level}</color>。");
    }

    [Command(name: "reset", shortHand: "r", adminOnly: true, usage: ".prestige r [Player] [PrestigeType]", description: "Handles resetting prestiging.")]
    public static void ResetPrestige(ChatCommandContext ctx, string name, string prestigeType)
    {
        if (!ConfigService.PrestigeSystem)
        {
            LocalizationService.HandleReply(ctx, "進階系統尚未啟用。");
            return;
        }

        if (!TryParsePrestigeType(prestigeType, out var parsedPrestigeType))
        {
            LocalizationService.HandleReply(ctx, "無效的進階類型，請使用 <color=white>'.prestige l'</color> 查看可用選項。");
            return;
        }

        if (!ConfigService.ExoPrestiging && parsedPrestigeType == PrestigeType.Exo)
        {
            LocalizationService.HandleReply(ctx, "Exo 進階尚未啟用。");
            return;
        }

        PlayerInfo playerInfo = GetPlayerInfo(name);
        if (!playerInfo.UserEntity.Exists())
        {
            ctx.Reply($"找不到玩家…");
            return;
        }

        Entity character = playerInfo.CharEntity;
        ulong steamId = playerInfo.User.PlatformId;

        if (steamId.TryGetPlayerPrestiges(out var prestigeData) &&
            prestigeData.TryGetValue(parsedPrestigeType, out var prestigeLevel))
        {
            if (parsedPrestigeType == PrestigeType.Experience)
            {
                RemovePrestigeBuffs(character);
            }

            prestigeData[parsedPrestigeType] = 0;
            steamId.SetPlayerPrestiges(prestigeData);

            if (parsedPrestigeType == PrestigeType.Exo)
            {
                if (steamId.TryGetPlayerExoFormData(out var exoFormData))
                {
                    KeyValuePair<DateTime, float> timeEnergyPair = new(DateTime.MinValue, 0f);
                    steamId.SetPlayerExoFormData(timeEnergyPair);
                }

                if (GetPlayerBool(steamId, SHAPESHIFT_KEY)) SetPlayerBool(steamId, SHAPESHIFT_KEY, false);
            }

            LocalizationService.HandleReply(ctx, $"<color=#90EE90>{parsedPrestigeType}</color> 的進階等級已重置，目標：<color=white>{playerInfo.User.CharacterName.Value}</color>。");
        }
    }

    [Command(name: "syncbuffs", shortHand: "sb", adminOnly: false, usage: ".prestige sb", description: "Applies prestige buffs appropriately if not present.")]
    public static void SyncPrestigeBuffsCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.PrestigeSystem)
        {
            LocalizationService.HandleReply(ctx, "進階系統尚未啟用。");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (steamId.TryGetPlayerPrestiges(out var prestigeData) &&
            prestigeData.TryGetValue(PrestigeType.Experience, out var prestigeLevel) && prestigeLevel > 0)
        {
            Entity character = ctx.Event.SenderCharacterEntity;
            ApplyPrestigeBuffs(character, prestigeLevel);

            LocalizationService.HandleReply(ctx, "進階增益已套用！（若原本未啟用）");
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"你尚未在 <color=#90EE90>{PrestigeType.Experience}</color> 進行過進階。");
        }
    }

    [Command(name: "list", shortHand: "l", adminOnly: false, usage: ".prestige l", description: "Lists prestiges available.")]
    public static void ListPlayerPrestigeTypes(ChatCommandContext ctx)
    {
        if (!ConfigService.PrestigeSystem)
        {
            LocalizationService.HandleReply(ctx, "進階系統尚未啟用。");
            return;
        }

        List<string> prestigeTypes = [..Enum.GetNames(typeof(PrestigeType)).Select(prestigeType => $"<color=#90EE90>{prestigeType}</color>")];

        const int maxPerMessage = 6;

        LocalizationService.HandleReply(ctx, $"進階紀錄：");
        for (int i = 0; i < prestigeTypes.Count; i += maxPerMessage)
        {
            var batch = prestigeTypes.Skip(i).Take(maxPerMessage);
            string prestiges = string.Join(", ", batch);
            LocalizationService.HandleReply(ctx, $"{prestiges}");
        }
    }

    [Command(name: "leaderboard", shortHand: "lb", adminOnly: false, usage: ".prestige lb [PrestigeType]", description: "Lists prestige leaderboard for type.")]
    public static void ListPrestigeTypeLeaderboard(ChatCommandContext ctx, string prestigeType)
    {
        if (!ConfigService.PrestigeSystem)
        {
            LocalizationService.HandleReply(ctx, "進階系統尚未啟用。");
            return;
        }

        if (!ConfigService.PrestigeLeaderboard)
        {
            LocalizationService.HandleReply(ctx, "排行榜尚未啟用。");
            return;
        }

        if (!TryParsePrestigeType(prestigeType, out var parsedPrestigeType))
        {
            LocalizationService.HandleReply(ctx, "無效的進階類型，請使用 <color=white>'.prestige l'</color> 查看可用選項。");
            return;
        }

        if (!ConfigService.ExoPrestiging && parsedPrestigeType == PrestigeType.Exo)
        {
            LocalizationService.HandleReply(ctx, "Exo 進階尚未啟用。");
            return;
        }

        var prestigeData = GetPrestigeForType(parsedPrestigeType)
            .Where(p => p.Value > 0)
            .OrderByDescending(p => p.Value)
            .ToList();

        if (!prestigeData.Any())
        {
            LocalizationService.HandleReply(ctx, $"尚無玩家在 <color=#90EE90>{parsedPrestigeType}</color> 進行過進階！");
            return;
        }

        var leaderboard = prestigeData
            .Take(10)
            .Select((p, index) =>
            {
                var playerName = SteamIdPlayerInfoCache.Values.FirstOrDefault(x => x.User.CharacterName.Value == p.Key).User.CharacterName.Value ?? "Unknown";
                return $"<color=yellow>{index + 1}</color>| <color=green>{playerName}</color>, <color=#90EE90>{parsedPrestigeType}</color>: <color=white>{p.Value}</color>";
            })
            .ToList();

        if (leaderboard.Count == 0)
        {
            LocalizationService.HandleReply(ctx, $"尚無玩家在 <color=#90EE90>{parsedPrestigeType}</color> 進行過進階！");
        }
        else
        {
            for (int i = 0; i < leaderboard.Count; i += 4)
            {
                var batch = leaderboard.Skip(i).Take(4);
                string replyMessage = string.Join(", ", batch);

                LocalizationService.HandleReply(ctx, replyMessage);
            }
        }
    }

    [Command(name: "exoform", adminOnly: false, usage: ".prestige exoform", description: "Toggles taunting to enter exoform.")]
    public static void ToggleExoFormEmote(ChatCommandContext ctx)
    {
        if (!ConfigService.ExoPrestiging)
        {
            ctx.Reply("Exo 進階尚未啟用。");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (steamId.TryGetPlayerPrestiges(out var prestiges) && prestiges.TryGetValue(PrestigeType.Exo, out int exoPrestiges) && exoPrestiges > 0)
        {
            if (!Progression.ConsumedMegara(ctx.Event.SenderUserEntity) && !Progression.ConsumedDracula(ctx.Event.SenderUserEntity))
            {
                ctx.Reply("你必須吞噬至少一個太初精華……");
                return;
            }

            TogglePlayerBool(steamId, SHAPESHIFT_KEY);
            ctx.Reply($"變形體表情動作（<color=white>挑釁</color>）{(GetPlayerBool(steamId, SHAPESHIFT_KEY) ? "<color=green>已啟用</color>" : "<color=red>已停用</color>")}");
        }
        else
        {
            ctx.Reply("你還未獲得資格……");
        }
    }

    [Command(name: "selectform", shortHand: "sf", adminOnly: false, usage: ".prestige sf [EvolvedVampire|CorruptedSerpent]", description: "Select active exoform shapeshift.")]
    public static void SelectFormCommand(ChatCommandContext ctx, string shapeshift)
    {
        if (!ConfigService.ExoPrestiging)
        {
            ctx.Reply("Exo 進階尚未啟用。");
            return;
        }

        if (!Enum.TryParse<ShapeshiftType>(shapeshift, ignoreCase: true, out var form))
        {
            var shapeshifts = string.Join(", ", Enum.GetNames(typeof(ShapeshiftType)).Select(name => $"<color=white>{name}</color>"));
            ctx.Reply($"無效的變形形態！可用選項：{shapeshifts}");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (steamId.TryGetPlayerPrestiges(out var prestiges) && prestiges.TryGetValue(PrestigeType.Exo, out int exoPrestiges) && exoPrestiges > 0)
        {
            if (form.Equals(ShapeshiftType.EvolvedVampire) && !Progression.ConsumedDracula(ctx.Event.SenderUserEntity))
            {
                ctx.Reply("你必須先吞噬德古拉的精華，方能化身其形……");
                return;
            }

            if (form.Equals(ShapeshiftType.CorruptedSerpent) && !Progression.ConsumedMegara(ctx.Event.SenderUserEntity))
            {
                ctx.Reply("你必須先吞噬梅伽拉的精華，方能化身其形……");
                return;
            }

            steamId.SetPlayerShapeshift(form);
            ctx.Reply($"目前的 Exo 形態：<color=white>{form}</color>");
        }
        else
        {
            ctx.Reply("你還未獲得資格……");
        }
    }

    [Command(name: "ignoreleaderboard", shortHand: "ignore", adminOnly: true, usage: ".prestige ignore [Player]", description: "Adds (or removes) player to list of those who will not appear on prestige leaderboards. Intended for admin-duties only accounts.")]
    public static void IgnorePrestigeLeaderboardPlayerCommand(ChatCommandContext ctx, string name)
    {
        if (!ConfigService.PrestigeSystem)
        {
            LocalizationService.HandleReply(ctx, "進階系統尚未啟用。");
            return;
        }

        PlayerInfo playerInfo = GetPlayerInfo(name);
        if (!playerInfo.UserEntity.Exists())
        {
            ctx.Reply($"找不到玩家…");
            return;
        }

        if (!DataService.PlayerDictionaries._ignorePrestigeLeaderboard.Contains(playerInfo.User.PlatformId))
        {
            DataService.PlayerDictionaries._ignorePrestigeLeaderboard.Add(playerInfo.User.PlatformId);
            DataService.PlayerPersistence.SaveIgnoredPrestigeLeaderboard();

            ctx.Reply($"<color=green>{playerInfo.User.CharacterName.Value}</color> 已被加入忽略進階排行榜名單！");
        }
        else if (DataService.PlayerDictionaries._ignorePrestigeLeaderboard.Contains(playerInfo.User.PlatformId))
        {
            DataService.PlayerDictionaries._ignorePrestigeLeaderboard.Remove(playerInfo.User.PlatformId);
            DataService.PlayerPersistence.SaveIgnoredPrestigeLeaderboard();

            ctx.Reply($"<color=green>{playerInfo.User.CharacterName.Value}</color> 已從忽略進階排行榜名單中移除！");
        }
    }

    [Command(name: "permashroud", shortHand: "shroud", adminOnly: false, usage: ".prestige shroud", description: "Toggles permashroud if applicable.")]
    public static void PermaShroudToggle(ChatCommandContext ctx)
    {
        if (!ConfigService.PrestigeSystem)
        {
            LocalizationService.HandleReply(ctx, "進階系統尚未啟用。");
            return;
        }

        Entity playerCharacter = ctx.Event.SenderCharacterEntity;
        ulong steamId = ctx.Event.User.PlatformId;

        TogglePlayerBool(steamId, SHROUD_KEY);
        if (GetPlayerBool(steamId, SHROUD_KEY))
        {
            LocalizationService.HandleReply(ctx, "永久迷霧 <color=green>已啟用</color>！");

            if (UpdateBuffsBufferDestroyPatch.PrestigeBuffs.Contains(_shroudBuff) && !playerCharacter.HasBuff(_shroudBuff)
                && steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(PrestigeType.Experience, out var experiencePrestiges) && experiencePrestiges > UpdateBuffsBufferDestroyPatch.PrestigeBuffs.IndexOf(_shroudBuff))
            {
                Buffs.TryApplyPermanentBuff(playerCharacter, _shroudBuff);
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "永久迷霧 <color=red>已停用</color>！");
            Equipment equipment = playerCharacter.Read<Equipment>();

            if (!equipment.IsEquipped(_shroudCloak, out var _) && playerCharacter.HasBuff(_shroudBuff))
            {
                playerCharacter.TryRemoveBuff(buffPrefabGuid: _shroudBuff);
            }
        }
    }

    [Command(name: "iacknowledgethiswillremoveallprestigebuffsfromplayersandwantthattohappen", adminOnly: true, usage: ".prestige iacknowledgethiswillremoveallprestigebuffsfromplayersandwantthattohappen", description: "Globally removes prestige buffs from players to facilitate changing prestige buffs in config.")]
    public static void GlobalClassPurgeCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.PrestigeSystem)
        {
            LocalizationService.HandleReply(ctx, "進階系統尚未啟用。");
            return;
        }

        GlobalPurgePrestigeBuffs(ctx);
    }
}
