using UnityEngine;
using TMPro;
using System.Collections;

public class FloatingText : MonoBehaviour
{
    [SerializeField] private float moveDistance = 1.5f;
    [SerializeField] private float duration = 1f;

    private TextMeshPro textMesh;
    private Color startColor;
    private Vector3 startPosition;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    private void Start()
    {
        startPosition = transform.position;

        if (textMesh != null)
        {
            startColor = textMesh.color;
            startColor.a = 1f;
            textMesh.color = startColor;
        }

        StartCoroutine(AnimateText());
    }

    private IEnumerator AnimateText()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            // Move upward
            transform.position = startPosition
                + Vector3.up * moveDistance * t;

            // Fade out
            Color color = startColor;
            color.a = 1f - t;
            textMesh.color = color;

            yield return null;
        }

        Destroy(gameObject);
    }

    public static TMPro.TextMeshPro Create(string text, Vector3 position, Color color)
    {
        TMPro.TextMeshPro textMeshPro = new GameObject("FloatingText").AddComponent<TMPro.TextMeshPro>();
        textMeshPro.gameObject.AddComponent<FloatingText>();
        textMeshPro.text = text;
        textMeshPro.color = color;
        textMeshPro.fontSize = 2.5f; // Adjust font size as needed
        textMeshPro.gameObject.transform.position = position;
        textMeshPro.alignment = TMPro.TextAlignmentOptions.Center;
        textMeshPro.font = Resources.Load<TMP_FontAsset>("Fonts/JetBrainsMono-Regular SDF");
        return textMeshPro;
    }
}