using UnityEngine;
using UnityEngine.InputSystem;

public class McPlaceholderSwim : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private SpriteRenderer visual;

    [Tooltip("Swim speed of the placeholder in units per second.")]
    [SerializeField] private float swimSpeed = 4f;

    [Tooltip("Optional camera that stays on this placeholder during the underwater test.")]
    [SerializeField] private Camera followCamera;

    private InputActionMap playerMap;
    private InputAction moveAction;

    private void OnEnable()
    {
        if (inputActions == null)
            return;

        playerMap = inputActions.FindActionMap("Player", true).Clone();
        moveAction = playerMap.FindAction("Move", true);
        playerMap.Enable();
    }

    private void OnDisable()
    {
        playerMap?.Disable();
    }

    private void Update()
    {
        if (moveAction == null)
            return;

        Vector2 move = moveAction.ReadValue<Vector2>();
        transform.Translate(new Vector3(move.x, move.y, 0f) * swimSpeed * Time.deltaTime, Space.World);
        ApplyFacing(move);
    }

    private void LateUpdate()
    {
        if (followCamera == null)
            return;

        Vector3 cameraPosition = followCamera.transform.position;
        cameraPosition.x = transform.position.x;
        cameraPosition.y = transform.position.y;
        followCamera.transform.position = cameraPosition;
    }

    private void ApplyFacing(Vector2 move)
    {
        if (visual == null || Mathf.Abs(move.x) <= 0.01f)
            return;

        visual.flipX = move.x < 0f;
    }
}
