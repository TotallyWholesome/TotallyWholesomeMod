using MessagePack;

namespace TWNetCommon.Data;

[MessagePackObject]
public class TagData
{
    [Key(0)]
    public string TagText;
    [Key(1)]
    public string BackgroundColour;
    [Key(2)]
    public string TextColour;
    [Key(3)]
    public bool Success;
    [Key(4)]
    public string SuccessMessage;

    public override string ToString()
    {
        return $"TagData - [TagText: {TagText}, BackgroundColour: {BackgroundColour}, TextColour: {TextColour}]";
    }
}
