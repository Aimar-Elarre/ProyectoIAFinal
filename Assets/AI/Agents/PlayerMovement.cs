using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    CharacterController _controller;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
        Vector2 input = GetInput();
        Vector3 movement = new Vector3(input.x, 0f, input.y);

        if (movement.sqrMagnitude > 0.001f)
        {
            movement = movement.normalized * moveSpeed;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(movement),
                rotationSpeed * Time.deltaTime);
        }

        _controller.Move(movement * Time.deltaTime);
    }

    Vector2 GetInput()
    {
        if (Keyboard.current == null)
            return Vector2.zero;

        float x = 0f;
        float z = 0f;

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            z += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            z -= 1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            x += 1f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            x -= 1f;

        return new Vector2(x, z);
    }
}
