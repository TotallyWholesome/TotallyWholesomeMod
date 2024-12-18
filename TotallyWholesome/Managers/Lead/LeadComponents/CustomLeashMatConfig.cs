using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace TotallyWholesome.Managers.Lead.LeadComponents;

public class CustomLeashMatConfig : MonoBehaviour
{
    public int segmentCount = 22;
    public LineTextureMode lineTextureMode = LineTextureMode.RepeatPerSegment;
    public float lineWidth = 1f;

    public override string ToString()
    {
        return $"CustomLeashMatConfig - [SegmentCount: {segmentCount}, LineTextureMode: {Enum.GetName(typeof(LineTextureMode), lineTextureMode)}, LineWidth: {lineWidth}]";
    }
}