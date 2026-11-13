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
    private const string PrefabPath = "Assets/Player/MC.prefab";
    private const string ScenePath = "Assets/Scenes/RechercheUnderwaterTest.unity";
    private const string CapsulePath = "Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Capsule.png";
    private const string SquarePath = "Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Square.png";
    private const string PreviewDirectory = "Logs";

    private static readonly Color GlowColor = new Color(0.25f, 0.85f, 1f, 1f);
    private const float GlowOpacity = 0.85f;
    private static readonly Vector2 GlowSize = new Vector2(3.2f, 1.2f);
    private const float GlowOffset = 1.15f;

    [MenuItem("Naucternal/Build Recherche Glow")]
    public static void Build()
    {
        var shader = CreateShaderGraph();
        var material = CreateMaterial(shader);
        var prefab = CreatePrefab(material);
        var sceneCamera = CreateScene(prefab);
        Verify(sceneCamera, prefab);
        Debug.Log("Recherche glow shader, prefab, and underwater test scene are ready.");
    }

    private static Shader CreateShaderGraph()
    {
        var shaderGraph = Assembly.Load("Unity.ShaderGraph.Editor");
        var urp = Assembly.Load("Unity.RenderPipelines.Universal.Editor");
        var graphType = shaderGraph.GetType("UnityEditor.ShaderGraph.GraphData");
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

        var colorProperty = CreateColorProperty();
        var opacityProperty = CreateOpacityProperty();
        Invoke(graphType, graph, "AddGraphInput", colorProperty, -1);
        Invoke(graphType, graph, "AddGraphInput", opacityProperty, -1);

        if (colorProperty.referenceName != "_GlowColor" || opacityProperty.referenceName != "_Opacity")
        {
            throw new InvalidOperationException(
                "Unexpected shader property names: " + colorProperty.referenceName + ", " + opacityProperty.referenceName);
        }

        var colorNode = CreatePropertyNode(shaderGraph, graphType, graph, colorProperty);
        var opacityNode = CreatePropertyNode(shaderGraph, graphType, graph, opacityProperty);
        var uvNode = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.UVNode");
        var splitNode = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.SplitNode");
        var combineNode = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.CombineNode");
        var centerNode = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.Vector2Node");
        var subtractNode = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.SubtractNode");
        var lengthNode = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.LengthNode");
        var innerNode = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.Vector1Node");
        var outerNode = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.Vector1Node");
        var smoothstepNode = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.SmoothstepNode");
        var oneMinusNode = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.OneMinusNode");
        var multiplyNode = CreateNode(shaderGraph, graphType, graph, "UnityEditor.ShaderGraph.MultiplyNode");

        SetVector2(centerNode, new Vector2(0.5f, 0.5f));
        SetVector1(innerNode, 0.02f);
        SetVector1(outerNode, 0.48f);

        // UV.xy - 0.5 keeps the distance circular. OneMinus flips smoothstep so the center stays opaque
        // and the falloff reaches zero before the square border.
        Connect(shaderGraph, graphType, graph, uvNode, 0, splitNode, 0);
        Connect(shaderGraph, graphType, graph, splitNode, 1, combineNode, 0);
        Connect(shaderGraph, graphType, graph, splitNode, 2, combineNode, 1);
        Connect(shaderGraph, graphType, graph, combineNode, 6, subtractNode, 0);
        Connect(shaderGraph, graphType, graph, centerNode, 0, subtractNode, 1);
        Connect(shaderGraph, graphType, graph, subtractNode, 2, lengthNode, 0);
        Connect(shaderGraph, graphType, graph, innerNode, 0, smoothstepNode, 0);
        Connect(shaderGraph, graphType, graph, outerNode, 0, smoothstepNode, 1);
        Connect(shaderGraph, graphType, graph, lengthNode, 1, smoothstepNode, 2);
        Connect(shaderGraph, graphType, graph, smoothstepNode, 3, oneMinusNode, 0);
        Connect(shaderGraph, graphType, graph, oneMinusNode, 1, multiplyNode, 0);
        Connect(shaderGraph, graphType, graph, opacityNode, 0, multiplyNode, 1);

        var baseColor = FindBlock(shaderGraph, graphType, graph, "SurfaceDescription.BaseColor");
        var alpha = FindBlock(shaderGraph, graphType, graph, "SurfaceDescription.Alpha");
        Connect(shaderGraph, graphType, graph, colorNode, 0, baseColor, 0);
        Connect(shaderGraph, graphType, graph, multiplyNode, 2, alpha, 0);
        Invoke(graphType, graph, "ValidateGraph");

        Place(uvNode, -1400f, 200f);
        Place(splitNode, -1160f, 200f);
        Place(combineNode, -920f, 200f);
        Place(centerNode, -920f, 40f);
        Place(subtractNode, -680f, 160f);
        Place(lengthNode, -440f, 160f);
        Place(innerNode, -440f, 360f);
        Place(outerNode, -440f, 480f);
        Place(smoothstepNode, -180f, 240f);
        Place(oneMinusNode, 80f, 240f);
        Place(multiplyNode, 320f, 240f);
        Place(opacityNode, 80f, 40f);
        Place(colorNode, 320f, -80f);

        Directory.CreateDirectory(Path.GetDirectoryName(ShaderPath) ?? "Assets");
        var fileUtilities = shaderGraph.GetType("UnityEditor.ShaderGraph.FileUtilities");
        var written = fileUtilities.GetMethod("WriteShaderGraphToDisk").Invoke(null, new object[] { ShaderPath, graph }) as string;
        if (string.IsNullOrEmpty(written))
            throw new InvalidOperationException("Could not write the shader graph.");

        AssetDatabase.ImportAsset(ShaderPath, ImportAssetOptions.ForceUpdate);
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        if (shader == null)
            throw new InvalidOperationException("The shader graph did not import as a shader.");

        if (ShaderUtil.ShaderHasError(shader))
        {
            var messages = ShaderUtil.GetShaderMessages(shader);
            var details = string.Join("\n", messages.Select(message => message.message));
            throw new InvalidOperationException("Recherche glow shader failed to compile.\n" + details);
        }

        return shader;
    }

    private static Material CreateMaterial(Shader shader)
    {
        if (!HasShaderProperty(shader, "_GlowColor") || !HasShaderProperty(shader, "_Opacity"))
            throw new InvalidOperationException("Recherche glow shader is missing _GlowColor or _Opacity.");

        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "RechercheGlow" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        material.shader = shader;
        material.SetColor("_GlowColor", GlowColor);
        material.SetFloat("_Opacity", GlowOpacity);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
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
        glowData.FindProperty("glowColor").colorValue = new Color(1f, 0.05f, 0.05f, 1f);
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

        glowData.FindProperty("glowColor").colorValue = GlowColor;
        glowData.FindProperty("opacity").floatValue = GlowOpacity;
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

    private static ColorShaderProperty CreateColorProperty()
    {
        var property = (ColorShaderProperty)Activator.CreateInstance(typeof(ColorShaderProperty), true);
        property.displayName = "GlowColor";
        property.value = GlowColor;
        return property;
    }

    private static Vector1ShaderProperty CreateOpacityProperty()
    {
        var property = (Vector1ShaderProperty)Activator.CreateInstance(typeof(Vector1ShaderProperty), true);
        property.displayName = "Opacity";
        property.value = GlowOpacity;
        return property;
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
