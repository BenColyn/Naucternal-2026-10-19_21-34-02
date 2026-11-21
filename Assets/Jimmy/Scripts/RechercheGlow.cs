using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class RechercheGlow : MonoBehaviour
{
    [SerializeField] private Color fogColor = new Color(0.012f, 0.028f, 0.05f, 1f);

    [Tooltip("How opaque the deep-sea fog is outside the clear circle.")]
    [SerializeField] [Range(0f, 1f)] private float opacity = 0.97f;

    [Tooltip("World size of the fog quad. It must stay larger than the camera view.")]
    [SerializeField] private float coverSize = 30f;

    [Tooltip("World radius around the character that stays visible.")]
    [SerializeField] private float viewRadius = 4f;

    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock propertyBlock;

    private static readonly int FogColorId = Shader.PropertyToID("_FogColor");
    private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
    private static readonly int RadiusId = Shader.PropertyToID("_Radius");

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

        coverSize = Mathf.Max(1f, coverSize);
        viewRadius = Mathf.Clamp(viewRadius, 0.2f, coverSize * 0.45f);

        transform.localScale = new Vector3(coverSize, coverSize, 1f);
        transform.localPosition = Vector3.zero;

        spriteRenderer.color = Color.white;
        spriteRenderer.sortingOrder = 1;
        spriteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(FogColorId, fogColor);
        propertyBlock.SetFloat(OpacityId, opacity);
        propertyBlock.SetFloat(RadiusId, viewRadius / coverSize);
        spriteRenderer.SetPropertyBlock(propertyBlock);
    }
}
