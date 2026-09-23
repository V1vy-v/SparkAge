using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] Image fill;
    [SerializeField] TextMeshProUGUI txtValue;

    RectTransform fillRect;
    public RectTransform Rect { get; private set; }

    private void Awake()
    {
        Rect = GetComponent<RectTransform>();
        fillRect = fill.rectTransform;
    }

    public void SetValue(int current, int max)
    {
        float ratio = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);

        fillRect.anchorMin = new Vector2(0, 0.05f);
        fillRect.anchorMax = new Vector2(ratio, 0.95f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        txtValue.SetText("{0}/{1}", Mathf.Max(0, current), max);
    }

    public void SetColor(Color color)
    {
        fill.color = color;
    }
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}