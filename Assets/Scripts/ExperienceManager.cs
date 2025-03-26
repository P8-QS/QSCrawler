using System.Collections.Generic;
using Managers;
using UnityEngine;
using UnityEngine.SceneManagement;


using System;

public class ExperienceManager
{
    private static ExperienceManager _instance;
    public static ExperienceManager Instance => _instance ??= new ExperienceManager();

    private ExperienceManager(int xp = 0)
    {
        Experience = xp;
        if (PlayerPrefs.HasKey("Cooldown"))
        {
            CooldownEnd = DateTime.Parse(PlayerPrefs.GetString("Cooldown"));
        }
        else
        {
            CooldownEnd = DateTime.Now;
            PlayerPrefs.SetString("Cooldown", CooldownEnd.ToString());
        }
    }

    /// <summary>
    /// The bonus multiplier for experience.
    /// </summary>
    private const double BonusXpMultiplier = 1.5;

    private bool _bonusXpEnabled;

    /// <summary>
    /// Whether the bonus multiplier is enabled.
    /// </summary>
    public bool BonusXpEnabled => GetXpCooldown().Milliseconds == 0;
    
    private int _experience;

    /// <summary>
    /// The current total experience.
    /// </summary>
    public int Experience
    {
        get => _experience;
        set
        {
            // Update experience
            _experience = value;

            // Update level if necessary
            Level = CalculateLevelFromTotalXp(value);
            ExperienceMax = LevelToXpRequired(Level);
            Debug.Log("XP Val: " + value + " Level: " + Level + " Max: " + ExperienceMax);
        }
    }
    /// <summary>
    /// Experience required to reach next level.
    /// </summary>
    public int ExperienceMax { get; private set; }
    /// <summary>
    /// The current level.
    /// </summary>
    public int Level { get; private set; } = 1;

    public DateTime CooldownEnd;
    
    /// <summary>
    /// Calculates the level based on the total experience.
    /// </summary>
    /// <param name="totalXp"> Experience to calculate the level from.</param>
    /// <returns>The level.</returns>
    private int CalculateLevelFromTotalXp(int totalXp)
    {   
        var level = 1;
        while (LevelToXpRequired(level) <= totalXp) level++;
        return level;
    }
    /// <summary>
    /// Calculates the amount of experience required to reach the level.
    /// </summary>
    /// <param name="level">Level to reach.</param>
    /// <returns>Experience required to reach the level.</returns>
    private int LevelToXpRequired(int level)
    {
        return (int)Math.Pow(level, 1.5)*100;
    }

    /// <summary>
    /// Adds experience and returns the amount of experience added after applying the bonus multiplier.
    /// </summary>
    /// <param name="xp">Amount of experience to add. </param>
    /// <returns> The experience added.</returns>
    private int AddExperience(int xp)
    {
        var adjustedXp = (int)(xp * (BonusXpEnabled ? BonusXpMultiplier : 1));
        Experience += adjustedXp;
        return adjustedXp;
    }

    /// <summary>
    /// Adds experience based on the enemy level.
    /// </summary>
    /// <param name="enemyLevel">Level of the enemy.</param>
    /// <returns>The experience added.</returns>
    public int AddEnemy(int enemyLevel) => AddExperience(10 * (enemyLevel * enemyLevel));
    /// <summary>
    /// Adds experience based on the boss level.
    /// </summary>
    /// <param name="bossLevel">Level of the boss.</param>
    /// <returns>The experience added.</returns>
    public int AddBoss(int bossLevel) => AddExperience(30 * (bossLevel * bossLevel));
    /// <summary>
    /// Adds experience based on the quest difficulty.
    /// </summary>
    /// <param name="difficulty">Difficulty of the quest.</param>
    /// <returns>The experience added.</returns>
    public int AddQuest(int difficulty) => AddExperience(50 + (20 * difficulty));
    /// <summary>
    /// Adds experience based on the achievement tier.
    /// </summary>
    /// <param name="tier">Tier of the achievement</param>
    /// <returns>The experience added.</returns>
    public int AddAchievement(int tier) => AddExperience((int)(100 * Math.Pow(tier, 1.5)));
    /// <summary>
    /// Adds game win experience.
    /// </summary>
    /// <returns>The experience added.</returns>
    public int AddGameWin() => AddExperience((int)(10 + (100 * Math.Pow(Level, 1.2))));
    
    /// <summary>
    /// Resets the cooldown for the bonus multiplier.
    /// </summary>
    public void ResetCooldown()
    {
        CooldownEnd = DateTime.Now.AddDays(1);
        PlayerPrefs.SetString("Cooldown", CooldownEnd.ToString());
    }
    
    /// <summary>
    /// Calculates the time remaining for the bonus multiplier cooldown.
    /// </summary>
    /// <returns> The time remaining for the cooldown. </returns>
    public TimeSpan GetXpCooldown() => CooldownEnd > DateTime.Now ? CooldownEnd - DateTime.Now : TimeSpan.Zero;
    
}
