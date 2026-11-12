using Godot;

public partial class CollectibleItem : Area3D
{
	private bool _playerIsClose = false;
	private Label _pickupLabel;

	public override void _Ready()
{
	BodyEntered += OnBodyEntered;
	BodyExited += OnBodyExited;

	_pickupLabel = GetTree().CurrentScene.FindChild("pickupLabel", true, false) as Label;
}

	private void OnBodyEntered(Node3D body)
	{
		if (body is Player)
		{
			_playerIsClose = true;

			// Show prompt on screen
			if (_pickupLabel != null)
			{
				_pickupLabel.Text = "Press E to pick up item";
			}
		}
	}

	private void OnBodyExited(Node3D body)
	{
		if (body is Player)
		{
			_playerIsClose = false;

			// Clear prompt when leaving the area
			if (_pickupLabel != null && _pickupLabel.Text == "Press E to pick up item")
			{
				_pickupLabel.Text = "";
			}
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_playerIsClose && @event.IsActionPressed("interact"))
		{
			PickUp();
		}
	}

	private async void PickUp()
	{
		// Disable further interactions immediately so it can't be triggered twice
		_playerIsClose = false;

		if (_pickupLabel != null)
		{
			// Display the pick up message on screen
			_pickupLabel.Text = "[Item] has been picked up";
		}

		// Hide the visual mesh and disable collisions so the item disappears instantly
		Visible = false;
		GetNode<CollisionShape3D>("CollisionShape3D")?.SetDeferred("disabled", true);

		// Wait for 1.5 seconds so the player can actually read the message on screen before clearing it
		await ToSignal(GetTree().CreateTimer(1.5f), "timeout");

		// Clear the screen message
		if (_pickupLabel != null)
		{
			_pickupLabel.Text = "";
		}

		// Finally, safely delete the item node from the world
		GetParent().QueueFree();
	}
}
