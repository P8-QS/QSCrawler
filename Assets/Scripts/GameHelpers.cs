using UnityEngine;

public static class GameHelpers
{
    private static System.Random random = new System.Random();

    /// <summary>
    /// Calculates damage based on a range and applies critical hit chance and multiplier.
    /// </summary>
    /// <param name="minDamage"></param>
    /// <param name="maxDamage"></param>
    /// <param name="critChance"></param>
    /// <param name="critMultiplier"></param>
    /// <returns></returns>
    public static int CalculateDamage(int minDamage, int maxDamage, float critChance = 0, float critMultiplier = 1)
    {
        int damage = random.Next(minDamage, maxDamage + 1);

        if (critChance > 0 && random.NextDouble() < critChance)
        {
            damage = Mathf.RoundToInt(damage * critMultiplier);
            return damage;
        }

        return damage;
    }

    /// <summary>
    /// Calculates damage based on base damage, level and scaling factor.
    /// </summary>
    /// <param name="baseDamage"></param>
    /// <param name="level"></param>
    /// <param name="scalingFactor"></param>
    /// <returns></returns>
    public static int CalculateDamageStat(int baseDamage, int level, float scalingFactor)
    {
        if (level <= 0)
        {
            return baseDamage;
        }

        return baseDamage + (int)(baseDamage * (Mathf.Pow(level - 1, scalingFactor) * 0.1f));
    }

}