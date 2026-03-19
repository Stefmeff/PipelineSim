using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Editor utility that imports Windows .cur files from C:\Windows\Cursors\
/// and saves them as transparent PNGs in Resources/Art/Cursors/.
/// Run via menu: Tools > Import Windows Cursors
/// </summary>
public class CursorImporter
{
    // Mapping from our cursor name to the Windows .cur filename (without extension)
    static readonly Dictionary<string, string> CursorMap = new Dictionary<string, string>
    {
        { "arrow",       "aero_arrow" },
        { "move",        "aero_move" },
        { "help",        "aero_helpsel" },
        { "pen",         "aero_pen" },
        { "person",      "aero_person" },
        { "pin",         "aero_pin" },
        { "unavailable", "aero_unavail" },
        { "resize_ew",   "aero_ew" },
        { "resize_ns",   "aero_ns" },
        { "resize_nesw", "aero_nesw" },
        { "resize_nwse", "aero_nwse" },
        // crosshair and hand have no direct .cur match in aero set
    };

    [MenuItem("Tools/Import Windows Cursors")]
    static void ImportCursors()
    {
        string windowsCursorsPath = @"C:\Windows\Cursors";
        string outputPath = "Assets/Resources/Art/Cursors";

        int imported = 0;
        int failed = 0;

        foreach (var pair in CursorMap)
        {
            string curFile = Path.Combine(windowsCursorsPath, pair.Value + ".cur");
            if (!File.Exists(curFile))
            {
                Debug.LogWarning($"Cursor file not found: {curFile}");
                failed++;
                continue;
            }

            try
            {
                var (texture, hotspot) = ParseCurFile(curFile);
                if (texture == null)
                {
                    Debug.LogWarning($"Failed to parse: {curFile}");
                    failed++;
                    continue;
                }

                byte[] png = texture.EncodeToPNG();
                string outFile = Path.Combine(outputPath, pair.Key + ".png");
                File.WriteAllBytes(outFile, png);
                Debug.Log($"Imported cursor: {pair.Key} ({texture.width}x{texture.height}, hotspot: {hotspot})");
                imported++;

                UnityEngine.Object.DestroyImmediate(texture);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error importing {pair.Key}: {e.Message}");
                failed++;
            }
        }

        AssetDatabase.Refresh();

        // Set correct import settings for all cursor textures
        foreach (var pair in CursorMap)
        {
            string assetPath = $"Assets/Resources/Art/Cursors/{pair.Key}.png";
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Cursor;
                importer.alphaIsTransparency = true;
                importer.isReadable = true;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        Debug.Log($"Cursor import complete. Imported: {imported}, Failed: {failed}");
    }

    static (Texture2D texture, Vector2 hotspot) ParseCurFile(string path)
    {
        byte[] data = File.ReadAllBytes(path);

        // .cur header: 2 bytes reserved, 2 bytes type (2=cursor), 2 bytes image count
        if (data.Length < 6) return (null, Vector2.zero);

        ushort imageCount = BitConverter.ToUInt16(data, 4);
        if (imageCount == 0) return (null, Vector2.zero);

        // Pick the largest image (first entry is usually fine)
        int bestIndex = 0;
        int bestSize = 0;
        for (int i = 0; i < imageCount; i++)
        {
            int entryOffset = 6 + i * 16;
            int w = data[entryOffset] == 0 ? 256 : data[entryOffset];
            int h = data[entryOffset + 1] == 0 ? 256 : data[entryOffset + 1];
            if (w * h > bestSize)
            {
                bestSize = w * h;
                bestIndex = i;
            }
        }

        int dirOffset = 6 + bestIndex * 16;
        int width = data[dirOffset] == 0 ? 256 : data[dirOffset];
        int height = data[dirOffset + 1] == 0 ? 256 : data[dirOffset + 1];
        ushort hotX = BitConverter.ToUInt16(data, dirOffset + 4);
        ushort hotY = BitConverter.ToUInt16(data, dirOffset + 6);
        uint dataSize = BitConverter.ToUInt32(data, dirOffset + 8);
        uint dataOffset = BitConverter.ToUInt32(data, dirOffset + 12);

        // Check if the image data is PNG (starts with PNG signature)
        if (dataOffset + 8 <= data.Length &&
            data[dataOffset] == 0x89 && data[dataOffset + 1] == 0x50 &&
            data[dataOffset + 2] == 0x4E && data[dataOffset + 3] == 0x47)
        {
            // Embedded PNG - extract and load directly
            byte[] pngData = new byte[dataSize];
            Array.Copy(data, dataOffset, pngData, 0, dataSize);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(pngData);
            return (tex, new Vector2(hotX, hotY));
        }

        // BMP/DIB format
        // Read BITMAPINFOHEADER
        uint biSize = BitConverter.ToUInt32(data, (int)dataOffset);
        int biWidth = BitConverter.ToInt32(data, (int)dataOffset + 4);
        int biHeight = BitConverter.ToInt32(data, (int)dataOffset + 8);
        ushort biBitCount = BitConverter.ToUInt16(data, (int)dataOffset + 14);

        // biHeight in cursor DIBs is 2x the actual height (includes mask)
        int actualHeight = biHeight / 2;
        if (actualHeight <= 0) actualHeight = height;

        int pixelDataOffset = (int)dataOffset + (int)biSize;

        Texture2D texture = new Texture2D(biWidth, actualHeight, TextureFormat.RGBA32, false);

        if (biBitCount == 32)
        {
            // 32-bit BGRA
            for (int y = 0; y < actualHeight; y++)
            {
                for (int x = 0; x < biWidth; x++)
                {
                    int idx = pixelDataOffset + (y * biWidth + x) * 4;
                    if (idx + 3 >= data.Length) continue;
                    byte b = data[idx];
                    byte g = data[idx + 1];
                    byte r = data[idx + 2];
                    byte a = data[idx + 3];
                    texture.SetPixel(x, y, new Color32(r, g, b, a));
                }
            }
        }
        else if (biBitCount == 24)
        {
            // 24-bit BGR, use AND mask for transparency
            int rowSize = ((biWidth * 3 + 3) / 4) * 4; // rows are padded to 4 bytes
            int maskOffset = pixelDataOffset + rowSize * actualHeight;
            int maskRowSize = ((biWidth + 31) / 32) * 4;

            for (int y = 0; y < actualHeight; y++)
            {
                for (int x = 0; x < biWidth; x++)
                {
                    int idx = pixelDataOffset + y * rowSize + x * 3;
                    if (idx + 2 >= data.Length) continue;
                    byte blue = data[idx];
                    byte green = data[idx + 1];
                    byte red = data[idx + 2];

                    // Check AND mask
                    byte alpha = 255;
                    int maskIdx = maskOffset + y * maskRowSize + x / 8;
                    if (maskIdx < data.Length)
                    {
                        int bit = 7 - (x % 8);
                        if ((data[maskIdx] & (1 << bit)) != 0)
                            alpha = 0;
                    }

                    texture.SetPixel(x, y, new Color32(red, green, blue, alpha));
                }
            }
        }
        else if (biBitCount == 8 || biBitCount == 4 || biBitCount == 1)
        {
            // Paletted - read color table
            int paletteCount = 1 << biBitCount;
            Color32[] palette = new Color32[paletteCount];
            int paletteOffset = (int)dataOffset + (int)biSize;
            for (int i = 0; i < paletteCount; i++)
            {
                int po = paletteOffset + i * 4;
                palette[i] = new Color32(data[po + 2], data[po + 1], data[po], 255);
            }

            pixelDataOffset = paletteOffset + paletteCount * 4;
            int maskOffset;

            if (biBitCount == 8)
            {
                int rowSize = ((biWidth + 3) / 4) * 4;
                maskOffset = pixelDataOffset + rowSize * actualHeight;
                int maskRowSize = ((biWidth + 31) / 32) * 4;

                for (int y = 0; y < actualHeight; y++)
                    for (int x = 0; x < biWidth; x++)
                    {
                        int idx = pixelDataOffset + y * rowSize + x;
                        Color32 c = palette[data[idx]];
                        int mi = maskOffset + y * maskRowSize + x / 8;
                        if (mi < data.Length && (data[mi] & (1 << (7 - x % 8))) != 0)
                            c.a = 0;
                        texture.SetPixel(x, y, c);
                    }
            }
            else if (biBitCount == 4)
            {
                int rowSize = ((biWidth * 4 + 31) / 32) * 4;
                maskOffset = pixelDataOffset + rowSize * actualHeight;
                int maskRowSize = ((biWidth + 31) / 32) * 4;

                for (int y = 0; y < actualHeight; y++)
                    for (int x = 0; x < biWidth; x++)
                    {
                        int byteIdx = pixelDataOffset + y * rowSize + x / 2;
                        int nibble = (x % 2 == 0) ? (data[byteIdx] >> 4) : (data[byteIdx] & 0xF);
                        Color32 c = palette[nibble];
                        int mi = maskOffset + y * maskRowSize + x / 8;
                        if (mi < data.Length && (data[mi] & (1 << (7 - x % 8))) != 0)
                            c.a = 0;
                        texture.SetPixel(x, y, c);
                    }
            }
            else // 1-bit
            {
                int rowSize = ((biWidth + 31) / 32) * 4;
                maskOffset = pixelDataOffset + rowSize * actualHeight;
                int maskRowSize = ((biWidth + 31) / 32) * 4;

                for (int y = 0; y < actualHeight; y++)
                    for (int x = 0; x < biWidth; x++)
                    {
                        int byteIdx = pixelDataOffset + y * rowSize + x / 8;
                        int bit = (data[byteIdx] >> (7 - x % 8)) & 1;
                        Color32 c = palette[bit];
                        int mi = maskOffset + y * maskRowSize + x / 8;
                        if (mi < data.Length && (data[mi] & (1 << (7 - x % 8))) != 0)
                            c.a = 0;
                        texture.SetPixel(x, y, c);
                    }
            }
        }

        texture.Apply();
        return (texture, new Vector2(hotX, hotY));
    }
}
