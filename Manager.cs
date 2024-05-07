using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Globalization;
using CE301.Graph;
using CE301.GO_Graph;
using System.Diagnostics;

namespace CE301
{
    public partial class Manager : Node
    {
        [Signal] public delegate void changeFixedStateEventHandler(bool button_pressed); // default graph toggle flicked
        [Signal] public delegate void pauseButtonPressedEventHandler(); // pause/unpause button pressed
        [Signal] public delegate void playButtonPressedEventHandler(); // play/stop button pressed
        [Signal] public delegate void categoryButtonPressedEventHandler(Category category); // returns algo button clicked / sets enum for it too
        [Signal] public delegate void contextMenuButtonPressedEventHandler(string buttonName); // returns which button on contextmenu being clicked (menu changes on what was right clicked on panel)
        [Signal] public delegate void graphObjHoveredChangedEventHandler(Node2D obj); // emitted whenever new node/edge/none is hovered to keep track of what is being clicked
        [Signal] public delegate void lineEditSubmittedEventHandler(bool isNum, int val); // entering weight to lineedit, provides text entered to validate and use/error back
        [Signal] public delegate void startTokenVisualisationEventHandler(Godot.Collections.Array<int> path, GraphColour colour, bool second = false); // called to initialise tokens in GO_GraphPanel and token visualisation start
        [Signal] public delegate void secondTokenVisualisationCompleteEventHandler(); // emitted by GO_TokenVisualiser when second token (as made by instantiation args) is finished. is also manually called when resetting/skipping visualisation.
        [Signal] public delegate void firstTokenVisualisationCompleteEventHandler();
        [Signal] public delegate void errorMessageEventHandler(string msg); // sends to GO_GraphPanel to make popup with correct text (for window precedence)
        [Signal] public delegate void skipVisualisationStatesEventHandler(int skip); // from multiple skip buttons, -9999, -1, 1, 9999 as args depending on button
        [Signal] public delegate void appropriateGraphEventHandler(); // change button from play to stop when visualisation starts (graph is acceptable)
        [Signal] public delegate void visualisationPausedEventHandler(); // set pause button to unpause and red font when pausing/skipping
        [Signal] public delegate void resetStateEventHandler(); // change play button from stop to play when pressed
        [Signal] public delegate void generateRandomGraphPressedEventHandler(); // calls generateRandomGraph()

        public Graph_Graph graph { get; private set; }
        public bool fixedSwitch { get; private set; }
        public bool requestsUpdate { get; private set; }
        public bool paused { get; private set; }
        public bool playing { get; private set; }
        public Category currentCategory { get; private set; }
        private CanvasItem currentVarsGrid;
        private VisualisationState defaultVisualisationGrid;
        public List<GridContainer> varsGrids = new List<GridContainer>();
        public List<VisualisationState> visualisationStates = new List<VisualisationState>();
        public int currentVisualisationState;
        private bool tokensRunning = false;

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            changeFixedState += onFixedGraphToggleToggled;
            pauseButtonPressed += onPauseButtonPressed;
            playButtonPressed += onPlayButtonPressed;
            categoryButtonPressed += onCategoryChanged;
            skipVisualisationStates += skipVisualisationState;
            generateRandomGraphPressed += generateRandomGraph;

            fixedSwitch = false;
            paused = false;
            playing = false;
            graph = new Graph_UserGraph(false);
            onCategoryChanged(Category.AStar);
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(double delta)
        {
        }

        private void onCategoryChanged(Category category) // TODO?: not convinced relative path is best way for this. maybe use Groups?
        {

            if (currentVarsGrid != null)
            {
                currentVarsGrid.Hide();
            }
            else { (GetTree().Root.GetNode("Root/MainLayer/Algos+VBox/Graph+HBox/Vars+Control/VarsPanel/A*Grid") as CanvasItem).Hide(); } // A* as default, cant define currentVarsGrid in Ready() as its not created yet in scenetree

            currentCategory = category;
            //onFixedGraphToggleToggled(fixedSwitch); // resets graph

            switch (currentCategory)
            {
                case Category.AStar:
                    currentVarsGrid = GetTree().Root.GetNode("Root/MainLayer/Algos+VBox/Graph+HBox/Vars+Control/VarsPanel/A*Grid") as CanvasItem;
                    break;
                case Category.Dijkstra:
                    currentVarsGrid = GetTree().Root.GetNode("Root/MainLayer/Algos+VBox/Graph+HBox/Vars+Control/VarsPanel/DijkstraGrid") as CanvasItem;
                    break;
                case Category.CAStar:
                    currentVarsGrid = GetTree().Root.GetNode("Root/MainLayer/Algos+VBox/Graph+HBox/Vars+Control/VarsPanel/CA*Grid") as CanvasItem;
                    break;
                case Category.WHCAStar:
                    break;
            }
            resetVisualiationState();
            currentVarsGrid.Show();
        }

        private void onFixedGraphToggleToggled(bool button_pressed)
        {
            resetVisualiationState();
            fixedSwitch = button_pressed;
            if (fixedSwitch)
            {
                if (currentCategory == Category.CAStar) // this needs to be some form of collection that includes any multi agent algo
                {
                    graph = new Graph_FixedGraph(true);
                }
                else
                {
                    graph = new Graph_FixedGraph(false);
                }
            }
            else
            {
                if (currentCategory == Category.CAStar) // this needs to be some form of collection that includes any multi agent algo
                {
                    graph = new Graph_UserGraph(true);
                }
                else
                {
                    graph = new Graph_UserGraph(false);
                }
            }
            requestUpdate();
        }

        public void requestUpdate()
        {
            GD.Print("------------ request drawing ---------------");
            requestsUpdate = true;
        }

        public void completeUpdate()
        {
            requestsUpdate = false;
        }

        private void onPauseButtonPressed()
        {
            paused = !paused;
        }

        // could also not do bfs here and just let the visualisation run on unconnected graphs and show error when it reaches that point...
        private bool checkGraphAppropriate() // check graph has a connection between start/end nodes +  start and end nodes are set + start/end nodes are different + at least 2 nodes
        {
            if (graph.nodeList.ContainsKey(graph.startNode) == false || graph.nodeList.ContainsKey(graph.endNode) == false)
            {
                displayErrorBox("A start or end node (first agent) have not been set");
                return false;
            }
            if (graph.startNode == graph.endNode)
            {
                displayErrorBox("The start and end node (first agent) are the same node");
                return false;
            }
            if (graph.nodeList.Count < 2)
            {
                displayErrorBox("There are less than two nodes");
                return false;
            }

            // breadth-first search for connectedness between start and end (new func)

            if (graph.multiAgent) // more checks if multi agent, checking the same things but on the second agent
            {
                if (graph.nodeList.ContainsKey(graph.startNode2) == false || graph.nodeList.ContainsKey(graph.endNode2) == false) // checking the same things but on the second agent
                {
                    displayErrorBox("The start or end node (second agent) have not been set");
                    return false;
                }
                if (graph.startNode2 == graph.endNode2)
                {
                    displayErrorBox("The start and end node (second agent) are the same node");
                    return false;
                }
                if (graph.startNode2 == graph.startNode)
                {
                    displayErrorBox("The first and second agent start node are the same");
                    return false;
                }
                if (graph.nodeList.Count < 3)
                {
                    displayErrorBox("There are less than three nodes");
                    return false;
                }

                // breadth-first search for connectedness of second agent
            }

            EmitSignal(nameof(appropriateGraph)); // if visualising, change play button to stop button
            return true;
        }

        public void displayErrorBox(string msg)
        {
            EmitSignal(nameof(errorMessage), msg);
        }

        // these all return the visualisationstates containing graph copies and varsgrid states
        private async void onPlayButtonPressed()
        {
            // generate graph - make function, can probs add as functionality lol
            //      generate nodes in random pos
            //      generate edges with weight relative to distance
            // need to make sure it is not unconnected before using it
            // run all three algos on it
            // record time taken to get visualisationstates list
            // record results in a file
            if (!playing && checkGraphAppropriate())
            {
                visualisationStates.Clear();

                Stopwatch sw = new Stopwatch();
                sw.Start();

                switch (currentCategory)
                {
                    case Category.AStar:
                        getAStarVisualisationStates(graph);
                        break;
                    case Category.Dijkstra:
                        getDijkstraVisualisationStates(graph);
                        break;
                    case Category.CAStar:
                        getCAStarVisualisationStates(graph);
                        break;
                    case Category.WHCAStar:
                        break;
                }

                sw.Stop();
                GD.Print("Time elapsed: " + sw.Elapsed);

                playing = true;

                await runVisualisation();
            }
            else if (playing) // stop playing and set graph back to beginning
            {
                await resetVisualiationState();
            }

        }

        private void generateRandomGraph()
        {
            bool connected = false;
            while (!connected)
            {
                graph = new Graph_UserGraph(false);
                graph.adjList.Clear();
                graph.nodeList.Clear();

                Random rnd = new Random();
                Control graphPanel = GetTree().Root.GetNode("Root/MainLayer/Algos+VBox/Graph+HBox/GraphPanel") as Control;
                Vector2 sizeContraints = graphPanel.GetRect().Size;
                List<(int, int)> gridSpots = new List<(int, int)>();

                int nodeCount = rnd.Next(8, 17);
                while (gridSpots.Count < nodeCount)
                {
                    // get grid spot
                    var spot = (rnd.Next(0, 5), rnd.Next(0, 5));
                    if (!gridSpots.Contains(spot))
                    {
                        gridSpots.Add(spot);
                    }
                }

                for (int i = 0; i < gridSpots.Count; i++)
                {
                    (graph as Graph_UserGraph).addNode(new Vector2(rnd.Next((int)((sizeContraints.X / 5) * gridSpots[i].Item1) + GO_GraphNode._RADIUS_, (int)((sizeContraints.X / 5) * (gridSpots[i].Item1 + 1)) - GO_GraphNode._RADIUS_), rnd.Next((int)((sizeContraints.Y / 5) * gridSpots[i].Item2) + GO_GraphNode._RADIUS_, (int)((sizeContraints.Y / 5) * (gridSpots[i].Item2 + 1) - GO_GraphNode._RADIUS_))));
                }

                foreach (Graph_Node node in graph.nodeList.Values)
                {
                    List<Graph_Node> sortedList = graph.nodeList.Values.ToList();
                    sortedList.Remove(node);
                    sortedList.Sort((n1, n2) => node.position.DistanceTo(n1.position).CompareTo(node.position.DistanceTo(n2.position)));

                    for (int i = 0; i < 3; i++)
                    {
                        int weight = (int)(sortedList[i].position.DistanceTo(node.position) / 50) + 1 + rnd.Next(-1, 1);
                        if (weight < 1)
                        {
                            weight = 1;
                        }
                        graph.addEdge(node.key, sortedList[i].key, weight);
                    }
                }

                // get node closest to 0,0 and closest to bottom right corner and set start/end (for multi agent - either opp corners or two closest each?)
                // two closest on each corner for now
                List<Graph_Node> startEndPoints = graph.nodeList.Values.ToList();
                startEndPoints.Sort((n1, n2) => new Vector2(0, 0).DistanceTo(n1.position).CompareTo(new Vector2(0, 0).DistanceTo(n2.position)));
                graph.startNode = startEndPoints[0].key;
                graph.startNode2 = startEndPoints[1].key;

                startEndPoints.Sort((n1, n2) => sizeContraints.DistanceTo(n1.position).CompareTo(sizeContraints.DistanceTo(n2.position)));
                graph.endNode = startEndPoints[1].key;
                graph.endNode2 = startEndPoints[0].key;

                // do bfs to check if connected, else make again
                Stack<Graph_Node> queue = new Stack<Graph_Node>();
                queue.Push(graph.nodeList[graph.startNode]);
                List<Graph_Node> explored = new List<Graph_Node>();
                while (queue.Count > 0)
                {
                    Graph_Node node = queue.Pop();
                    explored.Add(node);
                    foreach (Graph_Edge e in graph.adjList[node.key])
                    {
                        if (!explored.Contains(graph.nodeList[e.destination]) && !queue.Contains(graph.nodeList[e.destination]))
                        {
                            queue.Push(graph.nodeList[e.destination]);
                        }
                    }
                }
                if (explored.Count == graph.nodeList.Count)
                {
                    connected = true;
                }
            }      

            requestUpdate();
            //GD.Print(graph);
        }

        private async Task resetVisualiationState()
        {
            playing = false;
            if (tokensRunning) // stop tokens if running
            {
                tokensRunning = false;
                EmitSignal(nameof(firstTokenVisualisationComplete));
                EmitSignal(nameof(secondTokenVisualisationComplete));
            }
            EmitSignal(nameof(resetState));
            currentVisualisationState = 0;
            await updateShownState(reset: true);
        }

        private async Task runVisualisation() // while playing and not paused, cycle through states every [INSERT TIME HERE]
        {
            currentVisualisationState = 0;
            do
            {
                await updateShownState();
                do { await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout); }
                while ((playing && paused) || currentVisualisationState >= visualisationStates.Count);

                currentVisualisationState++;
            }
            while (playing);
        }

        private async Task updateShownState(bool reset = false) // requests update for new graph/varsgrid in visualisationstates
        {
            if (reset && defaultVisualisationGrid != null)
            {
                graph = defaultVisualisationGrid.graphState;
                foreach (KeyValuePair<string, string> kvp in defaultVisualisationGrid.varsGridText)
                {
                    if (currentVarsGrid.HasNode(kvp.Key))
                    {
                        (currentVarsGrid.GetNode(kvp.Key) as RichTextLabel).Text = kvp.Value;
                    }
                }
            }
            else if (!reset)
            {
                if (currentVisualisationState < visualisationStates.Count)
                {
                    if (visualisationStates[currentVisualisationState].token) // if next state is token visualisation, call it and wait
                    {
                        if (!tokensRunning) // dont want multiple sets of tokens running at the same time (failsafe)
                        {
                            if (visualisationStates[currentVisualisationState].firstAgentPath == null && visualisationStates[currentVisualisationState].secondAgentPath == null) // if no token to visualise, show error
                            {
                                displayErrorBox("The graph is unnconected, no path found for the first agent or second agent");
                            }
                            else if (visualisationStates[currentVisualisationState].firstAgentPath == null) // display the non-null path as token
                            {
                                EmitSignal(nameof(this.startTokenVisualisation), new Godot.Collections.Array<int>(visualisationStates[currentVisualisationState].secondAgentPath), (int)GraphColour.SecondToken, true);
                                tokensRunning = true;
                                await ToSignal(this, "secondTokenVisualisationComplete"); // await signal from token to say its done
                                displayErrorBox("The graph is unnconected, no path found for the first agent");
                            }
                            else if (visualisationStates[currentVisualisationState].secondAgentPath == null) // display the non-null path as token
                            {
                                EmitSignal(nameof(this.startTokenVisualisation), new Godot.Collections.Array<int>(visualisationStates[currentVisualisationState].firstAgentPath), (int)GraphColour.FirstToken, true);
                                tokensRunning = true;
                                await ToSignal(this, "firstTokenVisualisationComplete"); // await signal from token to say its done
                                displayErrorBox("The graph is unnconected, no path found for the second agent");
                            }
                            else // display both tokens as normal
                            {
                                // signals cant emit lists or enums, so have to convert to godot variant and to the underlying int for the enum and back again where signal recieved
                                EmitSignal(nameof(this.startTokenVisualisation), new Godot.Collections.Array<int>(visualisationStates[currentVisualisationState].firstAgentPath), (int)GraphColour.FirstToken, false);
                                EmitSignal(nameof(this.startTokenVisualisation), new Godot.Collections.Array<int>(visualisationStates[currentVisualisationState].secondAgentPath), (int)GraphColour.SecondToken, true);
                                tokensRunning = true;
                                if (visualisationStates[currentVisualisationState].secondSlower)
                                {
                                    await ToSignal(this, "secondTokenVisualisationComplete"); // await signal from second token to say its done, then can end this func
                                }
                                else
                                {
                                    await ToSignal(this, "firstTokenVisualisationComplete");
                                }                              
                            }
                        }                  
                        tokensRunning = false;
                    }
                    else // otherwise just update graph/varsgrid as necessary
                    {
                        graph = visualisationStates[currentVisualisationState].graphState;
                        foreach (KeyValuePair<string, string> kvp in visualisationStates[currentVisualisationState].varsGridText)
                        {
                            (currentVarsGrid.GetNode(kvp.Key) as RichTextLabel).Text = kvp.Value;
                        }
                    }
                }
            }

            requestUpdate();
        }

        private async void skipVisualisationState(int skip) // called when any navigation buttons pressed
        {
            if (playing)
            {
                EmitSignal(nameof(visualisationPaused));
                paused = true;
                if (tokensRunning) // stop all tokens if playing when skipping forward/back
                {
                    EmitSignal(nameof(secondTokenVisualisationComplete));
                    tokensRunning = false;
                }

                if (skip + currentVisualisationState >= visualisationStates.Count){ currentVisualisationState = visualisationStates.Count - 1; }
                else if (skip + currentVisualisationState < 0) { currentVisualisationState = 0; }
                else { currentVisualisationState += skip; }
                while (visualisationStates[currentVisualisationState].token) { currentVisualisationState--; } // so it cannot start on a token state (no graph to show attached)
                await updateShownState();
            }
        }

        private void resetAllColours() // reset colour of all nodes/edges to default (except start/goal nodes)
        {
            graph.resetNodeColours();
            foreach (List<Graph_Edge> list in graph.adjList.Values)
            {
                foreach (Graph_Edge edge in list)
                {
                    edge.setPrimaryColour(GraphColour.Default);
                }
            }
        }

        private string dictToText<T, S>(Dictionary<T, S> dict, bool valueSort = false) // true - sort by value, false - sort by key (ASC both) 
        {
            List<KeyValuePair<T, S>> outOrder = new List<KeyValuePair<T, S>>();
            bool start = true;
            foreach (KeyValuePair<T, S> pair in dict)
            {
                if (start)
                {
                    outOrder.Add(pair);
                    start = false;
                }
                else
                {
                    for (int i = 0; i < outOrder.Count; i++)
                    {
                        if (valueSort)
                        {
                            if (Convert.ToDouble(pair.Value) < Convert.ToDouble(outOrder[i].Value))
                            {
                                outOrder.Insert(i, pair);
                                break;
                            }
                        }
                        else
                        {
                            if (Convert.ToInt32(pair.Key) < Convert.ToInt32(outOrder[i].Key))
                            {
                                outOrder.Insert(i, pair);
                                break;
                            }
                        }
                    }
                    if (!outOrder.Contains(pair))
                    {
                        outOrder.Add(pair);
                    }
                }
            }

            string output = "[";
            foreach (KeyValuePair<T, S> outPair in outOrder)
            {
                output += "[" + outPair.Key.ToString() + ", " + (Convert.ToDouble(outPair.Value) < 99999 ? Convert.ToDouble(outPair.Value) != -1 ? Convert.ToDouble(outPair.Value).ToString("F", CultureInfo.InvariantCulture) : "S" : "\u221E") + "]"; // rounds to 2 d.p. and uses , for thousands instead of . (not sure if necessary but sounds nice). also has infinity sign instead of maxint.
                if (outOrder.IndexOf(outPair) != outOrder.Count - 1)
                {
                    output += ", ";
                }
            }
            output += "]";

            return output;
        }

        public string listToText<T>(List<T> list)
        {
            bool start = true;
            string output = "[";
            foreach (T element in list)
            {
                if (!start) { output += ", "; }
                output += element.ToString();
                start = false;
            }
            output += "]";
            return output;
        }

        public bool checkPathConnection(List<int> path, Graph_Graph graph)
        {
            if (path.Count <= 1)
            {
                return false;
            }
            for (int i = 1; i < path.Count; i++)
            {
                if (path[i] < 0)
                {
                    continue;
                }
                else if (path[i - 1] < 0)
                {
                    if (graph.getEdge(path[i - 2], path[i]) == null)
                    {
                        return false;
                    }
                }
                else
                {
                    if (graph.getEdge(path[i - 1], path[i]) == null)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public void visualisePath(List<int> path, Graph_Graph graph)
        {
            resetAllColours();
            for (int i = 1; i < path.Count; i++)
            {
                if (graph.nodeList[path[i]].key != graph.startNode && graph.nodeList[path[i]].key != graph.startNode2 && graph.nodeList[path[i]].key != graph.endNode && graph.nodeList[path[i]].key != graph.endNode2)
                {
                    graph.nodeList[path[i]].setPrimaryColour(GraphColour.Path);
                }

                // seems to need to change colour of reverse path in some cases where the edge that needs to be coloured is a two way same weight.
                Graph_Edge pathEdge = graph.adjList[path[i - 1]].Find(x => x.destination == path[i]);
                pathEdge.setPrimaryColour(GraphColour.Path);
                Graph_Edge reversePathEdge = graph.getReverseEdge(pathEdge);
                if (reversePathEdge != null && reversePathEdge.weight == pathEdge.weight)
                {
                    reversePathEdge.setPrimaryColour(GraphColour.Path);
                }
            }
        }

        void changeEdgeColour(Graph_Edge edge, GraphColour colour)
        {
            edge.setPrimaryColour(colour);

            Graph_Edge reverseEdge = graph.getReverseEdge(edge);
            if (reverseEdge != null && reverseEdge.weight == edge.weight)
            {
                reverseEdge.setPrimaryColour(colour);
            }
        }

        private void getAStarVisualisationStates(Graph_Graph g)
        {
            void createVisualisationState(Dictionary<int, double> prioList = null, Dictionary<int, int> cameFrom = null, Dictionary<int, int> costDict = null, int? currentNode = null, int? newCost = null, List<int> pathList = null)
            {
                Dictionary<string, string> varsGridText = new Dictionary<string, string>();

                // maybe dont bother with .count()==0? - just leave as [] when empty
                if (prioList != null) // order priolist by prio
                {
                    if (prioList.Count == 0)
                    {
                        varsGridText["prioqueueout"] = "Empty";
                    }
                    else
                    {
                        varsGridText["prioqueueout"] = dictToText(prioList, true);
                    }
                }
                if (cameFrom != null) // order by node key?
                {
                    if (cameFrom.Count == 0)
                    {
                        varsGridText["camefromdictout"] = "Empty";
                    }
                    else
                    {
                        varsGridText["camefromdictout"] = dictToText(cameFrom, false);
                    }
                }
                if (costDict != null) // order by node key?
                {
                    if (costDict.Count == 0)
                    {
                        varsGridText["costdictout"] = "Empty";
                    }
                    else
                    {
                        varsGridText["costdictout"] = dictToText(costDict, false);
                    }
                }
                if (currentNode != null)
                {
                    varsGridText["currentnodeout"] = currentNode.ToString();
                }
                if (newCost != null)
                {
                    varsGridText["newcostout"] = newCost.ToString();
                }
                if (pathList != null)
                {
                    if (pathList.Count == 0)
                    {
                        varsGridText["pathout"] = "Empty";
                    }
                    else
                    {
                        varsGridText["pathout"] = listToText(pathList);
                    }
                }

                visualisationStates.Add(new VisualisationState(graph.createGraphState(), varsGridText));
            }

            resetAllColours();            
            defaultVisualisationGrid = new VisualisationState(fixedSwitch ? graph.createGraphState() : (graph as Graph_UserGraph).createGraphState(), new Dictionary<string, string> { { "prioqueueout", "[]" }, { "camefromdictout", "[]" }, { "costdictout", "[]" }, { "currentnodeout", "null" }, { "newcostout", "null" }, { "pathout", "[]" } });

            Dictionary<int, double> prioList = new Dictionary<int, double>();
            prioList[g.startNode] = 0;
            Dictionary<int, int> cameFrom = new Dictionary<int, int>();
            cameFrom[g.startNode] = -1; // -1 for no node
            Dictionary<int, int> cost = new Dictionary<int, int>();
            cost[g.startNode] = 0;
            List<int> path = new List<int>();

            createVisualisationState(prioList: prioList, cameFrom: cameFrom, costDict: cost, pathList: path);

            while (prioList.Count != 0)
            {
                int currentNode = prioList.Aggregate((x, y) => x.Value < y.Value ? x : y).Key; // gets node with lowest distance + heuristic
                prioList.Remove(currentNode);

                if (g.nodeList[currentNode].key != g.startNode && g.nodeList[currentNode].key != g.endNode)
                {
                    g.nodeList[currentNode].setPrimaryColour(GraphColour.FrontierEdge);
                }

                createVisualisationState(prioList: prioList, currentNode: currentNode);

                if (currentNode.Equals(g.endNode))
                {
                    break;
                }

                foreach (Graph_Edge neighbour in g.adjList[currentNode])
                {
                    changeEdgeColour(neighbour, GraphColour.FrontierEdge);
                    int newCost = cost[currentNode] + neighbour.weight;

                    createVisualisationState(newCost: newCost);

                    if ((!cost.ContainsKey(neighbour.destination)) || newCost < cost[neighbour.destination])
                    {
                        cost[neighbour.destination] = newCost;
                        cameFrom[neighbour.destination] = currentNode;

                        double prio = newCost + (g.getDistanceBetweenNodes(g.nodeList[neighbour.destination], g.nodeList[g.endNode]) / graph.largestRatio);
                        prioList[neighbour.destination] = prio;

                        createVisualisationState(costDict: cost, cameFrom: cameFrom, prioList: prioList);
                    }
                }
            }

            int source = g.endNode;
            path.Add(source);
            while (cameFrom.ContainsKey(source) && cameFrom[source] != -1)
            {
                path.Add(cameFrom[source]);
                source = cameFrom[source];
            }
            path.Reverse();

            if (checkPathConnection(path, g))
            {
                visualisePath(path, g);

                createVisualisationState(pathList: path);
            }
            else
            {
                displayErrorBox("This graph is unconnected, no path found");
            }
        }

        private void getDijkstraVisualisationStates(Graph_Graph g)
        {
            void createVisualisationState(List<int> unvisitedNodes = null, Dictionary<int, int> previous = null, Dictionary<int, double> distance = null, int? currentNode = null, double? newCost = null, List<int> pathList = null)
            {
                Dictionary<string, string> varsGridText = new Dictionary<string, string>();

                // maybe dont bother with .count()==0? - just leave as [] when empty
                if (unvisitedNodes != null) // order priolist by prio
                {
                    if (unvisitedNodes.Count == 0)
                    {
                        varsGridText["unvisitednodesout"] = "Empty";
                    }
                    else
                    {
                        varsGridText["unvisitednodesout"] = listToText(unvisitedNodes);
                    }
                }
                if (previous != null) // order by node key?
                {
                    if (previous.Count == 0)
                    {
                        varsGridText["previousout"] = "Empty";
                    }
                    else
                    {
                        varsGridText["previousout"] = dictToText(previous, false);
                    }
                }
                if (distance != null) // order by node key?
                {
                    if (distance.Count == 0)
                    {
                        varsGridText["distanceout"] = "Empty";
                    }
                    else
                    {
                        varsGridText["distanceout"] = dictToText(distance, false);
                    }
                }
                if (currentNode != null)
                {
                    varsGridText["currentnodeout"] = currentNode.ToString();
                }
                if (newCost != null)
                {
                    varsGridText["newcostout"] = newCost.ToString();
                    //(currentVarsGrid.GetNode("newcostout") as RichTextLabel).Text = newCost.ToString(); WHAT IS THIS DOING HERE??? IS IT NEEDED??
                }
                if (pathList != null)
                {
                    if (pathList.Count == 0)
                    {
                        varsGridText["pathout"] = "Empty";
                    }
                    else
                    {
                        varsGridText["pathout"] = listToText(pathList);
                    }
                }

                visualisationStates.Add(new VisualisationState(graph.createGraphState(), varsGridText));
            }

            resetAllColours();
            defaultVisualisationGrid = new VisualisationState(fixedSwitch ? graph.createGraphState() : (graph as Graph_UserGraph).createGraphState(), new Dictionary<string, string> { { "unvisitednodesout", "[]" }, { "previousout", "[]" }, { "distanceout", "[]" }, { "currentnodeout", "null" }, { "newcostout", "null" }, { "pathout", "[]" } });

            Dictionary<int, double> distance = new Dictionary<int, double>();
            Dictionary<int, int> previous = new Dictionary<int, int>();
            List<int> unvisitedNodes = new List<int>();
            List<int> path = new List<int>();
            foreach (int node in g.nodeList.Keys)
            {
                distance[node] = Double.MaxValue;
                unvisitedNodes.Add(node);
            }
            distance[g.startNode] = 0;
            previous[g.startNode] = -1;

            createVisualisationState(unvisitedNodes: unvisitedNodes, distance: distance, previous: previous, pathList: path);

            while (unvisitedNodes.Count > 0)
            {
                // this should use 'prioqueue' instead of this?
                int currentNode = unvisitedNodes[0];
                foreach (int node in unvisitedNodes)
                {
                    if (distance[node] < distance[currentNode])
                    {
                        currentNode = node;
                    }
                }
                unvisitedNodes.Remove(currentNode);

                if (g.nodeList[currentNode].key != g.startNode && g.nodeList[currentNode].key != g.endNode)
                {
                    g.nodeList[currentNode].setPrimaryColour(GraphColour.FrontierEdge);
                }

                createVisualisationState(unvisitedNodes: unvisitedNodes, currentNode: currentNode);

                if (currentNode == g.endNode)
                {
                    break;
                }

                foreach (Graph_Edge neighbour in g.adjList[currentNode])
                {
                    if (unvisitedNodes.Contains(neighbour.destination))
                    {
                        changeEdgeColour(neighbour, GraphColour.FrontierEdge);

                        double newDistance = distance[currentNode] + neighbour.weight;

                        if (newDistance < distance[neighbour.destination])
                        {
                            distance[neighbour.destination] = newDistance;
                            previous[neighbour.destination] = currentNode;
                        }

                        createVisualisationState(newCost: newDistance, distance: distance, previous: previous);
                    }
                }
            }

            int source = g.endNode;
            path.Add(source);
            while (previous.ContainsKey(source) && previous[source] != -1)
            {
                path.Add(previous[source]);
                source = previous[source];
            }
            path.Reverse();

            if (checkPathConnection(path, g))
            {
                visualisePath(path, g);

                createVisualisationState(pathList: path);
            }
            else
            {
                displayErrorBox("This graph is unconnected, no path found");
            }
        }

        // CA* more like LRA* but allowing wait if there is no faster alternate route. no sharing edges or nodes. show tokens.
        // run A* on first agent. run A* on second agent. compare paths, if collision, go to first point of collision and run A* on second agent again to find new path (exlcuding that edge). repeat as long as there are conflicts.
        // use IClonable for new graphs to run A* on (removing edges/changing start/end nodes)
        // dont display A* vars here, display some LRA* specific ones, say "go run this on A* tab for A* vars". still display colour changes tho.
        // gonna need some way to handle if deleting edge causes unconnected graph (or maybe A* algo around has consistent output for that possibly that I just need to handle)

        public void getCAStarVisualisationStates(Graph_Graph g)
        {
            void createVisualisationState(List<int> firstAgentPath = null, List<int> secondAgentPath = null, Graph_Edge collisionEdge = null, List<int> newPath = null, bool? shorterPath = null, int? timeToWait = null)
            {
                Dictionary<string, string> varsGridText = new Dictionary<string, string>();

                // maybe dont bother with .count()==0? - just leave as [] when empty
                if (firstAgentPath != null) // order priolist by prio
                {
                    if (firstAgentPath.Count == 0)
                    {
                        varsGridText["firstagentpathout"] = "Empty";
                    }
                    else
                    {
                        varsGridText["firstagentpathout"] = listToText(stripPath(firstAgentPath));
                    }
                }
                if (secondAgentPath != null) 
                {
                    if (secondAgentPath.Count == 0)
                    {
                        varsGridText["secondagentpathout"] = "Empty";
                    }
                    else
                    {
                        varsGridText["secondagentpathout"] = listToText(stripPath(secondAgentPath));
                    }
                }
                if (collisionEdge != null) 
                {
                    varsGridText["collisionedgeout"] = collisionEdge.ToString();
                }
                else
                {
                    varsGridText["collisionedgeout"] = "null";
                }
                if (newPath != null)
                {
                    if (newPath.Count == 0)
                    {
                        varsGridText["newpathout"] = "Empty";
                    }
                    else
                    {
                        varsGridText["newpathout"] = listToText(stripPath(newPath));
                    }
                }
                if (shorterPath != null)
                {
                    varsGridText["newpathshorterout"] = shorterPath.ToString();
                }
                if (timeToWait != null)
                {
                    varsGridText["timetowaitout"] = timeToWait.ToString();
                }

                visualisationStates.Add(new VisualisationState(graph.createGraphState(), varsGridText));
            }

            List<int> getPath(Graph_Graph g, Graph_Node startNode, Graph_Node endNode) // taken from A* with no awaits / colour changing
            {
                Dictionary<int, double> prioList = new Dictionary<int, double>();
                prioList[startNode.key] = 0;
                Dictionary<int, int> cameFrom = new Dictionary<int, int>();
                cameFrom[startNode.key] = -1; // -1 for no node
                Dictionary<int, int> cost = new Dictionary<int, int>();
                cost[startNode.key] = 0;
                List<int> path = new List<int>();

                while (prioList.Count != 0)
                {
                    int currentNode = prioList.Aggregate((x, y) => x.Value < y.Value ? x : y).Key; // gets node with lowest distance + heuristic
                    prioList.Remove(currentNode);

                    if (currentNode.Equals(endNode.key))
                    {
                        break;
                    }

                    foreach (Graph_Edge neighbour in g.adjList[currentNode])
                    {
                        int newCost = cost[currentNode] + neighbour.weight;

                        if ((!cost.ContainsKey(neighbour.destination)) || newCost < cost[neighbour.destination])
                        {
                            cost[neighbour.destination] = newCost;
                            cameFrom[neighbour.destination] = currentNode;

                            double prio = newCost + (g.getDistanceBetweenNodes(g.nodeList[neighbour.destination], endNode) / graph.largestRatio);
                            prioList[neighbour.destination] = prio;
                        }
                    }
                }

                int source = endNode.key;
                path.Add(source);
                while (cameFrom.ContainsKey(source) && cameFrom[source] != -1)
                {
                    path.Add(cameFrom[source]);
                    source = cameFrom[source];
                }
                path.Reverse();

                if (!checkPathConnection(path, g)) // return null if unconnected
                {
                    return null;
                }

                return path;
            }

            Graph_Edge getFirstPathCollision(List<int> path1, List<int> path2, List<Graph_Edge> collisionsHandled)
            {
                Dictionary<int, int> convertToNodeTimeDict(List<int> path) // node - time taken to get there
                {
                    Dictionary<int, int> pathtime = new Dictionary<int, int>();
                    pathtime[path[0]] = 0;
                    for (int i = 1; i < path.Count; i++)
                    {
                        if (path[i] < 0)
                        {
                            pathtime[path[i - 1]] += Math.Abs(path[i]);
                        }
                        else if (path[i - 1] < 0)
                        {
                            pathtime[path[i]] = g.getEdge(path[i - 2], path[i]).weight + pathtime[path[i - 2]];
                        }
                        else
                        {
                            pathtime[path[i]] = g.getEdge(path[i - 1], path[i]).weight + pathtime[path[i - 1]];
                        }
                    }
                    return pathtime;
                }

                Dictionary<int, int> path1time = convertToNodeTimeDict(path1);
                Dictionary<int, int> path2time = convertToNodeTimeDict(path2);
                GD.Print("first dict: " + dictToText(path1time));
                GD.Print("second dict: " + dictToText(path2time));

                for (int i = 1; i < path1.Count; i++) // gonna check each pair of nodes in order
                {
                    int index = path2.IndexOf(path1[i - 1]); // returns -1 if it cant find it
                    if (index != -1) // if it exists, isnt the end of the path and is equal to second of pair in path1
                    {
                        if (index + 1 < path2.Count() && path2[index + 1] < 0)
                        {
                            if (index + 2 < path2.Count() && path2[index + 2] == path1[i])
                            {
                                // check if on this edge at the same time
                                if ((path2time[path2[index]] >= path1time[path1[i - 1]] && path2time[path2[index]] < path1time[path1[i]]) || (path2time[path2[index + 2]] > path1time[path1[i - 1]] && path2time[path2[index + 2]] <= path1time[path1[i]])) // both traversing same edge weight so one of the times has to be within or equal to the boundaries
                                {
                                    Graph_Edge possibleCollision = g.getEdge(path1[i - 1], path1[i]);

                                    if (!collisionsHandled.Contains(possibleCollision))
                                    {
                                        changeEdgeColour(possibleCollision, true, GraphColour.Collision);
                                        return possibleCollision; // return edge that has collision
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (index + 1 < path2.Count() && path2[index + 1] == path1[i])
                            {
                                // check if on this edge at the same time
                                if ((path2time[path2[index]] >= path1time[path1[i - 1]] && path2time[path2[index]] < path1time[path1[i]]) || (path2time[path2[index + 1]] > path1time[path1[i - 1]] && path2time[path2[index + 1]] <= path1time[path1[i]])) // both traversing same edge weight so one of the times has to be within or equal to the boundaries
                                {
                                    Graph_Edge possibleCollision = g.getEdge(path1[i - 1], path1[i]);

                                    if (possibleCollision != null && !collisionsHandled.Contains(possibleCollision))
                                    {
                                        changeEdgeColour(possibleCollision, true, GraphColour.Collision);
                                        return possibleCollision; // return edge that has collision
                                    }
                                }
                            }
                        }
                    }
                }
                return null;
            }

            int getPathWeight(List<int> path)
            {
                int total = 0;
                for (int i = 1; i < path.Count; i++)
                {
                    Graph_Edge e;
                    if (path[i] < 0)
                    {
                        total += Math.Abs(path[i]);
                        continue;
                    }
                    else if (path[i - 1] < 0)
                    {
                        e = g.getEdge(path[i - 2], path[i]);
                    }
                    else
                    {
                        e = g.getEdge(path[i - 1], path[i]);
                    }

                    total += e.weight;
                }
                return total;
            }

            int getWeightToNode(List<int> path, int node)
            {
                int total = 0;
                for (int i = 1; i <= path.IndexOf(node); i++)
                {
                    if (path[i] < 0)
                    {
                        continue;
                    }
                    else if (path[i - 1] < 0)
                    {
                        total += graph.getEdge(path[i - 2], path[i]).weight;
                    }
                    else
                    {
                        total += graph.getEdge(path[i - 1], path[i]).weight;
                    }
                }
                return total;
            }

            List<int> stripPath(List<int> path) // remove wait commands
            {
                List<int> tempPath = new List<int>(path);
                tempPath.RemoveAll(x => x < 0);
                return tempPath;
            }

            void setSecondAgentPathColour(List<int> secondAgentPath, bool reset = false)
            {
                for(int i = 1; i < secondAgentPath.Count; i++)
                {
                    Graph_Edge edge = g.getEdge(secondAgentPath[i - 1], secondAgentPath[i]);
                    if(edge != null)
                    {
                        if (reset && edge.primaryColour != GraphColour.Path)
                        {
                            changeEdgeColour(edge, true, GraphColour.Default);
                        }
                        else
                        {
                            if (edge.primaryColour == GraphColour.Path)
                            {
                                changeEdgeColour(edge, false, GraphColour.Path2);
                            }
                            else
                            {
                                changeEdgeColour(edge, true, GraphColour.Path2);
                            }
                        }                       
                    }
                }
            }

            void changeEdgeColour(Graph_Edge edge, bool primary, GraphColour colour)
            {
                if (primary)
                {
                    edge.setPrimaryColour(colour);
                }
                else
                {
                    edge.setSecondaryColour(colour);
                }

                Graph_Edge reverseEdge = graph.getReverseEdge(edge);
                if (reverseEdge != null && reverseEdge.weight == edge.weight)
                {
                    if (primary)
                    {
                        reverseEdge.setPrimaryColour(colour);
                    }
                    else
                    {
                        reverseEdge.setSecondaryColour(colour);
                    }
                }
            }

            int getTimeOfPath(List<int> path)
            {
                int total = 0;
                for (int i = 1; i < path.Count; i++)
                {
                    if (path[i] < 0)
                    {
                        total += Math.Abs(path[i]);
                    }
                    else if (path[i - 1] < 0)
                    {
                        total += graph.getEdge(path[i - 2], path[i]).weight;
                    }
                    else
                    {
                        total += graph.getEdge(path[i - 1], path[i]).weight;
                    }
                }
                return total;
            }

            resetAllColours();
            defaultVisualisationGrid = new VisualisationState(fixedSwitch ? graph.createGraphState() : (graph as Graph_UserGraph).createGraphState(), new Dictionary<string, string> { { "firstagentpathout", "[]" }, { "secondagentpathout", "[]" }, { "collisionedgeout", "null" }, { "newpathout", "[]" }, { "newpathshorterout", "null" }, { "timetowaitout", "null]" } });
            createVisualisationState();

            List<int> firstAgentPath = getPath(g, g.nodeList[g.startNode], g.nodeList[g.endNode]);
            if(firstAgentPath != null)
            {
                for (int i = 1; i < (firstAgentPath == null ? 0 : firstAgentPath.Count); i++)
                {
                    Graph_Edge edge = g.getEdge(firstAgentPath[i - 1], firstAgentPath[i]);
                    if (edge != null)
                    {
                        changeEdgeColour(edge, true, GraphColour.Path);
                    }
                }

                createVisualisationState(firstAgentPath: firstAgentPath);

                Graph_Node secondAgentStart = g.nodeList[g.startNode2];
                Graph_Node secondAgentEnd = g.nodeList[g.endNode2];
                Graph_UserGraph copiedGraph = g.getEditableCopy(); // set as usergraph even if fixed because it has ability to remove edges

                List<int> secondAgentPath = getPath(copiedGraph, secondAgentStart, secondAgentEnd); // input graph using second agent start/end

                if(secondAgentPath != null)
                {
                    setSecondAgentPathColour(secondAgentPath);

                    createVisualisationState(secondAgentPath: secondAgentPath);

                    List<Graph_Edge> collisionsHandled = new List<Graph_Edge>(); // store edges whose collisions have already been fixed
                    int timeWaited = 0;

                    Graph_Edge collisionEdge = null;
                    if (firstAgentPath != null && secondAgentPath != null) // cant get collisionedge unless both agents actually have a path
                    {
                        collisionEdge = getFirstPathCollision(firstAgentPath, secondAgentPath, collisionsHandled);

                        createVisualisationState(collisionEdge: collisionEdge);
                    }

                    while (collisionEdge != null) // repeat until no collisions need to be handled
                    {
                        GD.Print("-----------WHILE LOOP CYCLE------------");
                        List<int> oldPath = new List<int>(secondAgentPath); // stores old path in case new path is impossible/too slow

                        secondAgentPath = oldPath.GetRange(0, oldPath.IndexOf(collisionEdge.source));// gets old path upto collision edge (so new path can be appended)

                        copiedGraph.removeEdge([collisionEdge.source, collisionEdge.destination]); // remove colliding edge from copied graph
                        bool bidirectional = copiedGraph.removeEdge([collisionEdge.destination, collisionEdge.source]); // remove reverse edge if exists. think built in methods have error handling so none required here.

                        List<int> concatPath = getPath(copiedGraph, g.nodeList[collisionEdge.source], secondAgentEnd);

                        if (concatPath != null)
                        {
                            secondAgentPath.AddRange(concatPath); // adds new path onto end of sliced path
                        }

                        GD.Print("second path: " + listToText(secondAgentPath));
                        int timeToWait = getWeightToNode(firstAgentPath, collisionEdge.destination) - getWeightToNode(oldPath, collisionEdge.source) - timeWaited;
                        bool newPathConnection = checkPathConnection(secondAgentPath, copiedGraph);
                        GD.Print("DEBUG: " + (getPathWeight(oldPath) + timeToWait) + " | " + getPathWeight(secondAgentPath));
                        bool newPathShorter = getPathWeight(secondAgentPath) < getPathWeight(oldPath) + timeToWait;
                        GD.Print("DEUBG: " + newPathShorter + " " + newPathConnection);
                        if (concatPath == null)
                        {
                            newPathShorter = false;
                        }

                        createVisualisationState(collisionEdge: collisionEdge, newPath: secondAgentPath, shorterPath: newPathShorter);

                        if (!newPathConnection || !newPathShorter) // if the new path is longer or graph is unconnected, go back to old path and add 'wait'
                        {
                            GD.Print("DEBUG: in if??");
                            setSecondAgentPathColour(stripPath(secondAgentPath), reset: true); // change new path back to default before setting old path back to secondagentpath

                            secondAgentPath = oldPath;

                            // timeWaited incorporated to take account of previous waits when more than one collision with no alternative path
                            timeToWait = getWeightToNode(firstAgentPath, collisionEdge.destination) - getWeightToNode(secondAgentPath, collisionEdge.source) - timeWaited;
                            timeWaited += timeToWait;

                            if (firstAgentPath.Contains(collisionEdge.source) && firstAgentPath[firstAgentPath.IndexOf(collisionEdge.source) + 1] == collisionEdge.destination)
                            {
                                changeEdgeColour(collisionEdge, true, GraphColour.Path);
                            }
                            else
                            {
                                changeEdgeColour(collisionEdge, true, GraphColour.Default);
                            }
                            setSecondAgentPathColour(stripPath(secondAgentPath));

                            createVisualisationState(secondAgentPath: secondAgentPath, collisionEdge: collisionEdge, timeToWait: timeToWait);

                            secondAgentPath.Insert(secondAgentPath.IndexOf(collisionEdge.destination), -timeToWait); // using '-time' to signify wait at previous node in list for time
                        }
                        else // decolour old path and recolour new path
                        {
                            setSecondAgentPathColour(stripPath(oldPath), true);
                            setSecondAgentPathColour(stripPath(secondAgentPath));
                            changeEdgeColour(collisionEdge, true, GraphColour.Path);
                        }
                        // make sure collision is back to default before getting new collision - done in else above
                        collisionsHandled.Add(collisionEdge);

                        copiedGraph.addEdge(collisionEdge.source, collisionEdge.destination, collisionEdge.weight);
                        if (bidirectional) { copiedGraph.addEdge(collisionEdge.destination, collisionEdge.source, collisionEdge.weight); }

                        collisionEdge = getFirstPathCollision(firstAgentPath, secondAgentPath, collisionsHandled); // check for collision again with new path

                        createVisualisationState(secondAgentPath: secondAgentPath, collisionEdge: collisionEdge, newPath: new List<int>());
                    }

                    for (int i = 1; i < firstAgentPath.Count; i++)
                    {
                        Graph_Edge edge = g.getEdge(firstAgentPath[i - 1], firstAgentPath[i]);
                        if (edge != null)
                        {
                            changeEdgeColour(edge, true, GraphColour.Path);
                        }
                    }
                    setSecondAgentPathColour(stripPath(secondAgentPath));
                    createVisualisationState();

                    visualisationStates.Add(new VisualisationState(firstAgentPath, secondAgentPath, getTimeOfPath(firstAgentPath) < getTimeOfPath(secondAgentPath))); // for calling token visualisation function
                }
                else
                {
                    displayErrorBox("This graph is unconnected. No path for the second agent.");
                }             
            }
            else
            {
                displayErrorBox("This graph is unconnected. No path for the first agent.");
            }
        }
    }
}

// show same start/end node as two colour semicircle (need more colours for second agent...)
// visualisation of multi-agent, dont show A* just the route provided from it. repeated for as many collisions as there are. then token at the end.
// if route ends up on same edge, either dashed line of second agent over solid line of first OR doubled up line of two different colour (needs to be oriented depending on gradient of line)
// token is gonna need its own async func cos awaiting signals. need to provide some way of doing that in visualisation hmm...
// maybe need two visualisationstates? one for singleagent/multiagent? inheritance moment like fixed/user graph?