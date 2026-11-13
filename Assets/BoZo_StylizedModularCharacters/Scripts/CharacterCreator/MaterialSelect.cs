using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bozo.ModularCharacters
{
    public class MaterialSelect : MonoBehaviour
    {
        public CharacterCreator characterCreator;
        public Transform container;
        public MaterialSelector Selector;

        private void Start()
        {
            var mats = Resources.LoadAll<MaterialPackage>("");

            foreach (var mat in mats)
            {
                var s = Instantiate(Selector, container);
                s.Init(mat, characterCreator);
            }
        }
    }
}
