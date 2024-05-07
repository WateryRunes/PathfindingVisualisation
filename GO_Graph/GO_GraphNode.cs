using CE301.Graph;
using Godot;
using System;
using System.Collections.Generic;

namespace CE301.GO_Graph
{
	public partial class GO_GraphNode : Node2D
	{
		private Manager manager;
		public Vector2 drawPosition { get; private set; }
		public int uniqueInt { private set; get; }
		private Color primaryColour;
		private Color? secondaryColour;
		public static int _RADIUS_ = 30;
		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			manager = GetNode<Manager>("/root/Manager");
		}

		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
		}

		public void init(Vector2 drawPos, int uniqueInt, Color primaryColour, Color? secondaryColour)
		{
			drawPosition = drawPos;
			this.uniqueInt = uniqueInt;
			this.primaryColour = primaryColour;
			this.secondaryColour = (secondaryColour != null ? secondaryColour : null);
		}

		public override void _Draw()
		{
			Label uniqueIntLabel = GetNode("UniqueIntLabel") as Label;
			uniqueIntLabel.Text = uniqueInt.ToString();
			uniqueIntLabel.Position = new Vector2(drawPosition.X - 6, drawPosition.Y - 13);
			if (primaryColour == Graph_Graph.getColourToDraw(GraphColour.FrontierEdge))
			{
				uniqueIntLabel.Set("theme_override_colors/font_color", new Color("#000000"));
			}

			CollisionShape2D collisionCircle = GetNode("NodeCollisionArea/NodeCollision") as CollisionShape2D;
			collisionCircle.Position = drawPosition;
			(collisionCircle.Shape as CircleShape2D).Radius = _RADIUS_;			

            if (secondaryColour != null && manager.currentCategory == Category.CAStar) // drawing semicircles if two start/end nodes combine
            {
                // DRAW SEMICIRCLE OF PRIMARY COLOUR
                Vector2[] arcPoints = new Vector2[_RADIUS_ * 2 + 2];

                int index = 0;
                for (int i = (int)(drawPosition.Y - _RADIUS_); i <= drawPosition.Y + _RADIUS_; i++) // top of circle to bottom of circle
                {
                    arcPoints[index] = (new Vector2(drawPosition.X - (float)(Math.Sqrt(Math.Pow(_RADIUS_, 2) - Math.Pow(i - drawPosition.Y, 2))), i));
                    index++;
                }
				arcPoints[0] = new Vector2(drawPosition.X, arcPoints[0].Y); // for some reason on first point (top of circle) X gets calcd as NaN, so auto set to drawPos.X as thats what it should be.
                arcPoints[index] = new Vector2(drawPosition.X, drawPosition.Y + _RADIUS_); // for some reason loop dosent get to bottom of circle, so had to add final point here.

                DrawPolygon(arcPoints, [primaryColour]);

                // DRAW SEMICIRCLE OF SECONDARY COLOUR (comments from first set are repeated)
                arcPoints = new Vector2[_RADIUS_ * 2 + 2]; 

				index = 0;
				for (int i = (int)(drawPosition.Y - _RADIUS_); i <= drawPosition.Y + _RADIUS_; i++) 
				{
					arcPoints[index] = (new Vector2((float)(Math.Sqrt(Math.Pow(_RADIUS_,2) - Math.Pow(i - drawPosition.Y, 2)) + drawPosition.X), i));
                    index++;
				}
                arcPoints[0] = new Vector2(drawPosition.X, arcPoints[0].Y);
                arcPoints[index] = new Vector2(drawPosition.X, drawPosition.Y + _RADIUS_);

                DrawPolygon(arcPoints, [(Color)secondaryColour]);
            }
			else // draw normal filled circle if only one colour
			{
				if(manager.currentCategory != Category.CAStar && (primaryColour == Graph_Graph.getColourToDraw(GraphColour.StartNode2) || primaryColour == Graph_Graph.getColourToDraw(GraphColour.EndNode2)))
				{
					primaryColour = Graph_Graph.getColourToDraw(GraphColour.Default);
				}
                DrawCircle(drawPosition, _RADIUS_, primaryColour);
            }
            DrawArc(drawPosition, _RADIUS_, 0, 360, 360, Color.FromHtml("#FFFFFF")); // draw white outline around circle
        }

		private void _on_node_collision_area_input_event(Node viewport, InputEvent @event, long shape_idx) // when clicked
		{
			(GetParent() as GO_GraphPanel)._on_gui_input(@event, node: this);
		}

		private void _on_node_collision_area_mouse_entered()
		{
			manager.EmitSignal(nameof(Manager.graphObjHoveredChanged), this);
		}

		private void _on_node_collision_area_mouse_exited()
		{
			//GD.Print("exit node");
			manager.EmitSignal(nameof(Manager.graphObjHoveredChanged), new Node2D());
		}
	}
}
