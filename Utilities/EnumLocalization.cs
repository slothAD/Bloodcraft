using Bloodcraft.Interfaces; // WeaponType, BloodType, ProfessionType
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Systems.Familiars;
using WeaponStatType = Bloodcraft.Systems.Expertise.WeaponManager.WeaponStats.WeaponStatType;
using FamiliarStatType = Bloodcraft.Systems.Familiars.FamiliarBindingSystem.FamiliarStatType;
namespace Bloodcraft.Utilities.EnumLocalization;

internal static class EnumLocalization
{
    public static readonly Dictionary<string, WeaponType> ZhToWeaponType = new()
    {
        { "劍", WeaponType.Sword },
        { "斧", WeaponType.Axe },
        { "錘", WeaponType.Mace },
        { "長矛", WeaponType.Spear },
        { "十字弓", WeaponType.Crossbow },
        { "大劍", WeaponType.GreatSword },
        { "雙刃", WeaponType.Slashers },
        { "雙槍", WeaponType.Pistols },
        { "鐮刀", WeaponType.Reaper },
        { "長弓", WeaponType.Longbow },
        { "鞭", WeaponType.Whip },
        { "徒手", WeaponType.Unarmed },
        { "釣竿", WeaponType.FishingPole },
        { "雙刀", WeaponType.TwinBlades },
        { "匕首", WeaponType.Daggers },
        { "爪", WeaponType.Claws }
    };
    public static readonly Dictionary<WeaponType, string> WeaponTypeToZh = ZhToWeaponType.ToDictionary(x => x.Value, x => x.Key);
    public static readonly Dictionary<string, BloodType> ZhToBloodType = new()
    {
        { "工人", BloodType.Worker },
        { "戰士", BloodType.Warrior },
        { "學者", BloodType.Scholar },
        { "流氓", BloodType.Rogue },
        { "突變體", BloodType.Mutant },
        { "吸血鬼", BloodType.Draculin },
        { "不朽者", BloodType.Immortal },
        { "動物", BloodType.Creature },
        { "野蠻", BloodType.Brute },
        { "墮化者", BloodType.Corruption }
    };
    public static readonly Dictionary<BloodType, string> BloodTypeToZh = ZhToBloodType.ToDictionary(x => x.Value, x => x.Key);

    public static readonly Dictionary<string, ShapeshiftType> ZhToShapeshiftType = new()
    {
        { "進化吸血鬼", ShapeshiftType.EvolvedVampire },
        { "墮落蛇靈", ShapeshiftType.CorruptedSerpent }
    };
    public static readonly Dictionary<ShapeshiftType, string> ShapeshiftTypeToZh = ZhToShapeshiftType.ToDictionary(x => x.Value, x => x.Key);

    public static readonly Dictionary<string, ProfessionType> ZhToProfessionType = new()
    {
        { "附魔", ProfessionType.Enchanting },
        { "煉金", ProfessionType.Alchemy },
        { "採集", ProfessionType.Harvesting },
        { "鍛造", ProfessionType.Blacksmithing },
        { "裁縫", ProfessionType.Tailoring },
        { "伐木", ProfessionType.Woodcutting },
        { "採礦", ProfessionType.Mining },
        { "釣魚", ProfessionType.Fishing }
    };
    public static readonly Dictionary<ProfessionType, string> ProfessionTypeToZh = ZhToProfessionType.ToDictionary(x => x.Value, x => x.Key);

    public static readonly Dictionary<string, PrestigeType> ZhToPrestigeType = new()
    {
        { "經驗值", PrestigeType.Experience },
        { "進階體", PrestigeType.Exo },
        { "劍・進階", PrestigeType.SwordExpertise },
        { "斧・進階", PrestigeType.AxeExpertise },
        { "錘・進階", PrestigeType.MaceExpertise },
        { "矛・進階", PrestigeType.SpearExpertise },
        { "弩・進階", PrestigeType.CrossbowExpertise },
        { "大劍・進階", PrestigeType.GreatSwordExpertise },
        { "雙刃・進階", PrestigeType.SlashersExpertise },
        { "雙槍・進階", PrestigeType.PistolsExpertise },
        { "鐮刀・進階", PrestigeType.ReaperExpertise },
        { "長弓・進階", PrestigeType.LongbowExpertise },
        { "鞭・進階", PrestigeType.WhipExpertise },
        { "徒手・進階", PrestigeType.UnarmedExpertise },
        { "釣竿・進階", PrestigeType.FishingPoleExpertise },
        { "雙刀・進階", PrestigeType.TwinBladesExpertise },
        { "匕首・進階", PrestigeType.DaggersExpertise },
        { "爪・進階", PrestigeType.ClawsExpertise },
        { "工匠・進階", PrestigeType.WorkerLegacy },
        { "戰士・進階", PrestigeType.WarriorLegacy },
        { "學者・進階", PrestigeType.ScholarLegacy },
        { "盜賊・進階", PrestigeType.RogueLegacy },
        { "突變・進階", PrestigeType.MutantLegacy },
        { "吸血鬼・進階", PrestigeType.DraculinLegacy },
        { "不朽・進階", PrestigeType.ImmortalLegacy },
        { "野獸・進階", PrestigeType.CreatureLegacy },
        { "暴徒・進階", PrestigeType.BruteLegacy },
        { "墮化・進階", PrestigeType.CorruptionLegacy }
    };
    public static readonly Dictionary<PrestigeType, string> PrestigeTypeToZh = ZhToPrestigeType.ToDictionary(x => x.Value, x => x.Key);
    public static readonly Dictionary<string, string> BloodStatNameZh = new()
    {
        { "HealingReceived", "治療效果提升" },
        { "DamageReduction", "傷害減免" },
        { "PhysicalResistance", "物理抗性" },
        { "SpellResistance", "法術抗性" },
        { "ResourceYield", "資源採集效率" },
        { "ReducedBloodDrain", "血液消耗減緩" },
        { "SpellCooldownRecoveryRate", "法術冷卻縮短" },
        { "WeaponCooldownRecoveryRate", "武器冷卻縮短" },
        { "UltimateCooldownRecoveryRate", "終極技冷卻縮短" },
        { "MinionDamage", "召喚物傷害提升" },
        { "AbilityAttackSpeed", "技能攻速提升" },
        { "CorruptionDamageReduction", "墮化傷害減免" }
    };


    public static readonly Dictionary<string, string> WeaponStatNameZh = new()
    {
        { "MaxHealth", "最大生命" },
        { "MovementSpeed", "移動速度" },
        { "PrimaryAttackSpeed", "普攻攻速" },
        { "PhysicalLifeLeech", "物理吸血" },
        { "SpellLifeLeech", "法術吸血" },
        { "PrimaryLifeLeech", "普攻吸血" },
        { "PhysicalPower", "物理強度" },
        { "SpellPower", "法術強度" },
        { "PhysicalCritChance", "物理爆擊率" },
        { "PhysicalCritDamage", "物理爆傷" },
        { "SpellCritChance", "法術爆擊率" },
        { "SpellCritDamage", "法術爆傷" }
    };

    public static readonly Dictionary<string, string> FamiliarStatNameZh = new()
    {
        { "MaxHealth", "最大生命" },
        { "PhysicalPower", "物理強度" },
        { "SpellPower", "法術強度" },
        { "PrimaryLifeLeech", "普攻吸血" },
        { "PhysicalLifeLeech", "物理吸血" },
        { "SpellLifeLeech", "法術吸血" },
        { "HealingReceived", "治療效果提升" },
        { "DamageReduction", "傷害減免" },
        { "PhysicalResistance", "物理抗性" },
        { "SpellResistance", "法術抗性" },
        { "MovementSpeed", "移動速度" },
        { "CastSpeed", "施法速度" }
    };
}