using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Scriptable Objects/Weapon Data", order = 1)]
public class WeaponData : ScriptableObject
{
    [Header("Visual")]
    public string weaponName = "Weapon";
    public GameObject weaponPrefab;
    public Sprite weaponIcon;

    [Header("Weapon Type")]
    public WeaponType weaponType = WeaponType.Pistol;
    public AmmoType ammoType = AmmoType.PistolAmmo;

    [Header("Stats")]
    public float damage = 10f;
    public float fireRate = 0.5f;
    public int magazineCapacity = 10;
    public float reloadTime = 2f;
    public float bulletSpeed = 15f;
    public float bulletLifetime = 2f;

    [Header("Shotgun Specific")]
    [Tooltip("Number of pellets per shot (shotgun only)")]
    public int pelletsPerShot = 8;
    [Tooltip("Spread angle in degrees (shotgun only)")]
    public float spreadAngle = 15f;

    [Header("Projectile")]
    public GameObject bulletPrefab;

    [Header("Effects")]
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public GameObject muzzleFlashPrefab;
}

public enum WeaponType
{
    Pistol,
    SMG,
    Rifle,
    Sniper,
    Shotgun
}