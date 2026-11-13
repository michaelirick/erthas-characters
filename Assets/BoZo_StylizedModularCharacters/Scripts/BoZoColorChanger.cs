using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bozo.ModularCharacters
{

    public class BoZoColorChanger : MonoBehaviour
    {

        [SerializeField] bool initOnAwake;
        [SerializeField] TextureSize textureSize;
        public Material material;
        private Material orignialMaterial;
        private Material materialInstance;
        private RenderTexture rt;
        bool init;

        private Material copyMat;


        private void Awake()
        {
            if (initOnAwake) Init();
        }

        public void Init()
        {
            if (init) return;

            if(copyMat == null)
            {
                copyMat = new Material(Shader.Find("Hidden/TextureLayerer"));
                copyMat.SetFloat("_Alpha", 1);
                copyMat.SetFloat("_UsingIDMap", 0);
            }

            Renderer r = null;
            r = GetComponentInChildren<Renderer>();
            if (r)
            {
                orignialMaterial = r.sharedMaterial;
                if (orignialMaterial.shader.name != "Shader Graphs/BSMC_ClothesCustom")
                {
                    Debug.LogWarning($"Renderer's Material is not set to Expected Shader >Shader Graphs/BSMC_ClothesCustom< and may not work as expected");
                }

                for (int i = 0; i < 8; i++)
                {
                    if(r.sharedMaterial.HasProperty($"_Color_{i + 1}"))
                    {
                        copyMat.SetColor($"_Color_{i + 1}", orignialMaterial.GetColor($"_Color_{i + 1}"));
                    }
                }
            }

            int size = ((int)textureSize);
            if (textureSize == TextureSize.UseTextureSize) size = orignialMaterial.mainTexture.width;

            rt = new RenderTexture(size, size, 0);

            if (material == null)
            {
                //creating one based off of the material already on the renderer
                r = GetComponentInChildren<Renderer>();
                material = new Material(r.sharedMaterial);
            }

            materialInstance = new Material(material);

            copyMat.mainTexture = rt;
            copyMat.SetTexture("_BlendTex", orignialMaterial.mainTexture);

            Graphics.Blit(orignialMaterial.mainTexture,rt, copyMat);

            foreach (var item in GetComponentsInChildren<Renderer>())
            {
                item.sharedMaterial = materialInstance;
            }

            materialInstance.mainTexture = rt;
        }

        private void OnDestroy()
        {
            if (rt) rt.Release();
            if (copyMat) Destroy(copyMat);
        }

        private enum TextureSize
        {
            UseTextureSize = 0,
            _512 = 512,
            _1024 = 1024,
            _2048 = 2048,
            _4096 = 4096
        }
    }

}