using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class WebVerificationBuild
{
    private const string ScenePath = "Assets/Scenes/WebVerification.unity";

    // Initial setup only. Existing scenes and materials are never overwritten.
    public static void CreateAndBuild()
    {
        if (File.Exists(ScenePath))
            throw new InvalidOperationException("Verification scene already exists; use Build instead.");

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) throw new InvalidOperationException("URP Lit shader is unavailable.");
        var cubeMaterial = CreateMaterial("WebVerificationCube", shader, new Color(0.95f, 0.22f, 0.06f));
        var groundMaterial = CreateMaterial("WebVerificationGround", shader, new Color(0.12f, 0.5f, 0.32f));
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cameraObject = new GameObject("Verification Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(6, 5, -8);
        cameraObject.transform.LookAt(new Vector3(0, 0.6f, 0));
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.14f, 0.24f);
        camera.fieldOfView = 45;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100;
        camera.allowHDR = false;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;

        var lightObject = new GameObject("Verification Light");
        lightObject.transform.rotation = Quaternion.Euler(50, -30, 0);
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.5f;
        light.shadows = LightShadows.Soft;
        RenderSettings.sun = light;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.45f);
        RenderSettings.skybox = null;
        RenderSettings.fog = false;

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Verification Ground";
        ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;
        UnityEngine.Object.DestroyImmediate(ground.GetComponent<Collider>());
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Verification Cube";
        cube.transform.position = new Vector3(0, 0.75f, 0);
        cube.transform.localScale = Vector3.one * 1.5f;
        cube.GetComponent<Renderer>().sharedMaterial = cubeMaterial;
        UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());

        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException("Could not save verification scene.");
        AssetDatabase.SaveAssets();
        Build();
    }

    private static Material CreateMaterial(string name, Shader shader, Color color)
    {
        var path = "Assets/Materials/" + name + ".mat";
        if (File.Exists(path)) throw new InvalidOperationException("Material already exists: " + path);
        var material = new Material(shader) { name = name };
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", 0.15f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    [MenuItem("Dr.forms/Build Web Verification")]
    public static void Build()
    {
        if (!File.Exists(ScenePath)) throw new InvalidOperationException("Verification scene is missing.");
        var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var output = Path.GetFullPath(Environment.GetEnvironmentVariable("DRFORMS_WEB_BUILD_OUTPUT")
            ?? Path.Combine(projectRoot, "..", "DrFormsGame-WebVerification", "VerificationSceneBuild"));
        if (output.Equals(projectRoot, StringComparison.OrdinalIgnoreCase)
            || output.StartsWith(projectRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Build output must be outside the project.");

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = output,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });
        Debug.Log("WEB_VERIFICATION_RESULT: " + report.summary.result
            + "; errors=" + report.summary.totalErrors + "; warnings=" + report.summary.totalWarnings
            + "; bytes=" + report.summary.totalSize + "; output=" + output);
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Web verification build failed.");
    }
}
