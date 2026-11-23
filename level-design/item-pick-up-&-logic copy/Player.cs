using Godot;

public partial class Player : CharacterBody3D
{
	// Movement settings
	[Export] public float MoveSpeed = 5.0f;
	[Export] public float Acceleration = 20.0f;
	[Export] public float JumpVelocity = 4.5f;

	// Camera settings
	[Export] public float MouseSensitivity = 0.002f;

	// Camera reference
	private Node3D _cameraPivot;

	// Gravity
	private float _gravity = 9.8f;


	public override void _Ready()
	{
		_cameraPivot = GetNode<Node3D>("CameraPivot");

		_gravity = (float)ProjectSettings.GetSetting(
            "physics/3d/default_gravity"
		);

		Input.MouseMode = Input.MouseModeEnum.Captured;
	}


	public override void _UnhandledInput(InputEvent @event)
	{
		// Mouse camera movement
		if (@event is InputEventMouseMotion mouseMotion)
		{
			if (Input.MouseMode == Input.MouseModeEnum.Captured)
			{
				// Rotate player horizontally
				RotateY(-mouseMotion.Relative.X * MouseSensitivity);

				// Rotate camera vertically
				_cameraPivot.RotateX(
					-mouseMotion.Relative.Y * MouseSensitivity
				);

				// Prevent camera from flipping
				Vector3 cameraRotation = _cameraPivot.Rotation;

				cameraRotation.X = Mathf.Clamp(
					cameraRotation.X,
					Mathf.DegToRad(-60),
					Mathf.DegToRad(60)
				);

				_cameraPivot.Rotation = cameraRotation;
			}
		}

		// Escape releases mouse
		if (@event.IsActionPressed("ui_cancel"))
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}

		// Click captures mouse again
		if (@event is InputEventMouseButton mouseButton)
		{
			if (mouseButton.Pressed)
			{
				Input.MouseMode = Input.MouseModeEnum.Captured;
			}
		}
	}


	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		// Apply gravity
		if (!IsOnFloor())
		{
			Velocity = new Vector3(
				Velocity.X,
				Velocity.Y - _gravity * dt,
				Velocity.Z
			);
		}

		// Jump
		if (Input.IsActionJustPressed("jump") && IsOnFloor())
		{
			Velocity = new Vector3(
				Velocity.X,
				JumpVelocity,
				Velocity.Z
			);
		}

		// Get WASD input
		Vector2 inputDir = Input.GetVector(
			"move_left",
			"move_right",
			"move_forward",
            "move_back"
		);

		// Convert input to world direction
		Vector3 direction = Transform.Basis * new Vector3(
			inputDir.X,
			0,
			inputDir.Y
		);

		direction = direction.Normalized();

		// Calculate target velocity
		Vector3 targetVelocity = direction * MoveSpeed;

		// Smooth horizontal movement
		float newVelocityX = Mathf.MoveToward(
			Velocity.X,
			targetVelocity.X,
			Acceleration * dt
		);

		float newVelocityZ = Mathf.MoveToward(
			Velocity.Z,
			targetVelocity.Z,
			Acceleration * dt
		);

		Velocity = new Vector3(
			newVelocityX,
			Velocity.Y,
			newVelocityZ
		);

		// Apply movement and collision
		MoveAndSlide();
	}
}
