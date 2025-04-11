using System.Collections;
using UnityEngine;
using Effects;

public class Necromancer : Enemy
{
    [Header("Fireball Settings")]
    public GameObject fireballPrefab;
    public float fireballSpeed = 3.0f;
    public float attackCooldown = 2.0f;
    public int baseMinFireballDamage = 2;
    public int baseMaxFireballDamage = 4;
    public int MinFireBallDamage => GameHelpers.CalculateDamageStat(baseMinFireballDamage, level, fireballDamageScaling);
    public int MaxFireBallDamage => GameHelpers.CalculateDamageStat(baseMaxFireballDamage, level, fireballDamageScaling);
    public int fireballDamageScaling = 1;
    public float fireballLifetime = 1.5f;
    public Transform fireballSpawnPoint;

    [Header("Movement Settings")]
    public float retreatDistance = 0.5f;
    public float chaseDistance = 0.1f;
    private float lastAttackTime;
    private Animator animator;
    private bool isAttacking = false;

    protected override void Start()
    {
        base.Start();
        animator = GetComponent<Animator>();
        lastAttackTime = -attackCooldown;         // Allow immediate attack when first encountering player
    }

    protected new void FixedUpdate()
    {
        if (Vector3.Distance(playerTransform.position, startingPosition) < chaseLength)
        {
            if (Vector3.Distance(playerTransform.position, startingPosition) < triggerLength)
            {
                chasing = true;
            }

            if (chasing)
            {

                if (Time.time > lastAttackTime + attackCooldown && !isAttacking)
                {
                    StartCoroutine(CastFireball());
                }

                if (!collidingWithPlayer)
                {
                    // Try to keep some distance for ranged attacks
                    if (Vector3.Distance(transform.position, playerTransform.position) < retreatDistance)
                    {
                        // Move away from player if too close
                        Vector3 directionAwayFromPlayer = (transform.position - playerTransform.position).normalized * currentSpeed;
                        UpdateMotor(directionAwayFromPlayer);
                    }
                    else if (Vector3.Distance(transform.position, playerTransform.position) > chaseDistance)
                    {
                        // Move closer if too far
                        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized * currentSpeed;
                        UpdateMotor(directionToPlayer * 0.5f); // Move at half speed when approaching
                    }
                    else
                    {
                        UpdateMotor(Vector3.zero);

                    }
                }
            }
            else
            {
                UpdateMotor(startingPosition - transform.position);
            }
        }
        else
        {
            // Return to starting position if player is out of range
            UpdateMotor(startingPosition - transform.position);
            chasing = false;
        }

        collidingWithPlayer = false;
        boxCollider.Overlap(filter, hits);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null)
            {
                continue;
            }

            if (hits[i].tag == "Player")
            {
                collidingWithPlayer = true;
            }

            hits[i] = null;
        }
    }

    private IEnumerator CastFireball()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        yield return new WaitForSeconds(0.5f);

        // Spawn and shoot fireball
        ShootFireball();

        yield return new WaitForSeconds(0.5f);

        isAttacking = false;
    }

    private void ShootFireball()
    {
        Vector3 spawnPosition = fireballSpawnPoint != null ?
            fireballSpawnPoint.position :
            transform.position + new Vector3(0, 0.1f, 0);

        GameObject fireball = Instantiate(fireballPrefab, spawnPosition, Quaternion.identity);

        Vector3 direction = (playerTransform.position - spawnPosition).normalized;

        FireballProjectile fireballComponent = fireball.AddComponent<FireballProjectile>();
        int fireballDamage = GameHelpers.CalculateDamage(MinFireBallDamage, MaxFireBallDamage, 0f, 0f);
        fireballComponent.Initialize(direction, fireballSpeed, fireballDamage, fireballLifetime);

        // AudioManager.instance.PlaySound("fireball_cast");
    }
}

public class FireballProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private int damage;
    private float lifetime;
    private float spawnTime;
    private float timeAlive;

    private BoxCollider2D hitbox;
    private System.Random random = new System.Random();
    private Animator animator;

    public void Initialize(Vector3 direction, float speed, int damage, float lifetime)
    {
        this.direction = direction;
        this.speed = speed;
        this.damage = damage;
        this.lifetime = lifetime;
        this.spawnTime = Time.time;

        animator = GetComponent<Animator>();
        hitbox = GetComponent<BoxCollider2D>();

        // Set the rotation to face the direction of travel
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    private void Update()
    {
        timeAlive = (Time.time - spawnTime) / lifetime;
        animator.SetFloat("timeAlive", timeAlive);

        transform.position += direction * speed * Time.deltaTime;

        if (Time.time - spawnTime > lifetime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            Damage dmg = new Damage
            {
                damageAmount = damage,
                origin = transform.position,
                pushForce = 2.0f,
                isCritical = false,
                minPossibleDamage = damage - 1,
                maxPossibleDamage = damage + 1
            };

            collision.SendMessage("ReceiveDamage", dmg);

            Destroy(gameObject);
        }
        else if (collision.tag == "Blocking")
        {
            Destroy(gameObject);
        }
    }
}