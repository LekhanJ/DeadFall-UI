using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Scriptable Objects/Weapon Data", order = 1)]
public class WeaponData : ScriptableObject
{
    [Header("Visual")]
    public string weaponName = "Weapon";
    public GameObject weaponPrefab; // Visual model of the weapon (should have a child Transform named "FirePoint")
    public Sprite weaponIcon; // For UI

    [Header("Stats")]
    public float damage = 10f;
    public float fireRate = 0.5f; // Time between shots
    public int magazineCapacity = 10;
    public float reloadTime = 2f;
    public float bulletSpeed = 15f;
    public float bulletLifetime = 2f;

    [Header("Projectile")]
    public GameObject bulletPrefab;

    [Header("Effects")]
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public GameObject muzzleFlashPrefab;
}