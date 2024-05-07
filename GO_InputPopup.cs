using Godot;
using System;

namespace CE301
{
    public partial class GO_InputPopup : Node2D
    {
        private Manager manager;
        private DateTime timeOfCreation;

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            manager = GetNode<Manager>("/root/Manager");
            timeOfCreation = DateTime.Now;
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(double delta)
        {
        }

        public void init(Vector2 pos)
        {
            GD.Print("created");
            Position = pos;
            (GetNode("PopupPanel/InputLineEdit") as LineEdit).GrabFocus();
        }

        public void close() // allows for closing when clicking off popup. needs time check as every click is seen by panel + whatever is on top (tries to close after opening)
        {
            GD.Print((DateTime.Now - timeOfCreation).Milliseconds);
            if ((DateTime.Now - timeOfCreation).Milliseconds > 10)
            {
                GD.Print("closed");
                _on_line_edit_text_submitted("STOP");
            }
        }

        public void _on_line_edit_text_submitted(string new_text)
        {
            bool num = true;
            if (new_text.Length < 4)
            {
                foreach (char c in new_text)
                {
                    if (c < '0' || c > '9')
                    {
                        num = false;
                        break;
                    }
                }
            }
            else
            {
                num = false;
            }
            if(num == false && new_text != "STOP")
            {
                manager.displayErrorBox("Inputted weight is not valid. Must be an integer between 0 - 999");
            }
            manager.EmitSignal(nameof(manager.lineEditSubmitted), num, num ? int.Parse(new_text) : -1); // returns -1 if the value entered was not valid (this value is never checked if !num)
        }
    }
}