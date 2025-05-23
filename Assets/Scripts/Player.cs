using System.Linq;
using Effects;
using Managers;
using UnityEngine;

public class Player : Mover
{
    [Header("Stats")] public PlayerStats basePlayerStats;

    [HideInInspector] public RuntimePlayerStats playerStats;

    [Header("References")] public Animator animator;

    private float lastAttackTime = 0f;
    private Weapon weapon;
    private Animator weaponAnimator;
    public Joystick movementJoystick;
    private float hitAnimationTimer = 0f;
    private const float HIT_ANIMATION_DURATION = 0.15f;

    protected void Awake()
    {
        if (basePlayerStats == null)
        {
            Debug.LogError("Player base stats not assigned to " + gameObject.name);
            return;
        }

        playerStats = new RuntimePlayerStats(basePlayerStats);

        SetStats(basePlayerStats);

        weapon = GetComponentInChildren<Weapon>();

        boxCollider = GetComponent<BoxCollider2D>();
        initialSize = transform.localScale;

        GameObject weaponObj = transform.Find("weapon_00").gameObject;
        weaponAnimator = weaponObj.GetComponent<Animator>();

        if (animator == null)
            animator = GetComponent<Animator>();

        currentSpeed = playerStats.BaseSpeed;
    }

    protected override void Start()
    {
        level = ExperienceManager.Instance.Level;
        base.Start();

        movementJoystick = GameObject.Find("Canvas").transform.Find("Safe Area").Find("Variable Joystick")
            .GetComponent<Joystick>();

        var metrics = MetricsManager.Instance?.metrics.Values;
        if (metrics != null)
        {
            foreach (var effect in metrics.SelectMany(metric => metric.Effects))
            {
                effect.Apply();
            }
        }

        var perks = PerksManager.Instance?.Perks.Values;
        if (perks != null)
        {
            foreach (var perk in perks)
            {
                perk.Apply();
            }
        }
    }

    private void Update()
    {
        FloatingTextManager.Instance.Show("Level " + level, 6, Color.white, transform.position + Vector3.up * 0.2f,
            Vector3.zero, 0.0001f);

        if (ExperienceManager.Instance.Level > level)
        {
            FloatingTextManager.Instance.Show("Level Up!", 14, new Color(255, 215, 0), transform.position,
                Vector3.up * 1.5f, 1.5f);
            SoundFxManager.Instance.PlaySound(playerStats.LevelUpSound, transform, 0.3f);
        }

        level = ExperienceManager.Instance.Level;
        if (hitAnimationTimer > 0)
        {
            hitAnimationTimer -= Time.deltaTime;
            if (hitAnimationTimer <= 0)
            {
                animator.SetBool("hit", false);
            }
        }
    }

    private void FixedUpdate()
    {
        Vector3 input = new Vector3(movementJoystick.Direction.x * currentSpeed,
            movementJoystick.Direction.y * currentSpeed, 0);
        Animate(input);
        UpdateMotor(input);

        if (Time.time >= lastAttackTime + playerStats.AttackCooldown)
        {
            Attack();
        }
    }

    public void Attack()
    {
        SoundFxManager.Instance.PlaySound(playerStats.AttackSound, transform, 0.8f);
        lastAttackTime = Time.time;

        float anim_length = GetWeaponAnimationClipLength("weapon_swing");

        if (playerStats.AttackCooldown < anim_length)
        {
            float anim_speed = anim_length / playerStats.AttackCooldown;
            weaponAnimator.speed = anim_speed;
        }

        weaponAnimator.SetTrigger("Attack");

        weapon.canAttack = true;
        weapon.CreateSlashEffect();

        Invoke(nameof(DisableWeaponCollider), 0.3f);
    }

    public void DisableWeaponCollider()
    {
        weapon.canAttack = false;
    }

    public override void ReceiveDamage(Damage dmg)
    {
        int previousHitpoints = hitpoint;

        base.ReceiveDamage(dmg);

        if (hitpoint < previousHitpoints && animator != null)
        {
            animator.SetBool("hit", true);
            hitAnimationTimer = HIT_ANIMATION_DURATION;
        }
    }

    protected override void Death()
    {
        SoundFxManager.Instance.PlaySound(playerStats.DeathSound, 1f);
        Destroy(gameObject);
        GameSummaryManager.Instance.Show();
    }

    private void Animate(Vector3 input)
    {
        if (animator != null)
        {
            if (hitAnimationTimer <= 0)
            {
                float magnitude = input.magnitude;
                animator.SetBool("moving", magnitude > 0.1f);
            }
        }
        else
        {
            Debug.LogWarning("Animator component not found on Player!");
        }
    }

    private float GetWeaponAnimationClipLength(string clipName)
    {
        foreach (var clip in weaponAnimator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName)
                return clip.length;
        }

        Debug.LogWarning("Animation clip not found!");
        return 1f;
    }
}