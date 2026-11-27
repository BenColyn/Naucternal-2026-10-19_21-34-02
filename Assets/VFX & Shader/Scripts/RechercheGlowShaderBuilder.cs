using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public static class RechercheGlowShaderBuilder
{
    private const string ShaderPath = "Assets/Jimmy/Art/RechercheGlow.shadergraph";
    private const string MaterialPath = "Assets/Jimmy/Art/RechercheGlow.mat";
    private const string SeaShaderPath = "Assets/Jimmy/Art/DeepSeaBackdrop.shadergraph";
    private const string SeaMaterialPath = "Assets/Jimmy/Art/DeepSeaBackdrop.mat";
    private const string PrefabPath = "Assets/Player/MC.prefab";
    private const string ScenePath = "Assets/Scenes/RechercheUnderwaterTest.unity";
    private const string CapsulePath = "Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Capsule.png";
    private const string SquarePath = "Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Square.png";
    private const string PreviewDirectory = "Logs";

    private static readonly Color FogColor = new Color(0.012f, 0.028f, 0.05f, 1f);
    private static readonly Color DeepColor = new Color(0.03f, 0.09f, 0.16f, 1f);
    private static readonly Color MistColor = new Color(0.1f, 0.2f, 0.28f, 1f);
    private static readonly Color ShadowColor = new Color(0.008f, 0.03f, 0.06f, 1f);
    private const float FogOpacity = 0.97f;
    private const float CoverSize = 30f;
    private const float ViewRadius = 4f;

    [MenuItem("Naucternal/Apply Deep Sea Vision")]
    public static void ApplyDeepSeaVision()
    {
        var glowShader = CreateVisionShader();
        var glowMaterial = CreateMaterial(glowShader, MaterialPath, "RechercheGlow");
        var seaShader = CreateSeaShader();
        var seaMaterial = CreateMaterial(seaShader, SeaMaterialPath, "DeepSeaBackdrop");
        UpdatePrefab(glowMaterial);
        var camera = UpdateScene(seaMaterial);
        VerifyVision(camera);
        Debug.Log("Deep sea vision glow is ready.");
    }

    private static Shader CreateVisionShader()
    {
        var graph = BeginSpriteGraph(out var shaderGraph, out var graphType);
        var fogProperty = AddColor(shaderGraph, graphType, graph, "FogColor", FogColor);
        var opacityProperty = AddFloat(shaderGraph, graphType, graph, "Opacity", FogOpacity);
        var radiusProperty = AddFloat(shaderGraph, graphType, graph, "Radius", ViewRadius / CoverSize);
        RequireNames(fogProperty, "_FogColor", opacityProperty, "_Opacity", radiusProperty, "_Radius");
        var fog = CreatePropertyNode(shaderGraph, graphType, graph, fogProperty);
        var opacity = CreatePropertyNode(shaderGraph, graphType, graph, opacityProperty);
        var radius = CreatePropertyNode(shaderGraph, graphType, graph, radiusProperty);

        var distance = CreateDistance(shaderGraph, graphType, graph, out var uvNode);
        var noise = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.GradientNoiseNode");
        var noiseAmount = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.Vector1Node");
        var innerFactor = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.Vector1Node");
        var noisyDistance = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.AddNode");
        var noiseScale = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.MultiplyNode");
        var innerRadius = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.MultiplyNode");
        var smoothstep = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.SmoothstepNode");
        var fogAlpha = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.MultiplyNode");

        SetVector1(noiseAmount, 0.02f);
        SetVector1(innerFactor, 0.72f);
        Connect(shaderGraph, graphType, graph, uvNode, 0, noise, 0);
        Connect(shaderGraph, graphType, graph, noise, 2, noiseScale, 0);
        Connect(shaderGraph, graphType, graph, noiseAmount, 0, noiseScale, 1);
        Connect(shaderGraph, graphType, graph, distance, 1, noisyDistance, 0);
        Connect(shaderGraph, graphType, graph, noiseScale, 2, noisyDistance, 1);
        Connect(shaderGraph, graphType, graph, radius, 0, innerRadius, 0);
        Connect(shaderGraph, graphType, graph, innerFactor, 0, innerRadius, 1);
        Connect(shaderGraph, graphType, graph, innerRadius, 2, smoothstep, 0);
        Connect(shaderGraph, graphType, graph, radius, 0, smoothstep, 1);
        Connect(shaderGraph, graphType, graph, noisyDistance, 2, smoothstep, 2);
        Connect(shaderGraph, graphType, graph, smoothstep, 3, fogAlpha, 0);
        Connect(shaderGraph, graphType, graph, opacity, 0, fogAlpha, 1);

        var baseColor = FindBlock(shaderGraph, graphType, graph, "SurfaceDescription.BaseColor");
        var alphaBlock = FindBlock(shaderGraph, graphType, graph, "SurfaceDescription.Alpha");
        Connect(shaderGraph, graphType, graph, fog, 0, baseColor, 0);
        Connect(shaderGraph, graphType, graph, fogAlpha, 2, alphaBlock, 0);
        return SaveGraph(shaderGraph, graphType, graph, ShaderPath);
    }

    private static Shader CreateSeaShader()
    {
        var graph = BeginSpriteGraph(out var shaderGraph, out var graphType);
        var deepProperty = AddColor(shaderGraph, graphType, graph, "DeepColor", DeepColor);
        var mistProperty = AddColor(shaderGraph, graphType, graph, "MistColor", MistColor);
        var shadowProperty = AddColor(shaderGraph, graphType, graph, "ShadowColor", ShadowColor);
        var scrollProperty = AddFloat(shaderGraph, graphType, graph, "Scroll", 0f);
        RequireNames(deepProperty, "_DeepColor", mistProperty, "_MistColor", shadowProperty, "_ShadowColor", scrollProperty, "_Scroll");
        var deep = CreatePropertyNode(shaderGraph, graphType, graph, deepProperty);
        var mist = CreatePropertyNode(shaderGraph, graphType, graph, mistProperty);
        var shadow = CreatePropertyNode(shaderGraph, graphType, graph, shadowProperty);
        var scroll = CreatePropertyNode(shaderGraph, graphType, graph, scrollProperty);

        var uv = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.UVNode");
        var split = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.SplitNode");
        var combine = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.CombineNode");
        var scrollYFactor = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.Vector1Node");
        var scrollY = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.MultiplyNode");
        var scrollVector = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.CombineNode");
        var scrolled = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.AddNode");
        var noise = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.GradientNoiseNode");
        var heightLerp = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.LerpNode");
        var hazeLerp = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.LerpNode");
        var silhouette = CreateSilhouette(shaderGraph, graphType, graph, split);
        var shadowLerp = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.LerpNode");
        var opaque = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.Vector1Node");

        SetVector1(scrollYFactor, 0.35f);
        SetVector1(opaque, 1f);
        Connect(shaderGraph, graphType, graph, uv, 0, split, 0);
        Connect(shaderGraph, graphType, graph, split, 1, combine, 0);
        Connect(shaderGraph, graphType, graph, split, 2, combine, 1);
        Connect(shaderGraph, graphType, graph, scroll, 0, scrollY, 0);
        Connect(shaderGraph, graphType, graph, scrollYFactor, 0, scrollY, 1);
        Connect(shaderGraph, graphType, graph, scroll, 0, scrollVector, 0);
        Connect(shaderGraph, graphType, graph, scrollY, 2, scrollVector, 1);
        Connect(shaderGraph, graphType, graph, combine, 6, scrolled, 0);
        Connect(shaderGraph, graphType, graph, scrollVector, 6, scrolled, 1);
        Connect(shaderGraph, graphType, graph, scrolled, 2, noise, 0);
        Connect(shaderGraph, graphType, graph, deep, 0, heightLerp, 0);
        Connect(shaderGraph, graphType, graph, mist, 0, heightLerp, 1);
        Connect(shaderGraph, graphType, graph, split, 2, heightLerp, 2);
        Connect(shaderGraph, graphType, graph, heightLerp, 3, hazeLerp, 0);
        Connect(shaderGraph, graphType, graph, mist, 0, hazeLerp, 1);
        Connect(shaderGraph, graphType, graph, noise, 2, hazeLerp, 2);
        Connect(shaderGraph, graphType, graph, hazeLerp, 3, shadowLerp, 0);
        Connect(shaderGraph, graphType, graph, shadow, 0, shadowLerp, 1);
        Connect(shaderGraph, graphType, graph, silhouette, 2, shadowLerp, 2);

        var baseColor = FindBlock(shaderGraph, graphType, graph, "SurfaceDescription.BaseColor");
        var alphaBlock = FindBlock(shaderGraph, graphType, graph, "SurfaceDescription.Alpha");
        Connect(shaderGraph, graphType, graph, shadowLerp, 3, baseColor, 0);
        Connect(shaderGraph, graphType, graph, opaque, 0, alphaBlock, 0);
        return SaveGraph(shaderGraph, graphType, graph, SeaShaderPath);
    }

    private static object CreateDistance(Assembly shaderGraph, Type graphType, object graph, out object uvNode)
    {
        uvNode = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.UVNode");
        var split = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.SplitNode");
        var combine = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.CombineNode");
        var center = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.Vector2Node");
        var subtract = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.SubtractNode");
        var length = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.LengthNode");
        SetVector2(center, new Vector2(0.5f, 0.5f));
        Connect(shaderGraph, graphType, graph, uvNode, 0, split, 0);
        Connect(shaderGraph, graphType, graph, split, 1, combine, 0);
        Connect(shaderGraph, graphType, graph, split, 2, combine, 1);
        Connect(shaderGraph, graphType, graph, combine, 6, subtract, 0);
        Connect(shaderGraph, graphType, graph, center, 0, subtract, 1);
        Connect(shaderGraph, graphType, graph, subtract, 2, length, 0);
        return length;
    }

    private static object CreateSilhouette(Assembly shaderGraph, Type graphType, object graph, object split)
    {
        var first = CreateBlob(shaderGraph, graphType, graph, split, new Vector2(0.46f, 0.5f), 0.015f, 0.055f);
        var second = CreateBlob(shaderGraph, graphType, graph, split, new Vector2(0.56f, 0.44f), 0.02f, 0.07f);
        var third = CreateBlob(shaderGraph, graphType, graph, split, new Vector2(0.4f, 0.58f), 0.012f, 0.05f);
        var maxFirst = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.MaximumNode");
        var maxAll = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.MaximumNode");
        Connect(shaderGraph, graphType, graph, first, 1, maxFirst, 0);
        Connect(shaderGraph, graphType, graph, second, 1, maxFirst, 1);
        Connect(shaderGraph, graphType, graph, maxFirst, 2, maxAll, 0);
        Connect(shaderGraph, graphType, graph, third, 1, maxAll, 1);
        return maxAll;
    }

    private static object CreateBlob(
        Assembly shaderGraph,
        Type graphType,
        object graph,
        object split,
        Vector2 center,
        float inner,
        float outer)
    {
        var uv = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.CombineNode");
        var point = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.Vector2Node");
        var subtract = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.SubtractNode");
        var length = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.LengthNode");
        var innerNode = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.Vector1Node");
        var outerNode = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.Vector1Node");
        var smoothstep = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.SmoothstepNode");
        var inverted = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.OneMinusNode");
        SetVector2(point, center);
        SetVector1(innerNode, inner);
        SetVector1(outerNode, outer);
        Connect(shaderGraph, graphType, graph, split, 1, uv, 0);
        Connect(shaderGraph, graphType, graph, split, 2, uv, 1);
        Connect(shaderGraph, graphType, graph, uv, 6, subtract, 0);
        Connect(shaderGraph, graphType, graph, point, 0, subtract, 1);
        Connect(shaderGraph, graphType, graph, subtract, 2, length, 0);
        Connect(shaderGraph, graphType, graph, innerNode, 0, smoothstep, 0);
        Connect(shaderGraph, graphType, graph, outerNode, 0, smoothstep, 1);
        Connect(shaderGraph, graphType, graph, length, 1, smoothstep, 2);
        Connect(shaderGraph, graphType, graph, smoothstep, 3, inverted, 0);
        return inverted;
    }

    private static object BeginSpriteGraph(out Assembly shaderGraph, out Type graphType)
    {
        shaderGraph = Assembly.Load("Unity.ShaderGraph.Editor");
        var urp = Assembly.Load("Unity.RenderPipelines.Universal.Editor");
        graphType = shaderGraph.GetType("UnityEditor.ShaderGraph.GraphData");
        var graph = Activator.CreateInstance(graphType);
        Invoke(graphType, graph, "AddContexts");

        var targetType = urp.GetType("UnityEditor.Rendering.Universal.ShaderGraph.UniversalTarget");
        var target = Activator.CreateInstance(targetType);
        var subTargetType = urp.GetType("UnityEditor.Rendering.Universal.ShaderGraph.UniversalSpriteUnlitSubTarget");
        var selected = (bool)targetType.GetMethod("TrySetActiveSubTarget").Invoke(target, new object[] { subTargetType });
        if (!selected)
            throw new InvalidOperationException("Could not select the Sprite Unlit subtarget.");

        SetEnum(target, "surfaceType", urp, "UnityEditor.Rendering.Universal.ShaderGraph.SurfaceType", "Transparent");
        SetEnum(target, "alphaMode", urp, "UnityEditor.Rendering.Universal.ShaderGraph.AlphaMode", "Alpha");
        SetEnum(target, "zWriteControl", urp, "UnityEditor.Rendering.Universal.ShaderGraph.ZWriteControl", "ForceDisabled");

        var targetBase = shaderGraph.GetType("UnityEditor.ShaderGraph.Target");
        var targets = Array.CreateInstance(targetBase, 1);
        targets.SetValue(target, 0);
        Invoke(graphType, graph, "InitializeOutputs", targets, CreateSpriteBlocks(shaderGraph));
        Invoke(graphType, graph, "AddCategory", CreateDefaultCategory(shaderGraph));
        graphType.GetProperty("path").SetValue(graph, "Shader Graphs");
        return graph;
    }

    private static Shader SaveGraph(Assembly shaderGraph, Type graphType, object graph, string path)
    {
        Invoke(graphType, graph, "ValidateGraph");
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Assets");
        var fileUtilities = shaderGraph.GetType("UnityEditor.ShaderGraph.FileUtilities");
        var written = fileUtilities.GetMethod("WriteShaderGraphToDisk").Invoke(null, new object[] { path, graph }) as string;
        if (string.IsNullOrEmpty(written))
            throw new InvalidOperationException("Could not write " + path);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
        if (shader == null)
            throw new InvalidOperationException(path + " did not import as a shader.");

        if (ShaderUtil.ShaderHasError(shader))
        {
            var messages = ShaderUtil.GetShaderMessages(shader);
            var details = string.Join("\n", messages.Select(message => message.message));
            throw new InvalidOperationException(path + " failed to compile.\n" + details);
        }

        return shader;
    }

    private static Material CreateMaterial(Shader shader, string path, string materialName)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { name = materialName };
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;
        material.SetColor("_FogColor", FogColor);
        material.SetFloat("_Opacity", FogOpacity);
        material.SetFloat("_Radius", ViewRadius / CoverSize);
        material.SetColor("_DeepColor", DeepColor);
        material.SetColor("_MistColor", MistColor);
        material.SetColor("_ShadowColor", ShadowColor);
        material.SetFloat("_Scroll", 0f);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }

    private static void UpdatePrefab(Material glowMaterial)
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        var visual = root.transform.Find("Visual").GetComponent<SpriteRenderer>();
        visual.color = new Color(0.55f, 0.64f, 0.72f, 1f);
        visual.sortingOrder = 10;
        visual.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

        var glowObject = root.transform.Find("RechercheGlow");
        var glowRenderer = glowObject.GetComponent<SpriteRenderer>();
        glowRenderer.sharedMaterial = glowMaterial;
        glowRenderer.color = Color.white;
        glowRenderer.sortingOrder = 1;

        var glow = glowObject.GetComponent<RechercheGlow>();
        var data = new SerializedObject(glow);
        data.FindProperty("fogColor").colorValue = FogColor;
        data.FindProperty("opacity").floatValue = FogOpacity;
        data.FindProperty("coverSize").floatValue = CoverSize;
        data.FindProperty("viewRadius").floatValue = ViewRadius;
        data.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static Camera UpdateScene(Material seaMaterial)
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var backdrop = GameObject.Find("Backdrop");
        var backdropRenderer = backdrop.GetComponent<SpriteRenderer>();
        backdropRenderer.sharedMaterial = seaMaterial;
        backdropRenderer.color = Color.white;
        backdropRenderer.sortingOrder = -10;
        if (backdrop.GetComponent<DeepSeaBackdrop>() == null)
            backdrop.AddComponent<DeepSeaBackdrop>();

        var camera = Camera.main;
        camera.backgroundColor = FogColor;
        var visual = GameObject.Find("MC").transform.Find("Visual").GetComponent<SpriteRenderer>();
        visual.color = new Color(0.55f, 0.64f, 0.72f, 1f);
        visual.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        EditorSceneManager.SaveScene(scene);
        return camera;
    }

    private static void VerifyVision(Camera camera)
    {
        var character = GameObject.Find("MC");
        var glow = character.transform.Find("RechercheGlow");
        InvokeInstance(glow.GetComponent<RechercheGlow>(), "ApplyAppearance");
        if (glow.parent != character.transform || glow.localPosition.sqrMagnitude > 0.0001f)
            throw new InvalidOperationException("The vision glow is not centered on MC.");
        if (glow.GetComponent<SpriteRenderer>().sortingOrder >= character.transform.Find("Visual").GetComponent<SpriteRenderer>().sortingOrder)
            throw new InvalidOperationException("The fog must render below the character.");

        var origin = Capture(camera, "deep-sea-origin.png");
        var inside = Sample(origin, camera, character.transform.position + new Vector3(2.2f, 0f, 0f));
        var outside = Sample(origin, camera, character.transform.position + new Vector3(0f, 5.5f, 0f));
        Debug.Log("Deep sea samples inside " + inside + " outside " + outside);
        if (Luminance(inside) < Luminance(outside) + 0.05f)
            throw new InvalidOperationException("The circle around MC is not clearer than the surrounding sea.");

        var start = character.transform.position;
        character.transform.position = start + new Vector3(3f, 0f, 0f);
        InvokeInstance(glow.GetComponent<RechercheGlow>(), "ApplyAppearance");
        if (Vector3.Distance(glow.position, character.transform.position) > 0.05f)
            throw new InvalidOperationException("The fog quad is not parented to MC.");

        var moved = Capture(camera, "deep-sea-moved.png");
        var leftBehind = Sample(moved, camera, start + new Vector3(-2f, 0f, 0f));
        var withCharacter = Sample(moved, camera, character.transform.position + new Vector3(1.5f, 0f, 0f));
        character.transform.position = start;
        EditorSceneManager.SaveScene(character.scene);
        Debug.Log("Moved samples left behind " + leftBehind + " with character " + withCharacter);
        if (Luminance(withCharacter) < Luminance(leftBehind) + 0.05f)
            throw new InvalidOperationException("The clear circle did not stay with MC.");
    }

    private static GameObject CreatePrefab(Material glowMaterial)
    {
        var capsule = LoadSprite(CapsulePath, "Capsule");
        var square = LoadSprite(SquarePath, "Square");
        var unlit = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("9dfc825aed78fcd4ba02077103263b40"));
        var input = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/InputSystem_Actions.inputactions");
        if (unlit == null || input == null)
            throw new InvalidOperationException("Missing the URP sprite unlit material or the project input actions.");

        var root = new GameObject("MC");
        var visualObject = new GameObject("Visual");
        visualObject.transform.SetParent(root.transform, false);
        var visual = visualObject.AddComponent<SpriteRenderer>();
        visual.sprite = capsule;
        visual.sharedMaterial = unlit;
        visual.color = new Color(0.68f, 0.8f, 0.88f, 1f);
        visual.sortingOrder = 10;

        var glowObject = new GameObject("RechercheGlow");
        glowObject.transform.SetParent(root.transform, false);
        var glowRenderer = glowObject.AddComponent<SpriteRenderer>();
        glowRenderer.sprite = square;
        glowRenderer.sharedMaterial = glowMaterial;
        glowRenderer.color = Color.white;
        glowRenderer.sortingOrder = 0;
        glowObject.AddComponent<RechercheGlow>();

        var swim = root.AddComponent<McPlaceholderSwim>();
        var swimData = new SerializedObject(swim);
        swimData.FindProperty("inputActions").objectReferenceValue = input;
        swimData.FindProperty("visual").objectReferenceValue = visual;
        swimData.FindProperty("swimSpeed").floatValue = 4f;
        swimData.ApplyModifiedPropertiesWithoutUndo();

        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath) ?? "Assets");
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }

    private static Camera CreateScene(GameObject prefab)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraObject = new GameObject("Main Camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 6f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.012f, 0.04f, 0.07f, 1f);
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 50f;
        cameraObject.tag = "MainCamera";
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<UniversalAdditionalCameraData>();

        var square = LoadSprite(SquarePath, "Square");
        var unlit = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("9dfc825aed78fcd4ba02077103263b40"));
        var backdrop = new GameObject("Backdrop");
        var backdropRenderer = backdrop.AddComponent<SpriteRenderer>();
        backdropRenderer.sprite = square;
        backdropRenderer.sharedMaterial = unlit;
        backdropRenderer.color = new Color(0.02f, 0.05f, 0.09f, 1f);
        backdropRenderer.sortingOrder = -10;
        backdrop.transform.localScale = new Vector3(40f, 24f, 1f);

        var character = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        var swimData = new SerializedObject(character.GetComponent<McPlaceholderSwim>());
        swimData.FindProperty("followCamera").objectReferenceValue = camera;
        swimData.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        return camera;
    }

    private static void Verify(Camera camera, GameObject prefab)
    {
        var character = GameObject.Find("MC");
        var visual = character.transform.Find("Visual").GetComponent<SpriteRenderer>();
        var glowObject = character.transform.Find("RechercheGlow");
        var glow = glowObject.GetComponent<RechercheGlow>();
        var glowRenderer = glowObject.GetComponent<SpriteRenderer>();
        var rootScale = character.transform.localScale;

        if (glowObject.parent != character.transform || visual.transform.parent != character.transform)
            throw new InvalidOperationException("Visual and RechercheGlow must both be direct children of MC.");
        if (glowRenderer.sortingOrder >= visual.sortingOrder)
            throw new InvalidOperationException("The glow must render below the character.");
        if (glowObject.localPosition.y >= 0f)
            throw new InvalidOperationException("The glow is not below the character.");

        var glowPosition = glowObject.position;
        InvokeInstance(character.GetComponent<McPlaceholderSwim>(), "ApplyFacing", Vector2.left);
        if (!visual.flipX)
            throw new InvalidOperationException("Facing left did not flip Visual.");
        if (character.transform.localScale != rootScale || glowObject.position != glowPosition)
            throw new InvalidOperationException("Turning flipped more than Visual.");

        InvokeInstance(character.GetComponent<McPlaceholderSwim>(), "ApplyFacing", Vector2.up);
        if (!visual.flipX)
            throw new InvalidOperationException("Vertical movement changed the facing direction.");

        InvokeInstance(character.GetComponent<McPlaceholderSwim>(), "ApplyFacing", Vector2.right);
        if (visual.flipX || glowObject.localScale.x < 0f)
            throw new InvalidOperationException("Facing right did not restore Visual without flipping the glow.");

        character.transform.position = new Vector3(2f, 1f, 0f);
        var expectedGlow = character.transform.position + new Vector3(0f, glowObject.localPosition.y, 0f);
        if (Vector3.Distance(glowObject.position, expectedGlow) > 0.001f)
            throw new InvalidOperationException("The glow did not stay under MC while moving.");

        var previousSprite = visual.sprite;
        visual.sprite = glowRenderer.sprite;
        if (glowObject.parent != character.transform || Vector3.Distance(glowObject.position, expectedGlow) > 0.001f)
            throw new InvalidOperationException("Replacing the character sprite detached the glow.");
        visual.sprite = previousSprite;
        character.transform.position = Vector3.zero;

        var cyan = Capture(camera, "recherche-glow-cyan.png");
        AssertGlowShape(camera, cyan, glowObject, "cyan");

        var glowData = new SerializedObject(glow);
        glowData.FindProperty("fogColor").colorValue = FogColor;
        glowData.FindProperty("opacity").floatValue = 1f;
        glowData.ApplyModifiedPropertiesWithoutUndo();
        InvokeInstance(glow, "ApplyAppearance");
        var red = Capture(camera, "recherche-glow-red.png");
        var redCenter = Sample(red, camera, glowObject.position);
        if (redCenter.r <= redCenter.g + 0.08f)
        {
            throw new InvalidOperationException(
                "Changing the glow color in the inspector did not change the rendered halo. Center " + redCenter);
        }

        glowData.FindProperty("fogColor").colorValue = FogColor;
        glowData.FindProperty("opacity").floatValue = FogOpacity;
        glowData.ApplyModifiedPropertiesWithoutUndo();
        InvokeInstance(glow, "ApplyAppearance");
        EditorSceneManager.SaveScene(camera.gameObject.scene);

        if (prefab.GetComponentInChildren<RechercheGlow>(true) == null)
            throw new InvalidOperationException("The MC prefab is missing RechercheGlow.");
    }

    private static void AssertGlowShape(Camera camera, Texture2D image, Transform glow, string label)
    {
        var center = Sample(image, camera, glow.position);
        var softEdge = Sample(image, camera, glow.TransformPoint(new Vector3(0.35f, 0f, 0f)));
        var squareCorner = Sample(image, camera, glow.TransformPoint(new Vector3(0.48f, 0.48f, 0f)));
        var aboveCharacter = Sample(image, camera, new Vector3(0f, 1.7f, 0f));
        var background = camera.backgroundColor;

        Debug.Log(
            label + " samples center " + center + " edge " + softEdge + " corner " + squareCorner + " above " + aboveCharacter);

        if (center.g < 0.25f || center.b < center.r + 0.15f)
            throw new InvalidOperationException("The glow under the character is not a visible cyan halo. Center " + center);
        if (Luminance(softEdge) > Luminance(center) - 0.08f)
            throw new InvalidOperationException("The glow does not fade from the center outward. Edge " + softEdge);
        if (Luminance(squareCorner) > Luminance(background) + 0.12f || Luminance(squareCorner) > Luminance(center) * 0.45f)
            throw new InvalidOperationException("The glow still has a bright square edge. Corner " + squareCorner);
        if (Luminance(aboveCharacter) > Luminance(background) + 0.12f)
            throw new InvalidOperationException("The glow is not staying below the character.");
    }

    private static Texture2D Capture(Camera camera, string fileName)
    {
        var texture = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
        texture.Create();
        var previous = camera.targetTexture;
        camera.targetTexture = texture;
        camera.Render();
        camera.Render();

        var image = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        RenderTexture.active = texture;
        image.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0);
        image.Apply();
        RenderTexture.active = null;
        camera.targetTexture = previous;

        Directory.CreateDirectory(PreviewDirectory);
        File.WriteAllBytes(Path.Combine(PreviewDirectory, fileName), image.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        return image;
    }

    private static Color Sample(Texture2D image, Camera camera, Vector3 world)
    {
        var viewport = camera.WorldToViewportPoint(world);
        int x = Mathf.Clamp(Mathf.RoundToInt(viewport.x * (image.width - 1)), 1, image.width - 2);
        int y = Mathf.Clamp(Mathf.RoundToInt(viewport.y * (image.height - 1)), 1, image.height - 2);
        var color = Color.clear;
        for (var offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (var offsetX = -1; offsetX <= 1; offsetX++)
                color += image.GetPixel(x + offsetX, y + offsetY);
        }

        return color / 9f;
    }

    private static bool HasShaderProperty(Shader shader, string propertyName)
    {
        for (var index = 0; index < shader.GetPropertyCount(); index++)
        {
            if (shader.GetPropertyName(index) == propertyName)
                return true;
        }

        return false;
    }

    private static float Luminance(Color color)
    {
        return color.r * 0.2126f + color.g * 0.7152f + color.b * 0.0722f;
    }

    private static ColorShaderProperty AddColor(Assembly shaderGraph, Type graphType, object graph, string name, Color color)
    {
        var property = (ColorShaderProperty)Activator.CreateInstance(typeof(ColorShaderProperty), true);
        property.displayName = name;
        property.value = color;
        Invoke(graphType, graph, "AddGraphInput", property, -1);
        return property;
    }

    private static Vector1ShaderProperty AddFloat(Assembly shaderGraph, Type graphType, object graph, string name, float value)
    {
        var property = (Vector1ShaderProperty)Activator.CreateInstance(typeof(Vector1ShaderProperty), true);
        property.displayName = name;
        property.value = value;
        Invoke(graphType, graph, "AddGraphInput", property, -1);
        return property;
    }

    private static void RequireNames(params object[] names)
    {
        for (var index = 0; index < names.Length; index += 2)
        {
            var property = (UnityEditor.ShaderGraph.Internal.ShaderInput)names[index];
            var expected = (string)names[index + 1];
            if (property.referenceName != expected)
                throw new InvalidOperationException("Expected " + expected + " but got " + property.referenceName);
        }
    }

    private static Array CreateSpriteBlocks(Assembly shaderGraph)
    {
        var blockType = shaderGraph.GetType("UnityEditor.ShaderGraph.BlockFieldDescriptor");
        var blockFields = shaderGraph.GetType("UnityEditor.ShaderGraph.BlockFields");
        var vertex = blockFields.GetNestedType("VertexDescription");
        var surface = blockFields.GetNestedType("SurfaceDescription");
        var blocks = Array.CreateInstance(blockType, 5);
        blocks.SetValue(vertex.GetField("Position").GetValue(null), 0);
        blocks.SetValue(vertex.GetField("Normal").GetValue(null), 1);
        blocks.SetValue(vertex.GetField("Tangent").GetValue(null), 2);
        blocks.SetValue(surface.GetField("BaseColor").GetValue(null), 3);
        blocks.SetValue(surface.GetField("Alpha").GetValue(null), 4);
        return blocks;
    }

    private static object CreateDefaultCategory(Assembly shaderGraph)
    {
        var categoryType = shaderGraph.GetType("UnityEditor.ShaderGraph.CategoryData");
        return categoryType.GetMethod("DefaultCategory").Invoke(null, new object[] { null });
    }

    private static object CreatePropertyNode(Assembly shaderGraph, Type graphType, object graph, UnityEditor.ShaderGraph.Internal.ShaderInput property)
    {
        var node = Activator.CreateInstance(shaderGraph.GetType("UnityEditor.ShaderGraph.PropertyNode"));
        node.GetType().GetProperty("property").SetValue(node, property);
        Invoke(graphType, graph, "AddNode", node, false);
        return node;
    }

    private static object CreateNode(Assembly shaderGraph, Type graphType, object graph, string typeName)
    {
        var node = Activator.CreateInstance(shaderGraph.GetType(typeName));
        var latestVersion = node.GetType().GetProperty("latestVersion");
        if (latestVersion != null)
            FindField(node.GetType(), "m_SGVersion")?.SetValue(node, latestVersion.GetValue(node));
        Invoke(graphType, graph, "AddNode", node, false);
        return node;
    }

    private static void Connect(
        Assembly shaderGraph,
        Type graphType,
        object graph,
        object fromNode,
        int fromSlot,
        object toNode,
        int toSlot)
    {
        var from = Slot(shaderGraph, fromNode, fromSlot);
        var to = Slot(shaderGraph, toNode, toSlot);
        var edge = graphType.GetMethod("Connect").Invoke(graph, new[] { from, to });
        if (edge == null)
        {
            throw new InvalidOperationException(
                "Could not connect " + fromNode.GetType().Name + ":" + fromSlot + " to " + toNode.GetType().Name + ":" + toSlot);
        }
    }

    private static object Slot(Assembly shaderGraph, object node, int slotId)
    {
        var materialSlot = shaderGraph.GetType("UnityEditor.ShaderGraph.MaterialSlot");
        var findSlot = node.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .First(method => method.Name == "FindSlot" && method.IsGenericMethodDefinition);
        var slot = findSlot.MakeGenericMethod(materialSlot).Invoke(node, new object[] { slotId });
        if (slot == null)
            throw new InvalidOperationException(node.GetType().Name + " has no slot " + slotId);
        return slot.GetType().GetProperty("slotReference").GetValue(slot);
    }

    private static object FindBlock(Assembly shaderGraph, Type graphType, object graph, string blockName)
    {
        var blockType = shaderGraph.GetType("UnityEditor.ShaderGraph.BlockNode");
        var getNodes = graphType.GetMethods().First(method =>
            method.Name == "GetNodes" && method.IsGenericMethodDefinition && method.GetParameters().Length == 0);
        var blocks = (IEnumerable)getNodes.MakeGenericMethod(blockType).Invoke(graph, null);
        foreach (var block in blocks)
        {
            if ((string)block.GetType().GetProperty("name").GetValue(block) == blockName)
                return block;
        }

        throw new InvalidOperationException("Missing shader block " + blockName);
    }

    private static void SetVector1(object node, float value)
    {
        node.GetType().GetField("m_Value", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(node, value);
        SetSlotFloat(node, 1, value);
    }

    private static void SetVector2(object node, Vector2 value)
    {
        node.GetType().GetField("m_Value", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(node, value);
        SetSlotFloat(node, 1, value.x);
        SetSlotFloat(node, 2, value.y);
    }

    private static void SetSlotFloat(object node, int slotId, float value)
    {
        var materialSlot = node.GetType().Assembly.GetType("UnityEditor.ShaderGraph.MaterialSlot");
        var findSlot = node.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .First(method => method.Name == "FindSlot" && method.IsGenericMethodDefinition);
        var slot = findSlot.MakeGenericMethod(materialSlot).Invoke(node, new object[] { slotId });
        slot.GetType().GetProperty("value").SetValue(slot, value);
    }

    private static void Place(object node, float x, float y)
    {
        var field = FindField(node.GetType(), "m_DrawState");
        if (field == null)
            return;

        var state = field.GetValue(node);
        object boxed = state;
        boxed.GetType().GetProperty("position").SetValue(boxed, new Rect(x, y, 208f, 130f));
        boxed.GetType().GetProperty("expanded").SetValue(boxed, false);
        field.SetValue(node, boxed);
    }

    private static FieldInfo FindField(Type type, string name)
    {
        while (type != null)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null)
                return field;
            type = type.BaseType;
        }

        return null;
    }

    private static void SetEnum(object target, string propertyName, Assembly assembly, string enumName, string value)
    {
        var enumType = assembly.GetType(enumName);
        target.GetType().GetProperty(propertyName).SetValue(target, Enum.Parse(enumType, value));
    }

    private static object Invoke(Type type, object instance, string methodName, params object[] arguments)
    {
        var method = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(candidate => candidate.Name == methodName && candidate.GetParameters().Length == arguments.Length)
            .OrderByDescending(candidate => candidate.IsPublic)
            .FirstOrDefault();
        if (method == null)
            throw new MissingMethodException(type.FullName, methodName);

        try
        {
            return method.Invoke(instance, arguments);
        }
        catch (TargetInvocationException exception)
        {
            throw exception.InnerException ?? exception;
        }
    }

    private static object InvokeInstance(object instance, string methodName, params object[] arguments)
    {
        return Invoke(instance.GetType(), instance, methodName, arguments);
    }

    private static Sprite LoadSprite(string path, string spriteName)
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (asset is Sprite sprite && sprite.name == spriteName)
                return sprite;
        }

        throw new FileNotFoundException("Could not load sprite " + spriteName + " from " + path);
    }
}
