using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using CE301.Graph;
using Godot;

namespace CE301
{
    public class VisualisationState
    {
        public Graph_Graph graphState { get; private set; }
        public Dictionary<string, string> varsGridText { get; private set; }
        public bool token = false;
        public bool secondSlower = false; // whether second or first agent finishes second
        public List<int> firstAgentPath;
        public List<int> secondAgentPath;

        public VisualisationState(Graph_Graph graph, Dictionary<string, string> dict)
        {
            graphState = graph;
            varsGridText = dict;
        }

        public VisualisationState(List<int> firstAgentPath, List<int> secondAgentPath, bool secondSlower)
        {
            token = true;
            this.firstAgentPath = firstAgentPath;
            this.secondAgentPath = secondAgentPath;
            this.secondSlower = secondSlower;
        }
    }
}
