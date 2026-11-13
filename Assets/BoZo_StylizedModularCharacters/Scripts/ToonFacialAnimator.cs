using UnityEngine;

namespace Bozo.ModularCharacters
{
    public class ToonFacialAnimator : MonoBehaviour
    {

        public Vector3 commonLeftEyeOffset;
        public Vector3 commonRightEyeOffset;
        public Vector3 commonMouthOffset;

        public float eyeRotation;
        public float mouthRotation;
        public Vector3 spriteScale;


        private SpriteRenderer leftEye;
        private SpriteRenderer rightEye;
        private SpriteRenderer mouth;

        [SerializeField] ToonFacePreset[] facePresets;

        [SerializeField] SkinnedMeshRenderer rigOverride;

        [ContextMenu("test")]
        public void test()
        {
            SetFace(0);
        }

        private void GenerateFace()
        {

            Transform headBone = null;
            Transform eyeBoneL = null;
            Transform eyeBoneR = null;

            if (rigOverride) 
            {
                foreach (var bone in rigOverride.bones)
                {
                    if (bone.name == "head"){ headBone = bone; }
                    if (bone.name == "eyeRoot_l") { eyeBoneL = bone; }
                    if (bone.name == "eyeRoot_r") { eyeBoneR = bone; }
                }
            }
            else 
            {
                var system = GetComponentInParent<OutfitSystem>();
                if (!system) system = GetComponentInChildren<OutfitSystem>();



                headBone = system.GetBones()["head"];
                eyeBoneL = system.GetBones()["eyeRoot_l"];
                eyeBoneR = system.GetBones()["eyeRoot_r"];
            }


            if (leftEye == null) leftEye = new GameObject("ToonLeftEye", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            if (rightEye == null) rightEye = new GameObject("ToonRightEye", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            if (mouth == null) mouth = new GameObject("ToonMouth", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();


            leftEye.transform.parent = eyeBoneL;
            rightEye.transform.parent = eyeBoneR;
            mouth.transform.parent = headBone;

            leftEye.transform.localPosition = commonLeftEyeOffset;
            leftEye.transform.localScale = spriteScale;
            leftEye.transform.localRotation = Quaternion.Euler(0, eyeRotation, 180);

            rightEye.transform.localPosition = commonRightEyeOffset;
            rightEye.transform.localScale = spriteScale;
            rightEye.transform.localRotation = Quaternion.Euler(0, eyeRotation, 0);

            mouth.transform.localPosition = commonMouthOffset;
            mouth.transform.localScale = spriteScale;
            mouth.transform.localRotation = Quaternion.Euler(mouthRotation, 0, 0);
        }

        public void SetFace(int preset)
        {
            if(leftEye == null || rightEye == null || mouth == null)
            {
                GenerateFace();
            }
            var set = facePresets[preset];

            leftEye.sprite = set.leftEyeSprite;
            leftEye.flipX = set.leftEyeFlip;
            leftEye.transform.localPosition = set.leftEyeOffset + commonLeftEyeOffset;
            leftEye.transform.localScale = set.leftEyeScale + spriteScale;

            rightEye.sprite = set.rightEyeSprite;
            rightEye.flipX = set.rightEyeFlip;
            rightEye.transform.localPosition = set.rightEyeOffset + commonRightEyeOffset;
            rightEye.transform.localScale = set.rightEyeScale + spriteScale;

            mouth.sprite = set.mouthSprite;
            mouth.flipX = set.mouthFlip;
            mouth.transform.localPosition = set.mouthOffset + commonMouthOffset;
            mouth.transform.localScale = set.mouthScale + spriteScale;
        }

        [System.Serializable]
        public class ToonFacePreset
        {
            public string presetName;

            public Sprite leftEyeSprite;
            public bool leftEyeFlip = true;
            public Vector3 leftEyeOffset;
            public Vector3 leftEyeScale = Vector3.zero;


            public Sprite rightEyeSprite;
            public bool rightEyeFlip = false;
            public Vector3 rightEyeOffset;
            public Vector3 rightEyeScale = Vector3.zero;


            public Sprite mouthSprite;
            public bool mouthFlip = true;
            public Vector3 mouthOffset;
            public Vector3 mouthScale = Vector3.zero;
        }
    }
}

