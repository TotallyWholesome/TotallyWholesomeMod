namespace TotallyWholesome.Objects.ConfigObjects
{
    public class PiShockShocker
    {
        public string Key;
        public string Name;
        public bool Enabled = true;
        public bool Prioritized = false;

        public PiShockShocker(string key, string name)
        {
            Key = key;
            Name = name;
        }
    }
}