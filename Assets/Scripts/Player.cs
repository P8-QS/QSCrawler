using System;
using UnityEngine;

public class Player : Mover
{
    private JoystickMove joystickMove;

    protected override void Start()
    {
        currentLevel = GameManager.instance.XpManager.Level;
        maxHitpoint = 100 + (int)(25 + Mathf.Pow(currentLevel, 1.2f));
        hitpoint = maxHitpoint;
        //base.Start();
        boxCollider = GetComponent<BoxCollider2D>();
        joystickMove = GetComponent<JoystickMove>();
        initialSize = transform.localScale;
    }

    protected void Update()
    {
        currentLevel = GameManager.instance.XpManager.Level;
    }

    private void FixedUpdate()
    {
        Vector3 input = new Vector3(joystickMove.movementJoystick.Direction.x, joystickMove.movementJoystick.Direction.y, 0);
        
        UpdateMotor(input);
    }

    protected override void Death()
    {
        Debug.Log("player died");
        Destroy(gameObject);
    }

}