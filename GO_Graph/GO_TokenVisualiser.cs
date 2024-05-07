using CE301.Graph;
using Godot;
using System;
using System.Collections.Generic;

namespace CE301.GO_Graph
{
    public partial class GO_TokenVisualiser : Node2D
    {
        private Manager manager;
        private List<int> path;
        private Vector2 position;
        private bool second;
        private int currentIndex;
        private Vector2 distancePerSecond;
        private float distanceTravelled;
        private float distanceToTravel;
        private bool finished = false;
        private bool waiting = false;
        private Color colour;
        private int _RADIUS_ = 10;
        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            manager = GetNode<Manager>("/root/Manager");
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public async override void _Process(double delta) // change position and redraw every frame - use delta to make smooth
        {
            if (!finished && !waiting && !manager.paused) // if not finished or waiting
            {
                //GD.Print((second ? "first " : "second ") + "token position: " + position.ToString());
                if (distanceTravelled >= distanceToTravel) // if it has reached position of currentindex
                {
                    distanceTravelled = 0;
                    position = manager.graph.nodeList[path[currentIndex]].position; // set position to center of node for aesthetics before recalc for next node
                    QueueRedraw(); // redraw at new pos

                    if (currentIndex == path.Count - 1) // if reaches position of last index in path (finished)
                    {
                        finished = true;
                        if (second)
                        {
                            manager.EmitSignal(nameof(manager.secondTokenVisualisationComplete)); // emit signal to tell manager to end visualisation
                        }
                        else
                        {
                            manager.EmitSignal(nameof(manager.firstTokenVisualisationComplete));
                        }
                    }

                    currentIndex++;

                    if (currentIndex < path.Count)
                    {
                        if (path[currentIndex] < 0) // if new index is < 0, needs to wait
                        {
                            waiting = true;
                            await ToSignal(GetTree().CreateTimer(Math.Abs(path[currentIndex])), SceneTreeTimer.SignalName.Timeout);
                            waiting = false;
                            currentIndex++;
                        }

                        distanceToTravel = position.DistanceTo(manager.graph.nodeList[path[currentIndex]].position);
                        if (path[currentIndex - 1] < 0) // if last index was wait, need to use index - 2 for calc
                        {
                            Graph_Edge edgeToTraverse = manager.graph.getEdge(path[currentIndex - 2], path[currentIndex]);
                            if (edgeToTraverse.drawnEdge != null)
                            {
                                position = edgeToTraverse.drawnEdge.firstPoint;
                                distancePerSecond = edgeToTraverse.drawnEdge.firstPoint.DirectionTo(edgeToTraverse.drawnEdge.secondPoint) * edgeToTraverse.drawnEdge.firstPoint.DistanceTo(edgeToTraverse.drawnEdge.secondPoint) / edgeToTraverse.weight;
                            }
                            else
                            {
                                distancePerSecond = position.DirectionTo(manager.graph.nodeList[path[currentIndex]].position) * position.DistanceTo(manager.graph.nodeList[path[currentIndex]].position) / edgeToTraverse.weight;
                            }                           
                        }
                        else // otherwise just recalc distance normally
                        {
                            Graph_Edge edgeToTraverse = manager.graph.getEdge(path[currentIndex - 1], path[currentIndex]);
                            if(edgeToTraverse.drawnEdge != null)
                            {
                                position = edgeToTraverse.drawnEdge.firstPoint;
                                distancePerSecond = edgeToTraverse.drawnEdge.firstPoint.DirectionTo(edgeToTraverse.drawnEdge.secondPoint) * edgeToTraverse.drawnEdge.firstPoint.DistanceTo(edgeToTraverse.drawnEdge.secondPoint) / edgeToTraverse.weight;
                            }
                            else
                            {
                                distancePerSecond = position.DirectionTo(manager.graph.nodeList[path[currentIndex]].position) * position.DistanceTo(manager.graph.nodeList[path[currentIndex]].position) / edgeToTraverse.weight;
                            }                       
                        }
                    }
                }

                position += distancePerSecond * (float)delta; // increment position
                distanceTravelled += distancePerSecond.Length() * (float)delta;

                QueueRedraw(); // redraw at new pos
            }
        }

        public override void _Draw()
        {
            DrawCircle(position, _RADIUS_, colour);
        }

        public void init(List<int> path, GraphColour gc, bool second = false)
        {
            this.path = path;
            GD.Print("PATH FROM TOKEN: " + manager.listToText(path));
            position = manager.graph.nodeList[path[0]].position;
            currentIndex = 0;
            colour = Graph_Graph.getColourToDraw(gc);
            this.second = second;
        }
    }
}