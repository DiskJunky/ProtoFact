using ProtoFact.Domain;

namespace ProtoFact.Tests.Builders
{
    public class ItemBuilder
    {
        private string _id = "item";
        private string _name = "Item";
        private ItemType _type = ItemType.Raw;

        public static ItemBuilder Create() => new();

        public ItemBuilder WithId(string id)
        {
            _id = id;
            return this;
        }

        public ItemBuilder WithName(string name)
        {
            _name = name;
            return this;
        }

        public ItemBuilder WithType(ItemType type)
        {
            _type = type;
            return this;
        }

        public Item Build() => new(_id, _name, _type);
    }
}