using CE301;
using Godot;
using System;

public partial class RandomGraphButton : Button
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

	public void _on_pressed()
	{
        manager.EmitSignal(nameof(Manager.generateRandomGraphPressed));
    }
}
