using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class PlayerWalkSpriteImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/PlayerWalk/")) return;

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 220f;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = UnityEngine.FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
    }

    [InitializeOnLoadMethod]
    private static void ScheduleAnimatorCreation()
    {
        EditorApplication.delayCall += CreateAnimatorAssetsIfNeeded;
    }

    private static void CreateAnimatorAssetsIfNeeded()
    {
        const string folder = "Assets/Resources/PlayerWalk";
        const string idlePath = folder + "/PlayerIdle.anim";
        const string walkPath = folder + "/PlayerWalk.anim";
        const string controllerPath = folder + "/PlayerWalkController.controller";

        string[] spriteGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        System.Array.Sort(spriteGuids, (a, b) => string.CompareOrdinal(AssetDatabase.GUIDToAssetPath(a), AssetDatabase.GUIDToAssetPath(b)));
        Sprite[] sprites = new Sprite[spriteGuids.Length];
        for (int i = 0; i < spriteGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(spriteGuids[i]);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 220f;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        if (sprites.Length == 0 || sprites[0] == null) return;

        AnimationClip idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(idlePath);
        if (idle == null)
        {
            idle = CreateSpriteClip("PlayerIdle", new[] { sprites[0] }, 1f, false);
            AssetDatabase.CreateAsset(idle, idlePath);
        }

        AnimationClip walk = AssetDatabase.LoadAssetAtPath<AnimationClip>(walkPath);
        if (walk == null)
        {
            walk = CreateSpriteClip("PlayerWalk", sprites, 10f, true);
            AssetDatabase.CreateAsset(walk, walkPath);
        }

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idleState = machine.AddState("Idle");
            idleState.motion = idle;
            AnimatorState walkState = machine.AddState("Walk");
            walkState.motion = walk;
            machine.defaultState = idleState;
            AnimatorStateTransition toWalk = idleState.AddTransition(walkState);
            toWalk.hasExitTime = false;
            toWalk.duration = 0.05f;
            toWalk.AddCondition(AnimatorConditionMode.Greater, 0.01f, "Speed");
            AnimatorStateTransition toIdle = walkState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration = 0.05f;
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.01f, "Speed");
            AssetDatabase.SaveAssets();
        }
    }

    private static AnimationClip CreateSpriteClip(string clipName, Sprite[] sprites, float frameRate, bool loop)
    {
        AnimationClip clip = new AnimationClip { name = clipName, frameRate = frameRate };
        EditorCurveBinding binding = new EditorCurveBinding { type = typeof(SpriteRenderer), path = string.Empty, propertyName = "m_Sprite" };
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = i / frameRate, value = sprites[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        return clip;
    }
}
