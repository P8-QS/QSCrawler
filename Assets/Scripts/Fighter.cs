using UnityEngine;
using System.Collections;

public class Fighter : MonoBehaviour
{
    [HideInInspector] protected BaseFighterStats stats;

    // Current stats
    [Tooltip("Current hitpoints of the fighter")]
    [HideInInspector]
    public int hitpoint;
    [Tooltip("Maximum hitpoints of the fighter")]
    [HideInInspector]
    public int maxHitpoint;
    [HideInInspector]
    public float currentSpeed;
    [Tooltip("Current Level of entity")]
    public int level;

    // Immunity
    protected float lastImmune;

    protected HealthBar healthBar;
    protected DamageFlash damageFlash;

    // Effects
    private Coroutine dotCoroutine;
    private Coroutine slowCoroutine;

    protected Vector3 pushDirection;

    private Collider2D col;


    protected void SetStats(BaseFighterStats newStats)
    {
        stats = newStats;
    }

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        damageFlash = GetComponent<DamageFlash>();
    }

    protected virtual void Start()
    {
        if (stats == null)
        {
            Debug.LogError("Fighter stats not assigned to " + gameObject.name);
            return;
        }

        if (EnemyUIManager.Instance != null)
        {
            healthBar = EnemyUIManager.Instance.CreateHealthBar(this);
        }

        maxHitpoint = stats.CalculateMaxHealth(level);
        hitpoint = maxHitpoint;
        currentSpeed = stats.baseSpeed;
    }

    // Method to calculate max health based on level
    protected virtual int CalculateMaxHealth(int level)
    {
        return stats != null ? stats.CalculateMaxHealth(level) : 100;
    }

    public virtual void UpdateStatsForLevel(int newLevel)
    {
        int previousMax = maxHitpoint;
        maxHitpoint = CalculateMaxHealth(newLevel);

        if (maxHitpoint > previousMax)
        {
            hitpoint += (maxHitpoint - previousMax);
        }
    }

    protected virtual void OnDestroy()
    {
        if (EnemyUIManager.Instance != null)
        {
            EnemyUIManager.Instance.RemoveHealthBar(this);
        }
    }

    public virtual void ReceiveDamage(Damage dmg)
    {
        if (Time.time - lastImmune > (stats != null ? stats.immuneTime : 1.0f))
        {
            lastImmune = Time.time;
            hitpoint -= dmg.damageAmount;

            pushDirection = (transform.position - dmg.origin).normalized * dmg.pushForce;

            // Calculate damage percentage relative to max damage range
            float damagePercentage = (dmg.damageAmount - dmg.minPossibleDamage) /
                                   (float)(dmg.maxPossibleDamage - dmg.minPossibleDamage);

            // Determine text color based on damage type
            Color damageColor;

            // If custom color is provided, use it
            if (dmg.useCustomColor)
            {
                damageColor = dmg.customColor;
            }
            else
            {
                // Otherwise use standard coloring logic
                if (gameObject.CompareTag("Player"))
                {
                    // For player
                    damageColor = dmg.isCritical ? Color.white : Color.red;
                }
                else
                {
                    // For enemies
                    damageColor = dmg.isCritical ? Color.yellow : Color.white;
                }
            }

            // Get random position within collider bounds
            Vector3 textPosition = GetRandomPositionInCollider();

            // Scale font size based on damage percentage
            int baseFontSize = GameConstants.MIN_DAMAGE_FONT_SIZE;
            int maxFontSizeBonus = GameConstants.MAX_DAMAGE_FONT_SIZE - GameConstants.MIN_DAMAGE_FONT_SIZE;
            int fontSize = Mathf.RoundToInt(baseFontSize + (damagePercentage * maxFontSizeBonus));

            // Critical hits get extra size boost
            if (dmg.isCritical)
            {
                fontSize += GameConstants.CRIT_FONT_SIZE_BONUS;
            }

            // Clamp font size
            fontSize = Mathf.Clamp(fontSize, GameConstants.MIN_DAMAGE_FONT_SIZE, GameConstants.MAX_DAMAGE_FONT_SIZE);

            // Show damage text
            FloatingTextManager.Instance.Show(dmg.damageAmount.ToString(), fontSize, damageColor, textPosition, Vector3.up, 0.5f);

            // Flash the sprite
            if (damageFlash != null)
            {
                damageFlash.CallDamageFlash(damageColor);
            }

            // Update health bar if it exists
            if (healthBar != null)
            {
                healthBar.UpdateHealthBar();
            }

            if (hitpoint <= 0)
            {
                hitpoint = 0;
                Death();
            }
        }
    }

    public void ApplyDamageOverTime(Damage damage, float duration, float tickRate)
    {
        if (dotCoroutine != null)
        {
            StopCoroutine(dotCoroutine);
        }
        dotCoroutine = StartCoroutine(ApplyDamageOverTimeCoroutine(damage, duration, tickRate));
    }

    private IEnumerator ApplyDamageOverTimeCoroutine(Damage damage, float duration, float tickRate)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // Apply damage to the target
            ReceiveDamage(damage);

            // Wait for the next tick
            yield return new WaitForSeconds(tickRate);
            elapsed += tickRate;
        }
    }

    public void ApplySlowEffect(float slowFactor, float duration)
    {
        if (slowCoroutine != null)
        {
            StopCoroutine(slowCoroutine);
        }
        slowCoroutine = StartCoroutine(ApplySlowEffectCoroutine(slowFactor, duration));
    }

    private IEnumerator ApplySlowEffectCoroutine(float slowFactor, float duration)
    {
        // Apply the slow effect
        float originalSpeed = stats != null ? stats.baseSpeed : currentSpeed;
        currentSpeed = originalSpeed * slowFactor;

        // Wait for a short time
        yield return new WaitForSeconds(duration);

        // Restore the original speed if the player is not in another puddle
        currentSpeed = originalSpeed;
        slowCoroutine = null;
    }

    private Vector3 GetRandomPositionInCollider()
    {
        if (col is BoxCollider2D box)
        {
            // Get the world size of the box
            Vector3 size = box.bounds.size;
            Vector3 offset = new Vector3(Random.Range(-size.x / 2, size.x / 2), Random.Range(-size.y / 2, size.y / 2), 0);
            return box.bounds.center + offset;
        }
        else if (col is CircleCollider2D circle)
        {
            // Get a random position within the circle
            Vector2 randomInsideCircle = Random.insideUnitCircle * circle.bounds.extents.x; // Uses bounds to be more flexible
            return circle.bounds.center + new Vector3(randomInsideCircle.x, randomInsideCircle.y, 0);
        }

        return transform.position; // Fallback in case there's no recognized collider
    }

    protected virtual void Death()
    {
        // This method is meant to be overwritten
    }
}