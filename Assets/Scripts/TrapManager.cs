using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TrapManager : MonoBehaviour
{
    [Header("References")]
    private Transform playerTransform;
    public Tilemap trapTilemap;

    [Header("Trap Tiles")]
    public TileBase hiddenTrapTile;
    public TileBase[] trapAnimationTiles;

    [Header("Settings")]
    [Range(0, 100)]
    public float damagePercentage = 10f; // Damage as percentage of max HP
    public float animationSpeed = 0.3f;
    public float checkInterval = 0.05f;
    public bool resetAfterTriggering = true;

    private HashSet<Vector3Int> triggeredTraps = new HashSet<Vector3Int>();
    private float lastCheckTime;

    void Start()
    {
        if (trapTilemap == null)
            trapTilemap = transform.parent.GetComponent<Tilemap>();

        if (playerTransform == null)
            playerTransform = GameManager.Instance.player.transform;
    }

    void Update()
    {
        if (Time.time - lastCheckTime > checkInterval)
        {
            CheckForTraps();
            lastCheckTime = Time.time;
        }
    }

    void CheckForTraps()
    {
        if (!playerTransform) return;
        
        Vector3Int cellPosition = trapTilemap.WorldToCell(playerTransform.position);
        TileBase currentTile = trapTilemap.GetTile(cellPosition);

        if (currentTile == hiddenTrapTile && !triggeredTraps.Contains(cellPosition))
        {
            StartCoroutine(TriggerTrap(cellPosition, playerTransform.gameObject));
        }
    }

    IEnumerator TriggerTrap(Vector3Int cellPosition, GameObject player)
    {
        triggeredTraps.Add(cellPosition);

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

            fighter.SendMessage("ReceiveDamage", dmg);
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