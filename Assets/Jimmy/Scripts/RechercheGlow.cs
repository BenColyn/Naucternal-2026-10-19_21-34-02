using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class RechercheGlow : MonoBehaviour
{
    [SerializeField] private Color glowColor = new Color(0.25f, 0.85f, 1f, 1f);

    [SerializeField] [Range(0f, 1f)] private float opacity = 0.85f;

    [Tooltip("World size of the glow. A wider X keeps the halo elliptical.")]
    [SerializeField] private Vector2 size = new Vector2(3.2f, 1.2f);

    [Tooltip("Distance below the character. Positive values move the glow downward.")]
    [SerializeField] private float verticalOffset = 1.15f;

    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock propertyBlock;

    private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
    private static readonly int OpacityId = Shader.PropertyToID("_Opacity");

    private void OnEnable()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        propertyBlock ??= new MaterialPropertyBlock();
        ApplyAppearance();
    }

    private void LateUpdate()
    {
        ApplyAppearance();
    }

    private void OnValidate()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        propertyBlock ??= new MaterialPropertyBlock();
        ApplyAppearance();
    }

    private void ApplyAppearance()
    {
        if (spriteRenderer == null)
            return;

        size.x = Mathf.Max(0.01f, size.x);
        size.y = Mathf.Max(0.01f, size.y);
        verticalOffset = Mathf.Max(0f, verticalOffset);

        transform.localScale = new Vector3(size.x, size.y, 1f);
        transform.localPosition = new Vector3(0f, -verticalOffset, 0f);

        spriteRenderer.color = Color.white;
        spriteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(GlowColorId, glowColor);
        propertyBlock.SetFloat(OpacityId, opacity);
        spriteRenderer.SetPropertyBlock(propertyBlock);
    }
}
