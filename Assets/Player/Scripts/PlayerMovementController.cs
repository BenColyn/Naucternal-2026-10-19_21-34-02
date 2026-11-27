using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementController : MonoBehaviour
{
    public Rigidbody2D body;
    public SpriteRenderer spriteRenderer;

    [Header("Bewegung")]
    public float maxSpeed = 5f;
    public float acceleration = 8f;
    public float deceleration = 4f;

    [Header("Drehung")]
    public float rotationFactor = 30f;
    public float maxVelocityFlip = 0.01f;

    private InputAction moveAction;
    private Vector2 moveInput;
    private float currentAngle;

    void Awake()
    {
        moveAction = new InputAction("Move", InputActionType.Value);

        // Tastatur: WASD
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        // Tastatur: Pfeiltasten
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");

        // Controller: linker Stick (Deadzone wird vom System übernommen)
        moveAction.AddBinding("<Gamepad>/leftStick");

        // Controller: Steuerkreuz
        moveAction.AddBinding("<Gamepad>/dpad");
    }

    void OnEnable() => moveAction.Enable();
    void OnDisable() => moveAction.Disable();

    void Update()
    {
        moveInput = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);

        UpdateRotation();
        UpdateFlip();
    }

    void UpdateRotation()
    {
        Vector2 v = body.linearVelocity;

        if (v.sqrMagnitude > 0.1f)
        {
            float targetAngle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;

            float rate = moveInput.sqrMagnitude > 0.01f ? acceleration : deceleration;
            float turnSpeed = rate * rotationFactor;

            currentAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, turnSpeed * Time.deltaTime);
        }

        spriteRenderer.transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);
    }

    void UpdateFlip()
    {
        if (body.linearVelocity.magnitude > maxVelocityFlip) return;

        // cos > 0: Nase zeigt nach rechts, cos < 0: Nase zeigt nach links
        float facing = Mathf.Cos(currentAngle * Mathf.Deg2Rad);

        // Kleine Totzone, damit der Sprite bei fast senkrechter Nase nicht flackert
        if (facing < -0.1f) spriteRenderer.flipY = true;
        else if (facing > 0.1f) spriteRenderer.flipY = false;
    }

    void FixedUpdate()
    {
        Vector2 targetVelocity = moveInput * maxSpeed;
        float rate = moveInput.sqrMagnitude > 0.01f ? acceleration : deceleration;

        body.linearVelocity = Vector2.MoveTowards(
            body.linearVelocity,
            targetVelocity,
            rate * Time.fixedDeltaTime
        );
    }
}