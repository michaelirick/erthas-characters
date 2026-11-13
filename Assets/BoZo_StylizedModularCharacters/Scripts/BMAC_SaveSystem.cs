using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Profiling;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Threading.Tasks;
using System;
using System.IO;

namespace Bozo.ModularCharacters
{
    public static class BMAC_SaveSystem
    {
        public static string filePath = Application.persistentDataPath + "/BoZo_StylizedModularCharacters/CustomCharacters";
        public static string iconFilePath = Application.persistentDataPath + "/BoZo_StylizedModularCharacters/CustomCharacters/Icons";


        public static List<LoadSet> loadQueue = new List<LoadSet>();
        private static readonly Queue<TaskCompletionSource<bool>> pendingCompletions = new Queue<TaskCompletionSource<bool>>();

        public static bool isLoading;
        private static bool isShuttingDown;
        private static int sessionVersion;
        private static CancellationTokenSource queueCancellation;


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            // Invalidate tasks left from a previous Play Mode session (domain reload off).
            CleanupInternal();
            isShuttingDown = false;
            isLoading = false;
            queueCancellation = null;
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void RegisterEditorCleanup()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                Cleanup();
            }
        }
#endif
        public static void Cleanup()
        {
            if (isShuttingDown)
                return;

            isShuttingDown = true;
            loadQueue.Clear();

            Debug.Log("Character loading queue cleared.");
        }

        private static void CleanupInternal()
        {
            ++sessionVersion;
            queueCancellation?.Cancel();
            loadQueue.Clear();
            while (pendingCompletions.Count > 0) pendingCompletions.Dequeue().TrySetCanceled();

        }

        public static void SaveCharacter(OutfitSystem outfitSystem, string saveName, Texture2D icon = null)
        {
            var data = GetCharacterData(outfitSystem);

            data.characterName = saveName;


#if UNITY_EDITOR
            var assetPath = CharacterToolSettingsProvider.Get().saveDataFolder;
            var CharacterSave = ScriptableObject.CreateInstance<CharacterObject>();
            CharacterSave.data = data;
            CharacterSave.icon = icon;
            AssetDatabase.CreateAsset(CharacterSave, assetPath + "/" + saveName + ".asset");
            AssetDatabase.Refresh();
#endif

            string saveData = JsonUtility.ToJson(data);
            System.IO.File.WriteAllText(filePath + "/" + saveName + ".json", saveData);
            Debug.Log("Character Saved:" + filePath + "/" + saveName + ".json");
        }

        public static CharacterData GetCharacterData(OutfitSystem outfitSystem)
        {

            var data = new CharacterData();

            data.versionID = 4;

            //Saving Material
            data.material = outfitSystem.GetCharacterBody().sharedMaterial.name;

            //Saving BlendShapes
            var bodyShapeValues = outfitSystem.GetBodyShapeValues();
            data.bodyIDs = bodyShapeValues.Keys.ToList();
            data.bodyShapes = bodyShapeValues.Values.ToList();

            var faceShapeValues = outfitSystem.GetFaceShapeValues();
            data.faceIDs = faceShapeValues.Keys.ToList();
            data.faceShapes = faceShapeValues.Values.ToList();

            //Saving Body Mods
            var modData = new List<BodyModData>();
            var modKeys = outfitSystem.bodyModifiers.Keys.ToList();
            for (int i = 0; i < modKeys.Count; i++)
            {
                var mod = outfitSystem.bodyModifiers[modKeys[i]].GetData();
                modData.Add(mod);
            }
            data.bodyMods = modData;
            data.bodyModsKeys = modKeys;

            //Saving Outfits
            var outfits = outfitSystem.GetOutfits();
            var outfitDataList = new List<OutfitData>();

            for (int i = 0; i < outfits.Count; i++)
            {
                if (outfits[i] == null) continue;
                outfitDataList.Add(outfits[i].GetOutfitData());
            }

            data.stance = outfitSystem.stance;
            data.outfitDatas = outfitDataList;

            return data;
        }

        public static async Task LoadCharacter(OutfitSystem outfitSystem, CharacterData characterObject, bool async = false, bool loadShader = true, bool loadBodyShapes = true, List<string> exclusionList = null)
        {   

            loadQueue.Add(new LoadSet(outfitSystem, characterObject, async, loadShader, loadBodyShapes, exclusionList));

            if (loadQueue.Count > 0 && !isLoading) { LoadCharacter(loadQueue[0]); loadQueue.RemoveAt(0); }            
        }

        private static async Task ProcessLoadQueue()
        {

        }



        private static async Task LoadCharacter(LoadSet loadSet)
        {
            OutfitSystem outfitSystem = loadSet.outfitSystem;
            CharacterData characterObject = loadSet.characterData;
            bool asyc = loadSet.async;
            bool loadShader = loadSet.loadShader;
            bool loadBodyShapes = loadSet.loadBodyShapes;
            List<string> exclusionList = loadSet.exclusionList;

            isLoading = true;

            if (outfitSystem == null || characterObject == null)
            {
                Debug.Log("load empty abort " + loadQueue.Count);
                isLoading = false;
                if (loadQueue.Count > 0) { LoadCharacter(loadQueue[0]); loadQueue.RemoveAt(0); }
                return;
            }


            try
            {
                if (outfitSystem.isloading)
                {
                    Debug.LogWarning($"{outfitSystem.name} is already trying to load a save. Please wait until its finished before loading another one");

                    isLoading = false;
                    if (loadQueue.Count > 0)
                    {
                        LoadCharacter(loadQueue[0]); loadQueue.RemoveAt(0);
                    }
                    return;
                }

                outfitSystem.isloading = true;
                //outfitSystem.GetCharacterBody().enabled = false;

                CharacterData loadData = characterObject;

                //UPDATING SAVE FROM OLD VERSIONS
                try
                {
                    loadData = Bozo_SavePatcher.UpdateSave(characterObject);
                }
                catch (Exception ex)
                {

                    Debug.LogException(ex);

                    isLoading = false;
                    if (loadQueue.Count > 0) { LoadCharacter(loadQueue[0]); loadQueue.RemoveAt(0); }
                    return;
                }

                //LOADING OUTFITS 
                try
                {
                    loadData.outfitDatas = loadData.outfitDatas.OrderByDescending(item => item.outfit.Contains("Head")).ToList();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);

                    isLoading = false;
                    if (loadQueue.Count > 0) { LoadCharacter(loadQueue[0]); loadQueue.RemoveAt(0); }
                    return;
                }
                List<Outfit> outfits = new List<Outfit>();

                foreach (var item in loadData.outfitDatas)
                {
                    if (exclusionList != null)
                    {
                        int slashIndex = item.outfit.IndexOf('/');
                        string result = slashIndex >= 0 ? item.outfit[..slashIndex] : item.outfit;
                        if (exclusionList.Contains(result))
                        {
                            outfits.Add(null);
                            continue;
                        }
                    }


                    if (outfitSystem.async)
                    {
                        var request = Resources.LoadAsync<Outfit>(item.outfit);

                        while (!request.isDone)
                        {
                            await Task.Yield();
                        }

                        outfits.Add(request.asset as Outfit);
                    }
                    else
                    {
                        outfits.Add(Resources.Load<Outfit>(item.outfit));
                    }
                }

                if (exclusionList == null)
                {
                    outfitSystem.RemoveAllOutfits();
                }
                else
                {
                    outfitSystem.RemoveAllOutfits(exclusionList);
                }

                //Loading Outfit Data
                for (int i = 0; i < loadData.outfitDatas.Count; i++)
                {

                    var outfitData = loadData.outfitDatas[i];
                    var outfit = outfits[i];

                    if (outfit == null)
                    {
                        Debug.LogWarning("Outfit Path: " + outfitData.outfit + " returns null make sure Prefab is named correctly");
                        continue;
                    }

                    var inst = UnityEngine.Object.Instantiate(outfit, outfitSystem.transform);
                    inst.Attach(outfitSystem);


                    inst.alpha = outfitData.alpha;


                    if (loadData.outfitDatas[i].decalDatas != null)
                    {
                        var decalData = new List<DecalData>(loadData.outfitDatas[i].decalDatas);
                        for (int d = 0; d < decalData.Count; d++)
                        {
                            var data = decalData[d];
                            data.decalTexture = Resources.Load<Texture>("Decal/" + data.decal);
                            decalData[d] = data;
                        }
                        inst.decalDatas = decalData;
                    }

                    for (int c = 0; c < 9; c++)
                    {
                        if (outfitData.colors.Count > c)
                        {
                            inst.SetColor(outfitData.colors[c], c + 1);
                        }

                        if (c + 1 <= 3 && outfitData.pattern != "" && outfitData.decalColors != null)
                        {
                            inst.SetPatternColor(outfitData.patternColors[c], c + 1);
                        }
                    }

                    var pattern = Resources.Load<Texture>(outfitData.pattern);
                    inst.SetPattern(pattern);
                    inst.SetPatternSize(outfitData.patternScale);


                    if (loadData.outfitDatas[i].partVisibility != null)
                    {
                        if (inst.optionalPieces.Length == loadData.outfitDatas[i].partVisibility.Length)
                        {
                            for (int v = 0; v < loadData.outfitDatas[i].partVisibility.Length; v++)
                            {
                                if(inst.optionalPieces.Length == loadData.outfitDatas[i].partVisibility.Length) 
                                if (inst.optionalPieces[v] == null) continue;
                                inst.optionalPieces[v].SetActive(loadData.outfitDatas[i].partVisibility[v]);
                            }
                        }
                    }

                    //if (outfitSystem.async) await Task.Yield();
                }

                //Loading Body Morphs
                outfitSystem.GetCharacterBody().enabled = false;
                outfitSystem.isloading = false;

                await outfitSystem.MergeCharacter();
                outfitSystem.GetCharacterBody().enabled = true;

                //LOADING SHADER
                if (loadShader)
                {
                    var mat = Resources.Load<MaterialPackage>(loadData.material);
                    if (mat != null) outfitSystem.UpdateMaterial(mat);
                }

                if (loadBodyShapes)
                {
                    outfitSystem.ResetShapes();
                    for (int i = 0; i < loadData.bodyIDs.Count; i++)
                    {
                        outfitSystem.SetShape(loadData.bodyIDs[i], loadData.bodyShapes[i]);
                    }

                    for (int i = 0; i < loadData.faceIDs.Count; i++)
                    {
                        outfitSystem.SetShape(loadData.faceIDs[i], loadData.faceShapes[i]);
                    }
                    outfitSystem.ResetBodyMods();
                    LoadBodyMods(outfitSystem, loadData);
                    outfitSystem.SetStance(loadData.stance);
                }
                outfitSystem.MergeTextures();

                outfitSystem.OnCharacterLoaded?.Invoke();
                outfitSystem.OnLoadComplete?.Invoke();

            }
            catch (Exception e)
            {
                Debug.LogError(e);
                isLoading = false;
                if (loadQueue.Count > 0) { LoadCharacter(loadQueue[0]); loadQueue.RemoveAt(0); }
                return;
            }

            isLoading = false;
            if (loadQueue.Count > 0) { LoadCharacter(loadQueue[0]); loadQueue.RemoveAt(0); }
            return;
        }

        public static void LoadBodyMods(OutfitSystem outfitSystem, CharacterData loadData)
        {
            for (int i = 0; i < loadData.bodyModsKeys.Count; i++)
            {
                if (outfitSystem.bodyModifiers.ContainsKey(loadData.bodyModsKeys[i])) outfitSystem.bodyModifiers[loadData.bodyModsKeys[i]].SetData(loadData.bodyMods[i]);
            }
        }

        public static CharacterData GetDataFromID(string saveName)
        {
            CharacterData loadData;
            Debug.Log("Attempted Load at: " + filePath + "/" + saveName + ".json");
            if (!System.IO.File.Exists(filePath + "/" + saveName + ".json"))
            {
                Debug.LogWarning($"Save ID: {saveName} does not exist. Make sure input matches an existing Save");
                return null;
            }
            else
            {
                string data = System.IO.File.ReadAllText(filePath + "/" + saveName + ".json");
                loadData = JsonUtility.FromJson<CharacterData>(data);
                return loadData;
            }
        }

        public static async Task<List<Outfit>> LoadOutfits(List<OutfitData> outfitDatas)
        {
            var loadTasks = outfitDatas.Select(data => LoadResourceAsync<Outfit>(data.outfit));
            return (await Task.WhenAll(loadTasks)).ToList();
        }

        public static async Task<T> LoadResourceAsync<T>(string path) where T : UnityEngine.Object
        {
            ResourceRequest request = Resources.LoadAsync<T>(path);
            var tcs = new TaskCompletionSource<T>();

            request.completed += operation =>
            {
                if (request.asset == null)
                {
                    tcs.SetResult(null);
                }
                else if (request.asset is not T result)
                {
                    tcs.SetResult(null);
                }
                else
                {
                    tcs.SetResult(result);
                }
            };

            return await tcs.Task;
        }

        public static void DeleteCharacter(string characterName)
        {
            System.IO.File.Delete(filePath + "/" + characterName + ".json");
            System.IO.File.Delete(iconFilePath + "/" + characterName + ".png");
#if UNITY_EDITOR
            var assetPath = CharacterToolSettingsProvider.Get().saveDataFolder;
            var iconAssetPath = CharacterToolSettingsProvider.Get().iconFolder;
            AssetDatabase.DeleteAsset(assetPath + "/" + characterName + ".asset");
            AssetDatabase.DeleteAsset(assetPath + "/" + characterName + ".meta");
            AssetDatabase.DeleteAsset(iconAssetPath + "/" + characterName + ".png");
            AssetDatabase.DeleteAsset(iconAssetPath + "/" + characterName + ".meta");
            AssetDatabase.Refresh();
#endif
        }

        public static List<string> GetBodyList()
        {
            var list = new List<string>();

            list.Add("Head");
            list.Add("HeadType");
            list.Add("Ears");
            list.Add("EyeBrows");
            list.Add("EyeLashes");
            list.Add("Eyes");
            list.Add("EyeShine");
            list.Add("FaceDetails");
            list.Add("FaceTexture");
            list.Add("Teeth");
            list.Add("BodyType");
            list.Add("Body");
            list.Add("Iris");
            list.Add("Pupil");

            return list;
        }
        public static List<string> GetOutfitList()
        {
            var list = new List<string>();

            list.Add("Bottom");
            list.Add("Feet");
            list.Add("Gloves");
            list.Add("Hat");
            list.Add("HeadAcc");
            list.Add("Leggings");
            list.Add("MakeUpCheeks");
            list.Add("MakeUpEyes");
            list.Add("MakeUpLips");
            list.Add("Neck");
            list.Add("Overall");
            list.Add("Shoulders");
            list.Add("Socks");
            list.Add("Top");
            list.Add("UnderLower");
            list.Add("UnderUpper");
            list.Add("UpperFace");
            list.Add("Waist");

            return list;

        }
        public static List<string> GetHairList()
        {
            var list = new List<string>();

            list.Add("LowerFace");
            list.Add("HairBack");
            list.Add("HairFront");


            return list;

        }
    }

    [System.Serializable]
    public class CharacterData
    {
        public int versionID = 0;
        public string characterName;
        public List<string> bodyIDs;
        public List<float> bodyShapes;
        public List<string> faceIDs;
        public List<float> faceShapes;
        public List<string> bodyModsKeys;
        public List<BodyModData> bodyMods;
        public List<OutfitData> outfitDatas;
        public OutfitData bodyData;
        public float stance;
        public string material;


        public CharacterData()
        {
            versionID = 0;
        }
        public CharacterData(CharacterData copyData)
        {
            versionID = copyData.versionID;
            characterName = copyData.characterName;
            bodyIDs = new List<string>(copyData.bodyIDs);
            bodyShapes = new List<float>(copyData.bodyShapes);
            faceIDs = new List<string>(copyData.faceIDs);
            faceShapes = new List<float>(copyData.faceShapes);
            bodyModsKeys = new List<string>(copyData.bodyModsKeys);
            bodyMods = new List<BodyModData>(copyData.bodyMods);
            outfitDatas = new List<OutfitData>(copyData.outfitDatas);
            bodyData = copyData.bodyData;
            stance = copyData.stance;
        }
    }

    [System.Serializable]
    public struct OutfitData
    {
        public string outfit;
        public List<Color> colors;
        public float alpha;

        public string decal;
        public List<Color> decalColors;
        public Vector4 decalScale;

        public string pattern;
        public List<Color> patternColors;
        public Vector4 patternScale;

        public bool[] partVisibility;

        //Custom Shader Data
        public Color color;
        public int swatch;

        public List<DecalData> decalDatas;

        public OutfitData(OutfitData d)
        {
            outfit = d.outfit;
            colors = d.colors;
            alpha = d.alpha;
            decal = d.decal;
            decalColors = d.decalColors;
            decalScale = d.decalScale;
            pattern = d.pattern;
            patternColors = d.patternColors;
            patternScale = d.patternScale;
            partVisibility = d.partVisibility;
            color = d.color;
            swatch = d.swatch;
            decalDatas = d.decalDatas;
        }
    }

    public struct LoadSet
    {
        public OutfitSystem outfitSystem;
        public CharacterData characterData;
        public bool async;
        public bool loadShader;
        public bool loadBodyShapes;
        public List<string> exclusionList;

        public LoadSet(OutfitSystem outfitSystem, CharacterData characterObject, bool async = false, bool loadShader = true, bool loadBodyShapes = true, List<string> exclusionList = null)
        {
            this.outfitSystem = outfitSystem;
            this.characterData = characterObject;
            this.async = async;
            this.loadShader = loadShader;
            this.loadBodyShapes = loadBodyShapes;
            this.exclusionList = exclusionList;
        }
    }

    [System.Serializable]
    public struct DecalData
    {
        public string decal;
        public Texture decalTexture;
        public string parent;

        public Vector3 pos;

        public float yaw;
        public float roll;
        public float pitch;
        public float scale;

        public Color color;

        public DecalData(string decal, string parent, Vector3 pos, float yaw, float roll, float pitch, float scale, Color color)
        {
            this.decal = decal;
            this.decalTexture = null;
            this.parent = parent;

            this.pos = pos;

            this.yaw = yaw;
            this.roll = roll;
            this.pitch = pitch;
            this.scale = scale;

            this.color = color;
        }
    }



}