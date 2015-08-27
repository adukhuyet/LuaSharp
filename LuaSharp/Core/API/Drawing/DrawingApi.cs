#region

using System;
using System.Globalization;
using LuaSharp.Classes;
using MoonSharp.Interpreter;
using SharpDX;
using Color = System.Drawing.Color;

#endregion

namespace LuaSharp.Core.API.Drawing
{
    internal class DrawingApi
    {
        public static void AddApi(Script script)
        {
            UserData.RegisterType<ARGB>();

            script.Globals["ARGB"] = (Func<int, int, int, int, ARGB>) MakeARGB;

            script.Globals["DrawCircle"] = (Action<float, float, float, float, ARGB>) DrawCircle;
            script.Globals["DrawCircle"] = (Action<float, float, float, float, uint>) DrawCircle;

            script.Globals["DrawText"] = (Action<string, int, float, float, ARGB>) DrawText;
            script.Globals["DrawText"] = (Action<string, int, float, float, uint>) DrawText;

            script.Globals["DrawLine"] = (Action<float, float, float, float, float, ARGB>) DrawLine;
            script.Globals["DrawLine"] = (Action<float, float, float, float, float, uint>) DrawLine;
            
        }

        private static ARGB MakeARGB(int r, int g, int b, int a)
        {
            return new ARGB(a, r, g, b);
        }

        private static void DrawCircle(float x, float y, float z, float size, ARGB color)
        {
            LeagueSharp.Drawing.DrawCircle(new Vector3(x, y, z), size, color.ToSystemColor());
        }

        private static void DrawCircle(float x, float y, float z, float size, uint color)
        {
            LeagueSharp.Drawing.DrawCircle(new Vector3(x, y, z), size, ReadColor(color));
        }

        private static void DrawLine(float x1, float x2, float y1, float y2, float size, ARGB color)
        {
            LeagueSharp.Drawing.DrawLine(x1, y1, x1, x2, size, color.ToSystemColor());
        }

        private static void DrawLine(float x1, float x2, float y1, float y2, float size, uint color)
        {
            LeagueSharp.Drawing.DrawLine(x1, y1, x2, y2, size, ReadColor(color));
        }

        private static void DrawText(string text, int size, float x, float y, uint color)
        {
            LeagueSharp.Drawing.DrawText(x, y, ReadColor(color), text);
        }

        private static void DrawText(string text, int size, float x, float y, ARGB color)
        {
            LeagueSharp.Drawing.DrawText(x, y, color.ToSystemColor(), text);
        }

        private static Color ReadColor(uint color)
        {
            // Example: 0xFF80FF00
            // Convert unit to hex value 
            var hexString = color.ToString("X");

            // Get hex values of color
            var hexR = byte.Parse(hexString.Substring(0, 2), NumberStyles.HexNumber);
            var hexG = byte.Parse(hexString.Substring(2, 2), NumberStyles.HexNumber);
            var hexB = byte.Parse(hexString.Substring(4, 2), NumberStyles.HexNumber);
            var hexA = byte.Parse(hexString.Substring(6, 2), NumberStyles.HexNumber);

            return Color.FromArgb(hexA, hexR, hexG, hexB);

        }

        private class ARGB
        {
            private readonly int A;
            private readonly int B;
            private readonly int G;
            private readonly int R;

            public ARGB(int a, int r, int g, int b)
            {
                R = r;
                G = g;
                B = b;
                A = a;
            }

            public ARGB()
            {
                R = 0;
                G = 0;
                B = 0;
                A = 0;
            }

            public Color ToSystemColor()
            {
                return Color.FromArgb(A, R, G, B);
            }
        }
    }
}