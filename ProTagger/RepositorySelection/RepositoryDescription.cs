using System;

namespace ProTagger.RepositorySelection
{
    public class RepositoryDescription : IEquatable<RepositoryDescription>
    {
        public string Name { get; }
        public string Path { get; }

        public RepositoryDescription(string name, string path)
            => (Name, Path) = (name, path);

        public override bool Equals(object? obj)
        {
            if (!(obj is RepositoryDescription other))
                return false;
            return Equals(other);
        }

        public bool Equals(RepositoryDescription? obj)
            => !(obj is null) && Name.Equals(obj.Name) && Path.Equals(obj.Path);

        public static bool operator ==(RepositoryDescription? first, RepositoryDescription? second)
            => first is null ? second is null : first.Equals(second);

        public static bool operator !=(RepositoryDescription? first, RepositoryDescription? second)
            => !(first == second);

        public override int GetHashCode()
            => HashCode.Combine(Name, Path);
    }
}
