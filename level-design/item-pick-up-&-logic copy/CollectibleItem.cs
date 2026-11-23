using Godot;

public partial class CollectibleItem : Area3D
{
	private bool _playerIsClose = false;
	private Label _pickupLabel;
	private Tween _currentTween;
	private Player _player;
	private Label3D _interactionPrompt;
	private Tween _promptTween;
	
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
		
		_interactionPrompt = GetNode<Label3D>("InteractionPrompt");

		_interactionPrompt.Visible = false;

		Color promptColor = _interactionPrompt.Modulate;
		promptColor.A = 0.0f;
		_interactionPrompt.Modulate = promptColor;

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
		if (body is Player player)
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
	
	private void ShowPrompt()
	{
		if (_interactionPrompt == null)
			return;

		if (_promptTween != null && _promptTween.IsValid())
			_promptTween.Kill();

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

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_playerIsClose && @event.IsActionPressed("interact"))
		{
			PickUp();
		}
	}

	private async void PickUp()
{
	if (!_playerIsClose || _player == null)
		return;

	_player.CollectItem();

	_playerIsClose = false;

	HidePrompt();

	if (_pickupLabel != null)
	{
		_pickupLabel.Text = "[Item] has been picked up";
		FadeText(1.0f, 0.2f);
	}

	GetParent<Node3D>().Hide();

	GetNode<CollisionShape3D>("CollisionShape3D")
		.SetDeferred("disabled", true);

	GetParent()
		.GetNodeOrNull<CollisionShape3D>("CollisionShape3D")
		?.SetDeferred("disabled", true);

	await ToSignal(
		GetTree().CreateTimer(1.5f),
        "timeout"
	);

	if (_pickupLabel != null)
	{
		float fadeDuration = 0.5f;

		FadeText(0.0f, fadeDuration, () =>
		{
			_pickupLabel.Text = "";
		});

		await ToSignal(
			GetTree().CreateTimer(fadeDuration),
            "timeout"
		);
	}

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
