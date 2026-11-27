using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class DeepSeaBackdrop : MonoBehaviour
{
    [Tooltip("How fast the underwater haze drifts.")]
    [SerializeField] private float scrollSpeed = 0.015f;

    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock propertyBlock;

    private static readonly int ScrollId = Shader.PropertyToID("_Scroll");

    private void OnEnable()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        propertyBlock ??= new MaterialPropertyBlock();
    }

    private void Update()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        propertyBlock ??= new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(ScrollId, Time.time * scrollSpeed);
        spriteRenderer.SetPropertyBlock(propertyBlock);
    }
}
