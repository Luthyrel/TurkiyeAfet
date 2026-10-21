using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
internal static class QuaterniusPlayerSetup
{
    private const string CharacterPath = "Assets/Resources/PlayerCharacter/Models/PlayerMale.fbx";
    private const string CivilianCharacterPath = "Assets/Resources/PlayerCharacter/Models/CivilianMale.fbx";
    private const string HairPath = "Assets/Resources/PlayerCharacter/Models/Hair_Buzzed.fbx";
    private const string AnimationPath = "Assets/Resources/PlayerCharacter/Animations/UAL1_Standard.fbx";
    private const string ControllerPath = "Assets/Resources/PlayerCharacter/PlayerCharacterController.controller";
    private static bool setupQueued;
    private static bool setupRunning;

    static QuaterniusPlayerSetup()
    {
        EditorApplication.delayCall += QueueSetup;
    }

    [MenuItem("Türkiye Afet/Karakter Varlıklarını Hazırla")]
    private static void PrepareFromMenu()
    {
        setupQueued = false;
        QueueSetup();
    }

    internal static void QueueSetup()
    {
        if (setupQueued || setupRunning)
            return;

        setupQueued = true;
        EditorApplication.delayCall += ConfigureAssets;
    }

    private static void ConfigureAssets()
    {
        setupQueued = false;
        if (setupRunning)
            return;

        setupRunning = true;
        try
        {
            ConfigureAssetsNow();
        }
        finally
        {
            setupRunning = false;
        }
    }

    private static void ConfigureAssetsNow()
    {
        if (!File.Exists(CharacterPath) || !File.Exists(CivilianCharacterPath) || !File.Exists(AnimationPath))
            return;

        bool reimporting = ConfigureModel(CharacterPath, false)
            | ConfigureModel(CivilianCharacterPath, true)
            | ConfigureModel(AnimationPath, true)
            | (File.Exists(HairPath) && ConfigureModel(HairPath, false));
        if (reimporting)
        {
            EditorApplication.delayCall += QueueSetup;
            return;
        }

        BuildController();
    }

    private static bool ConfigureModel(string path, bool importAnimation)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
            return false;

        bool isHumanoidCharacter = path == CharacterPath || path == CivilianCharacterPath;
        HumanDescription humanDescription = importer.humanDescription;
        bool hasOldKenneyMapping = path == CivilianCharacterPath
            && humanDescription.skeleton.Any(bone => bone.name == "HipsCtrl" || bone.name == "characterMedium");
        bool needsCivilianMapping = path == CivilianCharacterPath
            && (hasOldKenneyMapping || !HasExpectedCivilianMapping(humanDescription));

        bool changed = importer.importAnimation != importAnimation
            || importer.optimizeGameObjects
            || importer.importCameras
            || importer.importLights
            || needsCivilianMapping;

        if (importAnimation)
            changed |= importer.animationType != ModelImporterAnimationType.Human
                || importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel;
        else if (isHumanoidCharacter)
            changed |= importer.animationType != ModelImporterAnimationType.Human
                || importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel;

        importer.importAnimation = importAnimation;
        importer.optimizeGameObjects = false;
        importer.importCameras = false;
        importer.importLights = false;
        if (importAnimation || isHumanoidCharacter)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        }

        if (importAnimation)
        {
            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
                clips = importer.defaultClipAnimations;
            for (int i = 0; i < clips.Length; i++)
            {
                string clipName = clips[i].name.ToLowerInvariant();
                bool shouldLoop = clipName.Contains("idle_loop")
                    || clipName.Contains("walk_loop")
                    || clipName.Contains("jog_fwd_loop")
                    || clipName.Contains("crouch_idle_loop")
                    || clipName.Contains("crouch_fwd_loop")
                    || clipName.Contains("man_idle")
                    || clipName.Contains("man_walk")
                    || clipName.Contains("man_run");
                if (shouldLoop && !clips[i].loopTime)
                {
                    clips[i].loopTime = true;
                    changed = true;
                }
            }

            if (changed)
                importer.clipAnimations = clips;
        }

        if (needsCivilianMapping)
        {
            // The source has a wrapper Bone, then Body (the actual pelvis), with
            // foot bones outside the leg chains. Map the real pelvis and ankle-end
            // transforms so Unity's required Humanoid hierarchy remains valid.
            if (!TryBuildCivilianHumanoidDescription(path, ref humanDescription))
            {
                Debug.LogError("Sivil karakter Humanoid eşlemesi oluşturulamadı. FBX iskeletindeki gerekli kemikler bulunamadı.");
                return false;
            }
            importer.humanDescription = humanDescription;
            importer.autoGenerateAvatarMappingIfUnspecified = false;
            changed = true;
        }
        else if (path == CivilianCharacterPath
            && (humanDescription.human == null || humanDescription.human.Length == 0))
        {
            if (!TryBuildCivilianHumanoidDescription(path, ref humanDescription))
            {
                Debug.LogError("Sivil karakter Humanoid eşlemesi oluşturulamadı. FBX iskeletindeki gerekli kemikler bulunamadı.");
                return false;
            }
            importer.humanDescription = humanDescription;
            importer.autoGenerateAvatarMappingIfUnspecified = false;
            changed = true;
        }

        if (!changed)
            return false;

        importer.SaveAndReimport();
        return true;
    }

    private static bool TryBuildCivilianHumanoidDescription(string path, ref HumanDescription description)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (model == null)
            return false;

        Transform[] transforms = model.GetComponentsInChildren<Transform>(true);
        var byName = transforms
            .GroupBy(transform => transform.name)
            .ToDictionary(group => group.Key, group => group.First());

        // Actual names and parent paths verified in the imported FBX metadata:
        // Bone > Body > Hips > Abdomen > Torso. Legs branch from Body.
        string[,] required =
        {
            { "Hips", "Body" }, { "Spine", "Hips" }, { "Chest", "Abdomen" },
            { "UpperChest", "Torso" }, { "Neck", "Neck" }, { "Head", "Head" },
            { "LeftShoulder", "Shoulder.L" }, { "LeftUpperArm", "UpperArm.L" },
            { "LeftLowerArm", "LowerArm.L" }, { "LeftHand", "Palm.L" },
            { "RightShoulder", "Shoulder.R" }, { "RightUpperArm", "UpperArm.R" },
            { "RightLowerArm", "LowerArm.R" }, { "RightHand", "Palm.R" },
            { "LeftUpperLeg", "UpperLeg.L" }, { "LeftLowerLeg", "LowerLeg.L" },
            { "LeftFoot", "LowerLeg.L_end" }, { "RightUpperLeg", "UpperLeg.R" },
            { "RightLowerLeg", "LowerLeg.R" }, { "RightFoot", "LowerLeg.R_end" }
        };

        var humanBones = new HumanBone[required.GetLength(0)];
        for (int i = 0; i < required.GetLength(0); i++)
        {
            string humanName = required[i, 0];
            string boneName = required[i, 1];
            if (!byName.ContainsKey(boneName))
            {
                Debug.LogError("Sivil karakter iskeletinde gerekli kemik yok: " + boneName);
                return false;
            }

            humanBones[i] = new HumanBone
            {
                humanName = humanName,
                boneName = boneName,
                limit = new HumanLimit { useDefaultValues = true }
            };
        }

        var skeleton = new SkeletonBone[transforms.Length];
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform bone = transforms[i];
            skeleton[i] = new SkeletonBone
            {
                name = bone.name,
                position = bone.localPosition,
                rotation = bone.localRotation,
                scale = bone.localScale
            };
        }

        description.human = humanBones;
        description.skeleton = skeleton;
        return true;
    }

    private static bool HasExpectedCivilianMapping(HumanDescription description)
    {
        if (description.human == null)
            return false;

        bool Maps(string humanName, string boneName) => description.human.Any(bone =>
            bone.humanName == humanName && bone.boneName == boneName);

        return Maps("Hips", "Body")
            && Maps("Spine", "Hips")
            && Maps("Chest", "Abdomen")
            && Maps("LeftFoot", "LowerLeg.L_end")
            && Maps("RightFoot", "LowerLeg.R_end");
    }

    private static void BuildController()
    {
        AnimationClip[] characterClips = LoadClips(CivilianCharacterPath);
        AnimationClip[] libraryClips = LoadClips(AnimationPath);

        Debug.Log("UAL ANİMASYON LİSTESİ:\n" +
            string.Join("\n", libraryClips.Select(clip => clip.name)));

        // Importer settings can be saved before Unity has materialized FBX
        // sub-assets. Force a synchronous import once the first clip lookup is
        // empty, then query the imported clips again before giving up.
        if (characterClips.Length == 0 || libraryClips.Length == 0)
        {
            AssetDatabase.ImportAsset(CivilianCharacterPath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(AnimationPath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            characterClips = LoadClips(CivilianCharacterPath);
            libraryClips = LoadClips(AnimationPath);
        }

        if (characterClips.Length == 0 || libraryClips.Length == 0)
        {
            Debug.LogError("FBX içe aktarma tamamlandıktan sonra animasyon klipleri bulunamadı. "
                + "CivilianMale klip sayısı: " + characterClips.Length
                + ", UAL klip sayısı: " + libraryClips.Length
                + ". Model Import Settings > Rig/Animation ayarlarını kontrol edin.");
            return;
        }

        AnimationClip[] LoadClips(string path) => AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<AnimationClip>()
            .Where(clip => clip != null && !clip.name.StartsWith("__preview__"))
            .ToArray();

        AnimationClip idle = FindClip(characterClips, "man_idle")
            ?? FindClip(libraryClips, "idle_loop", "idle");
        AnimationClip walk = FindClip(characterClips, "man_walk")
            ?? FindClip(libraryClips, "walk_loop", "walk");
        AnimationClip jog = FindClip(characterClips, "man_run")
            ?? FindClip(libraryClips, "jog_fwd_loop", "jog");
        AnimationClip crouch = FindClip(libraryClips, "crouch_idle_loop", "crouch_idle", "fixing_kneeling");
        AnimationClip crouchWalk = FindClip(libraryClips, "crouch_fwd_loop", "crouch_fwd");
        AnimationClip tableProtection = FindClip(characterClips.Concat(libraryClips).ToArray(),
            "protect", "cover", "guard", "hold");
        if (idle == null || walk == null || jog == null || crouch == null)
        {
            Debug.LogError("Oyuncu animasyon denetleyicisi kurulamadı. Bulunan klipler: "
                + string.Join(", ", characterClips.Concat(libraryClips).Select(clip => clip.name)));
            return;
        }

        string directory = Path.GetDirectoryName(ControllerPath);
        if (!AssetDatabase.IsValidFolder(directory))
            Directory.CreateDirectory(directory);

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        AnimatorControllerLayer[] layers = controller.layers;
        layers[0].iKPass = false;
        controller.layers = layers;

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idleState = SetState(machine, "Idle", idle, new Vector3(260f, 40f, 0f));
        machine.defaultState = idleState;
        SetState(machine, "Walk", walk, new Vector3(480f, 40f, 0f));
        SetState(machine, "Run", jog, new Vector3(700f, 40f, 0f));
        SetState(machine, "Crouch", crouch, new Vector3(480f, 180f, 0f));
        SetState(machine, "CrouchWalk", crouchWalk != null ? crouchWalk : crouch, new Vector3(700f, 180f, 0f));
        if (tableProtection != null)
            SetState(machine, "Hold", tableProtection, new Vector3(920f, 180f, 0f));
        else
        {
            AnimatorState existingHold = machine.states
                .Select(child => child.state)
                .FirstOrDefault(state => state.name == "Hold");
            if (existingHold != null)
                machine.RemoveState(existingHold);
        }
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log("Oyuncu animasyon denetleyicisi hazırlandı. Kullanılan klipler: "
            + idle.name + ", " + walk.name + ", " + jog.name + ", " + crouch.name
            + (crouchWalk != null ? ", " + crouchWalk.name : string.Empty)
            + (tableProtection != null ? ", masa korunma: " + tableProtection.name
                : ". Kaynaklarda masa korunma/tutunma klibi bulunamadı; E animasyonu kurulmadı."));
    }

    private static AnimatorState SetState(AnimatorStateMachine machine, string name, Motion motion, Vector3 position)
    {
        AnimatorState state = machine.states
            .Select(child => child.state)
            .FirstOrDefault(existing => existing.name == name);
        if (state == null)
            state = machine.AddState(name, position);

        state.motion = motion;
        state.writeDefaultValues = true;
        return state;
    }

    private static AnimationClip FindClip(AnimationClip[] clips, params string[] candidates)
    {
        foreach (string candidate in candidates)
        {
            AnimationClip match = clips.FirstOrDefault(clip => clip.name.IndexOf(candidate, System.StringComparison.OrdinalIgnoreCase) >= 0);
            if (match != null)
                return match;
        }
        return null;
    }
}

// SaveAndReimport can finish after the initial delayed setup pass. Re-queue the
// setup when one of the source FBXs actually completes import, so controller and
// Avatar creation do not stop at the transient "not imported yet" state.
internal sealed class QuaterniusPlayerImportPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        bool relevantImport = importedAssets.Any(path =>
            path == "Assets/Resources/PlayerCharacter/Models/CivilianMale.fbx"
            || path == "Assets/Resources/PlayerCharacter/Animations/UAL1_Standard.fbx"
            || path == "Assets/Resources/PlayerCharacter/Models/PlayerMale.fbx");

        if (relevantImport)
            QuaterniusPlayerSetup.QueueSetup();
    }
}
