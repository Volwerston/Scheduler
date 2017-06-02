using System.Collections.Generic;
using System.Linq;

namespace Algorithms
{
    class TargetNode
    {
        public int NodeDifficulty { get; set; }
        public int ChildrenDifficulty { get; set; }
        public string Title { get; set; }
        public int Order { get; set; }
        public int Days { get; set; }
        public List<TargetNode> Children { get; }
        public List<TargetNode> Parents { get; }

        public TargetNode(Target t = null)
        {
            Parents = new List<TargetNode>();
            Children = new List<TargetNode>();

            if (t != null)
            {
                Title = t.Name;
                NodeDifficulty = t.Difficulty;
                Days = t.Duration;
            }
        }

        public void AddChild(TargetNode t)
        {
            Children.Add(t);
        }

        public void RemoveChild(TargetNode t)
        {
            Children.Remove(t);
        }

        public void AddParent(TargetNode t)
        {
            Parents.Add(t);
        }

        public void RemoveParent(TargetNode t)
        {
            Parents.Remove(t);
        }

        public List<TargetNode> GetLeaves()
        {
            List<TargetNode> toReturn = new List<TargetNode>();

            foreach (var child in Children)
            {
                if (child.Children.Count() == 0)
                {
                    toReturn.Add(child);
                }
                else
                {
                    toReturn.AddRange(child.GetLeaves());
                }
            }

            return toReturn;
        }

        public override string ToString()
        {
            return string.Format("Title: {0}, Difficulty: {1}, Days: {2}, Order: {3}", Title, NodeDifficulty, Days, Order);
        }
    }
}
