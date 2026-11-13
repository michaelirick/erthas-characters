using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bozo.ModularCharacters
{
    public static class Bozo_SavePatcher
    {
        public static CharacterData UpdateSave(CharacterData data)
        {

            //Debug.Log(data.versionID);
            if (data.versionID < 1)
            {
                bool animeFlag = false;
                foreach (var item in data.outfitDatas)
                {
                    if (item.outfit == "Head/Head_Young" || item.outfit == "Head/Head_Young_v2") { animeFlag = true; break; }
                }
                if (animeFlag)
                {
                    UpdateANIME(data);
                }
                if (data.versionID < 1)
                {
                    data = Update1(data);
                }
            }

            if (data.versionID < 2) { data = Update2(data); }
            if (data.versionID < 3) { data = Update3(data); }
            if (data.versionID < 4) { data = Update4(data); }
            return data;
        }

        private static CharacterData UpdateANIME(CharacterData data)
        {
            var newOutfitData = new List<OutfitData>(data.outfitDatas);

            for (int i = 0; i < data.outfitDatas.Count; i++)
            {
                var od = data.outfitDatas[i];
                if (od.outfit == "Head/Head_Young" || od.outfit == "Head/Head_Young_v2")
                {
                    od.outfit = "Head/Head_AnimeYoung";
                }

                if (od.outfit == "Eyes/Eyes_Eyes01")
                {
                    od.outfit = "Eyes/Eyes_AnimeBasic";
                }

                if (od.outfit == "Eyes/Eyes_Eyes01")
                {
                    od.outfit = "Eyes/Eyes_AnimeBasic";
                }

                if (od.outfit == "Leggings/Leggings_Stockings")
                {
                    od.outfit = "Leggings/Leggings_Stocking";
                }

                if (od.outfit == "Socks/Socks_SimpleSocks")
                {
                    od.outfit = "Socks/Socks_BasicSocks";
                }

                if (od.outfit == "UnderUpper/UnderUpper_SimpleUnderShirt")
                {
                    od.outfit = "UnderUpper/UnderUpper_SimpleUnderShirt2";
                }

                if (od.outfit == "Body/Body_BasicBody")
                {
                    od.outfit = "Body/Body_AnimeBasic";
                }

                if (od.outfit == "Overall/Overall_Overall")
                {
                    od.outfit = "Top/Top_Overall";
                }

                if (od.outfit == "Overall/Overall_Sundress")
                {
                    od.outfit = "Top/Top_Sundress";
                }

                if (od.outfit == "Overall/Overall_SmartDress")
                {
                    od.outfit = "Top/Top_SmartDress";
                }

                if (od.outfit.Contains("Hands"))
                {
                    od.outfit = od.outfit.Replace("Hands", "Gloves");
                }
                data.outfitDatas[i] = od;
            }

            for (int i = 0; i < data.bodyModsKeys.Count; i++)
            {
                if (data.bodyModsKeys[i] == "eye_l") { data.bodyModsKeys[i] = "IrisRoot_l";};
            }

            float curvy = 100;
            for (int i = 0; i < data.bodyIDs.Count; i++)
            {
                if (data.bodyIDs[i] == "BodyType") { curvy -= data.bodyShapes[i];}  
            }
            data.bodyIDs.Add("Curvy");
            data.bodyShapes.Add(curvy);

            data.material = "BSMC_AnimeShader";
            data.versionID = 2;
            return data;
        }

        private static CharacterData Update1(CharacterData data)
        {
            OutfitData pupilOutfit = new OutfitData();
            OutfitData irisOutfit = new OutfitData();
            OutfitData UnderTopOutfit = new OutfitData();
            OutfitData UnderBottomOutfit = new OutfitData();

            OutfitData eyes = new OutfitData();
            int eyesIndex = 0;
            OutfitData body = new OutfitData();
            int bodyIndex = 0;
            OutfitData head = new OutfitData();
            int headIndex = 0;



            for (int i = 0; i < data.outfitDatas.Count; i++)
            {
                if (data.outfitDatas[i].outfit.Contains("Eyes/"))
                {
                    eyes = data.outfitDatas[i];
                    eyesIndex = i;
                }
                if (data.outfitDatas[i].outfit.Contains("Body/"))
                {
                    body = data.outfitDatas[i];
                    bodyIndex = i;
                }
                if (data.outfitDatas[i].outfit.Contains("Head/"))
                {
                    head = data.outfitDatas[i];
                    headIndex = i;
                }
            }

            //Body Changes
            if (body.pattern == "Pattern/Underwear_Boxers_D")
            {
                UnderTopOutfit.outfit = "";
                UnderBottomOutfit.outfit = "UnderLower/UnderLower_SimpleBoxers";
            }
            if (body.pattern == "Pattern/Underwear_BraPanties_D")
            {
                UnderTopOutfit.outfit = "UnderUpper/UnderUpper_SimpleBra";
                UnderBottomOutfit.outfit = "UnderLower/UnderLower_SimplePanties";
            }
            if (body.pattern == "Pattern/Underwear_Neutral_D")
            {
                UnderTopOutfit.outfit = "UnderUpper/UnderUpper_SimpleUnderShirt";
                UnderBottomOutfit.outfit = "UnderLower/UnderLower_SimpleBoxers";
            }


            //Eye Changes
            var pupil = eyes.decal;
            if (pupil != "")
            {
                pupil = pupil.Replace("Decal", "Pupil");

                pupilOutfit.outfit = pupil;
                pupilOutfit.colors = eyes.decalColors;

                eyes.decal = "";
            }
            var iris = eyes.pattern;
            if (pupil != "")
            {
                iris = iris.Replace("Pattern", "Eyes");

                irisOutfit.outfit = iris;
                irisOutfit.colors = eyes.patternColors;

                eyes.pattern = "";
            }

            head.colors[6] = eyes.colors[0];
            head.colors[7] = eyes.colors[1];



            UnderBottomOutfit.colors = body.patternColors;
            UnderTopOutfit.colors = body.patternColors;

            body.pattern = "";
            body.decal = "";


            if (pupilOutfit.outfit != "") data.outfitDatas.Add(pupilOutfit);
            if (irisOutfit.outfit != "") data.outfitDatas.Add(irisOutfit);
            if (UnderTopOutfit.outfit != "") data.outfitDatas.Add(UnderTopOutfit);
            if (UnderBottomOutfit.outfit != "") data.outfitDatas.Add(UnderBottomOutfit);

            data.outfitDatas[eyesIndex] = eyes;
            data.outfitDatas[headIndex] = head;
            data.outfitDatas[bodyIndex] = body;

            data.outfitDatas.RemoveAt(eyesIndex);

            data.bodyShapes.Add(100); //Neck
            data.bodyIDs.Add("NeckThickness"); //Neck




            data.versionID = 1;
            return data;
        }

        private static CharacterData Update2(CharacterData data)
        {
            data.material = "BSMC_StylizedShader";
            data.versionID = 2;
            return data;

        }

        private static CharacterData Update3(CharacterData data)
        {
            var newOutfitData = new List<OutfitData>();

            for (int i = 0; i < data.outfitDatas.Count; i++)
            {
                var od = data.outfitDatas[i];
                var colors = new List<Color>(od.colors);

                if (od.outfit == "Head/Head_YoungSharpHead")
                {
                    od.outfit = "Head/Head_BasicHead";
                }

                if (od.outfit == "Head/Head_SharpHead")
                {
                    od.outfit = "Head/Head_BasicHead";
                    var outfitData = new OutfitData();
                    outfitData.outfit = "HeadType/HeadType_SharpHead";
                    outfitData.colors = colors;
                    data.outfitDatas.Add(outfitData);
                }

                if (od.outfit == "Head/Head_Stern")
                {
                    od.outfit = "Head/Head_BasicHead";
                    var outfitData = new OutfitData();
                    outfitData.outfit = "HeadType/HeadType_Stern";
                    outfitData.colors = colors;
                    data.outfitDatas.Add(outfitData);
                }

                if (od.outfit == "Head/Head_BasicHead")
                {
                    od.colors[2] = colors[0];
                    od.colors[5] = colors[2];
                    od.colors[3] = colors[6];
                    od.colors[4] = colors[7];

                    var index = data.faceIDs.IndexOf("LashLength");
                    if(index != -1)
                    {
                        var outfitData = new OutfitData();
                        outfitData.colors = new List<Color>(colors);
                        outfitData.colors[0] = colors[3];
                        if (data.faceShapes[index] < 50)
                        {
                            outfitData.outfit = "EyeLashes/EyeLashes_StylizedShortLashes";
                        }
                        else
                        {
                            outfitData.outfit = "EyeLashes/EyeLashes_StylizedLongLashes";
                        }
                        data.outfitDatas.Add(outfitData);
                    }

                    index = data.faceIDs.IndexOf("BrowsThickness");
                    if (index != -1)
                    {
                        var outfitData = new OutfitData();
                        outfitData.colors = new List<Color>(colors);
                        outfitData.colors[0] = colors[4];
                        if (data.faceShapes[index] < 50)
                        {
                            outfitData.outfit = "EyeBrows/EyeBrows_StylizedBasicBrows";
                        }
                        else
                        {
                            outfitData.outfit = "EyeBrows/EyeBrows_StylizedThickBrows";
                        }
                        data.outfitDatas.Add(outfitData);
                    }

                    var teeth = new OutfitData();
                    teeth.outfit = "Teeth/Teeth_StylizedBasicTeeth";
                    teeth.colors = new List<Color>(colors);
                    teeth.colors[0] = colors[5];
                    data.outfitDatas.Add(teeth);


                }
                if (od.outfit == "Head/Head_AnimeYoung")
                {
                    var outfitData = new OutfitData();
                    colors[0] = od.colors[6];
                    outfitData.colors = colors;
                    outfitData.outfit = "Teeth/Teeth_AnimeBasicTeeth";
                    data.outfitDatas.Add(outfitData);
                }

                if (od.outfit == "Body/Body_BasicBody" || od.outfit == "Body/Body_StrongBody")
                {
                    od.colors[2] = colors[0];

                }
                data.outfitDatas[i] = od;
            }



            //data.outfitDatas = newOutfitData;

            data.versionID = 3;
            return data;

        }

        private static CharacterData Update4(CharacterData data)
        {
            for (int i = 0; i < data.outfitDatas.Count; i++)
            {
                var d = data.outfitDatas[i];
                d.alpha = 1;
                data.outfitDatas[i] = d;
            }

            data.versionID = 4;
            return data;

        }
    }
}
