using UnityEngine;

/// <summary>
/// Defines different ammunition types in the game
/// </summary>
public enum AmmoType
{
    None,           // For melee weapons
    PistolAmmo,     // Used by pistols and SMGs
    RifleAmmo,      // Used by rifles
    SniperAmmo,     // Used by snipers
    ShotgunShells   // Used by shotguns
}

/// <summary>
/// Static class to hold ammo configuration
/// </summary>
public static class AmmoConfig
{
    public static readonly int MaxPistolAmmo = 120;
    public static readonly int MaxRifleAmmo = 90;
    public static readonly int MaxSniperAmmo = 30;
    public static readonly int MaxShotgunShells = 24;

    public static int GetMaxAmmo(AmmoType type)
    {
        switch (type)
        {
            case AmmoType.PistolAmmo: return MaxPistolAmmo;
            case AmmoType.RifleAmmo: return MaxRifleAmmo;
            case AmmoType.SniperAmmo: return MaxSniperAmmo;
            case AmmoType.ShotgunShells: return MaxShotgunShells;
            default: return 0;
        }
    }

    public static string GetAmmoName(AmmoType type)
    {
        switch (type)
        {
            case AmmoType.PistolAmmo: return "Pistol Ammo";
            case AmmoType.RifleAmmo: return "Rifle Ammo";
            case AmmoType.SniperAmmo: return "Sniper Ammo";
            case AmmoType.ShotgunShells: return "Shotgun Shells";
            default: return "None";
        }
    }
}