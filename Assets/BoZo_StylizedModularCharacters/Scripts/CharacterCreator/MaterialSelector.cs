using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Bozo.ModularCharacters
{
    public class MaterialSelector : MonoBehaviour
    {
        public Image icon;
        public MaterialPackage mat;
        private CharacterCreator characterCreator;
        private Button button;
        public void Init(MaterialPackage mat, CharacterCreator characterCreator)
        {
            button = GetComponentInChildren<Button>();
            button.onClick.AddListener(OnSelect);

            this.mat = mat;
            this.characterCreator = characterCreator;
            icon.overrideSprite = this.mat.icon;
        }

        private void OnSelect()
        {
            characterCreator.character.UpdateMaterial(mat);
        }
    }
}
