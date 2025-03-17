using UnityEngine;
using System.IO;

public class GraphGenerator : MonoBehaviour
{
    public int width = 512;
    public int height = 256;
    public Color backgroundColor = Color.white;
    public Color lineColor = Color.blue;
    public int lineThickness = 3;  // You can adjust this value to increase or decrease line thickness

    // This method is called when the script starts.
    void Start()
    {
        // Create a new texture with the specified width and height
        Texture2D texture = new Texture2D(width, height);

        // Fill the texture with the background color
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                texture.SetPixel(x, y, backgroundColor);
            }
        }

        // Hardcoded test weather data:
        // Weather states: 0 = Rainy, 1 = Cloudy, 2 = Sunny
        int[] weatherStates = { 0, 1, 2, 1, 0, 2, 2, 1, 0 };

        // Map weather states to y-values (for visualization)
        // Adjust these values to match your graph's scale.
        int[] data = new int[weatherStates.Length];
        for (int i = 0; i < weatherStates.Length; i++)
        {
            switch (weatherStates[i])
            {
                case 0: // Rainy
                    data[i] = 50;
                    break;
                case 1: // Cloudy
                    data[i] = 128;
                    break;
                case 2: // Sunny
                    data[i] = 206;
                    break;
            }
        }

        int numPoints = data.Length;

        // Draw the graph as a simple line connecting data points
        for (int i = 0; i < numPoints - 1; i++)
        {
            Vector2 startPoint = new Vector2(i * width / (numPoints - 1), data[i]);
            Vector2 endPoint = new Vector2((i + 1) * width / (numPoints - 1), data[i + 1]);
            DrawLine(texture, startPoint, endPoint, lineColor);
        }

        // Apply changes to the texture
        texture.Apply();

        // Encode the texture to PNG format and save it to disk
        byte[] bytes = texture.EncodeToPNG();
        string filePath = Application.dataPath + "/GraphImages/Graph.png";
        Directory.CreateDirectory(Application.dataPath + "/GraphImages"); // Ensure the folder exists
        File.WriteAllBytes(filePath, bytes);
        Debug.Log("Graph saved to: " + filePath);
    }

    // Helper method to draw a thick pixel (a block of pixels) based on the line thickness.
    void DrawThickPixel(Texture2D tex, int x, int y, Color color)
    {
        int halfThickness = lineThickness / 2;
        for (int dx = -halfThickness; dx <= halfThickness; dx++)
        {
            for (int dy = -halfThickness; dy <= halfThickness; dy++)
            {
                int drawX = x + dx;
                int drawY = y + dy;
                if (drawX >= 0 && drawX < tex.width && drawY >= 0 && drawY < tex.height)
                {
                    tex.SetPixel(drawX, drawY, color);
                }
            }
        }
    }

    // Helper method: Draws a line on the texture using Bresenham’s algorithm and applies line thickness.
    void DrawLine(Texture2D tex, Vector2 p1, Vector2 p2, Color color)
    {
        int x0 = (int)p1.x;
        int y0 = (int)p1.y;
        int x1 = (int)p2.x;
        int y1 = (int)p2.y;
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = (x0 < x1) ? 1 : -1;
        int sy = (y0 < y1) ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            DrawThickPixel(tex, x0, y0, color);
            if (x0 == x1 && y0 == y1)
                break;
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }
}
