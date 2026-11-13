using UnityEngine;
using UnityEngine.UI;

namespace Bozo.ModularCharacters
{
    public class OutfitSelector : MonoBehaviour 
    {
        public Image icon;
        public Image visual;
        public Outfit outfit;
        public Outfit spawnedOutfit;
        private CharacterCreator characterCreator;   
        private Button button;

        public Color ActiveColor;
        public Color InactiveColor;
        public void Init(Outfit outfit, CharacterCreator characterCreator)
        {
            button = GetComponentInChildren<Button>();
            button.onClick.AddListener(OnSelect);

            this.outfit = outfit;
            this.characterCreator = characterCreator;
            icon.overrideSprite = this.outfit.OutfitIcon;
        }

        private void OnEnable()
        {
            if (characterCreator)
            {
                characterCreator.character.OnOutfitChanged += OnOutfitChanged;
            }
        }

        private void OnDisable()
        {
            if (characterCreator)
            {
                characterCreator.character.OnOutfitChanged += OnOutfitChanged;
            }
        }

        private void OnOutfitChanged(Outfit ot)
        {
            if (spawnedOutfit != null)
            {
                visual.color = ActiveColor;
            }
            else
            {
                visual.color = InactiveColor;
            }
        }

        private async void OnSelect() 
        {
            spawnedOutfit = await characterCreator.SetOutfit(outfit);
            OnOutfitChanged(null);
        }

        public void SetVisable(string type) 
        {
            if (outfit.Type == null)
            {
               gameObject.SetActive(false);
               Debug.LogWarning(outfit.name + " is missing an outfitType and will not show in the character creator");
               return;
            }

            if(outfit.Type.name == type) 
            {
                gameObject.SetActive(true);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
