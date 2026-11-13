using UnityEngine;

namespace Bozo.ModularCharacters
{
    public class Attachment : MonoBehaviour
    {
        [SerializeField] string attachPoint;

        public void Start()
        {
            Attach();
        }

        [ContextMenu("Attach")]
        public void Attach()
        {
            var os = GetComponentInParent<OutfitSystem>();
            os.AttachToSocket(transform, attachPoint);
        }
    }

}