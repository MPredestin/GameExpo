
using Godot;

public partial class Blockage : Area3D
{
	private bool _playerIsClose = false;
	private bool _isBroken = false;

	private Player _player;
	private Label _pickupLabel;
	private Label3D _interactionPrompt;

	private Tween _currentTween;
	private Tween _promptTween;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;

		// Find the 3D interaction prompt
		_interactionPrompt =
			GetNode<Label3D>("InteractionPrompt");

		_interactionPrompt.Visible = false;

		Color promptColor = _interactionPrompt.Modulate;
		promptColor.A = 0.0f;
		_interactionPrompt.Modulate = promptColor;

		// Find the existing UI message label
		_pickupLabel = GetTree().CurrentScene
			.FindChild("pickupLabel", true, false) as Label;
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is Player player && !_isBroken)
		{
			_player = player;
			_playerIsClose = true;

			ShowPrompt();
		}
	}

	private void OnBodyExited(Node3D body)
	{
		if (body is Player)
		{
			_playerIsClose = false;
			_player = null;

			HidePrompt();
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_playerIsClose &&
			!_isBroken &&
			@event.IsActionPressed("interact"))
		{
			Interact();
		}
	}

	private void Interact()
	{
		if (!_playerIsClose || _player == null || _isBroken)
			return;

		if (_player.HasItem)
		{
			BreakBlockage();
		}
		else
		{
			ShowMessage("Weird... I can't pass through...");
		}
	}

	private void ShowPrompt()
	{
		if (_interactionPrompt == null)
			return;

		if (_promptTween != null && _promptTween.IsValid())
			_promptTween.Kill();

		// Change the prompt depending on the inventory
		if (_player != null && _player.HasItem)
		{
			_interactionPrompt.Text = "e break";
		}
		else
		{
			_interactionPrompt.Text = "e";
		}

		_interactionPrompt.Visible = true;

		_promptTween = CreateTween();

		_promptTween.TweenProperty(
			_interactionPrompt,
			"modulate:a",
			1.0f,
			0.3f
		);
	}

	private void HidePrompt()
	{
		if (_interactionPrompt == null)
			return;

		if (_promptTween != null && _promptTween.IsValid())
			_promptTween.Kill();

		_promptTween = CreateTween();

		_promptTween.TweenProperty(
			_interactionPrompt,
			"modulate:a",
			0.0f,
			0.3f
		);

		_promptTween.TweenCallback(
			Callable.From(() =>
			{
				_interactionPrompt.Visible = false;
			})
		);
	}

	private async void BreakBlockage()
	{
		_isBroken = true;
		_playerIsClose = false;

		HidePrompt();

		// Get the entire blockage root
		Node3D blockageRoot = GetParent<Node3D>();

		// Hide the wall and interaction prompt
		blockageRoot.Hide();

		// Disable the wall's physical collision
		blockageRoot
			.GetNodeOrNull<CollisionShape3D>("CollisionShape3D")
			?.SetDeferred("disabled", true);

		// Disable the interaction area's collision
		GetNode<CollisionShape3D>("CollisionShape3D")
			.SetDeferred("disabled", true);

		// Display confirmation and wait for it to finish
		await ShowBreakMessage();

		// Delete the entire blockage
		blockageRoot.QueueFree();
	}

	private void ShowMessage(string message)
	{
		if (_pickupLabel == null)
			return;

		if (_currentTween != null && _currentTween.IsValid())
			_currentTween.Kill();

		_pickupLabel.Text = message;

		_currentTween = CreateTween();

		_currentTween.TweenProperty(
			_pickupLabel, "modulate:a", 1.0f, 0.2f
		);

		_currentTween.TweenInterval(1.5f);

		_currentTween.TweenProperty(
			_pickupLabel, "modulate:a", 0.0f, 0.5f
		);

		_currentTween.TweenCallback(
			Callable.From(() =>
			{
				_pickupLabel.Text = "";
			})
		);
	}

	private async System.Threading.Tasks.Task ShowBreakMessage()
	{
		ShowMessage("The blockage has been destroyed.");

		if (_pickupLabel != null && _currentTween != null)
		{
			await ToSignal(
				_currentTween,
				Tween.SignalName.Finished
			);
		}
	}
}
