using UnityEngine;

public static class CardUIUtility
{
    public static Sprite CreateRoundedRectSprite(float pixelsPerUnit)
    {
        int width = 128;
        int height = 128;
        float radius = 32;

        Texture2D texture = new Texture2D(width, height);
        Color[] colors = new Color[width * height];
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float alpha = 1;
                
                if (x < radius && y < radius)
                    alpha = GetCornerAlpha(x, y, radius);
                else if (x < radius && y > height - radius)
                    alpha = GetCornerAlpha(x, height - y, radius);
                else if (x > width - radius && y < radius)
                    alpha = GetCornerAlpha(width - x, y, radius);
                else if (x > width - radius && y > height - radius)
                    alpha = GetCornerAlpha(width - x, height - y, radius);
                
                colors[y * width + x] = new Color(1, 1, 1, alpha);
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }

    private static float GetCornerAlpha(float x, float y, float radius)
    {
        float distance = Mathf.Sqrt(x * x + y * y);
        return distance <= radius ? 1 : 0;
    }
} 