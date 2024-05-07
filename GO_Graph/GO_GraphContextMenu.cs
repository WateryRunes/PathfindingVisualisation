using CE301.ContextMenu;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CE301.GO_Graph
{
    public partial class GO_GraphContextMenu : Node2D
    {
        private Manager manager;

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            manager = GetNode<Manager>("/root/Manager");
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(double delta)
        {
        }

        public void init(Vector2 drawPosition, bool node = false, bool edge = false)
        {
            this.Position = drawPosition;

            if (node)
            {
                foreach (Node cmb in GetTree().GetNodesInGroup("NodeMenu")) { (cmb as ContextMenuButton).showButtonInMenu(); }

                if (manager.currentCategory == Category.CAStar || manager.currentCategory == Category.WHCAStar) // add set start/end 2
                {
                    foreach (Node cmb in GetTree().GetNodesInGroup("2NodeMenu")) { (cmb as ContextMenuButton).showButtonInMenu(); }
                }
            }
            else if (edge)
            {
                foreach (Node cmb in GetTree().GetNodesInGroup("EdgeMenu")) { (cmb as ContextMenuButton).showButtonInMenu(); }
            }
            else
            {
                foreach (Node cmb in GetTree().GetNodesInGroup("PanelMenu")) { (cmb as ContextMenuButton).showButtonInMenu(); }
            }
        }

        public void _on_add_node_button_pressed()
        {
            manager.EmitSignal(nameof(Manager.contextMenuButtonPressed), "addNode");
        }

        public void _on_delete_node_button_pressed()
        {
            manager.EmitSignal(nameof(Manager.contextMenuButtonPressed), "deleteNode");
        }

        public void _on_add_edge_button_pressed()
        {
            manager.EmitSignal(nameof(Manager.contextMenuButtonPressed), "addEdge");
        }

        public void _on_delete_edge_button_pressed()
        {
            manager.EmitSignal(nameof(Manager.contextMenuButtonPressed), "deleteEdge");
        }

        public void _on_start_button_pressed()
        {
            manager.EmitSignal(nameof(Manager.contextMenuButtonPressed), "startButton");
        }

        public void _on_end_button_pressed()
        {
            manager.EmitSignal(nameof(Manager.contextMenuButtonPressed), "endButton");
        }

        public void _on_start_2_button_pressed()
        {
            manager.EmitSignal(nameof(Manager.contextMenuButtonPressed), "start2Button");
        }

        public void _on_end_2_button_pressed()
        {
            manager.EmitSignal(nameof(Manager.contextMenuButtonPressed), "end2Button");
        }
    }
}
