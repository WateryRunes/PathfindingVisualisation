using System;
using Godot;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace CE301.Graph
{
    public partial class Graph_Graph
    {
        // default = seagrass
        // start node = dark orange (first agent) apple (second agent)
        // end node = light orange (first agent) lime green (second agent)
        // background = cool grey 11
        // panels = violet?
        // path = get midpoint colour from https://meyerweb.com/eric/tools/color-blend/#D55C19:E98300:1:hex
        // frontier edge = white 
        // token same as path for each agent
        private static Dictionary<GraphColour, Color> coloursDict = new Dictionary<GraphColour, Color>()
        {
            { GraphColour.Default, new Color("#007A87") },
            { GraphColour.StartNode, new Color("#D55C19") },
            { GraphColour.StartNode2, new Color("#58A618") },
            { GraphColour.EndNode, new Color("#E98300") },
            { GraphColour.EndNode2, new Color("#BED600") },
            { GraphColour.FrontierEdge, new Color("#FFFFFF") },
            { GraphColour.OldEdge, new Color("#FFFFFF") }, // NOT USED ATM
            { GraphColour.Path, new Color("#DF700D") },
            { GraphColour.Path2, new Color("#8BBE0C") },
            { GraphColour.FirstToken, new Color("#DF700D") },
            { GraphColour.SecondToken, new Color("#8BBE0C") },
            { GraphColour.Collision, new Color("#CD202C") },

            { GraphColour.Violet, new Color("#622567") },
            { GraphColour.Scarlet, new Color("#CD202C") },
            { GraphColour.BrightPink, new Color("#DA3D7E") },
            { GraphColour.DarkOrange, new Color("#D55C19") },
            { GraphColour.LightOrange, new Color("#E98300") },
            { GraphColour.Yellow, new Color("#F3D311") },
            { GraphColour.Apple, new Color("#58A618") },
            { GraphColour.LimeGreen, new Color("#BED600") },
            { GraphColour.Mint, new Color("#35C4B5") },
            { GraphColour.Seagrass, new Color("#007A87") },
            { GraphColour.Turquoise, new Color("#00AFD8") },
            { GraphColour.Cornflower, new Color("#0065BD") },
            { GraphColour.CoolGrey2, new Color("#EAEAEA") },
            { GraphColour.CoolGrey6, new Color("#ADADAD") },
            { GraphColour.CoolGrey11, new Color("#333333") }
        };

        public Dictionary<int, List<Graph_Edge>> adjList { get; private set; }
        public Dictionary<int, Graph_Node> nodeList { get; private set; }
        public double largestRatio { protected set; get; }
        public int startNode;
        public int endNode;
        public int startNode2;
        public int endNode2;
        public bool multiAgent { private set; get; }

        public Graph_Graph(bool multiAgent)
        {
            this.multiAgent = multiAgent;
            adjList = new Dictionary<int, List<Graph_Edge>>();
            nodeList = new Dictionary<int, Graph_Node>();
            largestRatio = 1;
            /*if (!multiAgent)
            {
                startNode2 = -1;
                endNode2 = -1;
            }*/
        }

        // FOR DEBUGGING PURPOSES
        public override string ToString() 
        {
            string output = "Graph: \n";
            foreach (Graph_Node gn in nodeList.Values)
            {
                output += gn.ToString() + "\n";
                foreach (Graph_Edge ge in adjList[gn.key])
                {
                    output += "\t" + ge.ToString() + "\n";
                }
            }
            return output;
        }

        // FOR MULTI-AGENT: need deep copy of adjList dict as well as the lists inside of it. Graph_Node/Graph_Edge instances are only pointers, and their values should not be changing, only read/removed from cloned list/adjList.
        public Graph_UserGraph getEditableCopy() 
        {
            Graph_UserGraph clonedGraph = new Graph_UserGraph(multiAgent)
            {
                adjList = adjList.ToDictionary(x => x.Key, x => x.Value.ToList()),
                nodeList = nodeList,
                largestRatio = largestRatio,
                startNode = startNode,
                endNode = endNode,
                startNode2 = startNode2,
                endNode2 = endNode2
            };
            return clonedGraph;
        }

        // FOR VISUALISATIONSTATES: need deep copy of all nodes and edges as colours will change
        // check that startnode/endnode references etc arent used when changing colours (should all go through nodelist)
        public Graph_UserGraph createGraphState()
        {
            Graph_UserGraph clonedGraph = new Graph_UserGraph(multiAgent)
            {
                adjList = adjList.ToDictionary(x => x.Key, x => new List<Graph_Edge>(x.Value.ConvertAll(x => (Graph_Edge)x.Clone()))),
                nodeList = nodeList.ToDictionary(x => x.Key, x => (Graph_Node)x.Value.Clone()),
                largestRatio = largestRatio,
                startNode = startNode,
                endNode = endNode,
                startNode2 = startNode2,
                endNode2 = endNode2               
            };
            return clonedGraph;
        }

        public Graph_Edge getEdge(int source, int destination)
        {
            return adjList[source].Find(x => x.destination == destination);
        }

        public Graph_Edge getReverseEdge(Graph_Edge e) // returns null if does not exist
        {
            return adjList[e.destination].Exists(x => x.destination == e.source) ? adjList[e.destination].Find(x => x.destination == e.source) : null; // if there is a reverse edge, then return it, else return null
        }

        public void addEdge(int source, int destination, int weight)
        {
            double distance = getDistanceBetweenNodes(nodeList[source], nodeList[destination]);

            if (distance / weight > largestRatio)
            {
                largestRatio = distance / weight;
            }            
            
            adjList[source].Add(new Graph_Edge(source, destination, weight, distance, GraphColour.Default));
        }

        public double getDistanceBetweenNodes(Graph_Node source, Graph_Node destination) // couldve just used Vector2.DistanceTo() for this??
        {
            return Math.Sqrt(Math.Pow(source.position.X - destination.position.X, 2) + Math.Pow(source.position.Y - destination.position.Y, 2));
        }

        public void addNode(int nodeInt, Vector2 pos)
        {
            Graph_Node node = new Graph_Node(nodeInt, pos);
            nodeList[nodeInt] = node;
            adjList[nodeInt] = new List<Graph_Edge>();
        }

        public static Color getColourToDraw(GraphColour gc)
        {
            return coloursDict[gc];
        }

        public void recalculateLargestRatio()
        {
            largestRatio = 1;
            foreach (KeyValuePair<int, List<Graph_Edge>> kvp in adjList)
            {
                foreach (Graph_Edge edge in kvp.Value)
                {
                    double newDist = getDistanceBetweenNodes(nodeList[edge.source], nodeList[edge.destination]);

                    if (newDist / edge.weight > largestRatio)
                    {
                        largestRatio = newDist / edge.weight;
                    }
                }
            }
        }

        public void resetNodeColours()
        {
            foreach (Graph_Node n in nodeList.Values)
            {
                if(n.key == endNode && n.key == endNode2)
                {
                    n.setPrimaryColour(GraphColour.EndNode);
                    n.setSecondaryColour(GraphColour.EndNode2);
                }
                else if(n.key == startNode && n.key == endNode2)
                {
                    n.setPrimaryColour(GraphColour.StartNode);
                    n.setSecondaryColour(GraphColour.EndNode2);
                }
                else if (n.key == startNode2 && n.key == endNode)
                {
                    n.setPrimaryColour(GraphColour.StartNode2);
                    n.setSecondaryColour(GraphColour.EndNode);
                }
                else
                {
                    if (n.key == startNode)
                    {
                        n.setPrimaryColour(GraphColour.StartNode);
                    }
                    else if (n.key == endNode)
                    {
                        n.setPrimaryColour(GraphColour.EndNode);
                    }
                    else if (n.key == startNode2)
                    {
                        n.setPrimaryColour(GraphColour.StartNode2);
                    }
                    else if (n.key == endNode2)
                    {
                        n.setPrimaryColour(GraphColour.EndNode2);
                    }
                    else
                    {
                        n.setPrimaryColour(GraphColour.Default);
                    }
                    n.setSecondaryColour(null);
                }         
            }
        }
    }
}
