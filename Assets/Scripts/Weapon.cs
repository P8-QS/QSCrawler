using Unity.Mathematics;
using UnityEngine;

public class Weapon : Collidable
{
    public int minDamage;
    public int maxDamage;
    public float basePushForce = 2.0f;
    public float critChance = 0.1f; // 10% chance
    public float critMultiplier = 2.0f;

    public int weaponLevel;
    public SpriteRenderer spriteRenderer;

    private float cooldown = 0.5f;
    private float lastSwing;

    private System.Random random = new System.Random();

    protected override void Update()
    {
        base.Update();
        if (Time.time - lastSwing > cooldown)
        {
            lastSwing = Time.time;
            //Swing();
            //getWeaponLevel();
            calcWeaponDmg();
        }
    }

    protected override void OnCollide(Collider2D coll)
    {
        if (coll.tag == "Fighter" && coll.name != "Player")
        {
            int damage = random.Next(minDamage, maxDamage + 1);
            bool isCritical = (random.NextDouble() < critChance);

            if (isCritical)
            {
                damage = Mathf.RoundToInt(damage * critMultiplier);
            }

            float pushForce = basePushForce * (damage / (float)maxDamage);

            Damage dmg = new Damage
            {
                damageAmount = damage,
                origin = transform.position,
                pushForce = pushForce,
                isCritical = isCritical,
                minPossibleDamage = minDamage,
                maxPossibleDamage = maxDamage
            };

            coll.SendMessage("ReceiveDamage", dmg);
        }
    }

    // Calculate weapon damage based on player level
    private void calcWeaponDmg()
    {
        weaponLevel = getWeaponLevel();
        float weaponDmg = (10f + (float)Mathf.Floor(4 * Mathf.Pow(weaponLevel, 1.2f)));
        minDamage = Mathf.RoundToInt(weaponDmg * 0.5f);
        maxDamage = Mathf.RoundToInt(weaponDmg * 1.5f);
    }

    private int getWeaponLevel()
    {
        return weaponLevel = GameManager.instance.XpManager.Level;
    }
    
    private void Swing()
    {
        // Weapon swing logic
    }
}