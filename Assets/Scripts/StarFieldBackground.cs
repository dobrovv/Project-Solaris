using UnityEngine;

/// <summary>
/// Draws a field of small circular stars behind a 2D scene.
/// Attach to an empty GameObject (at position 0,0,0 with scale 1).
/// No sprite asset needed: the circle texture is generated at runtime.
/// Works with an orthographic camera, including panning and zooming.
/// Stars are clumped using Perlin noise, so you get dense patches and empty voids.
/// </summary>
public class StarfieldBackground : MonoBehaviour
{
    [Header("Stars")]
    [SerializeField] private int starCount = 200;
    [SerializeField] private Vector2 diameterRange = new Vector2(0.03f, 0.12f); // world units
    [Tooltip("Above 1 = mostly tiny stars with a few big ones. 1 = evenly spread sizes.")]
    [SerializeField] private float sizeBias = 3f;
    [SerializeField]
    private Color[] starColors =
    {
        Color.white,
        new Color(0.80f, 0.90f, 1.00f), // slightly blue
        new Color(1.00f, 0.95f, 0.80f)  // slightly warm
    };

    [Header("Distribution")]
    [Tooltip("0 = evenly random, 1 = strong clumps with empty gaps.")]
    [Range(0f, 1f)]
    [SerializeField] private float clustering = 0.75f;
    [Tooltip("Roughly how many clumps fit across the field width. Lower = bigger clumps.")]
    [SerializeField] private float clusterScale = 3f;

    [Header("Twinkle")]
    [SerializeField] private bool twinkle = true;
    [SerializeField] private Vector2 alphaRange = new Vector2(0.35f, 1f);
    [SerializeField] private Vector2 twinkleSpeedRange = new Vector2(0.5f, 2.5f);

    [Header("Layout")]
    [SerializeField] private Camera targetCamera;
    [Tooltip("How much larger than the camera view the star field is. Raise it if you zoom out a lot.")]
    [SerializeField] private float areaMultiplier = 1.5f;
    [Tooltip("0 = stars fixed in the world, 1 = stars locked to the screen. Values near 1 feel far away.")]
    [Range(0f, 1f)]
    [SerializeField] private float parallax = 0.95f;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = -100;
    [SerializeField] private int randomSeed = 0; // 0 = different stars every run

    private Transform[] starTransforms;
    private SpriteRenderer[] starRenderers;
    private Vector2[] basePositions;   // positions inside the field, centered on (0,0)
    private Color[] baseColors;        // alpha holds each star's base brightness
    private float[] phases;
    private float[] speeds;
    private Vector2 fieldSize;
    private Sprite circleSprite;

    private void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null || !targetCamera.orthographic)
        {
            Debug.LogError("StarfieldBackground needs an orthographic camera.", this);
            enabled = false;
            return;
        }

        if (randomSeed != 0) Random.InitState(randomSeed);

        circleSprite = CreateCircleSprite(8);

        float height = targetCamera.orthographicSize * 2f * areaMultiplier;
        fieldSize = new Vector2(height * targetCamera.aspect, height);

        // Random offset so each run (or seed) gets a different clump pattern.
        var noiseOffset = new Vector2(Random.Range(0f, 1000f), Random.Range(0f, 1000f));

        starTransforms = new Transform[starCount];
        starRenderers = new SpriteRenderer[starCount];
        basePositions = new Vector2[starCount];
        baseColors = new Color[starCount];
        phases = new float[starCount];
        speeds = new float[starCount];

        for (int i = 0; i < starCount; i++)
        {
            var go = new GameObject($"Star_{i}");
            go.transform.SetParent(transform, false);

            // Skewed size: most stars tiny, a few larger (and brighter).
            float sizeT = Mathf.Pow(Random.value, sizeBias);
            float diameter = Mathf.Lerp(diameterRange.x, diameterRange.y, sizeT);
            go.transform.localScale = new Vector3(diameter, diameter, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = circleSprite;
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = sortingOrder;

            Color col = starColors[Random.Range(0, starColors.Length)];
            col.a = Mathf.Lerp(0.5f, 1f, sizeT); // small stars are dimmer

            starTransforms[i] = go.transform;
            starRenderers[i] = sr;
            basePositions[i] = SamplePosition(noiseOffset);
            baseColors[i] = col;
            phases[i] = Random.Range(0f, Mathf.PI * 2f);
            speeds[i] = Random.Range(twinkleSpeedRange.x, twinkleSpeedRange.y);

            sr.color = col;
        }
    }

    private void LateUpdate()
    {
        Vector2 cam = targetCamera.transform.position;
        Vector2 drift = cam * (1f - parallax); // how far the camera has moved past the stars
        Vector2 half = fieldSize * 0.5f;

        for (int i = 0; i < starCount; i++)
        {
            // Position relative to the camera, wrapped so the field never runs out.
            Vector2 p = basePositions[i] - drift;
            p.x = Mathf.Repeat(p.x + half.x, fieldSize.x) - half.x;
            p.y = Mathf.Repeat(p.y + half.y, fieldSize.y) - half.y;
            starTransforms[i].position = new Vector3(cam.x + p.x, cam.y + p.y, 0f);

            if (twinkle)
            {
                float t = (Mathf.Sin(Time.time * speeds[i] + phases[i]) + 1f) * 0.5f;
                Color c = baseColors[i];
                c.a *= Mathf.Lerp(alphaRange.x, alphaRange.y, t);
                starRenderers[i].color = c;
            }
        }
    }

    /// <summary>
    /// Picks a random point in the field, favouring areas where Perlin noise is high.
    /// This is rejection sampling: dense noise = accepted often, sparse noise = rarely.
    /// </summary>
    private Vector2 SamplePosition(Vector2 noiseOffset)
    {
        float aspect = fieldSize.y / fieldSize.x; // keeps clumps roughly round
        float u = 0f, v = 0f;

        for (int attempt = 0; attempt < 50; attempt++)
        {
            u = Random.value;
            v = Random.value;

            float noise = Mathf.PerlinNoise(
                noiseOffset.x + u * clusterScale,
                noiseOffset.y + v * clusterScale * aspect);

            // Perlin output mostly sits in 0.3-0.7, so stretch it for stronger contrast.
            float density = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 0.7f, noise));
            float acceptChance = Mathf.Lerp(1f, density, clustering);

            if (Random.value <= acceptChance) break;
        }

        return new Vector2((u - 0.5f) * fieldSize.x, (v - 0.5f) * fieldSize.y);
    }

    private void OnDestroy()
    {
        if (circleSprite != null)
        {
            Destroy(circleSprite.texture);
            Destroy(circleSprite);
        }
    }

    /// <summary>Creates a white, anti-aliased circle sprite that is exactly 1 world unit across.</summary>
    private static Sprite CreateCircleSprite(int resolution)
    {
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        float radius = resolution * 0.5f;
        var center = new Vector2(radius, radius);
        var pixels = new Color[resolution * resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float alpha = Mathf.Clamp01(radius - dist); // 1px soft edge
                pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution),
                             new Vector2(0.5f, 0.5f), resolution);
    }
}