using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class FishermanHatAnimationBaker
{
    private const string SourceAnimationFolder = "Assets/Animations/Fisher Man Animations/UsedAnimations";
    private const string HatFolder = "Assets/UI/ShopUI";
    private const string RedHairHeadSheetPath = "Assets/UI/ShopUI/FishermansAnimations-Head_Sheet.png";
    private const string OutputRoot = "Assets/UI/Game UI/Fisherman/Generated Hat Animations";
    private const int SpritePixelsPerUnit = 25;
    private const int FrameSize = 64;
    private const int FramesPerClip = 4;

    private static readonly string[] HatAssetNames =
    {
        "FisherMan_Hat_-Default_-_Fishing_Hat.png",
        "FisherMan_Hat_-Blue_Cap.png",
        "FisherMan_Hat_-Chef_Hat.png",
        "FisherMan_Hat_-Fish_Hat.png",
        "FisherMan_Hat_-Ranger_Hat.png",
        "FisherMan_Hat_-Red_Cap.png",
        "FisherMan_Hat_-Soda_Hat.png"
    };

    [MenuItem("Tools/Fisherman/Generate Hat Animation Sprites")]
    public static void GenerateHatAnimationSprites()
    {
        string[] clipGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { SourceAnimationFolder });
        if (clipGuids.Length == 0)
        {
            Debug.LogError("No fisherman animation clips found at: " + SourceAnimationFolder);
            return;
        }

        List<string> hatPaths = GetHatPaths();
        if (hatPaths.Count == 0)
        {
            Debug.LogError("No fisherman hat PNGs found at: " + HatFolder);
            return;
        }

        Texture2D redHairHeadSheet = ReadableCopy(AssetDatabase.LoadAssetAtPath<Texture2D>(RedHairHeadSheetPath));
        if (redHairHeadSheet == null)
        {
            Debug.LogError("Failed to load red-hair fisherman head sheet at: " + RedHairHeadSheetPath);
            return;
        }

        EnsureFolder(OutputRoot);

        int generatedFrameCount = 0;
        int generatedClipCount = 0;

        foreach (string hatPath in hatPaths)
        {
            Texture2D hatTexture = ReadableCopy(AssetDatabase.LoadAssetAtPath<Texture2D>(hatPath));
            if (hatTexture == null)
            {
                Debug.LogWarning("Skipped unreadable hat texture: " + hatPath);
                continue;
            }

            string hatKey = Path.GetFileNameWithoutExtension(hatPath);
            string hatOutputFolder = CombineAssetPath(OutputRoot, hatKey);
            EnsureFolder(hatOutputFolder);
            List<List<string>> generatedFrameRows = new List<List<string>>();
            List<string> generatedRowNames = new List<string>();

            foreach (string clipGuid in clipGuids)
            {
                string clipPath = AssetDatabase.GUIDToAssetPath(clipGuid);
                AnimationClip sourceClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (sourceClip == null)
                {
                    continue;
                }

                List<KeyframeSprite> frames = GetSpriteFrames(sourceClip);
                if (frames.Count == 0)
                {
                    Debug.LogWarning("Skipped clip with no SpriteRenderer frames: " + clipPath);
                    continue;
                }

                string safeClipName = MakeFileNameSafe(sourceClip.name);
                string frameFolder = CombineAssetPath(hatOutputFolder, safeClipName);
                EnsureFolder(frameFolder);

                List<Sprite> generatedSprites = new List<Sprite>(frames.Count);
                List<string> generatedFramePaths = new List<string>(frames.Count);
                for (int i = 0; i < frames.Count; i++)
                {
                    Sprite sourceSprite = frames[i].Sprite;
                    Texture2D sourceTexture = ExtractSpriteTexture(sourceSprite);
                    if (sourceTexture == null)
                    {
                        continue;
                    }

                    // Build the complete fisherman with head + hat (no baked default head removal)
                    Texture2D fishermanWithHeadAndHat = BuildFishermanWithHeadAndHat(sourceTexture, redHairHeadSheet, sourceClip.name, i, hatTexture);
                    
                    string framePath = CombineAssetPath(frameFolder, safeClipName + "_" + (i + 1) + ".png");
                    File.WriteAllBytes(framePath, fishermanWithHeadAndHat.EncodeToPNG());
                    generatedFramePaths.Add(framePath);
                    
                    UnityEngine.Object.DestroyImmediate(sourceTexture);
                    UnityEngine.Object.DestroyImmediate(fishermanWithHeadAndHat);
                    generatedFrameCount++;
                }

                generatedFrameRows.Add(generatedFramePaths);
                generatedRowNames.Add(sourceClip.name);

                AssetDatabase.ImportAsset(frameFolder, ImportAssetOptions.ForceUpdate);
                ConfigureGeneratedSprites(frameFolder);
                AssetDatabase.ImportAsset(frameFolder, ImportAssetOptions.ForceUpdate);

                for (int i = 0; i < frames.Count; i++)
                {
                    string framePath = CombineAssetPath(frameFolder, safeClipName + "_" + (i + 1) + ".png");
                    Sprite generatedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(framePath);
                    if (generatedSprite != null)
                    {
                        generatedSprites.Add(generatedSprite);
                    }
                }

                if (generatedSprites.Count == frames.Count)
                {
                    CreateGeneratedAnimationClip(sourceClip, frames, generatedSprites, hatOutputFolder, safeClipName);
                    generatedClipCount++;
                }
            }

            CreateSpriteSheet(hatKey, hatOutputFolder, generatedFrameRows, generatedRowNames);
            UnityEngine.Object.DestroyImmediate(hatTexture);
        }

        UnityEngine.Object.DestroyImmediate(redHairHeadSheet);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Generated {generatedFrameCount} hat fisherman frames and {generatedClipCount} animation clips under {OutputRoot}.");
    }

    [MenuItem("Tools/Fisherman/Analyze Used Fisherman Animations")]
    public static void AnalyzeUsedAnimations()
    {
        string[] clipGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { SourceAnimationFolder });
        Array.Sort(clipGuids, StringComparer.Ordinal);

        foreach (string clipGuid in clipGuids)
        {
            string clipPath = AssetDatabase.GUIDToAssetPath(clipGuid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                continue;
            }

            List<KeyframeSprite> frames = GetSpriteFrames(clip);
            string[] spriteNames = new string[frames.Count];
            for (int i = 0; i < frames.Count; i++)
            {
                spriteNames[i] = frames[i].Sprite != null ? frames[i].Sprite.name : "null";
            }

            Debug.Log($"{clip.name}: {frames.Count} frames, {clip.frameRate:0.##} fps, loop={clip.isLooping}, sprites=[{string.Join(", ", spriteNames)}]");
        }
    }

    private static List<string> GetHatPaths()
    {
        List<string> paths = new List<string>();
        foreach (string fileName in HatAssetNames)
        {
            string path = CombineAssetPath(HatFolder, fileName);
            if (File.Exists(path))
            {
                paths.Add(path);
            }
        }

        return paths;
    }

    private static List<KeyframeSprite> GetSpriteFrames(AnimationClip clip)
    {
        List<KeyframeSprite> frames = new List<KeyframeSprite>();
        EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
        foreach (EditorCurveBinding binding in bindings)
        {
            if (binding.type != typeof(SpriteRenderer) || binding.propertyName != "m_Sprite")
            {
                continue;
            }

            ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            foreach (ObjectReferenceKeyframe keyframe in keyframes)
            {
                frames.Add(new KeyframeSprite(keyframe.time, keyframe.value as Sprite));
            }
        }

        frames.Sort((a, b) => a.Time.CompareTo(b.Time));
        return frames;
    }

    private static void CreateGeneratedAnimationClip(AnimationClip sourceClip, List<KeyframeSprite> sourceFrames, List<Sprite> sprites, string outputFolder, string safeClipName)
    {
        AnimationClip generatedClip = new AnimationClip
        {
            name = safeClipName,
            frameRate = sourceClip.frameRate,
            legacy = sourceClip.legacy
        };

        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = string.Empty,
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Count];
        for (int i = 0; i < sprites.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = sourceFrames[i].Time,
                value = sprites[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(generatedClip, binding, keyframes);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(sourceClip);
        AnimationUtility.SetAnimationClipSettings(generatedClip, settings);

        string clipPath = CombineAssetPath(outputFolder, safeClipName + ".anim");
        AssetDatabase.DeleteAsset(clipPath);
        AssetDatabase.CreateAsset(generatedClip, clipPath);
    }

    private static void CreateSpriteSheet(string hatKey, string outputFolder, List<List<string>> frameRows, List<string> rowNames)
    {
        if (frameRows.Count == 0)
        {
            return;
        }

        const int cellSize = FrameSize;
        const int columns = FramesPerClip;
        int rows = frameRows.Count;
        Texture2D sheet = new Texture2D(columns * cellSize, rows * cellSize, TextureFormat.RGBA32, false);
        Color[] clearPixels = new Color[sheet.width * sheet.height];
        for (int i = 0; i < clearPixels.Length; i++)
        {
            clearPixels[i] = Color.clear;
        }

        sheet.SetPixels(clearPixels);

        for (int row = 0; row < frameRows.Count; row++)
        {
            List<string> framePaths = frameRows[row];
            for (int column = 0; column < framePaths.Count && column < columns; column++)
            {
                Texture2D frame = LoadPng(framePaths[column]);
                if (frame == null)
                {
                    continue;
                }

                int targetX = column * cellSize;
                int targetY = (rows - row - 1) * cellSize;
                sheet.SetPixels(targetX, targetY, frame.width, frame.height, frame.GetPixels());
                UnityEngine.Object.DestroyImmediate(frame);
            }
        }

        sheet.Apply();
        string sheetPath = CombineAssetPath(outputFolder, hatKey + "_AllUsedAnimations_Sheet.png");
        File.WriteAllBytes(sheetPath, sheet.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(sheet);

        string manifestPath = CombineAssetPath(outputFolder, hatKey + "_AllUsedAnimations_Sheet.txt");
        File.WriteAllLines(manifestPath, rowNames);

        AssetDatabase.ImportAsset(sheetPath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(sheetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = SpritePixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritesheet = BuildSheetSpriteMetadata(rowNames, rows, columns, cellSize);
            importer.SaveAndReimport();
        }

        AssetDatabase.ImportAsset(manifestPath, ImportAssetOptions.ForceUpdate);
    }

    private static SpriteMetaData[] BuildSheetSpriteMetadata(List<string> rowNames, int rows, int columns, int cellSize)
    {
        List<SpriteMetaData> metadata = new List<SpriteMetaData>(rows * columns);
        for (int row = 0; row < rows; row++)
        {
            string rowName = row < rowNames.Count ? MakeFileNameSafe(rowNames[row]) : "Row" + (row + 1);
            for (int column = 0; column < columns; column++)
            {
                metadata.Add(new SpriteMetaData
                {
                    name = rowName + "_" + (column + 1),
                    rect = new Rect(column * cellSize, (rows - row - 1) * cellSize, cellSize, cellSize),
                    pivot = new Vector2(0.5f, 0.5f),
                    alignment = (int)SpriteAlignment.Center
                });
            }
        }

        return metadata.ToArray();
    }

    private static Texture2D ExtractSpriteTexture(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null)
        {
            return null;
        }

        Texture2D readableTexture = ReadableCopy(sprite.texture);
        Rect rect = sprite.textureRect;
        Texture2D cropped = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false);
        Color[] pixels = readableTexture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
        cropped.SetPixels(pixels);
        cropped.Apply();
        UnityEngine.Object.DestroyImmediate(readableTexture);
        return cropped;
    }

    /// <summary>
    /// NEW METHOD: Build complete fisherman with head + hat overlay
    /// This preserves the body and overlays the head and hat properly
    /// </summary>
    private static Texture2D BuildFishermanWithHeadAndHat(Texture2D sourceTexture, Texture2D redHairHeadSheet, string clipName, int frameIndex, Texture2D hatTexture)
    {
        Texture2D output = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false);
        output.SetPixels(sourceTexture.GetPixels());

        // Extract and overlay the head from the red-hair head sheet
        Texture2D redHairHead = ExtractHeadFrame(redHairHeadSheet, GetAnimatedFishermanHeadRow(clipName), frameIndex);
        OverlayTextureTopLeftInPlace(output, redHairHead, Vector2Int.zero);
        UnityEngine.Object.DestroyImmediate(redHairHead);

        // Overlay the hat on top of the head
        OverlayTextureTopLeftInPlace(output, hatTexture, GetHatOffset(clipName, frameIndex));

        output.Apply();
        return output;
    }

    private static Texture2D ExtractHeadFrame(Texture2D headSheet, int row, int frameIndex)
    {
        int column = Mathf.Clamp(frameIndex, 0, FramesPerClip - 1);
        int maxRows = Mathf.Max(1, headSheet.height / FrameSize);
        int clampedRow = Mathf.Clamp(row, 0, maxRows - 1);
        Texture2D head = new Texture2D(FrameSize, FrameSize, TextureFormat.RGBA32, false);

        for (int y = 0; y < FrameSize; y++)
        {
            for (int x = 0; x < FrameSize; x++)
            {
                head.SetPixel(x, FrameSize - y - 1, GetPixelTopLeft(headSheet, column * FrameSize + x, clampedRow * FrameSize + y));
            }
        }

        head.Apply();
        return head;
    }

    private static Texture2D OverlayTextureTopLeft(Texture2D baseTexture, Texture2D overlayTexture, Vector2Int offset)
    {
        Texture2D output = new Texture2D(baseTexture.width, baseTexture.height, TextureFormat.RGBA32, false);
        output.SetPixels(baseTexture.GetPixels());
        OverlayTextureTopLeftInPlace(output, overlayTexture, offset);
        output.Apply();
        return output;
    }

    private static void OverlayTextureTopLeftInPlace(Texture2D baseTexture, Texture2D overlayTexture, Vector2Int offset)
    {
        for (int y = 0; y < overlayTexture.height; y++)
        {
            int targetY = y + offset.y;
            if (targetY < 0 || targetY >= baseTexture.height)
            {
                continue;
            }

            for (int x = 0; x < overlayTexture.width; x++)
            {
                int targetX = x + offset.x;
                if (targetX < 0 || targetX >= baseTexture.width)
                {
                    continue;
                }

                Color overlay = GetPixelTopLeft(overlayTexture, x, y);
                if (overlay.a <= 0f)
                {
                    continue;
                }

                Color under = GetPixelTopLeft(baseTexture, targetX, targetY);
                SetPixelTopLeft(baseTexture, targetX, targetY, AlphaBlend(under, overlay));
            }
        }
    }

    private static Color AlphaBlend(Color under, Color over)
    {
        float alpha = over.a + under.a * (1f - over.a);
        if (alpha <= 0f)
        {
            return Color.clear;
        }

        return new Color(
            (over.r * over.a + under.r * under.a * (1f - over.a)) / alpha,
            (over.g * over.a + under.g * under.a * (1f - over.a)) / alpha,
            (over.b * over.a + under.b * under.a * (1f - over.a)) / alpha,
            alpha);
    }

    private static Vector2Int GetHatOffset(string clipName, int frameIndex)
    {
        string state = NormalizeName(clipName);
        int x = 0;
        int y = -16;

        if (state.Contains("left"))
        {
            x -= 1;
        }
        else if (state.Contains("right"))
        {
            x += 1;
        }

        if (state.Contains("move"))
        {
            y += 0;
        }
        else if (state.Contains("cast") || state.Contains("fish") || state.Contains("reel") || state.Contains("fight"))
        {
            y -= 1;
        }

        if (frameIndex == 1 || frameIndex == 2)
        {
            y += 0;
        }

        return new Vector2Int(x, y);
    }

    private static int GetAnimatedFishermanHeadRow(string clipName)
    {
        string state = NormalizeName(clipName);
        switch (state)
        {
            case "castingleft": return 0;
            case "castingright": return 1;
            case "cryleft": return 2;
            case "cryright": return 3;
            case "fightingleft": return 4;
            case "fightingright": return 5;
            case "fishgotofffacingleft": return 6;
            case "fishgotofffacingright": return 7;
            case "fishingleft": return 8;
            case "fishingright": return 9;
            case "idelleft":
            case "idleleft": return 10;
            case "idelright":
            case "idleright": return 11;
            case "leftpoletooar":
            case "lefttorightpole": return 12;
            case "movebackwards": return 13;
            case "moveforward": return 14;
            case "movereversebackwards": return 15;
            case "movereverseforward": return 16;
            case "oartoleftpole": return 17;
            case "oartorightpole":
            case "ourtorightpole":
            case "rightpoletooar": return 18;
            case "reelingleft": return 19;
            case "reelingright": return 20;
            case "righttoleftpole": return 21;
            case "winningleft": return 22;
            case "winningright": return 23;
            default: return 10;
        }
    }

    private static Color GetPixelTopLeft(Texture2D texture, int x, int y)
    {
        return texture.GetPixel(x, texture.height - y - 1);
    }

    private static void SetPixelTopLeft(Texture2D texture, int x, int y, Color color)
    {
        texture.SetPixel(x, texture.height - y - 1, color);
    }

    private static Texture2D ReadableCopy(Texture2D texture)
    {
        if (texture == null)
        {
            return null;
        }

        RenderTexture renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        Graphics.Blit(texture, renderTexture);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;
        Texture2D readable = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
        readable.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTexture);
        return readable;
    }

    private static Texture2D LoadPng(string assetPath)
    {
        if (!File.Exists(assetPath))
        {
            return null;
        }

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(File.ReadAllBytes(assetPath)))
        {
            UnityEngine.Object.DestroyImmediate(texture);
            return null;
        }

        return texture;
    }

    private static void ConfigureGeneratedSprites(string folder)
    {
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        foreach (string textureGuid in textureGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(textureGuid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = SpritePixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }

    private static void EnsureFolder(string assetPath)
    {
        string[] parts = assetPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static string CombineAssetPath(string left, string right)
    {
        return left.TrimEnd('/') + "/" + right.TrimStart('/');
    }

    private static string MakeFileNameSafe(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalid, '_');
        }

        return value.Replace(" ", string.Empty);
    }

    private static string NormalizeName(string value)
    {
        return string.IsNullOrEmpty(value)
            ? string.Empty
            : value.ToLowerInvariant().Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty);
    }

    private readonly struct KeyframeSprite
    {
        public KeyframeSprite(float time, Sprite sprite)
        {
            Time = time;
            Sprite = sprite;
        }

        public float Time { get; }
        public Sprite Sprite { get; }
    }
}