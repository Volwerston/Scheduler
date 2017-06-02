using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Algorithms
{
    class Algorithms
    {

        public static TargetNode BuildTargetTree(List<Target> targets)
        {

            Dictionary<int, TargetNode> nodeByIndex = new Dictionary<int, TargetNode>();
            Dictionary<int, Target> targetByIndex = new Dictionary<int, Target>();

            List<TargetNode> nodes = new List<TargetNode>();

            foreach(var target in targets)
            {
                targetByIndex[target.Id] = target;
                nodeByIndex[target.Id] = new TargetNode(target);
            }

            foreach(var kvp in targetByIndex)
            {
                foreach(var index in kvp.Value.PreTargets)
                {
                    nodeByIndex[kvp.Key].AddChild(nodeByIndex[index]);
                    nodeByIndex[index].AddParent(nodeByIndex[kvp.Key]);
                }
            }

            Dictionary<int, bool> preTargetIndexes = new Dictionary<int, bool>();
            foreach(var el in nodeByIndex.Keys)
            {
                preTargetIndexes[el] = false;
            }

            foreach(var target in targetByIndex.Values)
            {
                foreach(var child in target.PreTargets)
                {
                    preTargetIndexes[child] = true;
                }
            }

            var indexes = preTargetIndexes.Where(x => !x.Value);
            if(indexes.Count() == 0)
            {
                throw new Exception("The target graph has no root");
            }

            return nodeByIndex[indexes.First().Key];
        }

        // 1.Order target tree
        public static List<TargetNode> TopologicalSort(TargetNode main)
        {
            CalculateChildrenDifficulties(main);

            int currOrder = 1;
            List<TargetNode> orderedTargets = new List<TargetNode>();
            Stack<TargetNode> memory = new Stack<TargetNode>();
            TargetNode currItem = null;

            while (main.Children.Count() != 0)
            {
                if (currItem == null)
                {
                    currItem = main.GetLeaves().OrderBy(x => x.NodeDifficulty).First();
                }

                while (currItem.Children.Count() == 0)
                {
                    if (currItem.Parents.Count() == 0)
                    {
                        break;
                    }

                    currItem.Order = currOrder;
                    ++currOrder;
                    orderedTargets.Add(currItem);
                    TargetNode buf = currItem;

                    TargetNode next = null;
                    if (memory.Count() == 0)
                    {
                        next = buf.Parents.OrderBy(x => x.NodeDifficulty).First();
                    }
                    else
                    {
                        next = memory.Pop();
                    }

                    currItem = next;

                    for (int i = 0; i < buf.Parents.Count(); ++i)
                    {
                        buf.Parents[i].RemoveChild(buf);
                    }
                }


                if (currItem != main)
                {
                    memory.Push(currItem);
                    while (currItem.Children.Count() != 0)
                    {
                        currItem = currItem.Children.OrderBy(x => x.ChildrenDifficulty).First();
                    }
                }
                else
                {
                    currItem = null;
                }
            }

            main.Order = currOrder;
            orderedTargets.Add(main);

            return orderedTargets;
        }

        public static int[,] GetAdjacencyMatrix(List<Target> targets)
        {
            for (int i = 0; i < targets.Count(); ++i)
            {
                if (targets[i].PreTargets == null)
                {
                    targets[i].PreTargets = new List<int>();
                }
            }

            Dictionary<int, int> indexToPos = new Dictionary<int, int>();

            for(int i = 0; i < targets.Count(); ++i)
            {
                indexToPos[targets[i].Id] = i;
            }

            int[,] toReturn = new int[indexToPos.Count(), indexToPos.Count()];

            for(int i = 0; i < targets.Count(); ++i)
            {
                for(int j = 0; j < targets[i].PreTargets.Count(); ++j)
                {
                    toReturn[indexToPos[targets[i].PreTargets[j]],indexToPos[targets[i].Id]] = 1;
                }
            }

            return toReturn;
        }

        public static bool ContainsCycle(int[,] Adjacency, List<int> CurrentCycleVisited, List<List<int>> Cycles, int CurrNode)
        {
            CurrentCycleVisited.Add(CurrNode);
            for (int OutEdgeCnt = 0; OutEdgeCnt < Adjacency.GetLength(0); OutEdgeCnt++)
            {
                if (Adjacency[CurrNode, OutEdgeCnt] == 1)
                {
                    if (CurrentCycleVisited.Contains(OutEdgeCnt))
                    {
                        return true;
                    }
                    else
                    {
                        return ContainsCycle(Adjacency,new List<int>(CurrentCycleVisited), Cycles, OutEdgeCnt);
                    }
                }
            }

            return false;
        }

        private static void CalculateChildrenDifficulties(TargetNode root)
        {
            if (root.Children.Count() == 0)
            {
                root.ChildrenDifficulty = root.NodeDifficulty;
            }
            else
            {
                foreach (var child in root.Children)
                {
                    CalculateChildrenDifficulties(child);
                }

                root.ChildrenDifficulty = root.Children.Select(x => x.ChildrenDifficulty).Aggregate((a, b) => a + b);
            }
        }

    }
}
