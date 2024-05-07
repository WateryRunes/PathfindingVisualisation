using CE301.Graph;
using Godot;
using System;

namespace CE301.GO_Graph
{
	public partial class GO_GraphEdge : Node2D
	{
		private Manager manager;
		public Vector2 firstPoint { get; private set; }
		public Vector2 secondPoint { get; private set; }
		private int weight;
		private Vector2[] arrowPoints = null;
		private Color primaryColour;
		private Color? secondaryColour;
		private float dashLineLength = 8;

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			manager = GetNode<Manager>("/root/Manager");
		}

		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
		}

		public void init(Vector2 first, Vector2 second, int weight, Color colour, Color? secondary) // reverse edge with same weight
		{
			firstPoint = first;
			secondPoint = second;
			this.weight = weight;
			this.primaryColour = colour;
			this.secondaryColour = secondary;
			Label weightLabel = GetNode("WeightLabel") as Label;
			weightLabel.Text = weight.ToString();
			weightLabel.Position = new Vector2((firstPoint.X + secondPoint.X) / 2, (firstPoint.Y + secondPoint.Y) / 2);
		}

		public void init(Vector2 first, Vector2 second, int weight, Color colour, Color? secondary, Vector2[] arrowPoints) // reverse edge with different weight
		{
			firstPoint = first;
			secondPoint = second;
			this.weight = weight;
			this.primaryColour = colour;
            this.secondaryColour = secondary;
            this.arrowPoints = new Vector2[arrowPoints.Length];
			Array.Copy(arrowPoints, this.arrowPoints, arrowPoints.Length);
		}

		public override void _Draw()
		{
			Label weightLabel = GetNode("WeightLabel") as Label;
			weightLabel.Text = weight.ToString();
			weightLabel.Position = new Vector2((firstPoint.X + secondPoint.X) / 2, (firstPoint.Y + secondPoint.Y) / 2);

			CollisionShape2D collisionRectangle = GetNode("EdgeCollisionArea/EdgeCollision") as CollisionShape2D;
			collisionRectangle.Position = (firstPoint + secondPoint) / 2;
			collisionRectangle.Rotation = firstPoint.DirectionTo(secondPoint).Angle();
			collisionRectangle.Shape = new RectangleShape2D();
			(collisionRectangle.Shape as RectangleShape2D).Size = new Vector2(firstPoint.DistanceTo(secondPoint) - 70, 12);

            DrawLine(firstPoint, secondPoint, primaryColour); // draw edge

			if (secondaryColour != null) // if need a dashed line too
			{
                GD.Print("FIRST: " + firstPoint + ". SECOND: " + secondPoint);
                int dashCount = (int)(firstPoint.DistanceTo(secondPoint) / (dashLineLength + (dashLineLength / 2)));
				Vector2 normalisedLength = new Vector2(firstPoint.X - secondPoint.X, firstPoint.Y - secondPoint.Y).Normalized();
				string debug = "";
				string temp = "";
				for (int i = 0; i < dashCount * 1.5; i+=2)
				{
					if(i == 0)
					{
						debug += "dashed at: " + (firstPoint + normalisedLength * i * dashLineLength) + ", ";
                    }
					temp = firstPoint + normalisedLength * (i + 1) * dashLineLength + "\n";

					DrawLine((secondPoint + (normalisedLength * i * dashLineLength)), secondPoint + (normalisedLength * (i+1) * dashLineLength), (Color)secondaryColour);
				}
				debug += temp;
				GD.Print(debug);
			}

			if (arrowPoints != null) // draw arrow on edge
			{
				DrawLine(arrowPoints[0], arrowPoints[1], primaryColour);
				DrawLine(arrowPoints[0], arrowPoints[2], primaryColour);
			}
        }

        private void _on_edge_collision_area_input_event(Node viewport, InputEvent @event, long shape_idx) // when clicked
		{
			(GetParent() as GO_GraphPanel)._on_gui_input(@event, edge: this);
		}

		private void _on_edge_collision_area_mouse_entered()
		{
			manager.EmitSignal(nameof(Manager.graphObjHoveredChanged), this);
		}

		private void _on_edge_collision_area_mouse_exited()
		{
			//GD.Print("exit edge");
			manager.EmitSignal(nameof(Manager.graphObjHoveredChanged), new Node2D());
		}
	}
}