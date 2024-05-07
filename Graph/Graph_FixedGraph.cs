using System;
using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CE301.Graph
{
    public class Graph_FixedGraph : Graph_Graph
    {
        public Graph_FixedGraph(bool multiAgent) : base(multiAgent)
        {
            addFixedValues();
            startNode = nodeList[0].key;
            endNode = nodeList[5].key;
            if (multiAgent)
            {
                startNode2 = nodeList[4].key;
                endNode2 = nodeList[5].key;
            }
            resetNodeColours();
        }

        private void addFixedValues()
        {
            addNode(0, new Vector2(300, 190));
            addNode(1, new Vector2(1150, 250));
            addNode(2, new Vector2(750, 150));
            addNode(3, new Vector2(1025, 590));
            addNode(4, new Vector2(450, 550));
            addNode(5, new Vector2(800, 450));

            addEdge(0, 2, 1);
            addEdge(0, 4, 3);
            addEdge(1, 2, 9);
            addEdge(2, 0, 1);
            addEdge(2, 1, 4);
            addEdge(2, 5, 5);
            addEdge(3, 1, 7);
            addEdge(3, 4, 1);
            addEdge(4, 0, 8);
            addEdge(4, 3, 6);
            addEdge(5, 4, 5);
            addEdge(5, 2, 5);
            addEdge(2, 3, 1);
            addEdge(4, 2, 1);
            //addEdge(1, 5, 5);
        }
    }
}