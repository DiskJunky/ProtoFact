using System;

namespace ProtoFact.Domain
{
    /// <summary>
    /// Represents a unique resource type in the system.
    /// </summary>
    public sealed class Item : IEquatable<Item>
    {
        public string Id { get; }
        public string Name { get; }
        public ItemType Type { get; }

        public Item(string id, string name, ItemType type)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Item id cannot be null or empty.", nameof(id));

            Id = id;
            Name = name ?? id;
            Type = type;
        }

        public bool Equals(Item? other)
        {
            if (other is null) return false;
            return Id.Equals(other.Id, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj) => Equals(obj as Item);

        public override int GetHashCode() => Id.GetHashCode(StringComparison.Ordinal);

        public override string ToString() => $"{Name} ({Id})";
    }

    public enum ItemType
    {
        Raw,
        Intermediate,
        Final
    }
}