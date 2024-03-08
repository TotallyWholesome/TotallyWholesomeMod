namespace TotallyWholesome.Managers;

// ReSharper disable once InconsistentNaming
public interface ITWManager
{
    public int Priority { get; }
    public void Setup();
    public void LateSetup();
}