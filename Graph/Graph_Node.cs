using Godot;
using System;

namespace CE301.Graph
{
    public class Graph_Node : ICloneable
    {
        public int key { private set; get; }
        public Vector2 position { private set; get; }
        public GraphColour primaryColour { private set; get; }
        public GraphColour? secondaryColour { private set; get; }

        public Graph_Node(int key, Vector2 pos)
        {
            this.key = key;
            position = pos;
            this.primaryColour = GraphColour.Default;
        }

        // FOR VISUALISATION STATE
        public object Clone()
        {
            Graph_Node copiedNode = (Graph_Node)this.MemberwiseClone();
            copiedNode.position = new Vector2(position.X, position.Y);
            return copiedNode;
        }

        public void setPrimaryColour(GraphColour gc)
        {
            //GD.Print("CHANGE PRIMARY OF " + ToString() + " to " + gc.ToString());
            primaryColour = gc;
            secondaryColour = null;
        }
        public void setSecondaryColour(GraphColour? gc)
        {
            //GD.Print("CHANGE SECONDARY OF " + ToString() + " to " + gc.ToString());
            secondaryColour = gc;
        }

        public override string ToString() // FOR DEBUGGING PURPOSES
        {
            return "Node: " + key + ". Position: " + position + ". Colour: " + primaryColour.ToString() + " " + secondaryColour.ToString();
        }

        public void updatePosition(Vector2 newPos)
        {
            position = newPos;
        }
    }
}
