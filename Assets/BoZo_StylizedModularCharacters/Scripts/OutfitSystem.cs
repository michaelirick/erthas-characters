using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Bozo.ModularCharacters
{
    public class OutfitSystem : MonoBehaviour
    {
        //[Header("Save Data")]
        public CharacterObject characterData;
        private CharacterObject _characterData;
        public string SaveID;

        //[Header("Dependencies")]
        [SerializeField] SkinnedMeshRenderer CharacterBody;

        //Height
        public bool muteHeightChange { get; private set; }
        public bool mutebodyMods { get; private set; }
        public bool isbindPose { get; private set; }
        public Dictionary<GameObject, float> heightSources = new Dictionary<GameObject, float>();
        public float height { get; private set; }

        //Animation
        public Animator animator
        {
            get
            {
                if (_animator == null)
                {
                    _animator = GetComponentInChildren<Animator>();
                    if (_animator == null) { _animator = GetComponentInParent<Animator>(); }
                }

                return _animator;
            }
            private set
            { _animator = value; }
        }
        private Animator _animator;
        AnimatorStateInfo stateInfo;
        RuntimeAnimatorController control;
        Avatar avatar;
        float progress;
        int stateHash;
        bool wasEnabled;
        public float stance { get; private set; }


        //Bones
        public Dictionary<string, Transform> boneMap = new Dictionary<string, Transform>();
        public Dictionary<string, Transform> originalBoneMap = new Dictionary<string, Transform>();
        public Dictionary<string, BonePose> bindPose = new();

        //Outfits
        public List<Outfit> _Outfits = new List<Outfit>();
        public Dictionary<string, Outfit> Outfits = new Dictionary<string, Outfit>();
        public Dictionary<OutfitType, List<Outfit>> hiddenTypes = new Dictionary<OutfitType, List<Outfit>>();

        //Shapes
        private Dictionary<string, int> shapes = new Dictionary<string, int>();
        private Dictionary<string, float> shapesValues = new Dictionary<string, float>();
        private Dictionary<string, int> bodyShapes = new Dictionary<string, int>();
        private Dictionary<string, float> bodyShapesValues = new Dictionary<string, float>();
        private Dictionary<string, int> faceShapes = new Dictionary<string, int>();
        private Dictionary<string, float> faceShapesValues = new Dictionary<string, float>();
        private Dictionary<string, int> tagShapes = new Dictionary<string, int>();
        public Dictionary<string, BodyShapeModifier> bodyModifiers = new Dictionary<string, BodyShapeModifier>();
        private List<string> tags = new List<string>();


        //Events
        public static UnityAction<OutfitSystem> OnCreated;
        public static UnityAction<OutfitSystem> OnDestroyed;
        public UnityAction<Outfit> OnOutfitChanged;
        public UnityAction<Outfit> OnOutfitColorChanged;
        public UnityAction<SkinnedMeshRenderer> OnRigChanged;
        public UnityAction<string, float> OnShapeChanged;
        public UnityAction<List<string>> OnTagsChanged;
        public UnityAction OnCharacterLoaded;

        //UnityEvents
        public UnityEvent<Outfit> OnOutfitAdded;
        public UnityEvent OnOutfitRemoved;
        public UnityEvent OnMergeStarted;
        public UnityEvent OnMergeComplete;
        public UnityEvent OnColorChanged;
        public UnityEvent OnLoadStarted;
        public UnityEvent OnLoadComplete;


        //Textures and Materials
        private Dictionary<string, RenderTexture> textures = new Dictionary<string, RenderTexture>();
        private RenderTexture copyTexture;
        private RenderTexture normalCopyTexture;
        private Material masterMaterial;
        public Material copyMaterial;
        private int textureSize { get { return (int)_textureSize; } }
        public TextureSize _textureSize = TextureSize._2048;

        //Sockets
        [SerializeField] Transform[] sockets;
        private Dictionary<string, Transform> socketMap = new Dictionary<string, Transform>();

        public enum TextureSize
        {
            _512 = 512,
            _1024 = 1024,
            _2048 = 2048,
            _4096 = 4096
        }


        // Merged Properties
        public string prefabName;
        public Material mergeMaterial;

        public CharacterData data;
        private Dictionary<string, OutfitData> outfitData;
        public Dictionary<string, Texture> customMaps = new Dictionary<string, Texture>();
        public MergedMaterialData[] materialData;
        public List<RenderTexture> renderTextures = new List<RenderTexture>();

        //Mesh
        public Mesh combinedMesh;

        public enum LoadMode { OnStartAndOnValidate, OnStart, Manual }
        public LoadMode loadMode;

        public bool async;

        public bool isloading;
        public bool isUpdatingTexture;
        [Tooltip("Removes all outfits proxies from under this system. Turning this on prevents further customization after inital start up")]
        public bool cleanAfterMerge;
        [Tooltip("Removes all BoZo Scripts when you save the character as a prefab. WARNING: Some effects may require these script to maintain your characters appearance")]
        public bool cleanScriptsAfterPrefab;
        public bool isMerging;
        public bool isStatic;
        public bool initalized { get; private set; }


#if MAGICACLOTH2
        //MagicaCloth
        private MagicaCloth2.ColliderComponent[] ClothColliders;
#endif


        private void OnValidate()
        {
            if (Application.isPlaying && gameObject.scene.isLoaded && loadMode == LoadMode.OnStartAndOnValidate && gameObject.activeSelf)
            {
                Invoke("LoadFromObject", 0f);
            }

        }

        private void Awake()
        {
            Init();
        }

        private void Start()
        {
            InitClothColliders();
            OnCreated?.Invoke(this);
            if (loadMode == LoadMode.OnStart || loadMode == LoadMode.OnStartAndOnValidate)
            {
                LoadFromObject();
            }
        }

        private void OnDestroy()
        {

            foreach (var item in renderTextures)
            {
                item.Release();
            }
            if(!isStatic)Destroy(combinedMesh);
            if (!isStatic) Destroy(masterMaterial);
            if(copyTexture) copyTexture.Release();

            if (initalized && !isStatic)
            {
                foreach (var item in textures.Values)
                { if (item == null) continue; item.Release(); Destroy(item); }
                if (copyMaterial) Destroy(copyMaterial);
            }

            OnDestroyed?.Invoke(this);
        }

        #region Initalizers

        public void Init()
        {
            if (initalized) return;
            if (isStatic) return;
            if (CharacterBody == null)
            {
                Debug.LogWarning("Outfit System does not have a Rig assigned please assign one to prevent this warning", gameObject);
                Debug.LogWarning("Attempting auto rig assignment...");
                var skinnedMeshes = GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var item in skinnedMeshes)
                {
                    if (item.name == "BMAC_Body")
                    {
                        CharacterBody = item;
                        Debug.Log("Rig Found Successfully!");
                        break;
                    }
                }
                Debug.LogError("Search Failed. Please Assign Mannually", gameObject);
                return;
            }

            InitAttachPoints();
            InitBoneMap();
            InitBodyShapes();
            InitBodyMods();
            InitMaterial();
            InitTextures();
            InitBindPose();

            initalized = true;
        }

        private void InitAttachPoints()
        {
            foreach (var item in sockets)
            {
                if (item == null) continue;
                socketMap[item.name] = item;
            }
        }

        private void InitBoneMap()
        {
            boneMap.Clear();
            foreach (Transform bone in CharacterBody.bones)
            {
                if (boneMap.ContainsKey(bone.name) == false)
                {
                    boneMap.Add(bone.name, bone);
                }
            }

            originalBoneMap = new Dictionary<string, Transform>(boneMap);
        }

        public void InitTextures()
        {
            if (isStatic) return;

            copyMaterial = new Material(Shader.Find("Hidden/TextureLayerer"));
            copyTexture = new RenderTexture(textureSize, textureSize, 0);
            copyTexture.wrapMode = TextureWrapMode.Repeat;

            var mrt = new RenderTexture(textureSize, textureSize, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB);
            mrt.name = "MainTexture";
            masterMaterial.SetTexture("mainTexture", mrt);
            masterMaterial.mainTexture = mrt;
            textures["mainTexture"] = mrt;
            mrt.wrapMode = TextureWrapMode.Repeat;

            foreach (var data in materialData)
            {
                if (data.toMateiralProperty == "mainTexture") continue;
                var p = data.toMateiralProperty;
                RenderTexture rt;
                if (p.Contains("normal") || p.Contains("Normal") || p.Contains("bump") || p.Contains("Bump"))
                {
                    var desc = new RenderTextureDescriptor(textureSize, textureSize)
                    {
                        graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm,
                        depthBufferBits = 0,
                        msaaSamples = 1,
                        sRGB = false,
                        useMipMap = false,
                        autoGenerateMips = false,
                    };
                    rt = new RenderTexture(desc);
                    rt.wrapMode = TextureWrapMode.Repeat;
                }
                else
                {
                    rt = new RenderTexture(textureSize, textureSize, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB);
                    rt.wrapMode = TextureWrapMode.Repeat;
                }

                rt.name = data.toMateiralProperty;
                rt.Create();

                textures[data.toMateiralProperty] = rt;
                masterMaterial.SetTexture(data.toMateiralProperty, rt);

            }
        }

        private void InitMaterial()
        {
            if (mergeMaterial != null)
            {
                masterMaterial = new Material(mergeMaterial);
                CharacterBody.sharedMaterial = masterMaterial;
            }
            else
            {
                var matID = "";
                if (characterData == null) matID = "BSMC_StylizedShader";
                if (matID == "") matID = "BSMC_StylizedShader";
                var matPackage = Resources.Load<MaterialPackage>(matID);
                masterMaterial = new Material(matPackage.material);
                materialData = matPackage.materialData;
            }

                CharacterBody.sharedMaterial = masterMaterial;
        }

        private void InitBodyShapes()
        {
            var body = GetOutfit("Body");
            var head = GetOutfit("Head");

            shapes.Clear();
            bodyShapes.Clear();
            faceShapes.Clear();
            tagShapes.Clear();

            var newShapes = new Dictionary<string, int>();

            if (body != null)
            {
                var blendShapeCount = body.skinnedRenderer.sharedMesh.blendShapeCount;

                for (int i = 0; i < blendShapeCount; i++)
                {
                    var blendName = body.skinnedRenderer.sharedMesh.GetBlendShapeName(i);

                    int dot = blendName.IndexOf('.');
                    if (dot >= 0) blendName = blendName.Substring(dot + 1);

                    int us = blendName.IndexOf('_');
                    if (us < 0) continue;

                    else if (string.CompareOrdinal(blendName, 0, "Shape_", 0, 6) == 0)
                    {
                        bodyShapes.Add(blendName.Substring(6), i);
                    }
                }
            }

            if(head != null)
            {
                var blendShapeCount = head.skinnedRenderer.sharedMesh.blendShapeCount;

                for (int i = 0; i < blendShapeCount; i++)
                {
                    var blendName = head.skinnedRenderer.sharedMesh.GetBlendShapeName(i);

                    int dot = blendName.IndexOf('.');
                    if (dot >= 0) blendName = blendName.Substring(dot + 1);

                    int us = blendName.IndexOf('_');
                    if (us < 0) continue;

                    else if (string.CompareOrdinal(blendName, 0, "Shape_", 0, 6) == 0)
                    {
                        faceShapes.Add(blendName.Substring(6), i);
                    }
                }
            }

            if (CharacterBody != null)
            {
                var blendShapeCount = CharacterBody.sharedMesh.blendShapeCount;             

                for (int i = 0; i < blendShapeCount; i++)
                {
                    var blendName = CharacterBody.sharedMesh.GetBlendShapeName(i);

                    int dot = blendName.IndexOf('.');
                    if (dot >= 0) blendName = blendName.Substring(dot + 1);

                    int us = blendName.IndexOf('_');
                    if (us < 0) continue;

                    else if (string.CompareOrdinal(blendName, 0, "Shape_", 0, 6) == 0)
                    {
                        var bsName = blendName.Substring(6);
                        shapes.Add(bsName, i);
                        if(!shapesValues.ContainsKey(bsName)) shapesValues.Add(bsName, 0);
                    }
                }
            }
        }

        public void InitBodyMods()
        {
            var bodyMods = new List<BodyShapeModifier>(CharacterBody.transform.parent.GetComponentsInChildren<BodyShapeModifier>(true));
            bodyModifiers.Clear();
            for (int i = 0; i < bodyMods.Count; i++)
            {
               bodyModifiers[bodyMods[i].name] = bodyMods[i];
            }
        }


        public void InitBindPose()
        {
            bindPose.Clear();

            foreach (Transform bone in boneMap.Values)
            {
                bindPose[bone.name] = new BonePose
                {
                    localPosition = bone.localPosition,
                    localRotation = bone.localRotation,
                    localScale = bone.localScale
                };
            }
        }

        public void RestoreBindPose()
        {
            foreach (var pair in bindPose)
            {
                if (string.IsNullOrEmpty(pair.Key))
                    continue;

                boneMap[pair.Key].localPosition = pair.Value.localPosition;
                boneMap[pair.Key].localRotation = pair.Value.localRotation;
                boneMap[pair.Key].localScale = pair.Value.localScale;
            }
        }
        private void InitClothColliders()
        {
#if MAGICACLOTH2
            ClothColliders = GetComponentsInChildren<MagicaCloth2.ColliderComponent>();
#endif      
        }
        #endregion


        #region Saving and Loading


        //Object Loading--------------------------------------------------
        public void LoadFromObject(CharacterObject saveData)
        {
            if (isloading) return;
            characterData = saveData;
            LoadFromObject();
        }

        [ContextMenu("Load")]
        public void LoadFromObject()
        {
            if (isloading) return;
            if (characterData)
            {
                if (_characterData != characterData)
                {
                    _characterData = characterData;
                    LoadCharacter(characterData.GetCharacterData());
                }
            }
        }
        //--------------------------------------------------------------

        //ID Loading------------------------------------------------------
        [ContextMenu("LoadByID")]
        public void LoadFromID()
        {
            LoadFromID(SaveID);
        }

        public void LoadFromID(string saveName)
        {
            if (string.IsNullOrEmpty(saveName)) return;
            SaveID = saveName;

            var data = BMAC_SaveSystem.GetDataFromID(SaveID);
            if (data == null) return;

            LoadCharacter(data);
        }
        //--------------------------------------------------------------

        public async Task LoadCharacter(CharacterData data)
        {
            CharacterBody.enabled = false;
            OnLoadStarted.Invoke();
            if(!isStatic) await BMAC_SaveSystem.LoadCharacter(this, data, async);
            //OnLoadComplete.Invoke();
        }

        public async Task LoadOutfit(CharacterData data, bool loadShader, bool loadBodyShapes, List<string> exclusionList)
        {
            CharacterBody.enabled = false;
            OnLoadStarted.Invoke();
            if (!isStatic) await BMAC_SaveSystem.LoadCharacter(this, data, async, loadShader, loadBodyShapes, exclusionList);
            //OnLoadComplete.Invoke();
        }

        [ContextMenu("SaveToObject")]
        public void SaveToObject()
        {
            if (!characterData)
            {
                Debug.LogWarning("Character Data Field is empty. Please provide a BSMC_CharacterObject to " + transform.name);
                return;
            }
            BMAC_SaveSystem.SaveCharacter(this, characterData.GetCharacterData().characterName, characterData.GetCharacterIcon());
        }


        [ContextMenu("SaveByID")]
        public void SaveByID()
        {
            SaveByID(SaveID);
        }

        public void SaveByID(string characterName)
        {
            if (string.IsNullOrEmpty(characterName))
            {
                Debug.LogWarning("No ID provided saving aborted");
                return;
            }

            //Creating EmptyIcon
            if (!System.IO.File.Exists(BMAC_SaveSystem.iconFilePath + "/" + characterName + ".png"))
            {
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                byte[] bytes = tex.EncodeToPNG();
                System.IO.File.WriteAllBytes(BMAC_SaveSystem.iconFilePath + "/" + characterName + ".png", bytes);
            }

            BMAC_SaveSystem.SaveCharacter(this, characterName);
        }

        #endregion


        #region Outfit Removeal
        public void RemoveOutfit(string type, bool merge = true)
        {
            var currentOutfitInSlot = GetOutfit(type);
            if (currentOutfitInSlot != null) RemoveOutfit(currentOutfitInSlot.Type, merge);
        }

        public void RemoveOutfit(Outfit outfit, bool merge = true)
        {
            if(outfit == null) { return; }
            var currentOutfitInSlot = GetOutfit(outfit.Type);
            if(currentOutfitInSlot == null) { return; }
            if (currentOutfitInSlot == outfit)
            {
                RemoveOutfit(currentOutfitInSlot.Type, merge);
                    
            }
        }


        public async void RemoveOutfit(OutfitType type, bool merge = true)
        {
            if (Outfits.TryGetValue(type.name, out Outfit currentOutfitInSlot))
            {
                if (currentOutfitInSlot != null)
                {
                    foreach (var bone in currentOutfitInSlot.additionalBones)
                    {
                        foreach (Transform child in bone.GetComponentsInChildren<Transform>(true))
                        {
                            boneMap.Remove(child.name);
                        }
                    }

                    RemoveHide(currentOutfitInSlot);
                    currentOutfitInSlot.ReturnBones();
                    currentOutfitInSlot.transform.parent = null;
                    Destroy(currentOutfitInSlot.gameObject);
                    Outfits[type.name] = null;
                    _Outfits.Remove(currentOutfitInSlot);
                }
            }
            AddTags();
            OnOutfitChanged?.Invoke(null);

           if(merge) MergeCharacter();
        }

        public void RemoveTags(string[] outfitTags)
        {
            foreach (var item in outfitTags)
            {
                tags.Remove(item);
            }
            OnTagsChanged?.Invoke(tags);
            //tags.RemoveAll(item => removedOutfit.tags.Contains(item));
        }

        public void RemoveAllOutfits(bool returnBones = true)
        {
            var o = new List<Outfit>(Outfits.Values);

            Outfits.Clear();
            _Outfits.Clear();
            tags.Clear();
            hiddenTypes.Clear();
            boneMap = new(originalBoneMap);

            
            foreach (var item in o)
            {
                if (item == null) continue;
                if (returnBones) item.ReturnBones();
                Destroy(item.gameObject);
            }
        }

        public void RemoveAllOutfits(List<string> exlusionList)
        {
            var tempList = new List<string>(Outfits.Keys);
            foreach(var item in tempList)
            {
                if (exlusionList.Contains(item)) { continue; }
                RemoveOutfit(item);
            }
            
        }
        #endregion

        //Legacy Method
        public void AttachSkinnedOutfit(Outfit outfit)
        {
            AttachOutfit(outfit);
        }

        public async void AttachOutfit(Outfit outfit)
        {
            if (!initalized || isStatic) return;

            outfit.transform.parent = transform;
            outfit.transform.SetPositionAndRotation(transform.position, transform.rotation);
            outfit.transform.localScale = (Vector3.one);
            if(outfit.mergeMesh) outfit.gameObject.SetActive(false);

            //check if an outfit is already in that slot and replace it
            RemoveOutfit(outfit.Type, false);
            _Outfits.Add(outfit);
            Outfits[outfit.Type.name] = outfit;
            //Merging outfit bones or attaching outfit to specified bone
            MergeBones(outfit);

            outfit.CopyColor(null);

            outfit.OnColorChanged += UpdateTexture;
            outfit.OnOutfitChanged += MergeCharacter;

            //Apply the Current Body Morphs to the Outfit
            ApplyShapesToOufit(outfit);

            //If Head get its Morphs
            if (outfit.Type.name == "Head" && outfit.Type.name == "Body") { InitBodyMods(); InitBodyShapes(); }

            AddTags();
            SetHide(outfit);

            OnOutfitChanged?.Invoke(outfit);
            OnOutfitAdded.Invoke(outfit);

            if (isMerging || isloading) return;
            MergeCharacter();
        }

        #region Sockets

        public Transform[] GetSockets()
        {
            return socketMap.Values.ToArray();
        }

        public Transform GetSocket(string point)
        {
            if (socketMap.TryGetValue(point, out Transform value))
            {
                return value;
            }
            else
            {
                return null;
            }
        }

        public void AttachToSocket(Transform transform, string point)
        {
            if(socketMap.TryGetValue(point, out Transform value))
            {
                transform.parent = value;
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }
            else
            {
                Debug.LogError($"Attach Point {point} not found. Make sure name is correct", transform);
            }
        }

        #endregion

        private void MergeBones(Outfit outfit)
        {
            if (outfit.additionalBones.Length != 0)
            {
                for (int i = 0; i < outfit.additionalBones.Length; i++)
                {
                    Transform bone = outfit.additionalBones[i];


                    if (bone == null || bone.parent == null)
                        continue;

                    if (!GetBones().TryGetValue(bone.parent.name, out Transform newParent))
                    {
                        Debug.LogWarning($"Missing parent bone '{bone.parent.name}' for additional bone '{bone.name}'");
                        continue;
                    }


                    if (boneMap.TryGetValue(bone.name, out Transform oldBone) && oldBone != null && oldBone != bone)
                    {
                        Destroy(oldBone.gameObject);
                        boneMap.Remove(bone.name);
                    }


                    bone.SetParent(newParent, false);

                    foreach (Transform item in bone.GetComponentsInChildren<Transform>(true))
                    {
                        if (boneMap.TryGetValue(item.name, out Transform existing) && existing != null && existing != item)
                        {
                            Destroy(existing.gameObject);
                        }

                        boneMap[item.name] = item;
                    }
                }

            }

            foreach (var smr in outfit.skinnedRenderers)
            {
                var renderer = smr;

                if (outfit.AttachPoint == "" && renderer)
                {
                    var oldBones = renderer.bones.ToArray();
                    var newBones = new Transform[renderer.bones.Length];
                    for (int i = 0; i < oldBones.Length; i++)
                    {
                        var bone = oldBones[i];
                        if (bone == null) continue;
                        boneMap.TryGetValue(bone.name, out Transform baseBone);
                        newBones[i] = baseBone;
                    }
                    renderer.bones = newBones;
                    if(outfit.mergeMesh) renderer.rootBone = CharacterBody.rootBone;
                    else renderer.rootBone = GetBones()[renderer.rootBone.name];
                }
                else
                {
                    Transform bone = null;
                    try
                    {
                        bone = boneMap[outfit.AttachPoint];
                    }
                    catch
                    {
                        Debug.LogError(name + " is missing " + outfit.AttachPoint + " that " + outfit.name + " requires");
                        return;
                    }

                }
            }

            outfit.Initalized = true;

            if (outfit.outfitRenderer && outfit.AttachPoint != "")
            {
                Transform bone = null;
                try
                {
                    bone = boneMap[outfit.AttachPoint];
                }
                catch
                {
                    Debug.LogError(name + " is missing " + outfit.AttachPoint + " that " + outfit.name + " requires");
                    return;
                }


                outfit.transform.parent = bone.transform;
                outfit.transform.position = bone.position;
                outfit.transform.rotation = bone.rotation;
                outfit.transform.localScale = Vector3.one;
            }
        }

        private async void UpdateTexture(Outfit outfit)
        {
            OnOutfitColorChanged?.Invoke(outfit);
            if (isUpdatingTexture || isMerging || isloading || hiddenTypes.ContainsKey(outfit.Type)) return;
            isUpdatingTexture = true;

            await Task.Yield();


            StoreAnimationState(true);

            MuteBodyMods(true);
            RestoreBindPose();

            MergeTextures();

            ReturnAnimationState();
            SetStance(stance);
            MuteBodyMods(false);
            isUpdatingTexture = false;
            OnColorChanged?.Invoke();
            return;

        }

        public void MergeTextures()
        {
            var rt = textures["mainTexture"];

            var activeRT = RenderTexture.active;
            RenderTexture.active = copyTexture;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = activeRT;

            _Outfits.Sort((a, b) => b.materialPriority.CompareTo(a.materialPriority));
            foreach (var o in _Outfits)
            {
                if (!o.mergeTexture) continue;
                if (hiddenTypes.ContainsKey(o.Type)) continue;

                copyMaterial.mainTexture = rt;
                copyMaterial.SetTexture("_BlendTex", o.material.mainTexture);

                copyMaterial.SetFloat("_UseCustomColors", 1);
                copyMaterial.SetFloat("_isNormalMap", 0);

                copyMaterial.SetTexture("_IDMap", o.material.GetTexture("_IDMap"));
                if (copyMaterial.GetTexture("_IDMap") == null) copyMaterial.SetFloat("_UsingIDMap", 0);
                else copyMaterial.SetFloat("_UsingIDMap", 1);

                copyMaterial.SetFloat("_Alpha", o.alpha);

                for (int i = 0; i < o.colors.Length; i++)
                {
                    var index = i + 1;
                    copyMaterial.SetColor("_Color_" + index, o.colors[i]);
                }

                //Pattern
                copyMaterial.SetTexture("_PatternMap", o.pattern);
                copyMaterial.SetVector("_PatternScale", o.patternSize);
                for (int i = 0; i < o.patternColors.Length; i++)
                {
                    var index = i + 1;
                    copyMaterial.SetColor("_PatternColor_" + index, o.colors[i]);
                }

                Graphics.Blit(rt, copyTexture, copyMaterial);
                Graphics.Blit(copyTexture, rt);

                o.ApplyDecals(rt, copyTexture);
            }


            foreach (var data in materialData)
            {
                var p = data.toMateiralProperty;
                if (p == "mainTexture")
                {
                    continue;
                }
                rt = textures[p];

                activeRT = RenderTexture.active;
                RenderTexture.active = copyTexture;
                GL.Clear(true, true, Color.clear);
                RenderTexture.active = rt;
                GL.Clear(true, true, data.backgroundColor);

                RenderTexture.active = activeRT;

                foreach (var o in _Outfits)
                {
                    if (!o.material.HasProperty(p)) continue;
                    if (!o.mergeTexture) continue;
                    if (o.material.GetTexture(p) == null) continue;
                    if (hiddenTypes.ContainsKey(o.Type)) continue;

                    copyMaterial.mainTexture = rt;
                    copyMaterial.SetTexture("_BlendTex", o.material.GetTexture(p));

                    copyMaterial.SetFloat("_Alpha", o.alpha);
                    copyMaterial.SetFloat("_UseCustomColors", 0);
                    copyMaterial.SetFloat("_UsingIDMap", 1);
                    copyMaterial.SetTexture("_IDMap", o.material.mainTexture); //Using MainTexture as a mask

                    if (p.Contains("normal") || p.Contains("Normal") || p.Contains("bump") || p.Contains("Bump"))
                    {
                        copyMaterial.SetFloat("_isNormalMap", 1);
                    }
                    else { copyMaterial.SetFloat("_isNormalMap", 0); }

                    Graphics.Blit(rt, copyTexture, copyMaterial);
                    Graphics.Blit(copyTexture, rt);
                }
            }

        }

        public void UpdateMaterial(MaterialPackage material)
        {
            if (mergeMaterial != null) return;
            var newMat = new Material(material.material);

            for (int i = 0; i < material.materialData.Length; i++)
            {
                if (i + 1 > materialData.Length) break;
                if (material.materialData[i].toMateiralProperty == "mainTexture")
                {
                    newMat.mainTexture = masterMaterial.mainTexture;
                    continue;
                }

                var to = material.materialData[i].toMateiralProperty;
                var from = materialData[i].toMateiralProperty;

                if (!newMat.HasProperty(from)) continue;
                newMat.SetTexture(to, textures[from]);
            }
            Destroy(masterMaterial);
            masterMaterial = newMat;
            CharacterBody.sharedMaterial = newMat;
        }

        public void AddTags()
        {
            tags.Clear();
            foreach (var o in _Outfits)
            {
                if (hiddenTypes.ContainsKey(o.Type)) continue;
                tags.AddRange(o.tags);
                foreach (var t in o.GetComponentsInChildren<ApplyTags>(true))
                {
                    if (t.gameObject.activeSelf) tags.AddRange(t.tags);
                }
            }

            OnTagsChanged?.Invoke(this.tags);
        }

        public void SetHide(Outfit outfit)
        {
            if (outfit == null) return;
            foreach (var item in outfit.HideTypes)
            {
                if (hiddenTypes.ContainsKey(item))
                {
                    hiddenTypes[item].Add(outfit);
                }
                else
                {
                    hiddenTypes.Add(item, new List<Outfit>());
                    hiddenTypes[item].Add(outfit);
                }
                var hidden = GetOutfit(item);
                if (hidden) hidden.gameObject.SetActive(false);
            }

        }

        public void RemoveHide(Outfit outfit)
        {
            if (outfit == null) return;
            foreach (var item in outfit.HideTypes)
            {
                if (hiddenTypes.ContainsKey(item))
                {
                    hiddenTypes[item].Remove(outfit);
                    if (hiddenTypes[item].Count == 0)
                    {
                        hiddenTypes.Remove(item);
                    }
                }
            }
        }

        public void SetStance(float value)
        {
            if (animator)
            {
                foreach (AnimatorControllerParameter param in animator.parameters)
                {
                    if (param.name == "Stance") animator.SetFloat("Stance", value);
                }
                stance = value;
            }
        }

        #region Height

        public void AddHeightSource(GameObject source, float value)
        {
            heightSources[source] = value;
            SetHeight();
        }

        public void RemoveHeightSource(GameObject source, float value)
        {
            heightSources.Remove(source);
            SetHeight();
        }

        public void SetHeight()
        {
            if (muteHeightChange) return;


            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y - height, transform.localPosition.z);

            height = 0f;
            foreach (var value in heightSources)
            {
                height += value.Value;
            }
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y + height, transform.localPosition.z);

        }

        public void MuteHeightChange()
        {
            MuteHeightChange(!muteHeightChange);
        }

        public void MuteHeightChange(bool value)
        {
            if (value == muteHeightChange) return;


            if(muteHeightChange) transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y - height, transform.localPosition.z);
            SetHeight();
        }

        #endregion

        #region BodyShapes
        private void ApplyShapesToOufit(Outfit outfit)
        {
            var keys = new List<string>(bodyShapes.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                GetBodyShapeValues();
                outfit.SetShape(keys[i], GetShape(keys[i]));
            }
        }

        private void ApplyAllShapes()
        {
            foreach (var shape in shapesValues)
            {
                if (shapes.TryGetValue(shape.Key, out int bodyIndex))
                {
                    CharacterBody.SetBlendShapeWeight(bodyIndex, shape.Value);
                }
            }
        }

        public void ResetShapes()
        {
            foreach (var item in shapes)
            {
                SetShape(item.Key, 0);
            }
        }

        public void ResetBodyMods()
        {
            foreach (var item in bodyModifiers)
            {
                if(item.Value != null)item.Value.ResetMod();
            }
        }

        public void SetShape(string key, float value)
        {
            SkinnedMeshRenderer renderer = CharacterBody;
            var index = -1;


            if (shapes.TryGetValue(key, out int bodyValue) && renderer)
            {
                index = bodyValue;
            }

            if (index != -1)
            {
                renderer.SetBlendShapeWeight(index, value);
                shapesValues[key] = value;
            }

            OnShapeChanged?.Invoke(key, value);
        }

        public void MuteBodyMods()
        {
            mutebodyMods = !mutebodyMods;
            MuteBodyMods(mutebodyMods);
        }

        public void MuteBodyMods(bool mute)
        {
            foreach (var mod in bodyModifiers.Values)
            {
                if(mod != null) mod.SetMute(mute);
            }
            mutebodyMods = mutebodyMods;
        }

        #endregion

        public void ForceBindPose()
        {
            ForceBindPose(!isbindPose);
        }

        public void ForceBindPose(bool bindPose)
        {
            if (isbindPose && animator)
            {
                if(animator) animator.enabled = false;
                RestoreBindPose();
            }
            else
            {
                if (animator) animator.enabled = true;
            }

            this.isbindPose = bindPose;
        }


        public bool ContainsTag(string tag)
        {
            return tags.Contains(tag);
        }

        #region Getters

        public Outfit GetOutfit(OutfitType outfitType)
        {
            if (Outfits.TryGetValue(outfitType.name, out Outfit item))
            {
                return item;
            }
            return null;
        }

        public Outfit GetOutfit(string outfitType)
        {

            if (Outfits.TryGetValue(outfitType, out Outfit item))
            {
                return item;
            }
            return null;
        }

        public List<Outfit> GetOutfits()
        {
            return new List<Outfit>(Outfits.Values);
        }

        public Dictionary<string, int> GetShapes()
        {
            return shapes;
        }

        public Dictionary<string, int> GetBodyShapes()
        {
            return bodyShapes;
        }

        public Dictionary<string, int> GetFaceShapes()
        {
            return faceShapes;
        }

#if MAGICACLOTH2
        public MagicaCloth2.ColliderComponent[] GetClothColliders()
        {
            return ClothColliders;
        }
#endif

        public float GetShape(string key)
        {
            if (bodyShapes.TryGetValue(key, out int value))
            {
                var body = GetOutfit("Body");
                if (body != null)
                {
                    var weightValue = body.skinnedRenderer.GetBlendShapeWeight(value);
                    return weightValue;
                }
                else return -10000;
            }
            else return -10000;
        }

        public Dictionary<string, BodyShapeModifier> GetMods()
        {
            return bodyModifiers;
        }
        public Dictionary<string, Transform> GetBones()
        {
            return boneMap;
        }

        public float GetShapeValue(string key)
        {
            var weight = -1f;

            var body = GetOutfit("Body");

            if (body == null) return -1;
            if (bodyShapes.TryGetValue(key, out int bodyValue))
            {
                weight = body.skinnedRenderer.GetBlendShapeWeight(bodyValue);
            }
            else if (faceShapes.TryGetValue(key, out int faceValue))
            {
                var face = GetOutfit("Head");
                if (face == null) return -1;
                weight = face.skinnedRenderer.GetBlendShapeWeight(faceValue);
            }

            return weight;
        }

        public float GetShapeValue(int key)
        {
            SkinnedMeshRenderer renderer;
            var body = GetOutfit("Body");
            if (body == null) renderer = CharacterBody;
            else renderer = body.skinnedRenderer;

            var weightValue = renderer.GetBlendShapeWeight(key);
            return weightValue;
        }

        public Dictionary<string, float> GetBodyShapeValues()
        {
            var bodyShapeValues = new Dictionary<string, float>();
            var shapes = bodyShapes.Values.ToArray();
            var keys = bodyShapes.Keys.ToArray();

            SkinnedMeshRenderer renderer;
            var body = GetOutfit("Body");
            if (body == null) renderer = CharacterBody;
            else renderer = body.skinnedRenderer;

            for (int i = 0; i < shapes.Length; i++)
            {
                var weightValue = renderer.GetBlendShapeWeight(shapes[i]);
                bodyShapeValues.Add(keys[i], weightValue);
            }

            return bodyShapeValues;
        }

        public Dictionary<string, float> GetFaceShapeValues()
        {
            var faceShapeValues = new Dictionary<string, float>();
            var shapes = faceShapes.Values.ToArray();
            var keys = faceShapes.Keys.ToArray();

            SkinnedMeshRenderer renderer;
            var head = GetOutfit("Head");
            if (head == null) renderer = CharacterBody;
            else renderer = head.skinnedRenderer;


            for (int i = 0; i < shapes.Length; i++)
            {
                var weightValue = renderer.GetBlendShapeWeight(shapes[i]);
                faceShapeValues.Add(keys[i], weightValue);
            }

            return faceShapeValues;
        }

        public SkinnedMeshRenderer GetCharacterBody() { return CharacterBody; }
        #endregion

#if UNITY_EDITOR

        public void SoftAttach(Outfit outfit)
        {
            //For Attaching outfits during in the Editor 
            if (CharacterBody == null)
            {
                Debug.LogWarning("Soft Attach attempted but OuftitSystem did not have a CharacterBody please assign in the inspector", gameObject);
                return;
            }

            Dictionary<string, Transform> boneMap = new Dictionary<string, Transform>();
            foreach (Transform bone in CharacterBody.bones)
            {
                if (boneMap.ContainsKey(bone.name) == false)
                {
                    boneMap.Add(bone.name, bone);
                }
            }

            var renderers = outfit.GetComponentsInChildren<SkinnedMeshRenderer>();


            //Already Attached
            if (outfit.originalBones.Length > 0)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].localBounds = CharacterBody.localBounds;

                if (outfit.AttachPoint == "" && renderers[i])
                {
                    if (outfit.Initalized == false)
                    {
                        outfit.originalBones = renderers[i].bones;
                        outfit.originalRootBone = renderers[i].rootBone;

                        var oldBones = renderers[i].bones.ToArray();
                        var newBones = new Transform[renderers[i].bones.Length];
                        for (int b = 0; b < oldBones.Length; b++)
                        {
                            var bone = oldBones[b];
                            boneMap.TryGetValue(bone.name, out newBones[b]);
                        }
                        renderers[i].bones = newBones;
                        renderers[i].rootBone = CharacterBody.rootBone;

                    }
                }
            }

        }
#endif

        public void StoreAnimationState(bool disableAnimator)
        {
            if (animator)
            {
                stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                control = animator.runtimeAnimatorController;
                avatar = animator.avatar;
                progress = stateInfo.normalizedTime;
                stateHash = stateInfo.fullPathHash;
                wasEnabled = animator.enabled;

                if (disableAnimator)
                {
                    animator.runtimeAnimatorController = null;
                    animator.enabled = false;
                }
            }
        }

        public void ReturnAnimationState()
        {
            if (animator)
            {
                animator.Rebind();
                animator.avatar = avatar;
                animator.runtimeAnimatorController = control;
                animator.enabled = wasEnabled;
                animator.Play(stateHash, 0, progress % 1);
            }
        }

        public void SetRenderTextures(List<RenderTexture> rt)
        {
            foreach (var item in renderTextures)
            {
                item.Release();
            }
            renderTextures = new List<RenderTexture>(rt);
        }

        public void MergeCharacter(Outfit outfit)
        {
            MergeCharacter();
        }

        [ContextMenu("Merge")]
        public async Task MergeCharacter()
        {
            try
            {
                if (isStatic) return;

                if (isMerging || isloading) return;
                isMerging = true;
                OnMergeStarted.Invoke();
                await Task.Yield();

                if (Application.isPlaying && gameObject.scene.isLoaded)
                {
                    //Remembering Animation State
                    if (animator)
                    {
                        StoreAnimationState(true);
                        animator.runtimeAnimatorController = null;
                        animator.enabled = false;
                    }




                    _Outfits.RemoveAll(x => x == null);

#if MAGICACLOTH2
                    customMaps.Remove("MagicaClothExtension");

                    foreach (var item in GetComponentsInChildren<BoZo_MagicaClothSupport>())
                    {
                        //item.DestroyCloth();
                    }

#endif

                    //Muting Body Changes as they mess up the merge process
                    MuteBodyMods(true);
                    MuteHeightChange(true);

                    //Adding Tags
                    AddTags();

                    //Merging Textures

                    MergeTextures();


                    var optimizer = new BoZo_CharacterOptimizer();

                    //Cleaning Out Bones
                    var keysToRemove = boneMap
                    .Where(pair => pair.Value == null)
                    .Select(pair => pair.Key)
                    .ToList();

                    foreach (var key in keysToRemove)
                    {
                        boneMap.Remove(key);
                    }

                    GetCharacterBody().bones = boneMap.Values.ToArray();

                    RestoreBindPose();

                    //Mesh Merging
                    optimizer.MergeButTheBetterOneV2(this);




                    //Returning Body Shapes
                    MuteBodyMods(false);
                    MuteHeightChange(false);

                    //Gathering blendshapes
                    InitBodyShapes();
                    InitBodyMods();
                    ApplyAllShapes();


                    ReturnAnimationState();
                    SetStance(stance);
                

                    if (cleanAfterMerge)
                    {
                        RemoveAllOutfits();
                        isStatic = true;
                    }

                    OnRigChanged?.Invoke(CharacterBody);
                    OnMergeComplete.Invoke();

                }
                else
                {
                    Debug.LogWarning("For stability reasons Character Merging is only available in Play Mode");
                }
                    isMerging = false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Character Merging on {gameObject} has failed {e}");
            }

        }


        [ContextMenu("SaveToPrefab")]
        public async Task SaveCharacterToPrefab()
        {
#if UNITY_EDITOR


            RemoveAllOutfits(false);
            await Task.Yield();
            if(animator) animator.Rebind();


#if MAGICACLOTH2
            foreach (var item in GetComponentsInChildren<MagicaCloth2.MagicaCloth>())
            {
                item.enabled = false;
            }
            foreach (var item in GetComponentsInChildren<BoZo_MagicaClothSupport>())
            {
                DestroyImmediate(item);
            }
#endif

            var settings = CharacterToolSettingsProvider.Get();

            var path = settings.prefabFolder;


            var saveName = prefabName;
            if (string.IsNullOrEmpty(saveName)) saveName = "NewCharacter";
            var assetPath = $"{path}/{saveName}";
            var savePath = $"Assets/{path}/{saveName}";

            if (!System.IO.Directory.Exists($"{Application.dataPath}{path}/{saveName}"))
            {
                System.IO.Directory.CreateDirectory(assetPath);
                AssetDatabase.Refresh();
            }

            //Saving Mesh
            string meshPath = $"{assetPath}/{saveName}_Mesh.asset";
            AssetDatabase.CreateAsset(CharacterBody.sharedMesh, meshPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);

            //Saving Materials and Textures
            var matList = new List<Material>();

            foreach (var item in CharacterBody.sharedMaterials)
            {
                var mat = new Material(item);
                mat.name = saveName + "_" + item.name;

                var rt = (RenderTexture)mat.mainTexture;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                RenderTexture.active = rt;

                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();

                string diffusePath = $"{assetPath}/{mat.name}_D.png";
                byte[] bytes = ImageConversion.EncodeToPNG(tex);
                System.IO.File.WriteAllBytes(diffusePath, bytes);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                var diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>($"{assetPath}/{mat.name}_D.png");

                mat.mainTexture = diffuse;

                foreach (var matData in materialData)
                {
                    if (matData.toMateiralProperty == "mainTexture") continue;
                    if(!item.HasProperty(matData.toMateiralProperty)) continue;

                    rt = (RenderTexture)item.GetTexture(matData.toMateiralProperty);
                    tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                    RenderTexture.active = rt;

                    tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    tex.Apply();

                    string texturePath = $"{assetPath}/{mat.name}{matData.toMateiralProperty}.png";
                    bytes = ImageConversion.EncodeToPNG(tex);
                    System.IO.File.WriteAllBytes(texturePath, bytes);

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{assetPath}/{mat.name}{matData.toMateiralProperty}.png");

                    mat.SetTexture(matData.toMateiralProperty, texture);

                    if (matData.toMateiralProperty.Contains("normal") || matData.toMateiralProperty.Contains("Normal") || matData.toMateiralProperty.Contains("bump") || matData.toMateiralProperty.Contains("Bump"))
                    {
                        TextureImporter importer = AssetImporter.GetAtPath($"{assetPath}/{mat.name}{matData.toMateiralProperty}.png") as TextureImporter;
                        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
                        {
                            Debug.LogWarning("Prefabs made when build target is set to android do not need their Normal Maps set to Normal in their Import settings. Doing so will result in unwanted lighting");
                            importer.sRGBTexture = false;
                            importer.SaveAndReimport();
                        }
                        else
                        {

                            importer.textureType = TextureImporterType.NormalMap;
                            importer.sRGBTexture = false;
                            importer.mipmapEnabled = true;
                            importer.SaveAndReimport();
                        }
                    }

                }


                //Saving Material
                string matPath = $"{assetPath}/{mat.name}_Mat.mat";
                AssetDatabase.CreateAsset(mat, matPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                matList.Add(AssetDatabase.LoadAssetAtPath<Material>(matPath));
            }

            CharacterBody.sharedMaterials = matList.ToArray();

            isStatic = true;
            _characterData = null;
            characterData = null;
            SaveID = "";

            var iconCam = GetComponentInChildren<Camera>();
            if (iconCam) DestroyImmediate(iconCam.gameObject);

            // Save the prefab
            string prefabPath = $"{assetPath}/{saveName}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(gameObject, prefabPath);


            if (cleanScriptsAfterPrefab)
            {
                var os = prefab.GetComponent<OutfitSystem>();
                if (os) DestroyImmediate(os, true);
                foreach (var item in prefab.GetComponentsInChildren<BodyShapeModifier>())
                {
                    DestroyImmediate(item, true);
                }
            }

            var prefabSkinnedMeshrenderer = prefab.GetComponentInChildren<SkinnedMeshRenderer>();
            prefabSkinnedMeshrenderer.sharedMesh = mesh;

#if MAGICACLOTH2
            foreach (var item in prefab.GetComponentsInChildren<MagicaCloth2.MagicaCloth>())
            {
                item.enabled = true;
            }
#endif


            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Saved Prefab at: {prefabPath}");


#endif


        }
        [ContextMenu("Copy Character Data To ClipBoard")]
        public void CopyCharacterDataToClipBoard()
        {
            GUIUtility.systemCopyBuffer = JsonUtility.ToJson(BMAC_SaveSystem.GetCharacterData(this));
        }
        public struct BonePose
        {
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
        }
    }
}
