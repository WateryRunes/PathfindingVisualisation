using Godot;
using System;

namespace CE301
{
	public partial class GO_FixedGraphToggle : CheckButton
	{
		private Manager manager;
		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			manager = GetNode<Manager>("/root/Manager");
			;
		}

		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
		}

		private void _on_toggled(bool button_pressed)
		{
			manager.EmitSignal(nameof(Manager.changeFixedState), button_pressed);
		}

	}
}


