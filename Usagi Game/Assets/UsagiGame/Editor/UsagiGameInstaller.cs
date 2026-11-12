using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace UsagiGame.Editor
{
    public static class UsagiGameInstaller
    {
        private const string Root = "Assets/UsagiGame";
        private const string DataPath = Root + "/Data";
        private const string PrefabPath = Root + "/Prefabs";
        private const string ScenePath = Root + "/Scenes";
        private static readonly float[] Radii = { .34f, .415f, .51f, .605f, .70f, .835f, .98f, 1.145f, 1.34f, 1.575f, 1.86f };
        private static readonly int[] Scores = { 1, 3, 6, 10, 15, 21, 28, 36, 45, 55, 66 };
        private static readonly string[] Names = { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11" };
        private static readonly Color Ink = new(.16f, .11f, .07f, 1f);
        private static readonly Color Cream = new(1f, .93f, .72f, .9f);
        private static readonly Color Pink = new(.95f, .62f, .68f, 1f);
        private static Sprite whiteSprite;

        [MenuItem("Tools/Usagi Game/Rebuild TitleScene")]
        public static void RebuildTitleScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolders();
            AssetDatabase.Refresh();
            ConfigureUiTextures();
            CreateTitleScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("TitleScene 재구성이 완료되었습니다. StartChoicePanel 연결을 확인하세요.");
        }

        private static void EnsureFolders()
        {
            EnsureFolder(DataPath);
            EnsureFolder(PrefabPath);
            EnsureFolder(ScenePath);
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void ConfigureFruitTextures()
        {
            for (int i = 1; i <= 11; i++)
            {
                string path = $"{Root}/Art/Fruits/usagi_{i:00}.png";
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 1024;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }
        }

        private static void ConfigureUiTextures()
        {
            const string path = Root + "/Art/UI/usagi_background.png";
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static PhysicsMaterial2D CreatePhysicsMaterial()
        {
            string path = DataPath + "/FruitPhysics.physicsMaterial2D";
            PhysicsMaterial2D material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
            if (material == null)
            {
                material = new PhysicsMaterial2D("Fruit Physics");
                AssetDatabase.CreateAsset(material, path);
            }
            material.friction = .3f;
            material.bounciness = .05f;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static FruitDefinition[] CreateFruitDefinitions()
        {
            var result = new FruitDefinition[11];
            for (int i = 0; i < result.Length; i++)
            {
                string path = $"{DataPath}/Fruit_{i + 1:00}.asset";
                FruitDefinition definition = AssetDatabase.LoadAssetAtPath<FruitDefinition>(path);
                if (definition == null)
                {
                    definition = ScriptableObject.CreateInstance<FruitDefinition>();
                    AssetDatabase.CreateAsset(definition, path);
                }
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Art/Fruits/usagi_{i + 1:00}.png");
                SerializedObject so = new(definition);
                so.FindProperty("<Stage>k__BackingField").intValue = i + 1;
                so.FindProperty("<DisplayName>k__BackingField").stringValue = Names[i];
                so.FindProperty("<Sprite>k__BackingField").objectReferenceValue = sprite;
                so.FindProperty("<Radius>k__BackingField").floatValue = Radii[i];
                so.FindProperty("<Mass>k__BackingField").floatValue = Mathf.Max(.1f, Radii[i] * Radii[i]);
                so.FindProperty("<MergeScore>k__BackingField").intValue = Scores[i];
                so.FindProperty("<DropWeight>k__BackingField").intValue = i < 5 ? 20 : 0;
                so.ApplyModifiedPropertiesWithoutUndo();
                result[i] = definition;
            }
            return result;
        }

        private static FruitDefinition[] LoadFruitDefinitions()
        {
            var result = new FruitDefinition[11];
            for (int i = 0; i < result.Length; i++)
                result[i] = AssetDatabase.LoadAssetAtPath<FruitDefinition>($"{DataPath}/Fruit_{i + 1:00}.asset");
            return result;
        }

        private static GameRuleConfig CreateRules(FruitDefinition[] fruits)
        {
            string path = DataPath + "/GameRules.asset";
            GameRuleConfig rules = AssetDatabase.LoadAssetAtPath<GameRuleConfig>(path);
            if (rules == null)
            {
                rules = ScriptableObject.CreateInstance<GameRuleConfig>();
                AssetDatabase.CreateAsset(rules, path);
            }
            SerializedObject so = new(rules);
            SerializedProperty array = so.FindProperty("<Fruits>k__BackingField");
            array.arraySize = fruits.Length;
            for (int i = 0; i < fruits.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = fruits[i];
            so.FindProperty("<BoardWidth>k__BackingField").floatValue = 7.4f;
            so.FindProperty("<BoardBottomY>k__BackingField").floatValue = .75f;
            so.FindProperty("<DangerHeight>k__BackingField").floatValue = 9.3f;
            so.FindProperty("<AimOffset>k__BackingField").floatValue = 1.6f;
            so.FindProperty("<DropInterval>k__BackingField").floatValue = .45f;
            so.FindProperty("<OverflowGrace>k__BackingField").floatValue = 1f;
            so.FindProperty("<MergeVelocityLimit>k__BackingField").floatValue = 8f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return rules;
        }

        private static FruitActor CreateFruitPrefab(PhysicsMaterial2D material)
        {
            string path = PrefabPath + "/Fruit.prefab";
            GameObject root = new("Fruit");
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 1f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
            PolygonCollider2D spriteCollider = root.AddComponent<PolygonCollider2D>();
            spriteCollider.sharedMaterial = material;
            GameObject visual = new("Visual");
            visual.transform.SetParent(root.transform, false);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 5;
            FruitActor actor = root.AddComponent<FruitActor>();
            SetObject(actor, "body", body);
            SetObject(actor, "spriteCollider", spriteCollider);
            SetObject(actor, "spriteRenderer", renderer);
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return saved.GetComponent<FruitActor>();
        }

        private static void CreateTitleScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Camera titleCamera = CreateCamera(new Vector3(0, 0, -10), 9.6f, new Color(.95f, .69f, .18f));
            CreateBackground(titleCamera);
            Canvas canvas = CreateCanvas();
            RectTransform safe = CreateSafeRoot(canvas.transform);
            TMP_Text title = CreateText(safe, "USAGI\nMERGE", 82, new Vector2(.1f, .63f), new Vector2(.9f, .88f), TextAlignmentOptions.Center);
            title.color = Ink;
            TMP_Text best = CreateText(safe, "BEST  0", 42, new Vector2(.1f, .49f), new Vector2(.9f, .59f), TextAlignmentOptions.Center);
            Button start = CreateButton(safe, "START", new Vector2(.24f, .32f), new Vector2(.76f, .43f));
            Button settings = CreateButton(safe, "SETTINGS", new Vector2(.3f, .22f), new Vector2(.7f, .29f));
            CreateText(safe, "좌우로 움직이고 손을 떼면 떨어져요", 25, new Vector2(.08f, .08f), new Vector2(.92f, .15f), TextAlignmentOptions.Center);

            GameObject settingsPanel = CreatePanel(safe, "SettingsPanel", new Color(1f, .93f, .72f, .97f));
            CreateText(settingsPanel.transform, "SETTINGS", 48, new Vector2(.1f, .72f), new Vector2(.9f, .85f), TextAlignmentOptions.Center);
            Toggle bgm = CreateToggle(settingsPanel.transform, "BGM", .61f);
            Toggle sfx = CreateToggle(settingsPanel.transform, "SFX", .51f);
            Toggle vibration = CreateToggle(settingsPanel.transform, "VIBRATION", .41f);
            Button close = CreateButton(settingsPanel.transform, "SAVE & CLOSE", new Vector2(.25f, .2f), new Vector2(.75f, .3f));

            GameObject startChoicePanel = CreatePanel(safe, "StartChoicePanel", new Color(1f, .93f, .72f, .97f));
            CreateText(startChoicePanel.transform, "CONTINUE?", 48, new Vector2(.1f, .68f), new Vector2(.9f, .82f), TextAlignmentOptions.Center);
            Button continueGame = CreateButton(startChoicePanel.transform, "CONTINUE", new Vector2(.24f, .49f), new Vector2(.76f, .59f));
            Button newGame = CreateButton(startChoicePanel.transform, "NEW GAME", new Vector2(.24f, .37f), new Vector2(.76f, .47f));
            Button cancelStart = CreateButton(startChoicePanel.transform, "CANCEL", new Vector2(.3f, .25f), new Vector2(.7f, .33f));

            GameObject controllerObject = new("TitleController");
            TitleController controller = controllerObject.AddComponent<TitleController>();
            SetObject(controller, "highScoreText", best);
            SetObject(controller, "settingsPanel", settingsPanel);
            SetObject(controller, "bgmToggle", bgm);
            SetObject(controller, "sfxToggle", sfx);
            SetObject(controller, "vibrationToggle", vibration);
            SetObject(controller, "startChoicePanel", startChoicePanel);
            UnityEventTools.AddPersistentListener(start.onClick, controller.StartGame);
            UnityEventTools.AddPersistentListener(settings.onClick, controller.OpenSettings);
            UnityEventTools.AddPersistentListener(close.onClick, controller.SaveSettings);
            UnityEventTools.AddPersistentListener(continueGame.onClick, controller.ContinueGame);
            UnityEventTools.AddPersistentListener(newGame.onClick, controller.StartNewGame);
            UnityEventTools.AddPersistentListener(cancelStart.onClick, controller.CloseStartChoice);
            settingsPanel.SetActive(false); startChoicePanel.SetActive(false);
            EditorSceneManager.SaveScene(scene, ScenePath + "/TitleScene.unity");
        }

        private static void CreateGameScene(GameRuleConfig rules, FruitActor prefab, PhysicsMaterial2D material, FruitDefinition[] fruits)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            float boardCenterY = rules.BoardBottomY + (rules.DangerHeight + rules.AimOffset) * .5f;
            Camera camera = CreateCamera(new Vector3(0, boardCenterY, -10), 7.2f, new Color(.95f, .69f, .18f));
            camera.gameObject.AddComponent<ResponsiveCamera>();
            CameraBackground background = CreateBackground(camera);
            Transform world = new GameObject("World").transform;
            float wallX = rules.HalfBoardWidth + .25f;
            float wallCenterY = rules.BoardBottomY + rules.DangerHeight * .5f;
            SpriteRenderer floor = CreateBoardPart(world, "Floor", new Vector2(0, rules.BoardBottomY - .25f), new Vector2(rules.BoardWidth + 1f, .5f), material);
            SpriteRenderer leftWall = CreateBoardPart(world, "LeftWall", new Vector2(-wallX, wallCenterY), new Vector2(.5f, rules.DangerHeight + .5f), material);
            SpriteRenderer rightWall = CreateBoardPart(world, "RightWall", new Vector2(wallX, wallCenterY), new Vector2(.5f, rules.DangerHeight + .5f), material);
            SpriteRenderer danger = CreateVisual(world, "DangerLine", new Vector2(0, rules.DangerLineY), new Vector2(rules.BoardWidth, .06f), new Color(1f, .3f, .2f, .25f));
            SpriteRenderer aimLine = CreateVisual(world, "AimLine", new Vector2(0, rules.DangerLineY + rules.AimOffset * .5f), new Vector2(.04f, rules.AimOffset), new Color(1f, 1f, 1f, .5f));
            SpriteRenderer preview = new GameObject("Preview").AddComponent<SpriteRenderer>();
            preview.transform.SetParent(world, false);
            preview.sortingOrder = 10;
            Transform fruitRoot = new GameObject("Fruits").transform;
            fruitRoot.SetParent(world, false);

            GameObject systems = new("GameSystems");
            BoardLayout boardLayout = systems.AddComponent<BoardLayout>();
            FruitSpawner spawner = systems.AddComponent<FruitSpawner>();
            MergeResolver resolver = systems.AddComponent<MergeResolver>();
            OverflowDetector overflow = systems.AddComponent<OverflowDetector>();
            DropController drop = systems.AddComponent<DropController>();
            GameSession session = systems.AddComponent<GameSession>();
            SetObject(boardLayout, "worldCamera", camera); SetObject(boardLayout, "floor", floor);
            SetObject(boardLayout, "cameraBackground", background);
            SetObject(boardLayout, "leftWall", leftWall); SetObject(boardLayout, "rightWall", rightWall);
            SetObject(boardLayout, "dangerLine", danger); SetObject(boardLayout, "aimLine", aimLine);
            SetObject(spawner, "rules", rules); SetObject(spawner, "fruitPrefab", prefab); SetObject(spawner, "fruitRoot", fruitRoot); SetInt(spawner, "fruitLayer", -1); SetObject(spawner, "boardLayout", boardLayout);
            SetObject(resolver, "rules", rules); SetObject(resolver, "spawner", spawner);
            SetObject(overflow, "rules", rules); SetObject(overflow, "spawner", spawner); SetObject(overflow, "dangerLine", danger); SetObject(overflow, "boardLayout", boardLayout);
            SetObject(drop, "rules", rules); SetObject(drop, "worldCamera", camera); SetObject(drop, "spawner", spawner); SetObject(drop, "mergeResolver", resolver); SetObject(drop, "preview", preview); SetObject(drop, "aimLine", aimLine); SetObject(drop, "boardLayout", boardLayout);

            Canvas canvas = CreateCanvas();
            RectTransform safe = CreateSafeRoot(canvas.transform);
            CreateCard(safe, "ScoreCard", new Vector2(.025f, .865f), new Vector2(.35f, .985f));
            TMP_Text score = CreateText(safe, "SCORE\n0", 34, new Vector2(.055f, .905f), new Vector2(.33f, .98f), TextAlignmentOptions.Left);
            TMP_Text best = CreateText(safe, "BEST 0", 23, new Vector2(.055f, .87f), new Vector2(.33f, .915f), TextAlignmentOptions.Left);
            CreateCard(safe, "NextCard", new Vector2(.65f, .865f), new Vector2(.825f, .985f));
            CreateText(safe, "NEXT", 23, new Vector2(.66f, .94f), new Vector2(.815f, .98f), TextAlignmentOptions.Center);
            Image next = CreateImage(safe, "NextImage", new Vector2(.68f, .875f), new Vector2(.795f, .945f), Color.white);
            next.preserveAspect = true;
            Button pause = CreateButton(safe, "II", new Vector2(.84f, .895f), new Vector2(.96f, .97f));
            CreateEvolutionBar(safe, fruits);

            GameObject pausePanel = CreatePanel(safe, "PausePanel", new Color(1f, .93f, .72f, .97f));
            CreateText(pausePanel.transform, "PAUSED", 54, new Vector2(.1f, .65f), new Vector2(.9f, .78f), TextAlignmentOptions.Center);
            Button resume = CreateButton(pausePanel.transform, "CONTINUE", new Vector2(.24f, .49f), new Vector2(.76f, .59f));
            Button restartPause = CreateButton(pausePanel.transform, "RESTART", new Vector2(.24f, .37f), new Vector2(.76f, .47f));
            Button titlePause = CreateButton(pausePanel.transform, "TITLE", new Vector2(.24f, .25f), new Vector2(.76f, .35f));

            GameObject resultPanel = CreatePanel(safe, "ResultPanel", new Color(1f, .93f, .72f, .98f));
            CreateText(resultPanel.transform, "GAME OVER", 54, new Vector2(.1f, .7f), new Vector2(.9f, .82f), TextAlignmentOptions.Center);
            TMP_Text resultScore = CreateText(resultPanel.transform, "SCORE 0", 42, new Vector2(.1f, .57f), new Vector2(.9f, .66f), TextAlignmentOptions.Center);
            TMP_Text resultBest = CreateText(resultPanel.transform, "BEST 0", 30, new Vector2(.1f, .49f), new Vector2(.9f, .57f), TextAlignmentOptions.Center);
            GameObject record = CreateText(resultPanel.transform, "NEW RECORD!", 28, new Vector2(.1f, .42f), new Vector2(.9f, .49f), TextAlignmentOptions.Center).gameObject;
            Button retry = CreateButton(resultPanel.transform, "RETRY", new Vector2(.24f, .27f), new Vector2(.76f, .37f));
            Button titleResult = CreateButton(resultPanel.transform, "TITLE", new Vector2(.24f, .15f), new Vector2(.76f, .25f));

            GameHud hud = new GameObject("GameHud").AddComponent<GameHud>();
            SetObject(hud, "scoreText", score); SetObject(hud, "highScoreText", best); SetObject(hud, "nextImage", next);
            SetObject(hud, "pausePanel", pausePanel); SetObject(hud, "resultPanel", resultPanel); SetObject(hud, "resultScoreText", resultScore); SetObject(hud, "resultHighScoreText", resultBest); SetObject(hud, "newRecordLabel", record);
            SetObject(session, "spawner", spawner); SetObject(session, "mergeResolver", resolver); SetObject(session, "overflowDetector", overflow); SetObject(session, "dropController", drop); SetObject(session, "hud", hud);
            UnityEventTools.AddPersistentListener(pause.onClick, session.Pause); UnityEventTools.AddPersistentListener(resume.onClick, session.Resume);
            UnityEventTools.AddPersistentListener(restartPause.onClick, session.StartGame); UnityEventTools.AddPersistentListener(retry.onClick, session.StartGame);
            UnityEventTools.AddPersistentListener(titlePause.onClick, session.GoToTitle); UnityEventTools.AddPersistentListener(titleResult.onClick, session.GoToTitle);
            pausePanel.SetActive(false); resultPanel.SetActive(false);
            EditorSceneManager.SaveScene(scene, ScenePath + "/GameScene.unity");
        }

        private static void ConfigureProject()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath + "/TitleScene.unity", true),
                new EditorBuildSettingsScene(ScenePath + "/GameScene.unity", true)
            };
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.companyName = "UsagiGame";
            PlayerSettings.productName = "Usagi Merge";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.threadsSupport = false;
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
            PlayerSettings.WebGL.template = "PROJECT:UsagiResponsive";
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            EditorSettings.enterPlayModeOptionsEnabled = false;
            Time.fixedDeltaTime = .02f;
            EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        }

        private static Camera CreateCamera(Vector3 position, float size, Color color)
        {
            GameObject go = new("Main Camera");
            go.tag = "MainCamera";
            Camera camera = go.AddComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = size; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = color;
            go.transform.position = position;
            return camera;
        }

        private static Canvas CreateCanvas()
        {
            GameObject go = new("Canvas");
            Canvas canvas = go.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = go.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = .5f;
            go.AddComponent<GraphicRaycaster>();
            GameObject eventSystem = new("EventSystem"); eventSystem.AddComponent<EventSystem>(); eventSystem.AddComponent<InputSystemUIInputModule>();
            return canvas;
        }

        private static RectTransform CreateSafeRoot(Transform parent)
        {
            RectTransform rect = NewRect("SafeArea", parent, Vector2.zero, Vector2.one);
            rect.gameObject.AddComponent<SafeAreaFitter>();
            return rect;
        }

        private static TMP_Text CreateText(Transform parent, string value, float size, Vector2 min, Vector2 max, TextAlignmentOptions alignment)
        {
            RectTransform rect = NewRect(value.Replace("\n", " "), parent, min, max);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value; text.fontSize = size; text.alignment = alignment; text.color = Ink; text.enableAutoSizing = false;
            return text;
        }

        private static Button CreateButton(Transform parent, string label, Vector2 min, Vector2 max)
        {
            Image image = CreateImage(parent, label + "Button", min, max, Pink);
            Button button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            TMP_Text text = CreateText(image.transform, label, 30, Vector2.zero, Vector2.one, TextAlignmentOptions.Center);
            text.raycastTarget = false;
            return button;
        }

        private static Toggle CreateToggle(Transform parent, string label, float y)
        {
            RectTransform root = NewRect(label + "Toggle", parent, new Vector2(.25f, y), new Vector2(.75f, y + .07f));
            Image background = CreateImage(root, "Background", new Vector2(0, .1f), new Vector2(.14f, .9f), new Color(.2f, .18f, .3f));
            Image check = CreateImage(background.transform, "Checkmark", new Vector2(.18f, .18f), new Vector2(.82f, .82f), new Color(1f, .65f, .25f));
            Toggle toggle = root.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = background; toggle.graphic = check;
            CreateText(root, label, 27, new Vector2(.2f, 0), Vector2.one, TextAlignmentOptions.Left);
            return toggle;
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            Image image = CreateImage(parent, name, Vector2.zero, Vector2.one, color);
            return image.gameObject;
        }

        private static Image CreateImage(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            RectTransform rect = NewRect(name, parent, min, max);
            Image image = rect.gameObject.AddComponent<Image>(); image.color = color;
            return image;
        }

        private static RectTransform NewRect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            GameObject go = new(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform; rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static void CreateEvolutionBar(Transform parent, IReadOnlyList<FruitDefinition> fruits)
        {
            CreateCard(parent, "EvolutionRing", new Vector2(.1f, .01f), new Vector2(.9f, .18f));
            Vector2 center = new(.5f, .095f);
            for (int i = 0; i < fruits.Count; i++)
            {
                float angle = Mathf.PI * .5f - Mathf.PI * 2f * i / fruits.Count;
                Vector2 point = center + new Vector2(Mathf.Cos(angle) * .32f, Mathf.Sin(angle) * .064f);
                Image image = CreateImage(parent, $"Evolution{i + 1}", point - new Vector2(.026f, .025f), point + new Vector2(.026f, .025f), Color.white);
                image.sprite = fruits[i].Sprite; image.preserveAspect = true; image.raycastTarget = false;
            }
        }

        private static CameraBackground CreateBackground(Camera camera)
        {
            GameObject go = new("UsagiBackground");
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/UI/usagi_background.png");
            renderer.sortingOrder = -100;
            CameraBackground fitter = go.AddComponent<CameraBackground>();
            fitter.Configure(camera);
            return fitter;
        }

        private static Image CreateCard(Transform parent, string name, Vector2 min, Vector2 max)
        {
            Image card = CreateImage(parent, name, min, max, Cream);
            card.sprite = GetWhiteSprite();
            card.type = Image.Type.Sliced;
            return card;
        }

        private static SpriteRenderer CreateBoardPart(Transform parent, string name, Vector2 position, Vector2 size, PhysicsMaterial2D material)
        {
            SpriteRenderer renderer = CreateVisual(parent, name, position, size, new Color(.38f, .2f, .1f, .82f));
            BoxCollider2D collider = renderer.gameObject.AddComponent<BoxCollider2D>(); collider.sharedMaterial = material;
            return renderer;
        }

        private static SpriteRenderer CreateVisual(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            GameObject go = new(name); go.transform.SetParent(parent, false); go.transform.position = position; go.transform.localScale = size;
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = GetWhiteSprite(); renderer.color = color; renderer.sortingOrder = 1;
            return renderer;
        }

        private static Sprite GetWhiteSprite()
        {
            if (whiteSprite == null) whiteSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            return whiteSprite;
        }

        private static void SetObject(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            SerializedObject so = new(target); so.FindProperty(property).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(UnityEngine.Object target, string property, int value)
        {
            SerializedObject so = new(target); so.FindProperty(property).intValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
