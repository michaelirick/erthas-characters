using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Collections;
using TMPro;
using System.Linq;
using UnityEngine.Events;
using System.Threading.Tasks;


namespace Bozo.ModularCharacters
{
    public class CharacterCreator : MonoBehaviour
    {
        [Header("Creator Dependencies")]
        public OutfitSystem character;
        public DecalController decalController;
        public OutfitType[] outfitTypes;

        [Header("Outfit Dependencies")]
        private Dictionary<string, Outfit> OutfitDataBase = new Dictionary<string, Outfit>();
        [SerializeField] OutfitSelector outfitSelectorObject;
        private List<OutfitSelector> outfitSelectors = new List<OutfitSelector>();
        [SerializeField] Transform outfitContainer;
        [SerializeField] OutfitVisibilityToggler visibilityToggler;

        [Header("Texture Dependencies")]
        [SerializeField] TextureSelector textureSelectorObject;
        private List<TextureSelector> textureSelectors = new List<TextureSelector>();
        [SerializeField] Transform decalContainer;
        [SerializeField] Transform patternContainer;

        [Header("BodyShape Dependencies")]
        [SerializeField] BlendSlider blendSliderObject;
        private Dictionary<string, BlendSlider> blendSliders = new Dictionary<string, BlendSlider>();
        private Dictionary<string, BlendSlider> faceBlendSliders = new Dictionary<string, BlendSlider>();

        [SerializeField] BodyShapeSliders modSliderObject;
        private List<BodyShapeSliders> ModSliders = new List<BodyShapeSliders>();

        [SerializeField] Transform bodyShapeContainer;
        [SerializeField] Transform bodyModContainer;
        [SerializeField] Transform faceShapeContainer;
        [SerializeField] Transform faceModContainer;

        [SerializeField] string catagory;
        [SerializeField] GameObject currentPage;
        private GameObject previousPage;

        private Dictionary<string, List<GameObject>> outfits = new Dictionary<string, List<GameObject>>();
        private List<TexturePackage> textures = new List<TexturePackage>();

        [Header("UserSettings")]
        [SerializeField] GameObject RemoveButton;
        [SerializeField] string[] DisableRemoveByType;

        //Events
        public UnityAction<string> onCategoryChanged;
        public UnityAction<GameObject> onCharacterChanged;
        public OutfitType type;

        [Header("Debug Options")]
        public string ShowOnlyPack;

        private void Awake()
        {
            outfits.Clear();
            OutfitDataBase.Clear();

            var ob = Resources.LoadAll<Outfit>("").ToList();
            var textureObjects = Resources.LoadAll<TexturePackage>("");

            ob.Sort((a, b) => string.Compare(a.PackName, b.PackName, System.StringComparison.OrdinalIgnoreCase));

            foreach (var item in ob)
            {
                if (!item.showCharacterCreator) continue;

                if (OutfitDataBase.ContainsKey(item.name))
                {
                    Debug.LogWarning($"Outfit: {item.name} has already been added, you may have a duplicate outfit in your project");
                }
                else
                {
                    OutfitDataBase.Add(item.name, item.GetComponent<Outfit>());
                }
            }
            foreach (var item in textureObjects)
            {
                textures.Add(item.GetComponent<TexturePackage>());
            }
            GenerateOutfitSelection();
            GenerateTextureSelection();
        }

        private void OnEnable()
        {
            character.OnOutfitChanged += OnOutfitUpdate;
            character.OnRigChanged += OnRigUpdate;
        }

        private void OnDisable()
        {
            character.OnOutfitChanged -= OnOutfitUpdate;
            character.OnRigChanged -= OnRigUpdate;
        }

        public void Start()
        {
            GetBodyBlends();
            GetBodyMods();
            SwitchCatagory("Top");

            //UpdateCharacterSaves();
            onCharacterChanged?.Invoke(character.gameObject);
        }

        public void GenerateOutfitSelection()
        {
            if (outfitSelectorObject == null || outfitContainer == null) return;
            var outfits = OutfitDataBase.Values.ToArray();
            foreach (var item in outfits)
            {
                var selector = Instantiate(outfitSelectorObject, outfitContainer);
                selector.Init(item, this);
                outfitSelectors.Add(selector);
            }
        }

        public void GenerateTextureSelection()
        {
            if (textureSelectorObject == null) return;

            foreach (var item in textures)
            {
                Transform container = null;

                if (item.type == TextureType.Decal) container = decalContainer;
                if (item.type == TextureType.Pattern) container = patternContainer;

                if (container == null) continue;

                var selector = Instantiate(textureSelectorObject, container);
                selector.Init(item, this);
                textureSelectors.Add(selector);
            }
        }

        public void GetBodyBlends()
        {
            if (blendSliderObject == null || bodyModContainer == null) return;

            var shapes = character.GetShapes();
            var bodyShapes = character.GetBodyShapes();
            var faceShapes = character.GetFaceShapes();

            foreach (var item in faceBlendSliders.Values)
            {
                item.gameObject.SetActive(false);
            }

            foreach (var item in blendSliders.Values)
            {
                item.gameObject.SetActive(false);
            }

            foreach (var item in shapes)
            {

                if (bodyShapes.ContainsKey(item.Key))
                {
                    if (blendSliders.ContainsKey(item.Key))
                    {
                        blendSliders[item.Key].gameObject.SetActive(true);
                    }
                    else
                    {
                        var blendSlider = Instantiate(blendSliderObject, bodyShapeContainer);
                        blendSlider.Init(character, item.Key);
                        blendSliders.Add(item.Key, blendSlider);
                    }

                }
                else if (faceShapes.ContainsKey(item.Key))
                {
                    if (faceBlendSliders.ContainsKey(item.Key))
                    {
                        faceBlendSliders[item.Key].gameObject.SetActive(true);
                    }
                    else
                    {
                        var blendSlider = Instantiate(blendSliderObject, faceShapeContainer);
                        blendSlider.Init(character, item.Key);
                        faceBlendSliders.Add(item.Key, blendSlider);
                    }
                }
            }
        }

        public void GetBodyMods()
        {
            if (modSliderObject == null) return;

            var mods = character.GetMods().Values.ToList();

            for (int i = 0; i < ModSliders.Count; i++)
            {
                if(ModSliders[i] != null) Destroy(ModSliders[i].gameObject);
            }
            ModSliders.Clear();

            var container = bodyShapeContainer;
            foreach (var item in mods)
            {
                //print(item.name);
                if (item.sorting == "Head") container = faceModContainer;
                else container = bodyModContainer;

                if (container == null) continue;

                var blendSlider = Instantiate(modSliderObject, container);
                blendSlider.Init(character, item);
                ModSliders.Add(blendSlider);
            }
        }

        public void OpenPage(GameObject page)
        {
            currentPage.SetActive(false);
            previousPage = currentPage;
            currentPage = page;
            currentPage.SetActive(true);
        }

        public void BackPage()
        {
            currentPage.SetActive(false);
            currentPage = previousPage;
            currentPage.SetActive(true);
            SwitchCatagory("");
        }

        public async Task<Outfit> SetOutfit(Outfit outfit)
        {
            var op = InstantiateAsync(outfit, character.transform);
            while (!op.isDone) await Task.Yield();

            var inst = op.Result[0];

            inst.gameObject.name = inst.gameObject.name.Replace("(Clone)", "");
            inst.gameObject.name = inst.gameObject.name.Replace(" ", "");

            inst.Attach();

            if(inst.TryGetComponent(out CreatorOutfitSet set))
            {
                foreach (var outfitID in set.outfits)
                {
                    if (OutfitDataBase.ContainsKey(outfitID))
                    {
                        Instantiate(OutfitDataBase[outfitID], character.transform);
                    }
                }
            }

            await Task.Yield();
            SwitchCatagory(outfit.Type.name);
            type = outfit.Type;
            return inst;
        }

        public void OnOutfitUpdate(Outfit outfit)
        {
            if(visibilityToggler) visibilityToggler.Set(outfit);

            if (outfit == null) return;
            if (outfit.Type.name == "Head" || outfit.Type.name == "Body")
            {
                GetBodyBlends();
                GetBodyMods();
            }
        }

        public void OnRigUpdate(SkinnedMeshRenderer rig)
        {
            GetBodyBlends();
            GetBodyMods();
        }

        public void RemoveOutfit()
        {
            if (type == null) return;
            character.RemoveOutfit(type);
        }

        public void SwitchCatagory(string catagory)
        {
            foreach (var item in outfitSelectors)
            {
                if(item.outfit.requiredTags.Length != 0)
                {
                    foreach (var r in item.outfit.requiredTags)
                    {
                        if (!character.ContainsTag(r))
                        {
                            item.SetVisable("");
                        }
                        else
                        {
                            item.SetVisable(catagory);
                        }
                    }
                }
                else
                {
                    item.SetVisable(catagory);
                }

                if(ShowOnlyPack != "")
                {
                    if (!ShowOnlyPack.Contains(item.outfit.PackName)) item.SetVisable("x");
                }
            }

            if (RemoveButton != null) RemoveButton.SetActive(!DisableRemoveByType.Contains(catagory));

            var outfit = character.GetOutfit(catagory);

            //SetColorPickerObject(outfit);
            if(visibilityToggler) visibilityToggler.Set(outfit);

            if (outfit == null) return;

            SwitchTextureCatagory(outfit.TextureCatagory);
            type = outfit.Type;

            this.catagory = catagory;
            onCategoryChanged?.Invoke(catagory);
        }

        public void SwitchTextureCatagory(string catagory)
        {
            foreach (var item in textureSelectors)
            {
                item.SetVisable(catagory);
            }
        }

        public void ReplaceCharacter(OutfitSystem character)
        {
            this.character = character;
        }

        public string GetCurrentCatagory()
        {
            return catagory;
        }

        public void ToggleWalk(bool value)
        {
            character.animator.SetBool("isWalk", value);
        }

        public Outfit GetOutfit(string outfitName)
        {
            return OutfitDataBase[outfitName];
        }
    }
}
