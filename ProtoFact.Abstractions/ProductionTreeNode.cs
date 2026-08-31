using System.Collections.Generic;
using ProtoFact.Domain;

namespace ProtoFact.Abstractions
{
    /// <summary>
    /// A display-oriented node in a recipe/production tree. Unlike
    /// <see cref="ProductionNode"/> (which is deduplicated per-item within a
    /// single <see cref="IRateSolver"/> call), instances of this type are
    /// intentionally duplicated for every edge in the tree so a UI can show
    /// the full chain from a goal down to raw materials, even when an item
    /// is required by more than one parent.
    /// </summary>
    public class ProductionTreeNode
    {
        public Item Item { get; set; } = null!;

        /// <summary>
        /// The rate required by this specific edge (i.e. how much of
        /// <see cref="Item"/> this particular parent needs).
        /// </summary>
        public double EdgeRate { get; set; }

        /// <summary>
        /// The total rate required for <see cref="Item"/> across the entire
        /// plan (i.e. summed across every edge that needs it, potentially
        /// under multiple different parents/goals).
        /// </summary>
        public double AggregateRate { get; set; }

        /// <summary>
        /// True when <see cref="Item"/> is required by more than one edge
        /// across the whole plan (i.e. <see cref="AggregateRate"/> is made
        /// up of contributions from multiple parents). When true,
        /// <see cref="EdgeRate"/> only reflects this one occurrence -
        /// <see cref="AggregateRate"/> should be consulted for the true
        /// total demand.
        /// </summary>
        public bool IsShared { get; set; }

        /// <summary>
        /// Machines required to satisfy just this edge's rate (edge rate /
        /// recipe output rate). Zero for raw resources (no recipe).
        /// </summary>
        public double MachinesRequired { get; set; }

        /// <summary>
        /// Current inventory stock for <see cref="Item"/> at the time the
        /// tree was built.
        /// </summary>
        public double CurrentStock { get; set; }

        public List<ProductionTreeNode> Inputs { get; set; } = new();
    }
}
