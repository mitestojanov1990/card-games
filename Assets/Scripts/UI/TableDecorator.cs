using UnityEngine;
using UnityEngine.UI;

public class TableDecorator : MonoBehaviour
{
    private float tableRadius = 400f;
    private Color tableColor = new Color(0.133f, 0.545f, 0.133f); // Forest green
    private Color tableBorderColor = new Color(0.4f, 0.26f, 0.13f); // Brown
    
    public static TableDecorator Create(Transform parent)
    {
        GameObject obj = new GameObject("TableDecorator");
        obj.transform.SetParent(parent, false);
        obj.transform.SetAsFirstSibling(); // Put table at the back
        return obj.AddComponent<TableDecorator>();
    }

    public void Initialize()
    {
        CreateTableBackground();
        CreateTableBorder();
        CreateCornerDecorations();
        CreateCenterPattern();
    }

    private void CreateTableBackground()
    {
        GameObject tableObj = new GameObject("TableBackground");
        tableObj.transform.SetParent(transform, false);

        Image tableImage = tableObj.AddComponent<Image>();
        tableImage.color = tableColor;

        // Make it circular
        tableImage.sprite = CreateCircleSprite();

        RectTransform rt = tableObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(tableRadius * 2, tableRadius * 2);
    }

    private void CreateTableBorder()
    {
        GameObject borderObj = new GameObject("TableBorder");
        borderObj.transform.SetParent(transform, false);

        Image borderImage = borderObj.AddComponent<Image>();
        borderImage.color = tableBorderColor;
        borderImage.sprite = CreateCircleSprite();
        borderImage.type = Image.Type.Sliced;

        RectTransform rt = borderObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(tableRadius * 2 + 20, tableRadius * 2 + 20);
    }

    private void CreateCornerDecorations()
    {
        for (int i = 0; i < 4; i++)
        {
            GameObject cornerObj = new GameObject($"CornerDecoration_{i}");
            cornerObj.transform.SetParent(transform, false);

            Image cornerImage = cornerObj.AddComponent<Image>();
            cornerImage.color = tableBorderColor;
            cornerImage.sprite = CreateDiamondSprite();

            RectTransform rt = cornerObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(50, 50);

            float angle = i * 90f * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * (tableRadius - 25);
            float y = Mathf.Sin(angle) * (tableRadius - 25);
            rt.anchoredPosition = new Vector2(x, y);
            rt.rotation = Quaternion.Euler(0, 0, i * 90f);
        }
    }

    private void CreateCenterPattern()
    {
        GameObject patternObj = new GameObject("CenterPattern");
        patternObj.transform.SetParent(transform, false);

        Image patternImage = patternObj.AddComponent<Image>();
        patternImage.color = new Color(0.1f, 0.4f, 0.1f); // Darker green
        patternImage.sprite = CreateCircleSprite();

        RectTransform rt = patternObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(300, 300);

        // Add a subtle rotation animation
        StartCoroutine(AnimatePattern(rt));
    }

    private System.Collections.IEnumerator AnimatePattern(RectTransform rt)
    {
        float rotationSpeed = 10f;
        while (true)
        {
            rt.Rotate(0, 0, rotationSpeed * Time.deltaTime);
            yield return null;
        }
    }

    private Sprite CreateCircleSprite()
    {
        Texture2D texture = new Texture2D(128, 128);
        Color[] colors = new Color[128 * 128];
        
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                float dx = x - 64;
                float dy = y - 64;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                colors[y * 128 + x] = dist <= 64 ? Color.white : Color.clear;
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateDiamondSprite()
    {
        Texture2D texture = new Texture2D(32, 32);
        Color[] colors = new Color[32 * 32];
        
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dx = Mathf.Abs(x - 16);
                float dy = Mathf.Abs(y - 16);
                colors[y * 32 + x] = (dx + dy) <= 16 ? Color.white : Color.clear;
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
    }
} 