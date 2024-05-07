using Godot;
using System;

namespace CE301
{
	public partial class GO_ErrorWindow : AcceptDialog
	{
		private Manager manager;
		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			manager = GetNode<Manager>("/root/Manager");
			manager.errorMessage += displayErrorMessage;
		}

		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
		}

		public void displayErrorMessage(string msg)
		{
			DialogText = msg;
			PopupCentered();
		}
	}
}