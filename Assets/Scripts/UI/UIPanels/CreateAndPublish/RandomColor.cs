using System;
using System.Collections.Generic;
using System.Drawing;

public static class RandomColor
{
    public static List<string> GenerateRandomColorHexStringList(int numberOfColors)
    {
        List<string> hexStrings = new List<string>();
        Random random = new Random();

        for (int i = 0; i < numberOfColors; i++)
        {
            int r = random.Next(256); // Random red value (0-255)
            int g = random.Next(256); // Random green value (0-255)
            int b = random.Next(256); // Random blue value (0-255)

            string hexString = ColorToHexString(Color.FromArgb(r, g, b));
            hexStrings.Add(hexString);
        }

        return hexStrings;
    }

    public static string ColorToHexString(Color color)
    {
        return "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
    }
}