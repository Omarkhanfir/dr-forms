using System;
using System.IO;
using DrForms.Prototype;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class GamePrototypeBuild
{
    private const string ScenePath = "Assets/Scenes/GamePrototype.unity";

    public static void CreateAndBuild()
    {
        if (File.Exists(ScenePath)) throw new InvalidOperationException("Prototype scene exists; use Build.");
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) throw new InvalidOperationException("URP Lit shader is unavailable.");
        var floorMaterial = Material("GamePrototypeGround", shader, new Color(0.13f, 0.35f, 0.3f));
        var wallMaterial = Material("GamePrototypeWall", shader, new Color(0.32f, 0.4f, 0.55f));
        var playerMaterial = Material("GamePrototypePlayer", shader, new Color(1, 0.4f, 0.08f));
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Box("Arena Ground", new Vector3(0, -0.25f, 0), new Vector3(22, 0.5f, 22), floorMaterial);
        Box("North Wall", new Vector3(0, 1, 10.5f), new Vector3(22, 2, 1), wallMaterial);
        Box("South Wall", new Vector3(0, 1, -10.5f), new Vector3(22, 2, 1), wallMaterial);
        Box("East Wall", new Vector3(10.5f, 1, 0), new Vector3(1, 2, 22), wallMaterial);
        Box("West Wall", new Vector3(-10.5f, 1, 0), new Vector3(1, 2, 22), wallMaterial);

        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Prototype Player";
        player.transform.position = Vector3.up;
        player.GetComponent<Renderer>().sharedMaterial = playerMaterial;
        UnityEngine.Object.DestroyImmediate(player.GetComponent<Collider>());
        var controller = player.AddComponent<CharacterController>();
        controller.height = 2;
        controller.radius = 0.45f;
        controller.center = Vector3.zero;
        controller.skinWidth = 0.03f;
        controller.stepOffset = 0.2f;
        controller.minMoveDistance = 0;

        var cameraObject = new GameObject("Prototype Camera");
        cameraObject.tag = "MainCamera";
        var offset = new Vector3(8, 14, -10);
        cameraObject.transform.position = player.transform.position + offset;
        cameraObject.transform.rotation = Quaternion.LookRotation(-offset, Vector3.up);
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.07f, 0.1f, 0.17f);
        camera.fieldOfView = 60;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100;
        camera.allowHDR = false;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
        var follow = cameraObject.AddComponent<PrototypeCameraFollow>();
        var followSettings = new SerializedObject(follow);
        followSettings.FindProperty("target").objectReferenceValue = player.transform;
        followSettings.ApplyModifiedPropertiesWithoutUndo();
        var movement = player.AddComponent<PrototypeMovement>();
        var movementSettings = new SerializedObject(movement);
        movementSettings.FindProperty("movementCamera").objectReferenceValue = cameraObject.transform;
        movementSettings.FindProperty("arenaFloor").objectReferenceValue = GameObject.Find("Arena Ground").GetComponent<Collider>();
        movementSettings.ApplyModifiedPropertiesWithoutUndo();

        var lightObject = new GameObject("Arena Light");
        lightObject.transform.rotation = Quaternion.Euler(50, -30, 0);
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.5f;
        light.shadows = LightShadows.Soft;
        RenderSettings.sun = light;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.gray;
        RenderSettings.skybox = null;
        RenderSettings.fog = false;
        if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Scene save failed.");
        AssetDatabase.SaveAssets();
        Build();
    }

    private static Material Material(string name, Shader shader, Color color)
    {
        var path = "Assets/Materials/" + name + ".mat";
        if (File.Exists(path)) throw new InvalidOperationException("Material exists: " + path);
        var material = new Material(shader) { name = name };
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", 0.1f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void Box(string name, Vector3 position, Vector3 scale, Material material)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.position = position;
        box.transform.localScale = scale;
        box.GetComponent<Renderer>().sharedMaterial = material;
    }

    [MenuItem("Dr.forms/Build Game Prototype (Web Development)")]
    public static void Build()
    {
        if (!File.Exists(ScenePath)) throw new InvalidOperationException("Prototype scene is missing.");
        var project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var output = Path.GetFullPath(Environment.GetEnvironmentVariable("DRFORMS_PROTOTYPE_BUILD_OUTPUT")
            ?? Path.Combine(project, "..", "DrFormsGame-WebVerification", "GamePrototypeBuild"));
        if (output.Equals(project, StringComparison.OrdinalIgnoreCase)
            || output.StartsWith(project + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Build output must be outside the project.");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath }, locationPathName = output,
            target = BuildTarget.WebGL, options = BuildOptions.Development
        });
        Debug.Log("PROTOTYPE_BUILD_RESULT: " + report.summary.result + "; errors=" + report.summary.totalErrors
            + "; warnings=" + report.summary.totalWarnings + "; bytes=" + report.summary.totalSize);
        if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Prototype build failed.");
    }

    public static void AddWavesAndBuild()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var player = GameObject.Find("Prototype Player");
        var waves = player.GetComponent<PrototypeWaves>();
        if (waves == null) waves = player.AddComponent<PrototypeWaves>();
        var template = UnityEngine.Object.FindAnyObjectByType<PrototypeEnemy>(FindObjectsInactive.Include);
        if (template == null) throw new InvalidOperationException("Enemy template missing.");
        template.gameObject.SetActive(false);
        var settings = new SerializedObject(waves);
        settings.FindProperty("enemyTemplate").objectReferenceValue = template;
        settings.ApplyModifiedPropertiesWithoutUndo();
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Scene save failed.");
        Build();
    }

    public static void AddEnemyAndBuild()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var player = GameObject.Find("Prototype Player");
        var health = player.GetComponent<PrototypePlayerHealth>();
        if (health == null) health = player.AddComponent<PrototypePlayerHealth>();
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeEnemy>() == null)
        {
            var enemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            enemy.name = "Prototype Enemy";
            enemy.transform.position = new Vector3(-4, 1, -4);
            // Reuse the existing capsule mesh with controller dimensions in world units.
            enemy.transform.localScale = Vector3.one;
            enemy.GetComponent<MeshFilter>().sharedMesh = player.GetComponent<MeshFilter>().sharedMesh;
            UnityEngine.Object.DestroyImmediate(enemy.GetComponent<Collider>());
            var controller = enemy.AddComponent<CharacterController>();
            controller.height = 2;
            controller.radius = 0.5f;
            controller.skinWidth = 0.03f;
            controller.stepOffset = 0.2f;
            enemy.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/GamePrototypeWall.mat");
            var target = enemy.AddComponent<PrototypeTarget>();
            var targetSettings = new SerializedObject(target);
            targetSettings.FindProperty("displayName").stringValue = "Enemy";
            targetSettings.ApplyModifiedPropertiesWithoutUndo();
            var chase = enemy.AddComponent<PrototypeEnemy>();
            var settings = new SerializedObject(chase);
            settings.FindProperty("player").objectReferenceValue = health;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Scene save failed.");
        Build();
    }

    public static void AddCombatAndBuild()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var player = GameObject.Find("Prototype Player");
        if (player.GetComponent<PrototypeCombat>() == null) player.AddComponent<PrototypeCombat>();
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeTarget>() == null)
        {
            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = "Stationary Target";
            target.transform.position = new Vector3(0, 1, 4);
            target.transform.localScale = new Vector3(1.2f, 2, 1.2f);
            target.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/GamePrototypeWall.mat");
            target.AddComponent<PrototypeTarget>();
        }
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Scene save failed.");
        Build();
    }

    // Update only the existing prototype with the new floor reference and control component.
    public static void UpdateControlsAndBuild()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var player = GameObject.Find("Prototype Player");
        if (player.GetComponent<PrototypeControls>() == null) player.AddComponent<PrototypeControls>();
        var settings = new SerializedObject(player.GetComponent<PrototypeMovement>());
        settings.FindProperty("arenaFloor").objectReferenceValue = GameObject.Find("Arena Ground").GetComponent<Collider>();
        settings.ApplyModifiedPropertiesWithoutUndo();
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Scene save failed.");
        Build();
    }
}
