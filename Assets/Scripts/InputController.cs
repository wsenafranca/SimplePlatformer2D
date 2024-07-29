using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class InputController : MonoBehaviour
{
    public bool jumpPressed;
    public bool jumpReleased;
    public float moveInput;
    public bool sprint;
    public bool dash;
    public bool groundSlide;
    public bool fallAttack;

    private void LateUpdate()
    {
        jumpPressed = jumpReleased = false;
        dash = false;
        groundSlide = false;
        fallAttack = false;
    }

    public void OnJump(InputValue input)
    {
        if(input.isPressed) jumpPressed = true;
        if (!input.isPressed) jumpReleased = true;
    }

    public void OnMove(InputValue input)
    {
        var move = input.Get<Vector2>();
        moveInput = move.x;
    }

    public void OnSprint(InputValue input)
    {
        sprint = input.isPressed;
    }

    public void OnDash()
    {
        dash = true;
    }

    public void OnGroundSlide()
    {
        groundSlide = true;
    }

    public void OnFallAttack()
    {
        fallAttack = true;
    }
}
