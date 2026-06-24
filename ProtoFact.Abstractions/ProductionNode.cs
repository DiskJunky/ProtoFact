using ProtoFact.Domain;

namespace ProtoFact.Abstractions
{
    /// <summary>
    /// Represents a node in the production dependency tree.
    /// </summary>
    public class ProductionNode
    {
        public Item Item { get; set; } = null!;
        public double RequiredRate { get; set; }
        public double MachinesRequired { get; set; }
        public List<ProductionNode> Inputs { get; set; } = new();
    }
}