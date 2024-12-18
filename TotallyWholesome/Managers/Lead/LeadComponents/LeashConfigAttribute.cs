using System;
using UnityEngine;

namespace TotallyWholesome.Managers.Lead.LeadComponents;

public class LeashConfigAttribute(string styleCategory = "Basic", int segmentCount = 22, LineTextureMode lineTextureMode = LineTextureMode.RepeatPerSegment, float lineWidth = 1f) : Attribute
{
    public static LeashConfigAttribute DefaultConfig = new LeashConfigAttribute();

    public string StyleCategory { get; private set; } = styleCategory;
    public int SegmentCount { get; private set; } = segmentCount;
    public LineTextureMode LineTextureMode { get; private set; } = lineTextureMode;
    public float LineWidth { get; private set; } = lineWidth;
}

public class LeashStyleAttributeHelper
{

}