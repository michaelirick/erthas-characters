using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;


namespace Bozo.ModularCharacters
{
    public class CreatorSaveLoad : MonoBehaviour
    {
        public CharacterCreator cc;

        [Header("Save Dependencies")]
        [SerializeField] SaveSelector saveSelector;
        [SerializeField] Dictionary<string, SaveSelector> saveSlots = new Dictionary<string, SaveSelector>();
        [SerializeField] Transform saveContainer;
        [SerializeField] GameObject DeleteConfirmWindow;
        [SerializeField] TMP_Text DeleteCharacterNameText;
        public TMP_InputField CharacterName;

        [Header("Icon")]
        [SerializeField] RenderTexture iconTexture;

        private bool loadOutfit = true;
        private bool loadBody = true;
        private bool loadHair = true;
        private bool loadShader = true;


        public void Awake()
        {
            UpdateCharacterSaves();
        }

        public void UpdateCharacterSaves()
        {
            if (saveContainer == null)
            {
                Debug.LogWarning("Save Container not assigned. Assigned one to display saves");
            }

            var saves = saveSlots.Values.ToArray();
            foreach (var item in saves)
            {
                Destroy(item.gameObject);
            }
            saveSlots.Clear();

            if (!System.IO.Directory.Exists(BMAC_SaveSystem.filePath))
            {
                System.IO.Directory.CreateDirectory(BMAC_SaveSystem.filePath);
                System.IO.Directory.CreateDirectory(BMAC_SaveSystem.iconFilePath);
                print("Created Save JSON save Location At: " + BMAC_SaveSystem.filePath);
            }

            //Scriptable Objects Loading-------------------------------------------------------------------------------------------------------------------------------
            var saveObjects = Resources.LoadAll<CharacterObject>("").ToList();

            for (int i = 0; i < saveObjects.Count; i++)
            {
                var ob = saveObjects[i];
                if (saveSlots.ContainsKey(ob.data.characterName))
                {
                    continue;
                }
                else
                {
                    var selector = Instantiate(saveSelector, saveContainer);
                    Sprite icon = null;
                    if (ob.icon != null)
                    {
                        icon = Sprite.Create(ob.icon, new Rect(0, 0, ob.icon.width, ob.icon.height), new Vector2(0.5f, 0.5f));
                    }
                    selector.Init(ob.data, icon, this);
                    saveSlots.Add(ob.data.characterName, selector);
                }
            }

            //Legacy Anime----------------------------------------------------------------------------------------------------------------------------------------------
            string path = BMAC_SaveSystem.filePath;
            List<string> jsonFiles = System.IO.Directory.GetFiles(path, "*.json").ToList();
            List<string> icons = System.IO.Directory.GetFiles(BMAC_SaveSystem.iconFilePath, "*.png").ToList();

            try
            {
                var animeFilePath = BMAC_SaveSystem.filePath.Replace("BoZo_StylizedModularCharacters/CustomCharacters", "BoZo_ModularAnimeCharacters/CharacterSaves");
                var animeIconFilePath = animeFilePath + "/Icons";
                if (System.IO.Directory.Exists(animeFilePath))
                {
                    jsonFiles.AddRange(System.IO.Directory.GetFiles(animeFilePath, "*.json"));
                    icons.AddRange(System.IO.Directory.GetFiles(animeIconFilePath, "*.png"));
                }
            }
            catch
            {

            }

            //Scriptable Objects Loading-------------------------------------------------------------------------------------------------------------------------------
            for (int i = 0; i < jsonFiles.Count; i++)
            {
                var json = System.IO.File.ReadAllText(jsonFiles[i]);
                var data = JsonUtility.FromJson<CharacterData>(json);
                var image = System.IO.File.ReadAllBytes(icons[i]);
                var texture = new Texture2D(2, 2);
                Sprite icon = null;

                if (saveSlots.ContainsKey(data.characterName)) continue;

                if (texture.LoadImage(image))
                {
                    icon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                }

                var selector = Instantiate(saveSelector, saveContainer);
                selector.Init(data, icon, this);
                saveSlots.Add(data.characterName, selector);
            }

            //Sorting---------------------------------------------------------------------------------------------------------------------------------------------------
            SaveSelector[] items = saveContainer.GetComponentsInChildren<SaveSelector>(true)
                .Where(item => item.transform.parent == saveContainer)
                .OrderBy(item => item.data.characterName, System.StringComparer.OrdinalIgnoreCase)
                .ToArray();

            for (int i = 0; i < items.Length; i++)
            {
                items[i].transform.SetSiblingIndex(i);
            }

        }

        public void LoadCharacter(CharacterData data)
        {
            List<string> excludeList = new List<string>();

            if (!loadOutfit) { excludeList.AddRange(BMAC_SaveSystem.GetOutfitList()); }
            if (!loadHair) { excludeList.AddRange(BMAC_SaveSystem.GetHairList()); }
            if (!loadBody) { excludeList.AddRange(BMAC_SaveSystem.GetBodyList()); }

            if (excludeList.Count == 0) excludeList = null;

            if (CharacterName == null) return;
            CharacterName.text = data.characterName;
            BMAC_SaveSystem.LoadCharacter(cc.character, data, cc.character.async, loadShader, loadBody, excludeList);
            //cc.character.LoadCharacter(data);
        }

        public void DeleteCharacter()
        {
            if (DeleteConfirmWindow == null)
            {
                Debug.LogWarning("Delete Character Confirmation Window is no assigned please assign one to delete character", gameObject);
            }

            if (CharacterName == null) return;
            if (CharacterName.text == "") return;
            DeleteCharacterNameText.text = "Delete: " + CharacterName.text;
            DeleteConfirmWindow.SetActive(true);
        }

        public void ConfirmDelete()
        {
            if (CharacterName == null) return;
            BMAC_SaveSystem.DeleteCharacter(CharacterName.text);
            CharacterName.text = "";
            UpdateCharacterSaves();
        }

        [ContextMenu("Save")]
        public void SaveCharacter()
        {
            if (CharacterName == null)
            {
                Debug.LogWarning("Save Name InputField not Assigned Please assign one to save character", gameObject);
                return;
            }
            if (CharacterName.text.Length == 0)
            {
                Debug.LogWarning("Please enter in a name with at least one letter");
                return;
            }

            RenderTexture.active = iconTexture;
            Texture2D icon = new Texture2D(iconTexture.width, iconTexture.height, TextureFormat.RGBA32, false);
            Rect rect = new Rect(new Rect(0, 0, iconTexture.width, iconTexture.height));
            icon.ReadPixels(rect, 0, 0);
            icon.Apply();

            byte[] bytes = icon.EncodeToPNG();

            Texture2D characterIcon = null;


#if UNITY_EDITOR
            var settings = CharacterToolSettingsProvider.Get();
            var iconPath = $"{settings.iconFolder}/{CharacterName.text}";

            System.IO.File.WriteAllBytes(iconPath + ".png", bytes);
            AssetDatabase.Refresh();

            characterIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath + ".png");
            TextureImporter importer = AssetImporter.GetAtPath(iconPath + ".png") as TextureImporter;

            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }
#endif

            if (!System.IO.Directory.Exists(BMAC_SaveSystem.iconFilePath))
            {
                System.IO.Directory.CreateDirectory(BMAC_SaveSystem.iconFilePath);
            }

            System.IO.File.WriteAllBytes(BMAC_SaveSystem.iconFilePath + "/" + CharacterName.text + ".png", bytes);
            BMAC_SaveSystem.SaveCharacter(cc.character, CharacterName.text, characterIcon);

            UpdateCharacterSaves();
        }

        public void ToggleOutfit(bool value) { loadOutfit = value; }
        public void ToggleBody(bool value) { loadBody = value; }
        public void ToggleHair(bool value) { loadHair = value; }
        public void ToggleShader(bool value) { loadShader = value; }
    }
}
