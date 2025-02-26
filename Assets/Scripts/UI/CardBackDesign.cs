using UnityEngine;
using UnityEngine.UI;

public class CardBackDesign : MonoBehaviour
{
    private static Color primaryColor = new Color(0.7f, 0.1f, 0.1f); // Dark red
    private static Color secondaryColor = new Color(0.9f, 0.8f, 0.3f); // Gold
    private static int patternSize = 128;

    public static Sprite CreateCardBackSprite()
    {
        Texture2D texture = new Texture2D(patternSize, patternSize);
        texture.filterMode = FilterMode.Bilinear;

        // Fill background
        Color[] colors = new Color[patternSize * patternSize];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = primaryColor;
        }
        texture.SetPixels(colors);

        // Draw border
        DrawBorder(texture, 4, secondaryColor);
        
        // Draw diagonal pattern
        DrawDiagonalPattern(texture);
        
        // Draw center ornament
        DrawCenterOrnament(texture);
        
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, patternSize, patternSize), new Vector2(0.5f, 0.5f));
    }

    private static void DrawBorder(Texture2D texture, int thickness, Color color)
    {
        // Draw outer border
        for (int x = 0; x < patternSize; x++)
        {
            for (int y = 0; y < thickness; y++)
            {
                texture.SetPixel(x, y, color); // Bottom
                texture.SetPixel(x, patternSize - 1 - y, color); // Top
                texture.SetPixel(y, x, color); // Left
                texture.SetPixel(patternSize - 1 - y, x, color); // Right
            }
        }

        // Draw inner border
        int offset = thickness * 3;
        for (int x = offset; x < patternSize - offset; x++)
        {
            for (int y = 0; y < thickness; y++)
            {
                texture.SetPixel(x, y + offset, color);
                texture.SetPixel(x, patternSize - 1 - y - offset, color);
            }
        }
        for (int y = offset; y < patternSize - offset; y++)
        {
            for (int x = 0; x < thickness; x++)
            {
                texture.SetPixel(x + offset, y, color);
                texture.SetPixel(patternSize - 1 - x - offset, y, color);
            }
        }
    }

    private static void DrawDiagonalPattern(Texture2D texture)
    {
        int spacing = 8;
        for (int x = 0; x < patternSize; x++)
        {
            for (int y = 0; y < patternSize; y++)
            {
                if (((x + y) % spacing) == 0)
                {
                    texture.SetPixel(x, y, secondaryColor);
                }
            }
        }
    }

    private static void DrawCenterOrnament(Texture2D texture)
    {
        int centerSize = patternSize / 3;
        int startX = (patternSize - centerSize) / 2;
        int startY = (patternSize - centerSize) / 2;

        // Draw diamond shape
        for (int x = 0; x < centerSize; x++)
        {
            for (int y = 0; y < centerSize; y++)
            {
                float dx = Mathf.Abs(x - centerSize/2f);
                float dy = Mathf.Abs(y - centerSize/2f);
                if ((dx + dy) <= centerSize/2f)
                {
                    texture.SetPixel(startX + x, startY + y, secondaryColor);
                }
            }
        }

        // Add detail to diamond
        int margin = 4;
        for (int x = margin; x < centerSize - margin; x++)
        {
            for (int y = margin; y < centerSize - margin; y++)
            {
                float dx = Mathf.Abs(x - centerSize/2f);
                float dy = Mathf.Abs(y - centerSize/2f);
                if ((dx + dy) <= (centerSize/2f - margin))
                {
                    texture.SetPixel(startX + x, startY + y, primaryColor);
                }
            }
        }

        // Add cross pattern
        int crossThickness = 4;
        int crossLength = centerSize / 2;
        for (int i = -crossThickness/2; i < crossThickness/2; i++)
        {
            for (int j = -crossLength/2; j < crossLength/2; j++)
            {
                // Horizontal
                texture.SetPixel(
                    startX + centerSize/2 + j,
                    startY + centerSize/2 + i,
                    secondaryColor
                );
                // Vertical
                texture.SetPixel(
                    startX + centerSize/2 + i,
                    startY + centerSize/2 + j,
                    secondaryColor
                );
            }
        }
    }
} 