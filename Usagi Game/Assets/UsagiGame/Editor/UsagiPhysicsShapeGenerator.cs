using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace UsagiGame.Editor
{
    public static class UsagiPhysicsShapeGenerator
    {
        private const string FruitFolder = "Assets/UsagiGame/Art/Fruits";
        private const float MaxOutlineDetail = 0.2f;
        private const float OutlineDetailStep = 0.05f;
        private const byte AlphaTolerance = 200;
        private const int MaxVertices = 24;
        private static readonly MethodInfo GenerateOutlineMethod =
            typeof(UnityEditor.Sprites.SpriteUtility).GetMethod(
                "GenerateOutline",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[]
                {
                    typeof(Texture2D), typeof(Rect), typeof(float), typeof(byte), typeof(bool),
                    typeof(Vector2[][]).MakeByRefType()
                },
                null);

        [MenuItem("Tools/Usagi Game/Generate Fruit Physics Shapes")]
        public static void GenerateAll()
        {
            string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { FruitFolder });
            int generatedCount = 0;
            var factories = new SpriteDataProviderFactories();
            factories.Init();

            for (int i = 0; i < textureGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(textureGuids[i]);
                if (Generate(path, factories)) generatedCount++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Usagi Physics Shape {generatedCount}개를 사전 생성했습니다.");
        }

        private static bool Generate(string path, SpriteDataProviderFactories factories)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer ||
                importer.textureType != TextureImporterType.Sprite)
                return false;

            ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
            if (dataProvider == null) return false;
            dataProvider.InitSpriteEditorDataProvider();

            ITextureDataProvider textureProvider = dataProvider.GetDataProvider<ITextureDataProvider>();
            ISpritePhysicsOutlineDataProvider physicsProvider =
                dataProvider.GetDataProvider<ISpritePhysicsOutlineDataProvider>();
            if (textureProvider == null || physicsProvider == null) return false;
            Texture2D texture = textureProvider.GetReadableTexture2D();
            if (texture == null) return false;

            textureProvider.GetTextureActualWidthAndHeight(out int actualWidth, out int actualHeight);
            if (actualWidth <= 0 || actualHeight <= 0) return false;
            Vector2 importScale = new(texture.width / (float)actualWidth, texture.height / (float)actualHeight);
            SpriteRect[] spriteRects = dataProvider.GetSpriteRects();
            if (spriteRects.Length == 0) return false;

            for (int i = 0; i < spriteRects.Length; i++)
            {
                SpriteRect spriteRect = spriteRects[i];
                Rect scaledRect = spriteRect.rect;
                scaledRect.xMin *= importScale.x;
                scaledRect.xMax *= importScale.x;
                scaledRect.yMin *= importScale.y;
                scaledRect.yMax *= importScale.y;

                if (!TryGeneratePath(texture, scaledRect, out Vector2[] largestPath, out float detail))
                {
                    Debug.LogError($"{path}: 유효한 Physics Shape를 만들지 못했습니다.");
                    return false;
                }

                for (int point = 0; point < largestPath.Length; point++)
                    largestPath[point] = new Vector2(
                        largestPath[point].x / importScale.x,
                        largestPath[point].y / importScale.y);

                physicsProvider.SetOutlines(spriteRect.spriteID, new List<Vector2[]> { largestPath });
                physicsProvider.SetTessellationDetail(spriteRect.spriteID, detail);
            }

            dataProvider.Apply();
            importer.SaveAndReimport();

            Sprite generatedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            int pointCount = generatedSprite != null && generatedSprite.GetPhysicsShapeCount() > 0
                ? generatedSprite.GetPhysicsShapePointCount(0)
                : 0;
            if (pointCount < 3)
            {
                Debug.LogError($"{path}: 생성된 Physics Shape가 유효하지 않습니다.");
                return false;
            }

            Debug.Log($"{path}: Physics Shape {pointCount} vertices");
            return true;
        }

        private static bool TryGeneratePath(
            Texture2D texture, Rect rect, out Vector2[] result, out float usedDetail)
        {
            for (float detail = MaxOutlineDetail; detail >= 0f; detail -= OutlineDetailStep)
            {
                if (!GenerateOutline(texture, rect, detail, out Vector2[][] paths)) break;
                Vector2[] largestPath = FindLargestPath(paths);
                if (largestPath == null) continue;
                Vector2[] convexHull = CreateConvexHull(largestPath);
                if (convexHull == null) continue;

                result = LimitConvexVertices(convexHull, MaxVertices);
                usedDetail = detail;
                return true;
            }

            result = null;
            usedDetail = 0f;
            return false;
        }

        private static bool GenerateOutline(
            Texture2D texture, Rect rect, float detail, out Vector2[][] paths)
        {
            if (GenerateOutlineMethod == null)
            {
                paths = null;
                return false;
            }

            object[] arguments = { texture, rect, detail, AlphaTolerance, false, null };
            GenerateOutlineMethod.Invoke(null, arguments);
            paths = arguments[5] as Vector2[][];
            return paths != null;
        }

        private static Vector2[] CreateConvexHull(Vector2[] points)
        {
            var sorted = new List<Vector2>(points);
            sorted.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            if (sorted.Count < 3) return null;

            var hull = new List<Vector2>(sorted.Count * 2);
            for (int i = 0; i < sorted.Count; i++)
            {
                while (hull.Count >= 2 && Cross(hull[^2], hull[^1], sorted[i]) <= 0f)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(sorted[i]);
            }

            int lowerCount = hull.Count;
            for (int i = sorted.Count - 2; i >= 0; i--)
            {
                while (hull.Count > lowerCount && Cross(hull[^2], hull[^1], sorted[i]) <= 0f)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(sorted[i]);
            }

            hull.RemoveAt(hull.Count - 1);
            return hull.Count >= 3 ? hull.ToArray() : null;
        }

        private static Vector2[] LimitConvexVertices(Vector2[] path, int maxVertices)
        {
            if (path.Length <= maxVertices) return path;

            var limited = new Vector2[maxVertices];
            for (int i = 0; i < maxVertices; i++)
                limited[i] = path[i * path.Length / maxVertices];
            return limited;
        }

        private static float Cross(Vector2 origin, Vector2 a, Vector2 b) =>
            (a.x - origin.x) * (b.y - origin.y) - (a.y - origin.y) * (b.x - origin.x);

        private static Vector2[] FindLargestPath(Vector2[][] paths)
        {
            Vector2[] largest = null;
            float largestArea = 0f;

            for (int i = 0; i < paths.Length; i++)
            {
                Vector2[] path = paths[i];
                if (path == null || path.Length < 3) continue;

                float area = 0f;
                for (int point = 0; point < path.Length; point++)
                {
                    Vector2 current = path[point];
                    Vector2 next = path[(point + 1) % path.Length];
                    area += current.x * next.y - next.x * current.y;
                }

                area = Mathf.Abs(area);
                if (area <= largestArea) continue;
                largestArea = area;
                largest = path;
            }

            return largest;
        }

    }
}
