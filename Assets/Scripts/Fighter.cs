using UnityEngine;

public class Fighter : MonoBehaviour
{
    // Public variables
    public int hitpoint;
    public int maxHitpoint;
    public float pushRecoverySpeed = 0.2f;
    public int currentLevel;

    // Immunity
    private float immuneTime = 1.0f;
    private float lastImmune;

    protected HealthBar healthBar;

    protected Vector3 pushDirection;

    private Collider2D col;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    protected virtual void Start()
    {
        // Get a health bar from the manager
        if (HealthBarManager.Instance != null)
        {
            healthBar = HealthBarManager.Instance.CreateHealthBar(this);
        }

        currentLevel = GameManager.instance.XpManager.Level;
        Debug.Log(currentLevel);

        maxHitpoint = 100 + (int)(25 + Mathf.Pow(currentLevel, 1.2f));
        hitpoint = maxHitpoint;

    }

    protected virtual void OnDestroy()
    {
        // Remove the health bar when entity is destroyed
        if (HealthBarManager.Instance != null)
        {
            HealthBarManager.Instance.RemoveHealthBar(this);
        }
    }

    protected virtual void ReceiveDamage(Damage dmg)
    {
        if (Time.time - lastImmune > immuneTime)
        {
            lastImmune = Time.time;
            hitpoint -= dmg.damageAmount;

            pushDirection = (transform.position - dmg.origin).normalized * dmg.pushForce;

            // Calculate damage percentage relative to max damage range
            float damagePercentage = (dmg.damageAmount - dmg.minPossibleDamage) /
                                   (float)(dmg.maxPossibleDamage - dmg.minPossibleDamage);

            // Determine text color based on whether it's the player, enemy, and if it's a critical hit
            Color damageColor;

            if (gameObject.name == "Player")
            {
                // For player
                damageColor = dmg.isCritical ? Color.white : Color.red;
            }
            else
            {
                // For enemies
                damageColor = dmg.isCritical ? Color.yellow : Color.white;
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
            GameManager.instance.ShowText(dmg.damageAmount.ToString(), fontSize, damageColor, textPosition, Vector3.up, 0.5f);

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
        // This method is meant to be overwritten 4Head
    }
}
