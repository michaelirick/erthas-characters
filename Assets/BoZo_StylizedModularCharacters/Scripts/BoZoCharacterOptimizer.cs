using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bozo.ModularCharacters
{
    public class BoZo_CharacterOptimizer
    {
        private static string path = "/BoZo_StylizedModularCharacters/CustomCharacters/Prefabs";

        private MergedMaterialData[] mergedMaterialDatas;
        private OutfitSystem sourceSystem;

        private sealed class MergeSource
        {
            public SkinnedMeshRenderer renderer;
            public Outfit outfit;
            public Mesh mesh;
            public int vertexOffset;
            public Matrix4x4 positionMatrix;
            public Matrix4x4 normalMatrix;
        }

        private struct BlendFrameSource
        {
            public MergeSource source;
            public int shapeIndex;
            public int frameIndex;
            public float currentWeight;
        }

        public void MergeButTheBetterOneV2(OutfitSystem outfitSystem)
        {
            GameObject temporaryBody = null;
            Mesh newMesh = null;
            bool committed = false;

            // Only needed to restore states if a merge fails partway through.
            var disabledOutfits = new List<GameObject>();
            var disabledAnimators = new List<Animator>();

            try
            {
                if (outfitSystem == null) throw new ArgumentNullException(nameof(outfitSystem));

                SkinnedMeshRenderer destination = outfitSystem.GetCharacterBody();
                if (destination == null || destination.transform.parent == null)
                    throw new InvalidOperationException("Character body or its parent is missing.");

                temporaryBody = UnityEngine.Object.Instantiate(
                    destination.transform.parent.gameObject, outfitSystem.transform);

                SkinnedMeshRenderer referenceBody =
                    temporaryBody.GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (referenceBody == null)
                    throw new InvalidOperationException("Temporary body has no SkinnedMeshRenderer.");

                temporaryBody.transform.localPosition = Vector3.zero;
                temporaryBody.transform.localRotation = referenceBody.transform.localRotation;

                foreach (BodyShapeModifier mod in temporaryBody.GetComponentsInChildren<BodyShapeModifier>(true))
                    mod.ResetMod();

                Transform[] masterBones = referenceBody.bones;
                var masterBoneIndices = new Dictionary<string, int>(masterBones.Length);

                for (int i = 0; i < masterBones.Length; i++)
                {
                    Transform bone = masterBones[i];
                    if (bone == null) throw new InvalidOperationException($"Master bone {i} is null.");

                    // Keep the first occurrence, matching the previous name-based mapping.
                    if (!masterBoneIndices.ContainsKey(bone.name))
                        masterBoneIndices.Add(bone.name, i);

                    if (outfitSystem.bindPose.TryGetValue(bone.name, out var pose))
                    {
                        bone.localPosition = pose.localPosition;
                        bone.localRotation = pose.localRotation;
                        bone.localScale = pose.localScale;
                    }
                    else
                    {
                        bone.localScale = Vector3.one;
                    }
                }

                var sources = new List<MergeSource>();
                int totalVertexCount = 0;
                int maxSourceVertexCount = 0;

                // Collect first, and only disable source outfits after data collection.
                foreach (var item in outfitSystem.GetOutfits())
                {
                    if (item == null || !item.mergeMesh || outfitSystem.hiddenTypes.ContainsKey(item.Type))
                        continue;

                    foreach (var renderer in item.skinnedRenderers)
                    {
                        if (renderer == null || !renderer.gameObject.activeSelf || renderer.sharedMesh == null)
                            continue;

                        Mesh mesh = renderer.sharedMesh;
                        var source = new MergeSource
                        {
                            renderer = renderer,
                            outfit = item,
                            mesh = mesh,
                            vertexOffset = totalVertexCount,
                            positionMatrix = referenceBody.transform.worldToLocalMatrix *
                                             renderer.transform.localToWorldMatrix
                        };
                        source.normalMatrix = source.positionMatrix.inverse.transpose;
                        sources.Add(source);

                        checked { totalVertexCount += mesh.vertexCount; }
                        maxSourceVertexCount = Math.Max(maxSourceVertexCount, mesh.vertexCount);
                    }
                }

                if (sources.Count == 0 || totalVertexCount == 0)
                    throw new InvalidOperationException("No active outfit skinned meshes to merge.");

                var vertices = new List<Vector3>(totalVertexCount);
                var normals = new List<Vector3>(totalVertexCount);
                var tangents = new List<Vector4>(totalVertexCount);
                var uv = new List<Vector2>(totalVertexCount);
                var colors = new List<Color>(totalVertexCount);
                var boneWeights = new List<BoneWeight>(totalVertexCount);

                // The original implementation places texture-merged outfits into submesh 0.
                var trianglesBySubmesh = new List<List<int>> { new List<int>() };
                var materials = new List<Material> { destination.sharedMaterial };

                // String -> weight -> frame sources. Handles multiple blendshape frames.
                var blendshapeGroups = new SortedDictionary<string, SortedDictionary<float, List<BlendFrameSource>>>(
                    StringComparer.Ordinal);
                var currentBlendshapeWeights = new Dictionary<string, float>(StringComparer.Ordinal);

                // Reused scratch lists avoid Mesh.vertices / Mesh.GetTriangles array allocations.
                var srcVertices = new List<Vector3>(maxSourceVertexCount);
                var srcNormals = new List<Vector3>(maxSourceVertexCount);
                var srcTangents = new List<Vector4>(maxSourceVertexCount);
                var srcUV = new List<Vector2>(maxSourceVertexCount);
                var srcColors = new List<Color>(maxSourceVertexCount);
                var srcTriangles = new List<int>();

                foreach (MergeSource source in sources)
                {
                    Mesh mesh = source.mesh;
                    SkinnedMeshRenderer smr = source.renderer;
                    int count = mesh.vertexCount;

                    srcVertices.Clear(); mesh.GetVertices(srcVertices);
                    srcNormals.Clear(); mesh.GetNormals(srcNormals);
                    srcTangents.Clear(); mesh.GetTangents(srcTangents);
                    srcUV.Clear(); mesh.GetUVs(0, srcUV);
                    srcColors.Clear(); mesh.GetColors(srcColors);

                    bool hasNormals = srcNormals.Count == count;
                    bool hasTangents = srcTangents.Count == count;
                    bool hasUV = srcUV.Count == count;
                    bool hasColors = srcColors.Count == count;

                    for (int v = 0; v < count; v++)
                    {
                        vertices.Add(source.positionMatrix.MultiplyPoint3x4(srcVertices[v]));
                        normals.Add(hasNormals
                            ? source.normalMatrix.MultiplyVector(srcNormals[v]).normalized
                            : Vector3.up);

                        if (hasTangents)
                        {
                            Vector4 t = srcTangents[v];
                            Vector3 direction = source.positionMatrix.MultiplyVector(
                                new Vector3(t.x, t.y, t.z)).normalized;
                            tangents.Add(new Vector4(direction.x, direction.y, direction.z, t.w));
                        }
                        else tangents.Add(new Vector4(1, 0, 0, 1));

                        uv.Add(hasUV ? srcUV[v] : Vector2.zero);
                        colors.Add(hasColors ? srcColors[v] : Color.red);
                    }

                    Transform[] sourceBones = smr.bones;
                    var boneMap = new int[sourceBones.Length];
                    int fallbackIndex;
                    if (smr.rootBone == null ||
                        !masterBoneIndices.TryGetValue(smr.rootBone.name, out fallbackIndex))
                    {
                        throw new InvalidOperationException($"Root bone for {smr.name} is missing from the master skeleton.");
                    }

                    for (int b = 0; b < sourceBones.Length; b++)
                    {
                        Transform bone = sourceBones[b];
                        if (bone != null && masterBoneIndices.TryGetValue(bone.name, out int mapped))
                            boneMap[b] = mapped;
                        else
                            boneMap[b] = fallbackIndex;
                    }

                    BoneWeight[] sourceWeights = mesh.boneWeights;
                    if (sourceWeights.Length != count)
                        throw new InvalidOperationException($"Mesh '{mesh.name}' has {count} vertices but {sourceWeights.Length} legacy bone weights.");

                    for (int v = 0; v < sourceWeights.Length; v++)
                    {
                        BoneWeight bw = sourceWeights[v];
                        // A fallback is only used for invalid indices; weights are unchanged.
                        bw.boneIndex0 = (uint)bw.boneIndex0 < (uint)boneMap.Length ? boneMap[bw.boneIndex0] : fallbackIndex;
                        bw.boneIndex1 = (uint)bw.boneIndex1 < (uint)boneMap.Length ? boneMap[bw.boneIndex1] : fallbackIndex;
                        bw.boneIndex2 = (uint)bw.boneIndex2 < (uint)boneMap.Length ? boneMap[bw.boneIndex2] : fallbackIndex;
                        bw.boneIndex3 = (uint)bw.boneIndex3 < (uint)boneMap.Length ? boneMap[bw.boneIndex3] : fallbackIndex;
                        boneWeights.Add(bw);
                    }

                    List<int> dstTriangles = trianglesBySubmesh[0];
                    if (!source.outfit.mergeTexture)
                    {
                        dstTriangles = new List<int>();
                        trianglesBySubmesh.Add(dstTriangles);
                        materials.Add(source.outfit.material);
                    }

                    for (int s = 0; s < mesh.subMeshCount; s++)
                    {
                        srcTriangles.Clear();
                        mesh.GetTriangles(srcTriangles, s);
                        for (int t = 0; t < srcTriangles.Count; t++)
                            dstTriangles.Add(srcTriangles[t] + source.vertexOffset);
                    }

                    for (int b = 0; b < mesh.blendShapeCount; b++)
                    {
                        string rawName = mesh.GetBlendShapeName(b);
                        int dot = rawName.IndexOf('.');
                        string shapeName = dot >= 0 ? rawName.Substring(dot + 1) : rawName;
                        float currentWeight = smr.GetBlendShapeWeight(b);
                        if (!currentBlendshapeWeights.TryGetValue(shapeName, out float oldWeight) || currentWeight > oldWeight)
                            currentBlendshapeWeights[shapeName] = currentWeight;

                        if (!blendshapeGroups.TryGetValue(shapeName, out var frames))
                        {
                            frames = new SortedDictionary<float, List<BlendFrameSource>>();
                            blendshapeGroups.Add(shapeName, frames);
                        }

                        for (int f = 0; f < mesh.GetBlendShapeFrameCount(b); f++)
                        {
                            float weight = mesh.GetBlendShapeFrameWeight(b, f);
                            if (!frames.TryGetValue(weight, out var entries))
                            {
                                entries = new List<BlendFrameSource>();
                                frames.Add(weight, entries);
                            }
                            entries.Add(new BlendFrameSource
                            {
                                source = source,
                                shapeIndex = b,
                                frameIndex = f,
                                currentWeight = currentWeight
                            });
                        }
                    }
                }

                // Create the mesh completely before swapping it onto the destination.
                newMesh = new Mesh { name = "MergedMesh" };
                if (totalVertexCount > 65535)
                    newMesh.indexFormat = IndexFormat.UInt32;

                newMesh.SetVertices(vertices);
                newMesh.SetNormals(normals);
                newMesh.SetTangents(tangents);
                newMesh.SetUVs(0, uv);
                newMesh.SetColors(colors);
                newMesh.boneWeights = boneWeights.ToArray();

                var bindposes = new Matrix4x4[masterBones.Length];
                for (int b = 0; b < masterBones.Length; b++)
                    bindposes[b] = masterBones[b].worldToLocalMatrix * referenceBody.transform.localToWorldMatrix;
                newMesh.bindposes = bindposes;

                newMesh.subMeshCount = trianglesBySubmesh.Count;
                for (int s = 0; s < trianglesBySubmesh.Count; s++)
                    newMesh.SetTriangles(trianglesBySubmesh[s], s);

                // Only one full-size set of output arrays is allocated for all frames.
                var outDV = new Vector3[totalVertexCount];
                var outDN = new Vector3[totalVertexCount];
                var outDT = new Vector3[totalVertexCount];
                var inDV = new Vector3[maxSourceVertexCount];
                var inDN = new Vector3[maxSourceVertexCount];
                var inDT = new Vector3[maxSourceVertexCount];

                foreach (var shape in blendshapeGroups)
                {
                    foreach (var frame in shape.Value)
                    {
                        Array.Clear(outDV, 0, outDV.Length);
                        Array.Clear(outDN, 0, outDN.Length);
                        Array.Clear(outDT, 0, outDT.Length);

                        foreach (BlendFrameSource entry in frame.Value)
                        {
                            MergeSource source = entry.source;
                            // Unity expects blendshape read buffers sized to this source mesh.
                            // Resize only when source vertex count changes, not per frame.
                            int sourceCount = source.mesh.vertexCount;
                            if (inDV.Length != sourceCount)
                            {
                                inDV = new Vector3[sourceCount];
                                inDN = new Vector3[sourceCount];
                                inDT = new Vector3[sourceCount];
                            }
                            source.mesh.GetBlendShapeFrameVertices(
                                entry.shapeIndex, entry.frameIndex, inDV, inDN, inDT);

                            for (int v = 0; v < source.mesh.vertexCount; v++)
                            {
                                int dst = source.vertexOffset + v;
                                // Blendshape deltas are directions, not points.
                                // Do not normalize deltas: that would change their magnitude.
                                outDV[dst] = source.positionMatrix.MultiplyVector(inDV[v]);
                                outDN[dst] = source.normalMatrix.MultiplyVector(inDN[v]);
                                outDT[dst] = source.positionMatrix.MultiplyVector(inDT[v]);
                            }
                        }

                        newMesh.AddBlendShapeFrame(shape.Key, frame.Key, outDV, outDN, outDT);
                    }
                }

                // Commit once all processing has succeeded.
                Mesh previousMesh = outfitSystem.combinedMesh;
                destination.sharedMesh = newMesh;
                destination.sharedMaterials = materials.ToArray();
                foreach (var weight in currentBlendshapeWeights)
                {
                    int index = newMesh.GetBlendShapeIndex(weight.Key);
                    if (index >= 0) destination.SetBlendShapeWeight(index, weight.Value);
                }
                destination.localBounds = newMesh.bounds;
                destination.gameObject.SetActive(true);
                outfitSystem.combinedMesh = newMesh;
                committed = true;

                // Maintain the original behavior: merged outfits are disabled.
                // No Animator.Rebind() here; it was previously done only on animators being disabled.
                var seenOutfits = new HashSet<Outfit>();
                foreach (MergeSource source in sources)
                {
                    Outfit outfit = source.outfit;
                    if (!seenOutfits.Add(outfit)) continue;
                    Animator animator = outfit.GetComponentInChildren<Animator>();
                    if (animator != null)
                    {
                        animator.enabled = false;
                        disabledAnimators.Add(animator);
                    }
                    outfit.gameObject.SetActive(false);
                    disabledOutfits.Add(outfit.gameObject);
                }

                if (previousMesh != null && previousMesh != newMesh)
                    UnityEngine.Object.Destroy(previousMesh);
            }
            catch (Exception ex)
            {
                Debug.LogError("Character mesh merge failed: " + ex);
                if (!committed)
                {
                    foreach (GameObject go in disabledOutfits)
                        if (go != null) go.SetActive(true);
                    foreach (Animator animator in disabledAnimators)
                        if (animator != null) animator.enabled = true;
                    if (newMesh != null) UnityEngine.Object.Destroy(newMesh);
                }
            }
            finally
            {
                // Destroy is deferred to end of frame, which is safer at runtime.
                if (temporaryBody != null)
                    UnityEngine.Object.Destroy(temporaryBody);
            }
        }

        public void MergeButTheBetterOne(OutfitSystem outfitSystem)
        {
            try
            {
                var body = UnityEngine.Object.Instantiate(outfitSystem.GetCharacterBody().transform.parent,outfitSystem.transform).GetComponentInChildren<SkinnedMeshRenderer>(true);

                body.transform.parent.localPosition = Vector3.zero;
                body.transform.parent.localRotation = body.transform.localRotation;

                foreach (var mod in body.GetComponentsInChildren<BodyShapeModifier>())
                {
                    mod.ResetMod();
                }
                foreach (var bone in body.bones)
                {
                    if (!outfitSystem.bindPose.ContainsKey(bone.name)) continue;
                    bone.localPosition = outfitSystem.bindPose[bone.name].localPosition;
                    bone.localRotation = outfitSystem.bindPose[bone.name].localRotation;
                    bone.localScale = outfitSystem.bindPose[bone.name].localScale;
                }


                var outfitsToMerge = outfitSystem.GetOutfits();
                List<Renderer> rendererList = new List<Renderer>();

                var rootBone = body.rootBone;

                //Creating Bone Map
                var boneMap = new Dictionary<string, boneData>();
                var boneList = body.bones.ToList();



                for (int i = 0; i < boneList.Count; i++)
                {
                    try
                    {
                        var data = new boneData();
                        data.bone = boneList[i];
                        data.index = i;

                        boneMap.Add(boneList[i].name, data);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($" Bone {i} is missing In Bone Map " + e);
                        //return;
                    }
                }

                foreach (var bone in boneList)
                {
                    if (outfitSystem.bindPose.ContainsKey(bone.name))
                    {
                        bone.localPosition = outfitSystem.bindPose[bone.name].localPosition;
                        bone.localRotation = outfitSystem.bindPose[bone.name].localRotation;
                        bone.localScale = outfitSystem.bindPose[bone.name].localScale;
                    }
                    else
                    {
                        bone.localScale = Vector3.one;
                    }
                }


                int totalVertexCount = 0;


                try
                {

                    foreach (var item in outfitsToMerge)
                    {
                        if (item == null) continue;
                        if (!item.mergeMesh) continue;
                        if (outfitSystem.hiddenTypes.ContainsKey(item.Type)) continue;

                        foreach (var r in item.skinnedRenderers)
                        {
                            if (r.gameObject.activeSelf) { rendererList.Add(r); totalVertexCount += r.sharedMesh.vertexCount;}
                        }

                        //Stopping Animators
                        var anim = item.GetComponentInChildren<Animator>();
                        if (anim != null)
                        {
                            anim.enabled = false;
                            anim.Rebind();
                        }

                        item.gameObject.SetActive(false);
                    }
                }
                catch
                {
                    Debug.LogError("Mesh Merge Initalization failed");
                    return;
                }

                //Mesh Data Initaliztion
                List<Vector3> vertices = new List<Vector3>(totalVertexCount);
                List<Vector3> normals = new List<Vector3>(totalVertexCount);
                List<Vector4> tangents = new List<Vector4>(totalVertexCount);
                List<Vector2> uv = new List<Vector2>(totalVertexCount);
                List<Color> colors = new List<Color>(totalVertexCount);
                List<BoneWeight> boneWeights = new List<BoneWeight>(totalVertexCount);
                List<Matrix4x4> bindposes = new List<Matrix4x4>();
                List<List<int>> submeshTriangles = new List<List<int>>();
                List<Material> materials = new List<Material>();

                materials.Add(outfitSystem.GetCharacterBody().sharedMaterial);
                List<string> finalBlendshapeOrder = new List<string>();
                Dictionary<string, List<BlendshapeData>> blendshapeGroups = new Dictionary<string, List<BlendshapeData>>();
                int vertexOffset = 0;

                submeshTriangles.Add(new List<int>());



                try
                {
                    #region ------------------ Main Merge Loop ------------------
                    for (int i = 0; i < rendererList.Count; i++)
                    {
                        var outfit = rendererList[i].GetComponentInParent<Outfit>(true);
                        var smr = rendererList[i].GetComponentInChildren<SkinnedMeshRenderer>(true);
                        Dictionary<int, int> boneIndexMap = new Dictionary<int, int>();

                        try
                        {
                            #region Mapping Bones


                            Transform[] meshBones = smr.bones;
                            if (smr.rootBone != null)
                            {
                                // Build remap table: mesh bone index -> master bone index
                                for (int b = 0; b < meshBones.Length; b++)
                                {
                                    Transform bone = meshBones[b];
                                    int masterIndex = -1;
                                    if (bone == null) continue;
                                    if (boneMap.ContainsKey(bone.name))
                                    {

                                        masterIndex = boneMap[bone.name].index;
                                    }
                                    else
                                    {
                                        Debug.LogWarning(bone.name + " Is not in BoneMap");
                                    }

                                    if (masterIndex == -1)
                                    {
                                        continue;
                                    }

                                    boneIndexMap[b] = masterIndex;
                                }
                            }
                            #endregion
                        }
                        catch
                        {
                            Debug.LogError("Bone Mapping failed");
                        }


                        #region meshVertices

                        Mesh mesh = smr.sharedMesh;

                        // Convert from this renderer's mesh local space into the final body's local space.
                        // This is correct even if the outfit object is offset/rotated/scaled differently.
                        Matrix4x4 meshToBodyLocal = body.transform.worldToLocalMatrix * smr.transform.localToWorldMatrix;

                        // For normals/tangents, use vector transform, not point transform.
                        // If you have weird non-uniform scaling, this is safer for normals.
                        Matrix4x4 normalToBodyLocal =
                            meshToBodyLocal.inverse.transpose;

                        int count = mesh.vertexCount;

                        Vector3[] meshVertices = mesh.vertices;
                        Vector3[] meshNormals = mesh.normals;
                        Vector4[] meshTangents = mesh.tangents;

                        bool hasNormals = meshNormals.Length == count;
                        bool hasTangents = meshTangents.Length == count;

                        for (int v = 0; v < count; v++)
                        {
                            vertices.Add(
                                meshToBodyLocal.MultiplyPoint3x4(meshVertices[v])
                            );

                            normals.Add(hasNormals
                                ? normalToBodyLocal.MultiplyVector(meshNormals[v]).normalized
                                : Vector3.up);

                            if (hasTangents)
                            {
                                Vector4 t = meshTangents[v];

                                Vector3 direction = new Vector3(t.x, t.y, t.z);
                                direction = meshToBodyLocal.MultiplyVector(direction).normalized;

                                tangents.Add(new Vector4(
                                    direction.x,
                                    direction.y,
                                    direction.z,
                                    t.w
                                ));
                            }
                            else
                            {
                                tangents.Add(new Vector4(1, 0, 0, 1));
                            }
                        }

                        // Append vertex attributes
                        //vertices.AddRange(transformedVertices);
                        //normals.AddRange(transformedNormals);
                        //tangents.AddRange(transformedTangents);

                        #endregion

                        #region UV
                        uv.AddRange(mesh.uv);
                        #endregion

                        #region Vertex colors
                        // Vertex colors
                        Color[] meshColors = mesh.colors;
                        if (meshColors != null && meshColors.Length == mesh.vertexCount)
                        {
                            colors.AddRange(meshColors);
                        }
                        else
                        {
                            // Fill with red if missing
                            for (int c = 0; c < mesh.vertexCount; c++)
                                colors.Add(Color.red);
                        }
                        #endregion

                        string remapStage = "Starting bone remap";
                        int currentWeightIndex = -1;

                        try
                        {
                            #region Remap bone weights

                            remapStage = "Reading mesh bone weights";
                            BoneWeight[] meshBoneWeights = mesh.boneWeights;

                            remapStage = "Checking the SkinnedMeshRenderer root bone";
                            if (smr.rootBone == null)
                            {
                                throw new System.NullReferenceException(
                                    $"The SkinnedMeshRenderer '{smr.name}' has no root bone."
                                );
                            }

                            remapStage = $"Finding root bone '{smr.rootBone.name}' in boneMap";
                            if (!boneMap.TryGetValue(smr.rootBone.name, out var rootBoneData))
                            {
                                throw new System.Collections.Generic.KeyNotFoundException(
                                    $"Root bone '{smr.rootBone.name}' was not found in boneMap."
                                );
                            }

                            int fallbackBoneIndex = rootBoneData.index;

                            remapStage = "Remapping bone weights";

                            for (int i2 = 0; i2 < meshBoneWeights.Length; i2++)
                            {
                                currentWeightIndex = i2;

                                BoneWeight bw = meshBoneWeights[i2];

                                BoneWeight newBw = new BoneWeight
                                {
                                    boneIndex0 = boneIndexMap.TryGetValue(bw.boneIndex0, out int b0)
                                        ? b0 : fallbackBoneIndex,

                                    boneIndex1 = boneIndexMap.TryGetValue(bw.boneIndex1, out int b1)
                                        ? b1 : fallbackBoneIndex,

                                    boneIndex2 = boneIndexMap.TryGetValue(bw.boneIndex2, out int b2)
                                        ? b2 : fallbackBoneIndex,

                                    boneIndex3 = boneIndexMap.TryGetValue(bw.boneIndex3, out int b3)
                                        ? b3 : fallbackBoneIndex,

                                    weight0 = bw.weight0,
                                    weight1 = bw.weight1,
                                    weight2 = bw.weight2,
                                    weight3 = bw.weight3
                                };

                                boneWeights.Add(newBw);
                            }

                            #endregion
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError(
                                $"Bone remap failed.\n" +
                                $"Renderer: {(smr != null ? smr.name : "NULL")}\n" +
                                $"Mesh: {(mesh != null ? mesh.name : "NULL")}\n" +
                                $"Stage: {remapStage}\n" +
                                $"Bone weight index: {currentWeightIndex}\n" +
                                $"Exception: {ex.GetType().Name}: {ex.Message}",
                                smr
                            );

                            Debug.LogException(ex, smr);
                            return;
                        }

                        #region Sub Meshes

                        var submesh = submeshTriangles[0];
                        if (!outfit.mergeTexture)
                        {
                            submeshTriangles.Add(new List<int>());
                            submesh = submeshTriangles[submeshTriangles.Count - 1];
                            materials.Add(outfit.material);
                        }

                        for (int s = 0; s < mesh.subMeshCount; s++)
                        {
                            List<int> triangles = mesh.GetTriangles(s).ToList();
                            for (int t = 0; t < triangles.Count; t++)
                            {
                                submesh.Add(triangles[t] + vertexOffset);
                            }

                        }
                        #endregion

                        #region Blend Shapes
                        int blendshapeCount = mesh.blendShapeCount;
                        for (int b = 0; b < blendshapeCount; b++)
                        {
                            string shapeName = mesh.GetBlendShapeName(b);
                            var split = shapeName.Split(".");
                            if (split.Length > 0)
                            {
                                shapeName = shapeName.Replace(split[0] + ".", "");
                            }
                            int frameCount = mesh.GetBlendShapeFrameCount(b);

                            for (int f = 0; f < frameCount; f++)
                            {
                                float weight = mesh.GetBlendShapeFrameWeight(b, f);
                                float currentWeight = smr.GetBlendShapeWeight(b);

                                Vector3[] deltaVertices = new Vector3[mesh.vertexCount];
                                Vector3[] deltaNormals = new Vector3[mesh.vertexCount];
                                Vector3[] deltaTangents = new Vector3[mesh.vertexCount];

                                mesh.GetBlendShapeFrameVertices(b, f, deltaVertices, deltaNormals, deltaTangents);

                                if (!blendshapeGroups.ContainsKey(shapeName))
                                    blendshapeGroups[shapeName] = new List<BlendshapeData>();
                                blendshapeGroups[shapeName].Add(new BlendshapeData
                                {
                                    name = shapeName,
                                    weight = weight,
                                    currentWeight = currentWeight,
                                    deltaVertices = deltaVertices,
                                    deltaNormals = deltaNormals,
                                    deltaTangents = deltaTangents,
                                    vertexOffset = vertexOffset
                                });
                            }
                        }
                        #endregion

                        vertexOffset += mesh.vertexCount;
                    }
                    #endregion
                }
                catch
                {
                    Debug.LogError("Merge Loop has failed");
                    return;
                }

                bindposes.Clear();
                //Bind Pose Merge
                for (int i = 0; i < boneList.Count; i++)
                {
                    bindposes.Add(boneList[i].worldToLocalMatrix * body.transform.localToWorldMatrix);
                }


                var m = outfitSystem.GetCharacterBody();

                try
                {
                    // Build combined mesh
                    if (outfitSystem.combinedMesh != null) UnityEngine.Object.Destroy(outfitSystem.combinedMesh);
                    var combinedMesh = new Mesh();

                    outfitSystem.combinedMesh = combinedMesh;

                    combinedMesh.name = "MergedMesh";
                    combinedMesh.SetVertices(vertices);
                    combinedMesh.SetNormals(normals);
                    combinedMesh.SetTangents(tangents);
                    combinedMesh.SetUVs(0, uv);
                    combinedMesh.SetColors(colors);
                    combinedMesh.boneWeights = boneWeights.ToArray();
                    combinedMesh.bindposes = bindposes.ToArray();
                    combinedMesh.subMeshCount = submeshTriangles.Count;

                    //Creating Material Regions
                    for (int i = 0; i < submeshTriangles.Count; i++)
                    {
                        combinedMesh.SetTriangles(submeshTriangles[i], i);
                    }

                    m.sharedMesh = combinedMesh;
                    //body.bones = boneList.ToArray();
                    //body.rootBone = rootBone;
                    m.sharedMaterials = materials.ToArray();

                    int totalVertices = combinedMesh.vertexCount;
                    Vector3[] mergedDeltaVertices = new Vector3[totalVertices];
                    Vector3[] mergedDeltaNormals = new Vector3[totalVertices];
                    Vector3[] mergedDeltaTangents = new Vector3[totalVertices];

                    #region BlendShapes
                    // Create merged blendshapes
                    foreach (string shapeName in blendshapeGroups.Keys.OrderBy(x => x))
                    {
                        Array.Clear(mergedDeltaVertices, 0, totalVertices);
                        Array.Clear(mergedDeltaNormals, 0, totalVertices);
                        Array.Clear(mergedDeltaTangents, 0, totalVertices);

                        List<BlendshapeData> entries = blendshapeGroups[shapeName];

                        float frameWeight = entries[0].weight;
                        float currentWeight = 0;

                        foreach (var e in entries)
                        {
                            for (int i = 0; i < e.deltaVertices.Length; i++)
                            {
                                int index = e.vertexOffset + i;

                                mergedDeltaVertices[index] = e.deltaVertices[i];
                                mergedDeltaNormals[index] = e.deltaNormals[i];
                                mergedDeltaTangents[index] = e.deltaTangents[i];
                            }

                            currentWeight = Mathf.Max(currentWeight, e.currentWeight);
                        }

                        combinedMesh.AddBlendShapeFrame(
                            shapeName,
                            frameWeight,
                            mergedDeltaVertices,
                            mergedDeltaNormals,
                            mergedDeltaTangents
                        );

                        int blendIndex = combinedMesh.GetBlendShapeIndex(shapeName);
                        if (blendIndex >= 0)
                            m.SetBlendShapeWeight(blendIndex, currentWeight);
                    }
                    #endregion

                    Bounds newBounds = new Bounds(Vector3.zero, Vector3.zero);
                    newBounds.Encapsulate(combinedMesh.bounds);
                    m.localBounds = newBounds;

                    m.gameObject.SetActive(true);
                    UnityEngine.Object.DestroyImmediate(body.transform.parent.gameObject);

                }
                catch
                {
                    Debug.Log("Mesh Creation Failed");
                }

            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        public async Task<(Texture2D texture, Dictionary<string, Texture2D> additionalMaps, Dictionary<string, Rect> rect, Dictionary<string, Texture2D> customMaps)> CreateMergedTextures(List<Outfit> outfits)
        {
            var diffuseMaps = new List<Texture2D>();
            var normalMaps = new List<Texture2D>();
            var additionalMaps = new Dictionary<string, Texture2D[]>();
            var customMaps = new Dictionary<string, Texture2D[]>();
            var bakeMaterial = new Material(Shader.Find("BoZo/BakeTexture"));
            int index = 0;

            foreach (var item in outfits)
            {

                var smr = item.GetComponentInChildren<SkinnedMeshRenderer>();
                var renderer = item.GetComponentInChildren<Renderer>();
                var mesh = new List<Mesh>();

                foreach (var s in item.skinnedRenderers)
                {
                    mesh.Add(s.sharedMesh);
                }

                //Doesnt have a SkinnedMesh
                if (smr == null)
                {
                    mesh.Add(item.GetComponentInChildren<MeshFilter>(true).sharedMesh);
                }

                var originalMaterial = renderer.sharedMaterial;
                renderer.sharedMaterial = bakeMaterial;

                if (!item.customShader)
                {
                    //Copy All Colors and Data To Bake Material
                    bakeMaterial.mainTexture = originalMaterial.mainTexture;
                    bakeMaterial.SetTexture("_DecalMap", originalMaterial.GetTexture("_DecalMap"));
                    bakeMaterial.SetFloat("_DecalUVSet", originalMaterial.GetFloat("_DecalUVSet"));
                    bakeMaterial.SetFloat("_DecalBlend", originalMaterial.GetFloat("_DecalBlend"));
                    bakeMaterial.SetVector("_DecalScale", originalMaterial.GetVector("_DecalScale"));

                    bakeMaterial.SetTexture("_PatternMap", originalMaterial.GetTexture("_PatternMap"));
                    bakeMaterial.SetFloat("_PatternUVSet", originalMaterial.GetFloat("_PatternUVSet"));
                    bakeMaterial.SetFloat("_PatternBlend", originalMaterial.GetFloat("_PatternBlend"));
                    bakeMaterial.SetVector("_PatternScale", originalMaterial.GetVector("_PatternScale"));

                    for (int i = 0; i < 9; i++)
                    {
                        bakeMaterial.SetColor("_Color_" + (i + 1), originalMaterial.GetColor("_Color_" + (i + 1)));
                        bakeMaterial.SetColor("_Color_" + (i + 1), originalMaterial.GetColor("_Color_" + (i + 1)));

                        if (i + 1 <= 3)
                        {
                            bakeMaterial.SetColor("_DecalColor_" + (i + 1), originalMaterial.GetColor("_DecalColor_" + (i + 1)));
                            bakeMaterial.SetColor("_PatternColor_" + (i + 1), originalMaterial.GetColor("_PatternColor_" + (i + 1)));
                        }

                    }
                }
                else
                {
                    diffuseMaps.Add((Texture2D)originalMaterial.mainTexture);
                }

                foreach (var map in mergedMaterialDatas)
                {
                    if (!additionalMaps.ContainsKey(map.toMateiralProperty)) { additionalMaps[map.toMateiralProperty] = new Texture2D[outfits.Count]; }
                    additionalMaps[map.toMateiralProperty][index] = (Texture2D)originalMaterial.GetTexture(map.fromMateiralProperty);
                }

                //Get CustomMaps form extensions
                var extensions = item.GetComponentsInChildren<IOutfitExtension>();

                foreach (var extension in extensions)
                {
                    if (extension.GetValue() is Texture2D && !customMaps.ContainsKey(extension.GetID())) customMaps[extension.GetID()] = new Texture2D[outfits.Count];
                    if (extension.GetValue() is Texture2D)
                    {
                        customMaps[extension.GetID()][index] = (Texture2D)extension.GetValue();
                    }
                }


                var tex = await BakeTextureAsyncTask(mesh, bakeMaterial);

                diffuseMaps.Add(tex);

                renderer.sharedMaterial = originalMaterial;
                RenderTexture.active = null;

                index++;
            }


            int atlasSize = 2048;

            Texture2D atlas = new Texture2D(atlasSize, atlasSize);
            Texture2D atlasNormal = new Texture2D(atlasSize, atlasSize);
            Dictionary<string, Texture2D> additionalMapsList = new Dictionary<string, Texture2D>();
            Dictionary<string, Texture2D> atlasCustomMapsList = new Dictionary<string, Texture2D>();
            atlas.wrapMode = TextureWrapMode.Repeat;
            Rect[] rects = atlas.PackTextures(diffuseMaps.ToArray(), 0, atlasSize);

            //Creating Map for Outfits with Multiple Meshes
            Dictionary<string, Rect> rectMap = new Dictionary<string, Rect>();
            for (int i = 0; i < outfits.Count; i++)
            {
                rectMap.Add(outfits[i].name, rects[i]);
            }

            var pixels = atlas.GetPixels32();
            pixels = await DilateTextureAsync(atlas, pixels);

            atlas.SetPixels32(pixels);
            atlas.Apply();

            var addIndex = 0;
            foreach (var additional in additionalMaps)
            {
                var additionalValues = additional.Value.ToList();
                Texture2D newAdditionalMap = new Texture2D(atlasSize, atlasSize);
                newAdditionalMap.wrapMode = TextureWrapMode.Repeat;
                additionalMapsList[additional.Key] = await RemapTextureAsync(additionalValues, rects, atlasSize, newAdditionalMap, mergedMaterialDatas[addIndex].backgroundColor);
                addIndex++;
            }

            foreach (var custom in customMaps)
            {
                var customValues = custom.Value.ToList();
                if (customValues.Count == 0) continue;
                Texture2D newCustomMap = new Texture2D(atlasSize, atlasSize);
                newCustomMap.wrapMode = TextureWrapMode.Repeat;
                atlasCustomMapsList[custom.Key] = await RemapTextureAsync(customValues, rects, atlasSize, newCustomMap, Color.black);
            }

            return (atlas, additionalMapsList, rectMap, atlasCustomMapsList);
        }

        public async Task<Texture2D> BakeTextureAsyncTask(List<Mesh> mesh, Material bakeMaterial)
        {
            var textureSize = bakeMaterial.mainTexture.width;
            var rt = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGB32)
            {
                useMipMap = false,
                autoGenerateMips = false
            };
            rt.Create();

            CommandBuffer cb = new CommandBuffer();
            cb.SetRenderTarget(rt);
            cb.ClearRenderTarget(true, true, Color.clear);
            foreach (var m in mesh)
            {
                cb.DrawMesh(m, Matrix4x4.identity, bakeMaterial);
            }
            Graphics.ExecuteCommandBuffer(cb);
            cb.Release();

            var tcs = new TaskCompletionSource<Texture2D>();

            AsyncGPUReadback.Request(rt, 0, TextureFormat.RGBA32, request =>
            {
                if (request.hasError)
                {
                    tcs.SetException(new Exception("Async GPU readback failed."));
                }
                else
                {
                    var data = request.GetData<Color32>();
                    var tex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
                    tex.LoadRawTextureData(data);
                    tex.Apply();
                    tcs.SetResult(tex);
                }
            });

            return await tcs.Task;
        }

        public async Task<Texture2D> RemapTextureAsync(List<Texture2D> normalMaps, Rect[] rects, int atlasSize, Texture2D atlasNormal, Color fillColor)
        {
            List<Task> pendingReads = new List<Task>();

            Color[] pixels = new Color[atlasSize * atlasSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = fillColor;
            }
            atlasNormal.SetPixels(pixels);

            for (int i = 0; i < normalMaps.Count; i++)
            {
                if (normalMaps[i] == null) continue;
                Rect r = rects[i];

                int x = Mathf.RoundToInt(r.x * atlasSize);
                int y = Mathf.RoundToInt(r.y * atlasSize);
                int w = Mathf.RoundToInt(r.width * atlasSize);
                int h = Mathf.RoundToInt(r.height * atlasSize);

                Texture2D normal = normalMaps[i];

                RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(normal, rt);

                int copyX = x, copyY = y, copyW = w, copyH = h;
                RenderTexture copyRT = rt;

                var tcs = new TaskCompletionSource<bool>();
                pendingReads.Add(tcs.Task);

                AsyncGPUReadback.Request(copyRT, 0, TextureFormat.RGBA32, request =>
                {
                    if (request.hasError)
                    {
                        Debug.LogError("Normal map readback failed");
                        tcs.SetResult(true);
                        RenderTexture.ReleaseTemporary(copyRT);
                        return;
                    }

                    var data = request.GetData<Color32>();
                    Color32[] pixels = data.ToArray();

                    // Fill into the atlas
                    Color[] colors = new Color[pixels.Length];
                    for (int j = 0; j < pixels.Length; j++)
                    {
                        colors[j] = pixels[j]; // Converts Color32 to Color
                    }

                    atlasNormal.SetPixels(copyX, copyY, copyW, copyH, colors);

                    RenderTexture.ReleaseTemporary(copyRT);
                    tcs.SetResult(true);
                });
            }

            await Task.WhenAll(pendingReads);

            atlasNormal.Apply(); // Apply once after all blocks are set
            return atlasNormal;
        }



        public async Task<Color32[]> DilateTextureAsync(Texture2D tex, Color32[] pixels)
        {
            var iterations = 2;
            int w = tex.width;
            int h = tex.height;

            Color32[] resultPixels = await Task.Run(() =>
            {
                Color32[] workingPixels = new Color32[pixels.Length];
                Color32[] temp = new Color32[pixels.Length];
                bool[] originalMask = new bool[pixels.Length];

                // Initialize workingPixels and mask
                for (int i = 0; i < pixels.Length; i++)
                {
                    workingPixels[i] = pixels[i];
                    originalMask[i] = pixels[i].a >= 255; // original valid pixels
                }

                for (int it = 0; it < iterations; it++)
                {
                    workingPixels.CopyTo(temp, 0);

                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            int idx = y * w + x;
                            if (workingPixels[idx].a >= 255) continue;

                            for (int dy = -1; dy <= 1; dy++)
                            {
                                for (int dx = -1; dx <= 1; dx++)
                                {
                                    if (dx == 0 && dy == 0) continue;

                                    int nx = x + dx;
                                    int ny = y + dy;
                                    if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;

                                    int nIdx = ny * w + nx;

                                    // ONLY copy from original source pixels
                                    if (originalMask[nIdx])
                                    {
                                        temp[idx] = workingPixels[nIdx];
                                        goto FoundPixel;
                                    }
                                }
                            }

                        FoundPixel:;
                        }
                    }

                    // Update working pixels
                    var swap = workingPixels;
                    workingPixels = temp;
                    temp = swap;

                    // After first iteration, also allow pixels from the new ones as source
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        if (workingPixels[i].a >= 1)
                            originalMask[i] = true;
                    }
                }

                return workingPixels;
            });

            return resultPixels;
        }



        private class boneData
        {
            public Transform bone;
            public int index;
        }

        struct BlendshapeData
        {
            public string name;
            public float weight;
            public float currentWeight;
            public Vector3[] deltaVertices;
            public Vector3[] deltaNormals;
            public Vector3[] deltaTangents;
            public int vertexOffset;
        }

    }

    [System.Serializable]
    public class MergedMaterialData
    {
        public string fromMateiralProperty;
        public string toMateiralProperty;
        public Color backgroundColor = Color.gray;
    }

}
