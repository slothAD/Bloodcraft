﻿using Bloodcraft.Interfaces;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Systems.Leveling.ClassManager;
using static Bloodcraft.Utilities.Classes;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;
using static VCF.Core.Basics.RoleCommands;
using User = ProjectM.Network.User;

namespace Bloodcraft.Commands;

[CommandGroup(name: "class")]
internal static class ClassCommands
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly bool _classes = ConfigService.ClassSystem;

    [Command(name: "select", shortHand: "s", adminOnly: false, usage: ".class s [Class]", description: "Select class.")]
    public static void SelectClassCommand(ChatCommandContext ctx, string input)
    {
        if (!_classes)
        {
            LocalizationService.HandleReply(ctx, "職業系統尚未啟用。");
            return;
        }

        Entity playerCharacter = ctx.Event.SenderCharacterEntity;
        ulong steamId = ctx.Event.User.PlatformId;
        PlayerClass? nullablePlayerClass = ParseClassFromInput(ctx, input);

        if (nullablePlayerClass.HasValue)
        {
            PlayerClass playerClass = nullablePlayerClass.Value;

            if (!steamId.HasClass(out PlayerClass? currentClass) || !currentClass.HasValue)
            {
                UpdatePlayerClass(playerCharacter, playerClass, steamId);
                // ApplyClassBuffs(playerCharacter, steamId);

                LocalizationService.HandleReply(ctx, $"你已選擇 {FormatClassName(playerClass)}！");
            }
            else
            {
                LocalizationService.HandleReply(ctx, $"你已選擇 {FormatClassName(currentClass.Value)}，如要更換請使用 <color=white>'.class c [職業]'</color>（<color=#ffd9eb>{new PrefabGUID(ConfigService.ChangeClassItem).GetLocalizedName()}</color>x<color=white>{ConfigService.ChangeClassQuantity}</color>）");
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "無效的職業，請使用 '<color=white>.class l</color>' 查看可用選項。");
        }
    }

    [Command(name: "choosespell", shortHand: "csp", adminOnly: false, usage: ".class csp [#]", description: "Sets shift spell for class if prestige level is high enough.")]
    public static void ChooseClassSpell(ChatCommandContext ctx, int choice)
    {
        if (!_classes)
        {
            LocalizationService.HandleReply(ctx, "職業系統尚未啟用。");
            return;
        }

        if (!ConfigService.ShiftSlot)
        {
            LocalizationService.HandleReply(ctx, "Shift 技能尚未啟用。");
            return;
        }

        Entity playerCharacter = ctx.Event.SenderCharacterEntity;

        if (!InventoryUtilities.TryGetInventoryEntity(EntityManager, playerCharacter, out Entity inventoryEntity) || InventoryUtilities.IsInventoryFull(EntityManager, inventoryEntity))
        {
            LocalizationService.HandleReply(ctx, "背包已滿，無法更換或啟用職業技能。請保留至少一格空位以安全更換寶石。");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (GetPlayerBool(steamId, SHIFT_LOCK_KEY) 
            && steamId.HasClass(out PlayerClass? playerClass) 
            && playerClass.HasValue)
        {
            if (ConfigService.PrestigeSystem && steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(PrestigeType.Experience, out var prestigeLevel))
            {
                List<int> spells = Configuration.ParseIntegersFromString(ClassSpellsMap[playerClass.Value]);

                if (spells.Count == 0)
                {
                    LocalizationService.HandleReply(ctx, $"{FormatClassName(playerClass.Value)} 尚未配置任何技能！");
                    return;
                }
                else if (choice < 0 || choice > spells.Count)
                {
                    LocalizationService.HandleReply(ctx, $"無效的技能，請使用 '<color=white>.class lsp</color>' 查看選項。");
                    return;
                }

                if (choice == 0) // set default for all classes
                {
                    if (ConfigService.DefaultClassSpell == 0)
                    {
                        LocalizationService.HandleReply(ctx, "尚未設定職業的預設技能！");
                        return;
                    }
                    else if (prestigeLevel < Configuration.ParseIntegersFromString(ConfigService.PrestigeLevelsToUnlockClassSpells)[choice])
                    {
                        LocalizationService.HandleReply(ctx, "你尚未達到該技能所需的進階等級！");
                        return;
                    }
                    else if (steamId.TryGetPlayerSpells(out var data))
                    {
                        PrefabGUID spellPrefabGUID = new(ConfigService.DefaultClassSpell);
                        data.ClassSpell = ConfigService.DefaultClassSpell;

                        steamId.SetPlayerSpells(data);
                        UpdateShift(ctx, playerCharacter, spellPrefabGUID);

                        return;
                    }
                }
                else if (prestigeLevel < Configuration.ParseIntegersFromString(ConfigService.PrestigeLevelsToUnlockClassSpells)[choice])
                {
                    LocalizationService.HandleReply(ctx, "你尚未達到該技能所需的進階等級！");
                    return;
                }
                else if (steamId.TryGetPlayerSpells(out var spellsData))
                {
                    spellsData.ClassSpell = spells[choice - 1];
                    steamId.SetPlayerSpells(spellsData);

                    UpdateShift(ctx, ctx.Event.SenderCharacterEntity, new(spellsData.ClassSpell));
                }
            }
            else
            {
                List<int> spells = Configuration.ParseIntegersFromString(ClassSpellsMap[playerClass.Value]);

                if (spells.Count == 0)
                {
                    LocalizationService.HandleReply(ctx, $"{FormatClassName(playerClass.Value)} 尚未配置任何技能！");
                    return;
                }
                else if (choice < 0 || choice > spells.Count)
                {
                    LocalizationService.HandleReply(ctx, $"無效的技能，請使用 <color=white>'.class lsp'</color> 查看選項。");
                    return;
                }

                if (choice == 0) // set default for all classes
                {
                    if (steamId.TryGetPlayerSpells(out var data))
                    {
                        if (ConfigService.DefaultClassSpell == 0)
                        {
                            LocalizationService.HandleReply(ctx, "尚未設定職業的預設技能！");
                            return;
                        }

                        PrefabGUID spellPrefabGUID = new(ConfigService.DefaultClassSpell);
                        data.ClassSpell = ConfigService.DefaultClassSpell;

                        steamId.SetPlayerSpells(data);
                        UpdateShift(ctx, ctx.Event.SenderCharacterEntity, spellPrefabGUID);

                        return;
                    }
                }

                if (steamId.TryGetPlayerSpells(out var spellsData))
                {
                    spellsData.ClassSpell = spells[choice - 1];
                    steamId.SetPlayerSpells(spellsData);

                    UpdateShift(ctx, ctx.Event.SenderCharacterEntity, new(spellsData.ClassSpell));
                }
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "你尚未選擇職業，或尚未啟用 Shift 技能！（<color=white>'.class s [職業]'</color> | <color=white>'.class shift'</color>）");
        }
    }

    [Command(name: "change", shortHand: "c", adminOnly: false, usage: ".class c [Class]", description: "Change classes.")]
    public static void ChangeClassCommand(ChatCommandContext ctx, string input)
    {
        if (!_classes)
        {
            LocalizationService.HandleReply(ctx, "職業系統尚未啟用。");
            return;
        }

        Entity playerCharacter = ctx.Event.SenderUserEntity;
        ulong steamId = ctx.Event.User.PlatformId;

        PlayerClass? nullablePlayerClass = ParseClassFromInput(ctx, input);

        if (nullablePlayerClass.HasValue)
        {
            PlayerClass playerClass = nullablePlayerClass.Value;

            if (!steamId.HasClass(out PlayerClass? currentClass) || !currentClass.HasValue)
            {
                LocalizationService.HandleReply(ctx, "你尚未選擇職業，請使用 <color=white>'.class s [職業]'</color>。");
                return;
            }

            if (GetPlayerBool(steamId, CLASS_BUFFS_KEY))
            {
                LocalizationService.HandleReply(ctx, "你目前啟用了職業增益，請使用 <color=white>'.class passives'</color> 關閉後再更換職業！");
                return;
            }

            if (ConfigService.ChangeClassItem != 0 && !HandleClassChangeItem(ctx))
            {
                return;
            }

            UpdatePlayerClass(playerCharacter, playerClass, steamId);
            LocalizationService.HandleReply(ctx, $"職業已更換為 {FormatClassName(playerClass)}！");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "無效的職業，請使用 '<color=white>.class l</color>' 查看可用選項。");
        }
    }

    [Command(name: "list", shortHand: "l", adminOnly: false, usage: ".class l", description: "List available classes.")]
    public static void ListClasses(ChatCommandContext ctx)
    {
        if (!_classes)
        {
            LocalizationService.HandleReply(ctx, "職業系統尚未啟用。");
            return;
        }

        var classes = Enum.GetValues(typeof(PlayerClass)).Cast<PlayerClass>().Select((playerClass, index) =>
        {
            return $"<color=yellow>{index + 1}</color>| {FormatClassName(playerClass, false)}";
        }).ToList();

        string classTypes = string.Join(", ", classes);
        LocalizationService.HandleReply(ctx, $"職業列表：{classTypes}");
    }

    [Command(name: "listspells", shortHand: "lsp", adminOnly: false, usage: ".class lsp [Class]", description: "Shows spells that can be gained from class.")]
    public static void ListClassSpellsCommand(ChatCommandContext ctx, string classType = "")
    {
        if (!_classes)
        {
            LocalizationService.HandleReply(ctx, "職業系統尚未啟用。");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;
        PlayerClass? nullablePlayerClass = ParseClassFromInput(ctx, classType);

        if (nullablePlayerClass.HasValue)
        {
            ReplyClassSpells(ctx, nullablePlayerClass.Value);
        }
        else if (string.IsNullOrEmpty(classType) && steamId.HasClass(out PlayerClass? currentClass) && currentClass.HasValue)
        {
            ReplyClassSpells(ctx, currentClass.Value);
        }

        /*
        else
        {
            LocalizationService.HandleReply(ctx, "無效的職業，請使用 '<color=white>.class l</color>' 查看可用選項。");
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (steamId.HasClass(out PlayerClass? playerClass)
            && playerClass.HasValue)
        {
            if (!string.IsNullOrEmpty(classType) && TryParseClass(classType, out PlayerClass requestedClass))
            {
                playerClass = requestedClass;
            }

            ReplyClassSpells(ctx, playerClass.Value);
        }
        else
        {
            if (!string.IsNullOrEmpty(classType) && TryParseClass(classType, out PlayerClass requestedClass))
            {
                ReplyClassSpells(ctx, requestedClass);
            }
            else
            {
                LocalizationService.HandleReply(ctx, "無效的職業，請使用 <color=white>'.class l'</color> 查看可用選項。");
            }
        }
        */
    }

    [Command(name: "liststats", shortHand: "lst", adminOnly: false, usage: ".class lst [Class]", description: "List weapon and blood stat synergies for a class.")]
    public static void ListClassStatsCommand(ChatCommandContext ctx, string classType = "")
    {
        if (!_classes)
        {
            LocalizationService.HandleReply(ctx, "職業系統尚未啟用。");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;
        PlayerClass? nullablePlayerClass = ParseClassFromInput(ctx, classType);

        if (nullablePlayerClass.HasValue)
        {
            ReplyClassSynergies(ctx, nullablePlayerClass.Value);
        }
        else if (string.IsNullOrEmpty(classType) && steamId.HasClass(out PlayerClass? currentClass) && currentClass.HasValue)
        {
            ReplyClassSynergies(ctx, currentClass.Value);
        }
        else
        {
            LocalizationService.HandleReply(ctx, "無效的職業，請使用 '<color=white>.class l</color>' 查看可用選項。");
        }

        /*
        ulong steamId = ctx.Event.User.PlatformId;

        if (steamId.HasClass(out PlayerClass? playerClass)
            && playerClass.HasValue)
        {
            if (!string.IsNullOrEmpty(classType) && TryParseClass(classType, out PlayerClass requestedClass))
            {
                playerClass = requestedClass;
            }

            ReplyClassSynergies(ctx, playerClass.Value);
        }
        else
        {
            if (!string.IsNullOrEmpty(classType) && TryParseClass(classType, out PlayerClass requestedClass))
            {
                ReplyClassSynergies(ctx, requestedClass);
            }
            else
            {
                LocalizationService.HandleReply(ctx, "無效的職業，請使用 <color=white>'.class l'</color> 查看可用選項。");
            }
        }
        */
    }

    [Command(name: "lockshift", shortHand: "shift", adminOnly: false, usage: ".class shift", description: "Toggle shift spell.")]
    public static void ShiftSlotToggleCommand(ChatCommandContext ctx)
    {
        if (!_classes)
        {
            LocalizationService.HandleReply(ctx, "職業系統尚未啟用，無法設置 Shift 技能。");
            return;
        }

        if (!ConfigService.ShiftSlot)
        {
            LocalizationService.HandleReply(ctx, "Shift 技能欄尚未啟用。");
            return;
        }

        Entity character = ctx.Event.SenderCharacterEntity;
        User user = ctx.Event.User;

        ulong steamId = user.PlatformId;

        if (!InventoryUtilities.TryGetInventoryEntity(EntityManager, character, out Entity inventoryEntity) || InventoryUtilities.IsInventoryFull(EntityManager, inventoryEntity))
        {
            LocalizationService.HandleReply(ctx, "背包已滿，無法更換或啟用職業技能。請保留至少一格空位以安全更換寶石。");
            return;
        }

        TogglePlayerBool(steamId, SHIFT_LOCK_KEY);
        if (GetPlayerBool(steamId, SHIFT_LOCK_KEY))
        {
            if (steamId.TryGetPlayerSpells(out var spellsData))
            {
                PrefabGUID spellPrefabGUID = new(spellsData.ClassSpell);

                if (spellPrefabGUID.HasValue())
                {
                    UpdateShift(ctx, ctx.Event.SenderCharacterEntity, spellPrefabGUID);
                }
            }

            LocalizationService.HandleReply(ctx, "Shift 技能 <color=green>已啟用</color>！");
        }
        else
        {
            RemoveShift(ctx.Event.SenderCharacterEntity);

            LocalizationService.HandleReply(ctx, "Shift 技能 <color=red>已停用</color>！");
        }
    }
}