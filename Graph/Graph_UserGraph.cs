using System;
using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CE301.Graph
{
    public class Graph_UserGraph : Graph_Graph
    {
        List<int> keysToUse = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 };
        public Graph_UserGraph(bool multiAgent) : base(multiAgent)
        {
            /*addStartValues();
            startNode = nodeList[0].key;
            endNode = nodeList[1].key;
            if (multiAgent)
            {
                startNode2 = nodeList[0].key;
                endNode2 = nodeList[1].key;
            }
            resetNodeColours();*/
        }

        public new Graph_UserGraph createGraphState()
        {
            Graph_UserGraph graphToReturn = base.createGraphState();
            graphToReturn.keysToUse = keysToUse;
            return graphToReturn;
        }

        private void addStartValues()
        {
            addNode(0, new Vector2(300, 190));
            addNode(1, new Vector2(1150, 250));

            addEdge(0, 1, 2);
            addEdge(1, 0, 3);
        }

        public void addNode(Vector2 pos)
        {
            addNode(keysToUse.Min(), pos);
            keysToUse.Remove(keysToUse.Min());
            resetNodeColours();
        }

        public void addEdge(int source, int destination, int weight) // before adding new edge, check if old one exists and needs to be deleted, then add new one
        {
            if (getEdge(source, destination) != null)
            {
                removeEdge([source, destination]);
            }

            base.addEdge(source, destination, weight);
        }

        public bool removeEdge(int[] startAndEnd) // removes edge and recalculates heuristic largestratio (SHOULD DO THIS WHENEVER DELETING AN EDGE)
        {
            double distance = getDistanceBetweenNodes(nodeList[startAndEnd[0]], nodeList[startAndEnd[1]]);
            GD.Print(startAndEnd[0] + " " + startAndEnd[1]);
            foreach (Graph_Edge e in adjList[startAndEnd[0]])
            {
                GD.Print(e.source + " | " + e.destination);
            }
            int weight;
            if(adjList[startAndEnd[0]].Find(x => x.destination == startAndEnd[1]) == null)
            {
                GD.Print("------------- funny user_graph error caught -------------");
                return false;
            }
            else
            {
                weight = adjList[startAndEnd[0]].Find(x => x.destination == startAndEnd[1]).weight;
            }

            if ((distance / weight).Equals(largestRatio)) // if the edge being removed was the largest ratio, recalc it on all the other edges
            {
                recalculateLargestRatio();
            }

            return adjList[startAndEnd[0]].Remove(adjList[startAndEnd[0]].Find(x => x.destination == startAndEnd[1]));
        }

        public void removeNode(int nodeInt)
        {
            for(int i = 0; i < nodeList.Count; i++)
            {
                if(i != nodeInt)
                {
                    Graph_Edge edgeToDelete = adjList[i].Find(x => x.destination == nodeInt);
                    while(edgeToDelete != null) 
                    {
                        removeEdge([edgeToDelete.source, edgeToDelete.destination]);
                        edgeToDelete = adjList[i].Find(x => x.destination == nodeInt);
                    }
                }
            }

            adjList.Remove(nodeInt);
            nodeList.Remove(nodeInt);
            keysToUse.Add(nodeInt);
            resetNodeColours();
        }

        public void setStartNode(int nodeInt)
        {
            startNode = nodeInt;
            resetNodeColours();
        }

        public void setEndNode(int nodeInt)
        {
            endNode = nodeInt;
            resetNodeColours();
        }

        public void setStart2Node(int nodeInt)
        {
            startNode2 = nodeInt;
            resetNodeColours();
        }

        public void setEnd2Node(int nodeInt)
        {
            endNode2 = nodeInt;
            resetNodeColours();
        }
    }
}
