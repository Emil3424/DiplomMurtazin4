using PdfSharp.Fonts;
using System;
using System.IO;

namespace DiplomMurtazin.Core
{
    public class FontResolver : IFontResolver
    {
        public string DefaultFontName => "Courier New";

        public byte[] GetFont(string faceName)
        {
            try
            {
                // Используем системные шрифты Windows
                var uri = new Uri(@"file:///" + Environment.GetFolderPath(Environment.SpecialFolder.Fonts) + "/");

                switch (faceName)
                {
                    case "Courier New":
                        return LoadFontData(uri, "cour.ttf");
                    case "Courier New Bold":
                        return LoadFontData(uri, "courbd.ttf");
                    default:
                        return LoadFontData(uri, "cour.ttf");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Could not load font '{faceName}'.", ex);
            }
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            try
            {
                if (familyName.Equals("Courier New", StringComparison.OrdinalIgnoreCase))
                {
                    if (isBold)
                        return new FontResolverInfo("Courier New Bold");
                    else
                        return new FontResolverInfo("Courier New");
                }

                // Fallback
                return new FontResolverInfo("Courier New");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Could not resolve typeface for '{familyName}'.", ex);
            }
        }

        private byte[] LoadFontData(Uri uri, string fileName)
        {
            string fullPath = Path.Combine(uri.LocalPath, fileName);
            return File.ReadAllBytes(fullPath);
        }
    }
}