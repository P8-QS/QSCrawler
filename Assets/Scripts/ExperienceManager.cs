using System.Collections.Generic;
using Managers;
using UnityEngine;
using UnityEngine.SceneManagement;


using System;

public class ExperienceManager
{
    private int _experience;
    public int Experience
    {
        get => _experience;
        set
        {
            Level = CalculateLevelFromTotalXp(value);
            ExperienceMax = LevelToXpRequired(Level);
            _experience = value;
            Debug.Log($"Experience changed to {_experience}");
            Debug.Log($"Level changed to {Level}");
        }
    }
    public int ExperienceMax { get; private set; }
    public int Level { get; private set; } = 1;

    public ExperienceManager(int xp = 0)
    {
        Experience = xp;
    }

    private int CalculateLevelFromTotalXp(int totalXp)
    {
        int level = 1;
        int xp = 0;

        while (xp <= totalXp)
        {
            xp = LevelToXpRequired(level);
            level++;
        }

        return level - 1;
    }
    // Calculates XP required to level up
    private int LevelToXpRequired(int level)
    {
        if (level == 1)
        {
            return 50;
        }
        return (int)Math.Pow(level, 1.5) * 100;
    }

    // Adds XP and handles level-ups
    private void AddExperience(int xp)
    {
        Experience += xp;
    }

    // XP gained from defeating an enemy
    public int AddEnemy(int enemyLevel)
    {
        int xp = 10 * (enemyLevel * enemyLevel);
        AddExperience(xp);
        return xp;
    }

    // XP gained from defeating a boss
    public int AddBoss(int enemyLevel)
    {
        int xp = 30 * (enemyLevel * enemyLevel);
        AddExperience(xp);
        return xp;
    }

    // XP gained from completing a quest
    public int AddQuest(int difficulty)
    {
        int xp = 50 + (20 * difficulty);
        AddExperience(xp);
        return xp;
    }

    // XP gained from achievements
    public int AddAchievement(int tier)
    {
        int xp = (int)(100 * Math.Pow(tier, 1.5));
        AddExperience(xp);
        return xp;
    }

    // XP gained from winning a game
    public int AddGameWin()
    {
        int xp = (int)(10 + (100 * Math.Pow(Level, 1.2)));
        AddExperience(xp);
        return xp;
    }
}
