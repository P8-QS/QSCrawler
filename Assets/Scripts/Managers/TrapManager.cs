using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Effects;
using Managers;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TrapManager : MonoBehaviour
{
    [Header("References")] private Transform playerTransform;
    public Tilemap trapTilemap;

    [Header("Trap Tiles")] public TileBase hiddenTrapTile;
    public TileBase[] trapAnimationTiles;

    [Header("Settings")] [Range(0, 100)] public float damagePercentage = 10f; // Damage as percentage of max HP
    public float animationSpeed = 0.3f;
    public bool resetAfterTriggering = true;

    private HashSet<Vector3Int> triggeredTraps = new HashSet<Vector3Int>();

    [SerializeField] private GameObject trapTriggerPrefab;
    public AudioClip trapSound;

    private bool _dodgeTraps;

    void Start()
    {
        if (trapTilemap == null)
            trapTilemap = transform.parent.GetComponent<Tilemap>();

        if (playerTransform == null)
            playerTransform = GameManager.Instance.player.transform;

        // Scuffed way to apply DodgeTraps effect
        var effects = MetricsManager.Instance.metrics.Values.SelectMany(metrics => metrics.Effects);
        _dodgeTraps = effects.Any(e => e is DodgeTrapsEffect);
        
        SetTrapTriggers();
    }

    void SetTrapTriggers()
    {
        foreach (Vector3Int pos in trapTilemap.cellBounds.allPositionsWithin)
        {
            if (trapTilemap.GetTile(pos) == hiddenTrapTile)
            {
                Vector3 worldPos = trapTilemap.GetCellCenterWorld(pos);
                GameObject triggerObj = Instantiate(trapTriggerPrefab.gameObject, worldPos, Quaternion.identity);

                var trapTrigger = triggerObj.GetComponent<TrapTrigger>();
                trapTrigger.trapManager = this;
                trapTrigger.trapCellPosition = pos;
            }
        }
    }

    public void TriggerTrapAt(Vector3Int cellPosition, GameObject player)
    {
        if (!_dodgeTraps &&!triggeredTraps.Contains(cellPosition))
        {
            StartCoroutine(TriggerTrap(cellPosition, player));
        }
    }

    IEnumerator TriggerTrap(Vector3Int cellPosition, GameObject player)
    {
        triggeredTraps.Add(cellPosition);

        SoundFxManager.Instance.PlaySound(trapSound, 0.3f);

        // Apply damage to player based on percentage of max HP
        Fighter fighter = player.GetComponent<Fighter>();
        if (fighter != null)
        {
            // Calculate damage as percentage of max HP
            int calculatedDamage = Mathf.RoundToInt(fighter.maxHitpoint * (damagePercentage / 100f));

            Damage dmg = new Damage
            {
                damageAmount = calculatedDamage,
                origin = trapTilemap.GetCellCenterWorld(cellPosition),
                pushForce = 2.0f,
                isCritical = false,
                minPossibleDamage = calculatedDamage,
                maxPossibleDamage = calculatedDamage
            };

            fighter.ReceiveDamage(dmg);
        }

        // Play animation
        foreach (TileBase animTile in trapAnimationTiles)
        {
            trapTilemap.SetTile(cellPosition, animTile);
            yield return new WaitForSeconds(animationSpeed);
        }

        // Reset trap if configured to do so
        if (resetAfterTriggering)
        {
            trapTilemap.SetTile(cellPosition, hiddenTrapTile);
            triggeredTraps.Remove(cellPosition);
        }
        else
        {
            // Leave the last animation frame
            trapTilemap.SetTile(cellPosition, trapAnimationTiles[trapAnimationTiles.Length - 1]);
        }
    }
}