using Godot;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Runtime.ConstrainedExecution;
using CE301.Graph;

namespace CE301.GO_Graph
{
	public partial class GO_GraphPanel : Control
	{
		private Manager manager;
		private PackedScene graphNodeScene;
		private PackedScene graphEdgeScene;
		private PackedScene graphContextMenuScene;
		private PackedScene inputPopupScene;
		private PackedScene tokenVisualisationScene;
		private GO_GraphNode addingEdge = null;
		public GO_GraphContextMenu contextMenuOpen = null;
		private GO_InputPopup inputPopupOpen = null;
		public Node2D graphObjHovered = null;
		private int timeHeld = 0;
		private bool holding = false;

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
            manager = GetNode<Manager>("/root/Manager");
			graphNodeScene = (PackedScene)ResourceLoader.Load("res://GraphNode.tscn");
			graphEdgeScene = (PackedScene)ResourceLoader.Load("res://GraphEdge.tscn");
			graphContextMenuScene = (PackedScene)ResourceLoader.Load("res://GraphContextMenu.tscn");
			inputPopupScene = (PackedScene)ResourceLoader.Load("res://InputPopup.tscn");
			tokenVisualisationScene = (PackedScene)ResourceLoader.Load("res://TokenVisualiser.tscn");
			manager.contextMenuButtonPressed += onContextMenuButtonPressed;
			manager.graphObjHoveredChanged += onGraphObjHoveredChanged;
			manager.startTokenVisualisation += visualiseToken;
		}

		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
			if (Input.IsActionPressed("left_click_down") && !manager.playing && graphObjHovered != null && graphObjHovered.GetType().Name == "GO_GraphNode") // hold drag to move node
			{
				timeHeld++;
				if(timeHeld > 10) // after holding for 10 frames (1/6 of a second)
				{
					holding = true;
					Vector2 mousePos = GetLocalMousePosition();
					if(mousePos.X < GO_GraphNode._RADIUS_) // dont let it be out of bounds in -ve direction (dunno how to do +ve tbh)
					{
						mousePos.X = GO_GraphNode._RADIUS_;
                    }
					else if (mousePos.X > GetRect().Size.X - GO_GraphNode._RADIUS_)
					{
                        mousePos.X = GetRect().Size.X - GO_GraphNode._RADIUS_;
                    }                       
                    if (mousePos.Y < GO_GraphNode._RADIUS_)
                    {
                        mousePos.Y = GO_GraphNode._RADIUS_;
                    }
                    else if (mousePos.Y > GetRect().Size.Y - GO_GraphNode._RADIUS_)
                    {
                        mousePos.Y = GetRect().Size.Y - GO_GraphNode._RADIUS_;
                    }

                    manager.graph.nodeList[(graphObjHovered as GO_GraphNode).uniqueInt].updatePosition(mousePos);

                    manager.requestUpdate();
                }				
			}
			else
			{
				if(timeHeld > 10)
				{
                    manager.graph.recalculateLargestRatio(); // recalculate after moving
					
                }
                holding = false;
                timeHeld = 0;
			}

			if (manager.requestsUpdate)
			{
				foreach (Node n in GetChildren())
				{
					n.QueueFree();
				}
				QueueRedraw();
			}
			manager.completeUpdate();
		}

		public void onGraphObjHoveredChanged(Node2D obj) // using == Node2D as null isnt working even though it is :|. also using it in on_gui_input because null doesnt work. want a method round that. (see JIRA 52)
		{
			//GD.Print("open?: " + (contextMenuOpen == null ? "true" : contextMenuOpen.GetType().Name) + ", passed null?: " + (obj == null ? "true" : "false"));
			if ((contextMenuOpen == null || contextMenuOpen.GetType().Name == "Node2D") && holding == false)
			{
				//GD.Print("newobjhovered " + obj.GetType().Name);
				graphObjHovered = obj;
			}
            //GD.Print(graphObjHovered.GetType().Name);
        }

        public override void _Draw()
		{
			foreach (KeyValuePair<int, Graph_Node> uniqueNodePair in manager.graph.nodeList)
			{
				Graph_Node uniqueNode = uniqueNodePair.Value;
				if (manager.graph.adjList[uniqueNodePair.Key] != null)
				{
					foreach (Graph_Edge edge in manager.graph.adjList[uniqueNodePair.Key])
					{
						Graph_Edge rEdge = manager.graph.getReverseEdge(edge);

						if (rEdge != null && edge.source < rEdge.source)
						{
							//GD.Print(edge.source, edge.destination, edge.weight + " // " + rEdge.source, rEdge.destination, rEdge.weight);
							if (edge.weight != rEdge.weight) // reverse edge with different weight
							{
								Vector2[] startPoints = getSplitPoints(uniqueNode.position, manager.graph.nodeList[edge.destination].position);
								Vector2[] endPoints = getSplitPoints(manager.graph.nodeList[edge.destination].position, uniqueNode.position);

								// instantiate the two lines separately - work out different starting/ending points
								GO_GraphEdge edgeScene = (GO_GraphEdge)graphEdgeScene.Instantiate(); // draw edge
								edgeScene.init(startPoints[0], endPoints[0], edge.weight, Graph_Graph.getColourToDraw(edge.primaryColour), edge.secondaryColour != null ? Graph_Graph.getColourToDraw((GraphColour)edge.secondaryColour) : null, getArrowPoints(startPoints[0], endPoints[0]));
								AddChild(edgeScene);

								GO_GraphEdge edgeScene2 = (GO_GraphEdge)graphEdgeScene.Instantiate(); // draw rEdge
								edgeScene2.init(endPoints[1], startPoints[1], rEdge.weight, Graph_Graph.getColourToDraw(rEdge.primaryColour), rEdge.secondaryColour != null ? Graph_Graph.getColourToDraw((GraphColour)rEdge.secondaryColour) : null, getArrowPoints(endPoints[1], startPoints[1]));
								AddChild(edgeScene2);
				
								edge.drawnEdge = edgeScene;
								rEdge.drawnEdge = edgeScene2;
                            }
							else if (edge.weight == rEdge.weight && edge.source < rEdge.source) // reverse edge with same weight
							{
								// one line shows both (no arrow)
								GO_GraphEdge edgeScene = (GO_GraphEdge)graphEdgeScene.Instantiate();
								edgeScene.init(uniqueNode.position, manager.graph.nodeList[edge.destination].position, edge.weight, Graph_Graph.getColourToDraw(edge.primaryColour), edge.secondaryColour != null ? Graph_Graph.getColourToDraw((GraphColour)edge.secondaryColour) : null);
								AddChild(edgeScene);
                            }

						}
						else if (rEdge == null) // no reverse edge, so unidirectional
						{
							// one line with arrow
							GO_GraphEdge edgeScene = (GO_GraphEdge)graphEdgeScene.Instantiate();
							edgeScene.init(uniqueNode.position, manager.graph.nodeList[edge.destination].position, edge.weight, Graph_Graph.getColourToDraw(edge.primaryColour), edge.secondaryColour != null ? Graph_Graph.getColourToDraw((GraphColour)edge.secondaryColour) : null, getArrowPoints(uniqueNode.position, manager.graph.nodeList[edge.destination].position));
							AddChild(edgeScene);
                        }
						else // debug message
						{
							//GD.Print("GO_GraphPanel: edge with reverse but not same or different weight??");
						}
					}
				}
				GO_GraphNode node = (GO_GraphNode)graphNodeScene.Instantiate();
				node.init(uniqueNode.position, uniqueNodePair.Key, Graph_Graph.getColourToDraw(uniqueNode.primaryColour), uniqueNode.secondaryColour != null ? Graph_Graph.getColourToDraw((GraphColour)uniqueNode.secondaryColour) : null);
				AddChild(node);
			}
		}

		private Vector2[] getSplitPoints(Vector2 start, Vector2 end)
		{
			Vector2[] splitPoints = new Vector2[2];
			float _distance = 16;
			float _arrowheadAngle = 90;

			float angle = (float)Math.Atan((start.Y - end.Y) / (start.X - end.X));

			float offset1 = angle + (float)(_arrowheadAngle * Math.PI / 180);
			float offset2 = angle + (float)(-_arrowheadAngle * Math.PI / 180);

			float x1 = (float)(start.X - (_distance * Math.Cos(offset1)));
			float y1 = (float)(start.Y - (_distance * Math.Sin(offset1)));

			float x2 = (float)(start.X - (_distance * Math.Cos(offset2)));
			float y2 = (float)(start.Y - (_distance * Math.Sin(offset2)));

			splitPoints[0] = new Vector2(x1, y1);
			splitPoints[1] = new Vector2(x2, y2);

			return splitPoints;
		}

		private Vector2[] getArrowPoints(Vector2 start, Vector2 end)
		{
			float _arrowheadAngle = 45;
			float _distance = 16;
			Vector2[] arrowPoints = new Vector2[3]; // midpoint, one end, other end

			arrowPoints[0] = new Vector2((start.X + end.X) / 2, (start.Y + end.Y) / 2); // midpoint of StartEnd

			float dx = end.X - start.X;
			float dy = start.Y - end.Y;

			float angle = (float)Math.Atan(dy / dx);
			float arrowRad1 = (float)(_arrowheadAngle * Math.PI / 180);
			float arrowRad2 = (float)(-_arrowheadAngle * Math.PI / 180);

			float offsetAngle1 = angle + arrowRad1;
			float offsetAngle2 = angle + arrowRad2;

			float x1, x2, y1, y2;

			if (end.X > start.X)
			{
				x1 = (float)(arrowPoints[0].X - (_distance * Math.Cos(offsetAngle1)));
				x2 = (float)(arrowPoints[0].X - (_distance * Math.Cos(offsetAngle2)));
				y1 = (float)(arrowPoints[0].Y + (_distance * Math.Sin(offsetAngle1)));
				y2 = (float)(arrowPoints[0].Y + (_distance * Math.Sin(offsetAngle2)));
			}
			else
			{
				x1 = (float)(arrowPoints[0].X + (_distance * Math.Cos(offsetAngle1)));
				x2 = (float)(arrowPoints[0].X + (_distance * Math.Cos(offsetAngle2)));
				y1 = (float)(arrowPoints[0].Y - (_distance * Math.Sin(offsetAngle1)));
				y2 = (float)(arrowPoints[0].Y - (_distance * Math.Sin(offsetAngle2)));
			}

			arrowPoints[1] = new Vector2(x1, y1);
			arrowPoints[2] = new Vector2(x2, y2);

			return arrowPoints;
		}

		public void onContextMenuButtonPressed(string buttonName)
		{
			switch (buttonName)
			{
				case "addNode":
					if(manager.graph.nodeList.Count >= 31) // just to give feedback to user (arbitrary number at the end of the day), doesnt cause exception.
					{
						manager.displayErrorBox("You have reached the maximum number of nodes");
					}
					(manager.graph as Graph_UserGraph).addNode(contextMenuOpen.Position);
					break;
				case "deleteNode":
					(manager.graph as Graph_UserGraph).removeNode((graphObjHovered as GO_GraphNode).uniqueInt);
					break;
				case "addEdge":
					if (addingEdge == null)
					{
						addingEdge = (GO_GraphNode)graphObjHovered;
					}
					break;
				case "deleteEdge":
					(manager.graph as Graph_UserGraph).removeEdge(findEdge((graphObjHovered as GO_GraphEdge)));
					break;
				case "startButton":
					(manager.graph as Graph_UserGraph).setStartNode((graphObjHovered as GO_GraphNode).uniqueInt);
					break;
				case "endButton":
					(manager.graph as Graph_UserGraph).setEndNode((graphObjHovered as GO_GraphNode).uniqueInt);
					break;
				case "start2Button":
					(manager.graph as Graph_UserGraph).setStart2Node((graphObjHovered as GO_GraphNode).uniqueInt);
					break;
				case "end2Button":
					(manager.graph as Graph_UserGraph).setEnd2Node((graphObjHovered as GO_GraphNode).uniqueInt);
					break;
			}
			closeContextMenu();
            graphObjHovered = null;
            manager.requestUpdate();
		}

		public int[] findEdge(GO_GraphEdge displayedEdge)
		{
			if (displayedEdge == null)
			{
				//GD.Print("pain");
			}
			int[] startAndEnd = new int[2]; // hopefully it never returns this...
			foreach (KeyValuePair<int, Graph_Node> kvp in manager.graph.nodeList)
			{
				//GD.Print(kvp.Value.position.ToString() + " " + displayedEdge.firstPoint.ToString() + " " + displayedEdge.secondPoint.ToString());
				if (kvp.Value.position.DistanceTo(displayedEdge.firstPoint) < GO_GraphNode._RADIUS_)
				{
					startAndEnd[0] = kvp.Key;
					//GD.Print("item 1 = " + kvp.Key);
				}
				if (kvp.Value.position.DistanceTo(displayedEdge.secondPoint) < GO_GraphNode._RADIUS_)
				{
					startAndEnd[1] = kvp.Key;
					//GD.Print("item 2 = " + kvp.Key);
				}
			}
			return startAndEnd;
		}

		public void closeContextMenu()
		{
			//GD.Print("context menu closed");
			try 
			{
                contextMenuOpen.QueueFree();
            }
			catch (Exception)
			{
                contextMenuOpen = null;
            }
			
			contextMenuOpen = null;
		}

		public void openContextMenu(Vector2 position, bool node = false, bool edge = false)
		{
			GO_GraphContextMenu gcm = (GO_GraphContextMenu)graphContextMenuScene.Instantiate();
			GetParent().AddChild(gcm); // instantiated with parent to allow buttons on menu to be clicked when overlapping lower panel (input order things)
			gcm.init(position, node, edge);
			contextMenuOpen = gcm;
			//GD.Print("context menu opened");
		}

		public void visualiseToken(Godot.Collections.Array<int> path, GraphColour colour, bool second = false)
		{
			GO_TokenVisualiser token = (GO_TokenVisualiser)tokenVisualisationScene.Instantiate();
			AddChild(token);
			token.init(new List<int>(path), colour, second);
		}

		public async void _on_gui_input(InputEvent @event, GO_GraphNode node = null, GO_GraphEdge edge = null) // node true when clicking on node, edge true when clicking on edge
		{

			// ---------------- gotten rid of left click shortcuts for now and putting all functionality on context menu buttons ----------------------
			if (@event is InputEventMouseButton mb && mb.Pressed && manager.playing == manager.paused) // if mouse event
			{
				//GD.Print(GetLocalMousePosition().ToString() + " node: " + (node != null ? true : false) + " edge: " + (edge != null ? true : false));

				if (contextMenuOpen != null)
				{
					closeContextMenu();
				}

				if (inputPopupOpen != null)
				{
					inputPopupOpen.close();
				}

				if (manager.fixedSwitch)
				{
					// allow changing start/end node maybe?
				}
				else // user graph
				{
					if (graphObjHovered != null && graphObjHovered.GetType().Name != "Node2D") // if clicking on a node or edge
					{
						if (node != null) // if clicking node
						{
							if (mb.ButtonIndex == MouseButton.Left)
							{
								//GD.Print("node left clicked");
								if (addingEdge != null && (addingEdge.uniqueInt != node.uniqueInt))
								{
									GO_InputPopup popup = (GO_InputPopup)inputPopupScene.Instantiate();
									AddChild(popup);
									popup.init(new Vector2(node.drawPosition.X + 35, node.drawPosition.Y - 15));
									inputPopupOpen = popup;
									Godot.Variant[] signalReturns = await ToSignal(manager, "lineEditSubmitted"); // signal apparently originates at manager, where its declared, instead of in popuppanel where it actually emits.
									//GD.Print("returns " + signalReturns[0] + " " + signalReturns[1]);
									if ((bool)signalReturns[0] && addingEdge != null) // first value returned by lineEditSubmitted is bool for whether the value is a valid int or not
									{
										(manager.graph as Graph_UserGraph).addEdge(addingEdge.uniqueInt, node.uniqueInt, (int)signalReturns[1]); // second value is the valid int that was entered
										manager.requestUpdate();
									}
									popup.QueueFree();
									addingEdge = null;
								}
							}
							else if (mb.ButtonIndex == MouseButton.Right)
							{
								//GD.Print("node right clicked");
								addingEdge = null;
								openContextMenu(GetLocalMousePosition(), node: true);
							}
						}
						else if (edge != null) // if clicking edge
						{
							addingEdge = null;

							if (mb.ButtonIndex == MouseButton.Right)
							{
								//GD.Print("edge right clicked");
								openContextMenu(GetLocalMousePosition(), edge: true);
							}
						}
					}
					else // clicking on panel (no hovering over node or edge)
					{
						addingEdge = null;
                        if (mb.ButtonIndex == MouseButton.Right)
						{
							//GD.Print("panel right clicked");
							openContextMenu(GetLocalMousePosition());
						}
					}
				}
			}
		}
	}
}