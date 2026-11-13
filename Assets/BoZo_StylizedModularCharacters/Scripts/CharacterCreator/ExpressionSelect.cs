using System.Threading.Tasks;
using UnityEngine;

namespace Bozo.ModularCharacters
{
    public class ExpressionSelect : MonoBehaviour
    {
        public OutfitSystem outfitSystem;
        public Animator animator;

        public string parameterID;
        float v;

        private void OnEnable()
        {
            if(outfitSystem) animator = outfitSystem.animator;
            if (outfitSystem) outfitSystem.OnMergeComplete.AddListener(OnMerge);
            if (outfitSystem) outfitSystem.OnColorChanged.AddListener(OnMerge);
        }

        private void OnDisable()
        {
            if (outfitSystem) outfitSystem.OnMergeComplete.RemoveListener(OnMerge);
            if (outfitSystem) outfitSystem.OnColorChanged.RemoveListener(OnMerge);
        }

        public void SetExpression(float value)
        {
            if (!animator) return;
            v = value;
            animator.SetFloat(parameterID, v);
        }

        public async void OnMerge()
        {
            //await Task.Delay(2);
            SetExpression(v);
        }
    }
}