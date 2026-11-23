using Godot;

public partial class CollectibleItem : Area3D
{
	private bool _playerIsClose = false;
	private Label _pickupLabel;
	private Tween _currentTween;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;

		// Find the UI label in the scene tree
		_pickupLabel = GetTree().CurrentScene.FindChild("pickupLabel", true, false) as Label;

		if (_pickupLabel != null)
		{
			// Make sure it starts completely transparent
			Color transparentColor = _pickupLabel.Modulate;
			transparentColor.A = 0.0f;
			_pickupLabel.Modulate = transparentColor;
		}
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is Player)
		{
			_playerIsClose = true;

			if (_pickupLabel != null)
			{
				_pickupLabel.Text = "E";
				FadeText(1.0f, 0.4f); // Fade in to full opacity over 0.4 seconds
			}
		}
	}

	private void OnBodyExited(Node3D body)
	{
		if (body is Player)
		{
			_playerIsClose = false;

			if (_pickupLabel != null && _pickupLabel.Text == "E")
			{
				// Fade out to transparent, then clear text when finished
				FadeText(0.0f, 0.4f, () => {
					if (_pickupLabel.Text == "E")
					{
						_pickupLabel.Text = "";
					}
				});
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
	_playerIsClose = false;

	if (_pickupLabel != null)
	{
		_pickupLabel.Text = "[Item] has been picked up";
		// Fade in AND slide up for the pickup confirmation message
		FadeText(1.0f, 0.2f);
	}

	// Hide the item mesh and disable physical collision instantly
	Visible = false;
	GetNode<CollisionShape3D>("CollisionShape3D")?.SetDeferred("disabled", true);

	// Wait 1.5 seconds so the player can read the message
	await ToSignal(GetTree().CreateTimer(1.5f), "timeout");

	// Fade out normally without moving when clearing up
	if (_pickupLabel != null)
	{
		float fadeDuration = 0.5f;
		FadeText(0.0f, fadeDuration, () => {
			_pickupLabel.Text = "";
		});

		// CRITICAL: Wait for the fade-out animation to finish BEFORE deleting the item!
		await ToSignal(GetTree().CreateTimer(fadeDuration), "timeout");
	}

	// Finally, delete the item node from the world
	GetParent().QueueFree();
}

	// Helper method to handle smooth Tweens on the Label's Modulate alpha
	private void FadeText(float targetAlpha, float duration, System.Action onFinished = null)
	{
		if (_pickupLabel == null) return;

		// Kill any active tween so they don't fight each other
		if (_currentTween != null && _currentTween.IsValid())
		{
			_currentTween.Kill();
		}

		_currentTween = CreateTween();
		_currentTween.TweenProperty(_pickupLabel, "modulate:a", targetAlpha, duration);

		if (onFinished != null)
		{
			_currentTween.TweenCallback(Callable.From(onFinished));
		}
	}
}
