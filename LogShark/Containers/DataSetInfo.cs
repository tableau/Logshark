using System;

namespace LogShark.Containers
{
    public class DataSetInfo
    {
        public string Group { get; }
        public string Name { get; }

        public DataSetInfo(string group, string name)
        {
            Group = group;
            Name = name;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (!(obj is DataSetInfo other))
            {
                return false;
            }

            return Equals(other);      
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(Group, Name);
        }

        public override string ToString()
        {
            return $"{Group}_{Name}";
        }

        public bool Equals(DataSetInfo other)
        {
            if (other == null)
            {
                return false;
            }

            return (Group == other.Group) && (Name == other.Name);
        }
        
        public static bool operator ==(DataSetInfo left, DataSetInfo right)
        {
            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)left == null) || ((object)right == null))
            {
                return false;
            }

            // Return true if the fields match:
            return left.Equals(right);
        }

        public static bool operator !=(DataSetInfo a, DataSetInfo b)
        {
            return !(a == b);
        }
    }
}