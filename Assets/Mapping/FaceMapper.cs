using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;

[Serializable]
public class FaceMappingRule
{
    public string blendShapeName;

    public int pointIndex1;
    public int pointIndex2;

    public float minDistance;
    public float maxDistance;
    
    public bool reverseValue;            // 100 - normalized value
}

public class FaceMapper : MonoBehaviour
{
    public SkinnedMeshRenderer faceSkinnedMeshRenderer;
    public SkinnedMeshRenderer teethSkinnedMeshRenderer;
    public List<FaceMappingRule> mappingRules;

    private Vector3 DistanceFromTwoPoint(Vector3 p1, Vector3 p2)
    {
        return (p1 - p2);
    }
    private Vector3 MakePositionVectorOfPoint(float[] verticesData, int vertexIndex)
    {
        return new Vector3(verticesData[vertexIndex * 4], verticesData[vertexIndex * 4 + 1], verticesData[vertexIndex * 4 + 2]);
    }

    /// <summary>
    /// Normalize to range [0,1]
    /// </summary>
    /// <param name="val"></param>
    /// <param name="min1"></param>
    /// <param name="max1"></param>
    /// <returns></returns>
    private float MinMaxNormalize(float val, float min, float max)
    {
        return ((val - min) / (max - min));
    }

    internal void MapFromBuffer(float[] verticesData, float4x4 cropFaceMatrix)
    {
        var xBoundingBox_min = Math.Min(Math.Min(Math.Min(cropFaceMatrix.c0[0], cropFaceMatrix.c1[0]), cropFaceMatrix.c2[0]), cropFaceMatrix.c3[0]);
        var xBoundingBox_max = Math.Max(Math.Max(Math.Max(cropFaceMatrix.c0[0], cropFaceMatrix.c1[0]), cropFaceMatrix.c2[0]), cropFaceMatrix.c3[0]);
        var yBoundingBox_min = Math.Min(Math.Min(Math.Min(cropFaceMatrix.c0[1], cropFaceMatrix.c1[1]), cropFaceMatrix.c2[1]), cropFaceMatrix.c3[1]);
        var yBoundingBox_max = Math.Max(Math.Max(Math.Max(cropFaceMatrix.c0[1], cropFaceMatrix.c1[1]), cropFaceMatrix.c2[1]), cropFaceMatrix.c3[1]);
        var boundingBoxWidth = xBoundingBox_max - xBoundingBox_min;
        var boundingBoxHeight = yBoundingBox_max - yBoundingBox_min;
        Debug.Log(boundingBoxHeight + " " + boundingBoxWidth);

        foreach (FaceMappingRule rule in mappingRules)
        {
            var blendShapeIdx = faceSkinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(rule.blendShapeName);
            var distanceVector = (MakePositionVectorOfPoint(verticesData, rule.pointIndex1) - MakePositionVectorOfPoint(verticesData, rule.pointIndex2));
            //Debug.Log(rule.blendShapeName + ": " + distanceVector.ToString("F4"));
            var distance = distanceVector.magnitude;
            Debug.Log(rule.blendShapeName + ": " + distance.ToString("F8"));

            var maxDistance = rule.maxDistance * boundingBoxHeight;
            var minDistance = rule.minDistance * boundingBoxHeight;     //face crop scale invariance
            var blendShapeWeight = MinMaxNormalize(distance, minDistance, maxDistance)  * 100;                   // normalize to [0, 100]

            if (rule.reverseValue)
                blendShapeWeight = 100 - blendShapeWeight;
            if (blendShapeWeight < 0) blendShapeWeight = 0;
            if (blendShapeWeight > 100) blendShapeWeight = 100;

            faceSkinnedMeshRenderer.SetBlendShapeWeight(blendShapeIdx, blendShapeWeight);
            teethSkinnedMeshRenderer?.SetBlendShapeWeight(blendShapeIdx, blendShapeWeight);      // face and teeth must have equal blend shapes setting
        }
    }
}
