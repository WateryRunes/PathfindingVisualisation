using Godot;
using System;

namespace CE301.NavigationButtons
{
    public partial class GO_PauseButtonPress : Button
    {
        private Manager manager;
        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            manager = GetNode<Manager>("/root/Manager");
            manager.visualisationPaused += toggleDown;
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(double delta)
        {
        }

        private void _on_toggled(bool button_pressed)
        {
            manager.EmitSignal(nameof(Manager.pauseButtonPressed));
            if(Text == "Pause")
            {
                Text = "Unpause";
            }
            else
            {
                Text = "Pause";
            }
        }

        private void toggleDown()
        {
            ButtonPressed = true;
            ToggleMode = true;
            Text = "Unpause";
        }
    }
}

