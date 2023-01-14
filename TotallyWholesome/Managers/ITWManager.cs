namespace TotallyWholesome.Managers
{
    public interface ITWManager
    {
        public int Priority();
        public void Setup();
        public void LateSetup();
        public string ManagerName();
    }
}