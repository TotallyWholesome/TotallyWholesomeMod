using System;
using MessagePack;

namespace TWNetCommon.Data.NestedObjects
{
    [MessagePackObject]
    public class TWVector3
    {
        [Key(0)]
        public float X;
        [Key(1)]
        public float Y;
        [Key(2)]
        public float Z;

        public TWVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static TWVector3 Zero = new TWVector3(0, 0, 0);

        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            return obj.GetType() == typeof(TWVector3) && Equals(obj as TWVector3);
        }

        private bool Equals(TWVector3 other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        }
    }
}