using UnityEngine;
using UnityEngine.UI;

namespace Bozo.ModularCharacters
{
    public class SaveSelector : MonoBehaviour
    {
        public Image icon;
        public CharacterData data;
        private CreatorSaveLoad cc;
        public Button button;
        public void Init(CharacterData characterData, Sprite icon, CreatorSaveLoad characterCreator)
        {
            button = GetComponentInChildren<Button>();
            button.onClick.AddListener(OnSelect);

            this.data = characterData;
            this.cc = characterCreator;
            this.icon.overrideSprite = icon;
        }

        private void OnSelect()
        {
            cc.LoadCharacter(data);
        }
    }
}
