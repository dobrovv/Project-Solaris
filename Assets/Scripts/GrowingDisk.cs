using System.Collections;
using TMPro;
using UnityEngine;

public class GrowingDisk : MonoBehaviour
{
    [SerializeField] private float startDiameter = 0.5f;
    [SerializeField] private float endDiameter = 1.0f;
    [SerializeField] private float duration = 3.0f;

    [SerializeField] private Color startColor = Color.red;
    [SerializeField] private Color startFill = Color.blue;

    private Material material;
    private Coroutine animationCoroutine;

    private static readonly int DiameterID = Shader.PropertyToID("_Diameter");
    private static readonly int FillID = Shader.PropertyToID("_Fill");
    private static readonly int ColorID = Shader.PropertyToID("_Color");

    private void Awake()
    {
        // Use the Renderer material so this instance has its own material.
        material = GetComponent<Renderer>().material;
    }

    private void Start()
    {
        Play();
    }

    public void Play()
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        float elapsed = 0f;

        material.SetFloat(DiameterID, startDiameter);
        material.SetColor(ColorID, startColor);
        material.SetColor(FillID, startFill);

        while (elapsed <= duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            // Smooth interpolation for a more natural effect.
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            float diameter = Mathf.Lerp(startDiameter, endDiameter, easedT);

            Color color = startColor;
            color.a = Mathf.Lerp(startColor.a, 0f, t);

            Color fill = startFill;
            fill.a = Mathf.Lerp(startFill.a, 0f, t);

            material.SetFloat(DiameterID, diameter);
            material.SetColor(ColorID, color);
            material.SetColor(FillID, fill);

            yield return null;
        }

        material.SetFloat(DiameterID, endDiameter);

        animationCoroutine = null;
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (material != null)
            Destroy(material);
    }

    public static GameObject CreateEnergyDisk(Vector3 position, Color color, float duration, float startDiameter = 0.3f)
    {

        // 1. Create a Quad primitive
        GameObject quadCircle = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quadCircle.name = "EnergyDisk";
        // 2. Access the Renderer and update its material
        Renderer quadRenderer = quadCircle.GetComponent<Renderer>();
        // 3. Load the material from the Assets/Resources/ folder
        string materialPath = "Shaders/Shader Graphs_Disk";
        Material newMaterial = Resources.Load<Material>(materialPath);
        if (newMaterial != null)
        {
            // 4. Assign the new material to the renderer
            quadRenderer.material = newMaterial;
        }
        else
        {
            Debug.LogError($"Material not found at path: Resources/{materialPath}");
        }

        quadCircle.transform.localScale = new Vector3(1f, 1f, 1f);
        GrowingDisk gc = quadCircle.AddComponent<GrowingDisk>();

        //quadRenderer.material.SetColor("_Color", new Color(252f / 255f, 211f / 255f, 77f / 255f));
        //quadRenderer.material.SetFloat("_Diameter", gc.startDiameter);
        quadRenderer.material.SetFloat("_Thickness", 0.07f);
        //quadRenderer.material.SetColor("_Fill", faction.color/2);
        quadRenderer.material.SetColor("_Fill", Color.black);


        quadCircle.transform.position = new Vector3(position.x, position.y, 102); // set the z-index to a background layer
        gc.duration = duration;
        gc.startColor = color;
        gc.startFill = color / 1.3f;
        gc.startDiameter = startDiameter;
        gc.endDiameter = 0.5f; // Example: double the size over time
        return quadCircle;
    }

}