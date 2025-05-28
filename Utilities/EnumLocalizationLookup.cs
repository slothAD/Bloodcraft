using Bloodcraft.Interfaces;

namespace Bloodcraft.Utilities.EnumLocalization;

public static class EnumLocalizationLookup
{
    public static string GetWeaponTypeZh(WeaponType type)
        => EnumLocalization.WeaponTypeToZh.TryGetValue(type, out var name) ? name : type.ToString();

    public static string GetBloodTypeZh(BloodType type)
        => EnumLocalization.BloodTypeToZh.TryGetValue(type, out var name) ? name : type.ToString();

    public static string GetProfessionZh(ProfessionType type)
        => EnumLocalization.ProfessionTypeToZh.TryGetValue(type, out var name) ? name : type.ToString();


    // 注意：這些是遊戲內建 enum，使用 .ToString() 作為查找
    public static string GetWeaponStatZh(Enum statType)
        => EnumLocalization.WeaponStatNameZh.TryGetValue(statType.ToString(), out var name) ? name : statType.ToString();

    public static string GetBloodStatZh(Enum statType)
        => EnumLocalization.BloodStatNameZh.TryGetValue(statType.ToString(), out var name) ? name : statType.ToString();

    public static string GetFamiliarStatZh(Enum statType)
        => EnumLocalization.FamiliarStatNameZh.TryGetValue(statType.ToString(), out var name) ? name : statType.ToString();
}
