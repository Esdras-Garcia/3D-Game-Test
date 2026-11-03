using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.Collections.Generic;

public static class CreateARPGScene
{
    [InitializeOnLoadMethod]
    private static void CreateWhenEditorCompiles()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            const string scenePath = "Assets/Scenes/ARPGPrototype.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
                Create();
            else if (SceneManager.GetActiveScene().path == scenePath)
            {
                var arenaChanged = ExpandArenaAndCreateEnemies();
                AddNavigationToOpenScene(arenaChanged);
                ConfigureCameraInOpenScene();
                ApplyFantasyHeroModels();
                ConfigureFireballInOpenScene();
            }
        };
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode) return;
        EditorApplication.delayCall += () =>
        {
            if (SceneManager.GetActiveScene().path != "Assets/Scenes/ARPGPrototype.unity") return;
            var arenaChanged = ExpandArenaAndCreateEnemies();
            AddNavigationToOpenScene(arenaChanged);
            ConfigureCameraInOpenScene();
            ApplyFantasyHeroModels();
            ConfigureFireballInOpenScene();
        };
    }

    [MenuItem("Tools/Test Project/Update ARPG Prototype Assets")]
    public static void UpdateSavedScene()
    {
        const string scenePath = "Assets/Scenes/ARPGPrototype.unity";
        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        if (sceneAsset == null)
        {
            Create();
            return;
        }

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var arenaChanged = ExpandArenaAndCreateEnemies();
        AddNavigationToOpenScene(arenaChanged);
        ConfigureCameraInOpenScene();
        ApplyFantasyHeroModels();
        ConfigureFireballInOpenScene();
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Updated ARPG scene with Fantasy Heroes models, weapons, palette texture and movement fixes.");
    }

    public static void Create()
    {
        const string folder = "Assets/Materials";
        const string scenePath = "Assets/Scenes/ARPGPrototype.unity";
        EnsureFolder(folder);
        var groundMat = MaterialAsset(folder + "/Ground.mat", new Color(0.22f, 0.29f, 0.24f));
        var wallMat = MaterialAsset(folder + "/Stone.mat", new Color(0.34f, 0.30f, 0.26f));
        var playerMat = MaterialAsset(folder + "/Player.mat", new Color(0.18f, 0.48f, 0.8f));

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientLight = new Color(0.48f, 0.50f, 0.54f);
        Block("Ground", new Vector3(0, -0.65f, 0), new Vector3(24, 1.3f, 24), groundMat);
        Block("North Wall", new Vector3(0, 1, 11.5f), new Vector3(24, 2.5f, 1), wallMat);
        Block("South Wall", new Vector3(0, 1, -11.5f), new Vector3(24, 2.5f, 1), wallMat);
        Block("West Wall", new Vector3(-11.5f, 1, 0), new Vector3(1, 2.5f, 22), wallMat);
        Block("East Wall", new Vector3(11.5f, 1, 0), new Vector3(1, 2.5f, 22), wallMat);
        Block("Ruined Wall A", new Vector3(-4, 0.8f, 2), new Vector3(1, 1.8f, 6), wallMat);
        Block("Ruined Wall B", new Vector3(3, 0.8f, -2), new Vector3(6, 1.8f, 1), wallMat);

        var player = new GameObject("Player"); player.transform.position = new Vector3(0, 0, -6);
        var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Character Visual"; visual.transform.SetParent(player.transform, false);
        visual.transform.localPosition = Vector3.up;
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.GetComponent<Renderer>().sharedMaterial = playerMat;
        var agent = player.AddComponent<NavMeshAgent>();
        agent.speed = 6f; agent.acceleration = 20f; agent.angularSpeed = 720f; agent.stoppingDistance = 0.12f;
        player.AddComponent<ClickToMove>();

        var lightObject = new GameObject("Directional Light");
        var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.15f;
        lightObject.transform.rotation = Quaternion.Euler(50, -35, 0);
        var cameraObject = new GameObject("Isometric Camera");
        var camera = cameraObject.AddComponent<Camera>(); camera.tag = "MainCamera";
        camera.orthographic = false; camera.fieldOfView = 42f;
        camera.backgroundColor = new Color(0.10f, 0.12f, 0.14f);
        cameraObject.AddComponent<AudioListener>();
        var cameraFollow = cameraObject.AddComponent<IsometricCameraFollow>();
        cameraFollow.target = player.transform;
        var cameraFollowData = new SerializedObject(cameraFollow);
        cameraFollowData.FindProperty("offset").vector3Value = new Vector3(-8.5f, 14f, -8.5f);
        cameraFollowData.FindProperty("pitch").floatValue = 50f;
        cameraFollowData.FindProperty("yaw").floatValue = 45f;
        cameraFollowData.ApplyModifiedPropertiesWithoutUndo();
        cameraObject.transform.position = player.transform.position + new Vector3(-8.5f, 14f, -8.5f);
        cameraObject.transform.rotation = Quaternion.Euler(50f, 45f, 0f);

        EditorSceneManager.SaveScene(scene, scenePath);
        ExpandArenaAndCreateEnemies();
        AddNavigationToOpenScene(true);
        ConfigureCameraInOpenScene();
        ApplyFantasyHeroModels();
        ConfigureFireballInOpenScene();
        AssetDatabase.SaveAssets();
        Debug.Log("Created playable ARPG scene at " + scenePath);
    }

    private static void AddNavigationToOpenScene(bool rebake = false)
    {
        var ground = GameObject.Find("Ground");
        if (ground == null) return;
        UpgradeExistingPlayer();
        var surface = ground.GetComponent<NavMeshSurface>();
        if (surface == null)
        {
            surface = ground.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            rebake = true;
        }
        if (!rebake) return;
        surface.BuildNavMesh();
        if (surface.navMeshData != null)
        {
            const string navFolder = "Assets/Navigation";
            if (!AssetDatabase.IsValidFolder(navFolder)) AssetDatabase.CreateFolder("Assets", "Navigation");
            const string dataPath = navFolder + "/ARPGPrototypeNavMesh.asset";
            var old = AssetDatabase.LoadAssetAtPath<NavMeshData>(dataPath);
            if (old != null) AssetDatabase.DeleteAsset(dataPath);
            AssetDatabase.CreateAsset(surface.navMeshData, dataPath);
            EditorUtility.SetDirty(surface);
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Baked the expanded ARPG arena NavMesh.");
    }

    private static bool ExpandArenaAndCreateEnemies()
    {
        var changed = false;
        var ground = GameObject.Find("Ground");
        if (ground != null && (ground.transform.localScale.x < 39f || ground.transform.localScale.z < 39f))
        {
            ground.transform.position = new Vector3(0f, -0.65f, 0f);
            ground.transform.localScale = new Vector3(40f, 1.3f, 40f);
            changed = true;
        }
        ResizeBlock("North Wall", new Vector3(0f, 1f, 19.5f), new Vector3(40f, 2.5f, 1f), ref changed);
        ResizeBlock("South Wall", new Vector3(0f, 1f, -19.5f), new Vector3(40f, 2.5f, 1f), ref changed);
        ResizeBlock("West Wall", new Vector3(-19.5f, 1f, 0f), new Vector3(1f, 2.5f, 38f), ref changed);
        ResizeBlock("East Wall", new Vector3(19.5f, 1f, 0f), new Vector3(1f, 2.5f, 38f), ref changed);

        var stone = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Stone.mat");
        changed |= EnsureBlock("Pillar North West", new Vector3(-8f, 1f, 8f), new Vector3(2f, 2f, 2f), stone);
        changed |= EnsureBlock("Pillar North East", new Vector3(8f, 1f, 8f), new Vector3(2f, 2f, 2f), stone);
        changed |= EnsureBlock("Pillar South West", new Vector3(-8f, 1f, -10f), new Vector3(2f, 2f, 2f), stone);
        changed |= EnsureBlock("Pillar South East", new Vector3(8f, 1f, -10f), new Vector3(2f, 2f, 2f), stone);
        changed |= EnsureBlock("Cross Barrier West", new Vector3(-13f, 0.8f, 0f), new Vector3(5f, 1.6f, 1f), stone);
        changed |= EnsureBlock("Cross Barrier East", new Vector3(13f, 0.8f, 2f), new Vector3(5f, 1.6f, 1f), stone);
        changed |= EnsureBlock("North Ruin", new Vector3(0f, 0.8f, 13f), new Vector3(7f, 1.6f, 1f), stone);

        changed |= EnsureEnemy("Enemy 1", new Vector3(-16f, 0f, -8f), stone);
        changed |= EnsureEnemy("Enemy 2", new Vector3(16f, 0f, -5f), stone);
        changed |= EnsureEnemy("Enemy 3", new Vector3(6f, 0f, 14f), stone);
        foreach (var vision in Object.FindObjectsByType<EnemyVisionChase>(FindObjectsSortMode.None))
        {
            var settings = new SerializedObject(vision);
            var angle = settings.FindProperty("fieldOfView");
            var distance = settings.FindProperty("viewDistance");
            if (Mathf.Abs(angle.floatValue - 70f) > 0.01f || Mathf.Abs(distance.floatValue - 15f) > 0.01f)
            {
                angle.floatValue = 70f;
                distance.floatValue = 15f;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        return changed;
    }

    private static void ResizeBlock(string objectName, Vector3 position, Vector3 scale, ref bool changed)
    {
        var block = GameObject.Find(objectName);
        if (block == null) return;
        if (Vector3.Distance(block.transform.position, position) > 0.01f || Vector3.Distance(block.transform.localScale, scale) > 0.01f)
        {
            block.transform.position = position;
            block.transform.localScale = scale;
            changed = true;
        }
    }

    private static bool EnsureBlock(string objectName, Vector3 position, Vector3 scale, Material material)
    {
        if (GameObject.Find(objectName) != null) return false;
        Block(objectName, position, scale, material);
        return true;
    }

    private static bool EnsureEnemy(string objectName, Vector3 position, Material material)
    {
        if (GameObject.Find(objectName) != null) return false;
        var player = GameObject.Find("Player");
        var enemy = new GameObject(objectName);
        enemy.transform.position = position;
        if (player != null)
        {
            var direction = player.transform.position - position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f) enemy.transform.rotation = Quaternion.LookRotation(direction);
        }
        var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Enemy Visual";
        visual.transform.SetParent(enemy.transform, false);
        visual.transform.localPosition = Vector3.up;
        visual.transform.localScale = new Vector3(0.85f, 1f, 0.85f);
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        if (material != null) visual.GetComponent<Renderer>().sharedMaterial = material;

        var trigger = enemy.AddComponent<CapsuleCollider>();
        trigger.center = Vector3.up;
        trigger.height = 2f;
        trigger.radius = 0.45f;
        trigger.isTrigger = true;
        var agent = enemy.AddComponent<NavMeshAgent>();
        agent.radius = 0.45f;
        agent.height = 2f;
        agent.speed = 3.8f;
        agent.acceleration = 14f;
        agent.angularSpeed = 600f;
        agent.stoppingDistance = 1.2f;
        enemy.AddComponent<EnemyVisionChase>();
        enemy.AddComponent<EnemyHealth>();
        return true;
    }

    private static void ConfigureFireballInOpenScene()
    {
        var player = GameObject.Find("Player");
        if (player == null) return;
        const string folder = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "Prefabs");
        const string materialPath = "Assets/Materials/Fireball.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { color = new Color(1f, 0.22f, 0.025f) };
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(1f, 0.12f, 0.015f) * 2.5f);
            }
            AssetDatabase.CreateAsset(material, materialPath);
        }

        const string prefabPath = folder + "/Fireball.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Fireball";
            ball.transform.localScale = Vector3.one * 0.3f;
            ball.GetComponent<Renderer>().sharedMaterial = material;
            ball.GetComponent<SphereCollider>().isTrigger = true;
            ball.AddComponent<FireballProjectile>();
            prefab = PrefabUtility.SaveAsPrefabAsset(ball, prefabPath);
            Object.DestroyImmediate(ball);
        }

        var ability = player.GetComponent<FireballAbility>();
        if (ability == null) ability = player.AddComponent<FireballAbility>();
        var staff = FindDescendant(player.transform, "Weapon - Hero_Skull_Staff");
        Transform projectileOrigin = null;
        if (staff != null)
        {
            projectileOrigin = staff.Find("Fireball Origin");
            if (projectileOrigin == null)
            {
                var originObject = new GameObject("Fireball Origin");
                originObject.transform.SetParent(staff, true);
                projectileOrigin = originObject.transform;
            }

            var staffRenderers = staff.GetComponentsInChildren<Renderer>(true);
            if (staffRenderers.Length > 0)
            {
                var bounds = staffRenderers[0].bounds;
                for (var i = 1; i < staffRenderers.Length; i++) bounds.Encapsulate(staffRenderers[i].bounds);
                var axis = bounds.size.y >= bounds.size.x && bounds.size.y >= bounds.size.z ? 1
                    : bounds.size.x >= bounds.size.z ? 0 : 2;
                var lower = bounds.center;
                var upper = bounds.center;
                if (axis == 0) { lower.x -= bounds.extents.x; upper.x += bounds.extents.x; }
                else if (axis == 1) { lower.y -= bounds.extents.y; upper.y += bounds.extents.y; }
                else { lower.z -= bounds.extents.z; upper.z += bounds.extents.z; }
                var characterCenter = player.transform.position + Vector3.up;
                var useLower = Vector3.SqrMagnitude(lower - characterCenter) > Vector3.SqrMagnitude(upper - characterCenter);
                var tip = useLower ? lower : upper;
                var opposite = useLower ? upper : lower;
                tip += (tip - opposite).normalized * 0.12f;
                projectileOrigin.position = tip;
            }
        }
        var serialized = new SerializedObject(ability);
        serialized.FindProperty("fireballPrefab").objectReferenceValue = prefab;
        serialized.FindProperty("projectileOrigin").objectReferenceValue = projectileOrigin;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Added Fireball prefab and ability to the open ARPG scene.");
    }

    private static Transform FindDescendant(Transform root, string targetName)
    {
        foreach (var child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == targetName) return child;
        return null;
    }

    private static void ConfigureCameraInOpenScene()
    {
        var cameraObject = GameObject.Find("Isometric Camera");
        var player = GameObject.Find("Player");
        if (cameraObject == null || player == null) return;
        var camera = cameraObject.GetComponent<Camera>();
        if (camera == null) return;
        camera.orthographic = false;
        camera.fieldOfView = 42f;
        var follow = cameraObject.GetComponent<IsometricCameraFollow>();
        if (follow == null) follow = cameraObject.AddComponent<IsometricCameraFollow>();
        var serialized = new SerializedObject(follow);
        serialized.FindProperty("target").objectReferenceValue = player.transform;
        serialized.FindProperty("offset").vector3Value = new Vector3(-8.5f, 14f, -8.5f);
        serialized.FindProperty("pitch").floatValue = 50f;
        serialized.FindProperty("yaw").floatValue = 45f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        cameraObject.transform.position = player.transform.position + new Vector3(-8.5f, 14f, -8.5f);
        cameraObject.transform.rotation = Quaternion.Euler(50f, 45f, 0f);
        EditorUtility.SetDirty(cameraObject);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    private static void ApplyFantasyHeroModels()
    {
        var palette = GetFantasyPaletteMaterial();
        var player = GameObject.Find("Player");
        if (player != null) ConfigureFantasyCharacter(player,
            "Assets/Fantasy_Heroes_Free/FBX/Characters/Wizard.fbx", 1.9f, palette,
            "Assets/Fantasy_Heroes_Free/FBX/Weapons/Hero_Skull_Staff.fbx", null);

        ConfigureEnemy("Enemy 1", "Assets/Fantasy_Heroes_Free/FBX/Characters/Knight.fbx", palette,
            "Assets/Fantasy_Heroes_Free/FBX/Weapons/Hero_Sword_01.fbx", null);
        ConfigureEnemy("Enemy 2", "Assets/Fantasy_Heroes_Free/FBX/Characters/Paladin.fbx", palette,
            "Assets/Fantasy_Heroes_Free/FBX/Weapons/Hero_Sword_02.fbx",
            "Assets/Fantasy_Heroes_Free/FBX/Weapons/Hero_Shield_01.fbx");
        ConfigureEnemy("Enemy 3", "Assets/Fantasy_Heroes_Free/FBX/Characters/Dwarf_Warrior.fbx", palette,
            "Assets/Fantasy_Heroes_Free/FBX/Weapons/Hero_Hammer.fbx", null);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    private static void ConfigureEnemy(string enemyName, string modelPath, Material palette, string rightWeaponPath, string leftWeaponPath)
    {
        var enemy = GameObject.Find(enemyName);
        if (enemy != null) ConfigureFantasyCharacter(enemy, modelPath, enemyName == "Enemy 3" ? 1.65f : 1.9f,
            palette, rightWeaponPath, leftWeaponPath);
    }

    private static Material GetFantasyPaletteMaterial()
    {
        const string texturePath = "Assets/Fantasy_Heroes_Free/Texture/Texture.png";
        var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer != null && (importer.filterMode != FilterMode.Point || importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed))
        {
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        const string materialPath = "Assets/Fantasy_Heroes_Free/FantasyHeroesPalette.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
        }
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        material.color = Color.white;
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", 0.15f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ConfigureFantasyCharacter(GameObject character, string modelPath, float targetHeight,
        Material palette, string rightWeaponPath, string leftWeaponPath)
    {
        var model = ReplacePrimitiveWithModel(character, modelPath, targetHeight);
        if (model == null) return;
        ApplyPalette(model, palette);
        RemoveAttachedWeapons(model.transform);
        AttachWeapon(model.transform, rightWeaponPath, false, palette);
        AttachWeapon(model.transform, leftWeaponPath, true, palette);
    }

    private static GameObject ReplacePrimitiveWithModel(GameObject character, string modelPath, float targetHeight)
    {
        var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (modelAsset == null)
        {
            Debug.LogWarning("Could not load character model: " + modelPath);
            return null;
        }

        var instanceName = "Fantasy Model - " + modelAsset.name;
        var existingInstance = character.transform.Find(instanceName);
        if (existingInstance != null) return existingInstance.gameObject;
        for (var i = character.transform.childCount - 1; i >= 0; i--)
        {
            var child = character.transform.GetChild(i);
            if (child.name.StartsWith("Fantasy Model -")) Object.DestroyImmediate(child.gameObject);
        }

        var primitive = character.transform.Find("Character Visual") ?? character.transform.Find("Enemy Visual");
        if (primitive != null) primitive.gameObject.SetActive(false);

        var model = PrefabUtility.InstantiatePrefab(modelAsset, character.transform) as GameObject;
        if (model == null) return null;
        model.name = instanceName;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;

        var renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return model;
        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        if (bounds.size.y > 0.001f)
            model.transform.localScale = Vector3.one * (targetHeight / bounds.size.y);

        renderers = model.GetComponentsInChildren<Renderer>(true);
        bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        model.transform.localPosition += Vector3.up * -bounds.min.y;
        EditorUtility.SetDirty(character);
        return model;
    }

    private static void ApplyPalette(GameObject model, Material palette)
    {
        foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            var materials = renderer.sharedMaterials;
            for (var i = 0; i < materials.Length; i++) materials[i] = palette;
            renderer.sharedMaterials = materials;
        }
    }

    private static void RemoveAttachedWeapons(Transform model)
    {
        var children = model.GetComponentsInChildren<Transform>(true);
        for (var i = children.Length - 1; i > 0; i--)
        {
            if (children[i] != null && children[i].name.StartsWith("Weapon - "))
                Object.DestroyImmediate(children[i].gameObject);
        }
    }

    private static void AttachWeapon(Transform model, string weaponPath, bool leftHand, Material palette)
    {
        if (string.IsNullOrEmpty(weaponPath)) return;
        var weaponAsset = AssetDatabase.LoadAssetAtPath<GameObject>(weaponPath);
        if (weaponAsset == null)
        {
            Debug.LogWarning("Could not load weapon model: " + weaponPath);
            return;
        }

        var hand = FindHand(model, leftHand);
        var bounds = GetCharacterBounds(model);
        var bottom = bounds.min.y;
        var height = Mathf.Max(0.1f, bounds.size.y);
        if (hand == model || hand.position.y < bottom + height * 0.4f || hand.position.y > bottom + height * 0.95f)
            hand = GetOrCreateHandSocket(model, leftHand, bounds);

        var weapon = PrefabUtility.InstantiatePrefab(weaponAsset, hand) as GameObject;
        if (weapon == null) return;
        weapon.name = "Weapon - " + weaponAsset.name;
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
        weapon.transform.localScale = Vector3.one;
        ApplyPalette(weapon, palette);

        var renderers = weapon.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;
        var weaponBounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++) weaponBounds.Encapsulate(renderers[i].bounds);
        var largestDimension = Mathf.Max(weaponBounds.size.x, weaponBounds.size.y, weaponBounds.size.z);
        var targetSize = weaponAsset.name.Contains("Shield") ? 0.75f : weaponAsset.name.Contains("Staff") ? 1.25f : 0.9f;
        if (largestDimension > 0.001f) weapon.transform.localScale = Vector3.one * (targetSize / largestDimension);
    }

    private static Bounds GetCharacterBounds(Transform model)
    {
        var allRenderers = model.GetComponentsInChildren<Renderer>(true);
        var renderers = new List<Renderer>();
        foreach (var renderer in allRenderers)
            if (renderer.gameObject.activeInHierarchy) renderers.Add(renderer);
        if (renderers.Count == 0) return new Bounds(model.position + Vector3.up, Vector3.one);
        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Count; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static Transform GetOrCreateHandSocket(Transform model, bool leftHand, Bounds characterBounds)
    {
        var socketName = leftHand ? "Weapon Socket Left" : "Weapon Socket Right";
        var socket = model.Find(socketName);
        if (socket == null)
        {
            var socketObject = new GameObject(socketName);
            socketObject.transform.SetParent(model, false);
            socket = socketObject.transform;
        }

        var localCenter = model.InverseTransformPoint(characterBounds.center);
        var localHeight = characterBounds.size.y / Mathf.Max(0.001f, model.lossyScale.y);
        var localWidth = characterBounds.size.x / Mathf.Max(0.001f, model.lossyScale.x);
        var localDepth = characterBounds.size.z / Mathf.Max(0.001f, model.lossyScale.z);
        var side = leftHand ? -1f : 1f;
        socket.localPosition = localCenter + new Vector3(side * localWidth * 0.34f,
            localHeight * 0.22f, localDepth * 0.12f);
        socket.localRotation = Quaternion.identity;
        return socket;
    }

    private static Transform FindHand(Transform root, bool leftHand)
    {
        var names = leftHand
            ? new[] { "lefthand", "left_hand", "hand_l", "handleft" }
            : new[] { "righthand", "right_hand", "hand_r", "handright" };
        var transforms = root.GetComponentsInChildren<Transform>(true);
        foreach (var bone in transforms)
        {
            var lowerName = bone.name.ToLowerInvariant();
            foreach (var candidate in names)
                if (lowerName.Contains(candidate)) return bone;
        }
        Debug.LogWarning("Hand bone not found under " + root.name + "; using a hand-height weapon socket.");
        return root;
    }

    private static void UpgradeExistingPlayer()
    {
        var player = GameObject.Find("Player");
        if (player == null || player.GetComponent<NavMeshAgent>() != null) return;
        var oldRenderer = player.GetComponent<Renderer>();
        var material = oldRenderer != null ? oldRenderer.sharedMaterial : null;
        if (oldRenderer != null) Object.DestroyImmediate(oldRenderer);
        var mesh = player.GetComponent<MeshFilter>();
        if (mesh != null) Object.DestroyImmediate(mesh);
        var collider = player.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);
        var body = player.GetComponent<Rigidbody>();
        if (body != null) Object.DestroyImmediate(body);
        player.transform.position = new Vector3(0, 0, -6);
        var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Character Visual"; visual.transform.SetParent(player.transform, false);
        visual.transform.localPosition = Vector3.up;
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        if (material != null) visual.GetComponent<Renderer>().sharedMaterial = material;
        var agent = player.AddComponent<NavMeshAgent>();
        agent.speed = 6f; agent.acceleration = 20f; agent.angularSpeed = 720f; agent.stoppingDistance = 0.12f;
    }

    private static void Block(string name, Vector3 position, Vector3 scale, Material material)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name; obj.transform.position = position; obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static Material MaterialAsset(string path, Color color)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader) { color = color };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder("Assets", "Materials");
    }
}
