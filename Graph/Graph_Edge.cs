using System;
using Godot;
using System.Numerics;
using CE301.GO_Graph;

namespace CE301.Graph
{
    public class Graph_Edge : ICloneable
    {
        public int source { private set; get; }
        public int destination { private set; get; }
        public int weight { private set; get; }
        public double distance { private set; get; }
        public GraphColour primaryColour { private set; get; }
        public GraphColour? secondaryColour { private set; get; }
        public GO_GraphEdge drawnEdge = null;

        public Graph_Edge(int source, int destination, int weight, double distance, GraphColour colour)
        {
            this.source = source;
            this.destination = destination;
            this.weight = weight;
            this.distance = distance;
            this.primaryColour = colour;
        }

        // FOR VISUALISATION STATE
        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public void setPrimaryColour(GraphColour gc)
        {
            GD.Print("CHANGE PRIMARY OF " + ToString() + " to " + gc.ToString());
            primaryColour = gc;
            secondaryColour = null;
        }

        public void setSecondaryColour(GraphColour gc)
        {
            GD.Print("CHANGE SECONDARY OF " + ToString() + " to " + gc.ToString());
            secondaryColour = gc;
        }

        public override string ToString() // FOR DEBUGGING PURPOSES
        {
            return "Edge: " + source + " -> " + destination;
        }

        public string ToDebugString() // FOR DEBUGGING PURPOSES
        {
            return "Edge: " + source + " -> " + destination + ": " + weight + ". Colour: " + primaryColour.ToString() + ", " + secondaryColour.ToString() + ". ";
        }

        public override bool Equals(object obj) // need to override GetHashCode() too if using Graph_Edge as a key in hash-tables
        {
            if (obj is Graph_Edge)
            {
                Graph_Edge other = (Graph_Edge)obj;
                if(source == other.source && destination == other.destination && weight == other.weight)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
