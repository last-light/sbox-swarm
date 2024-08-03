using System.Numerics;
using Sandbox;
using Sandbox.Citizen;

public sealed class PlayerMovement : Component
{
	// Movement Properties
	// Once created, can be modified from the s&box inspector window
	[Property] public float GroundControl { get; set; } = 4.0f;
	[Property] public float AirControl { get; set; } = 0.1f;
	[Property] public float MaxForce { get; set; } = 500f;
	[Property] public float Speed { get; set; } = 160f;
	[Property] public float RunSpeed { get; set; } = 290f;
	[Property] public float CrouchSpeed { get; set; } = 90f;
	[Property] public float JumpForce { get; set; } = 400f;

	// Object References
	// TODO: Author suggests there is a better way to access these objects than using the public keyword
	[Property] public GameObject Head { get; set; }
	[Property] public GameObject Body { get; set; }

	// Member Variables
	public Vector3 WishVelocity = Vector3.Zero;
	public bool IsCrouching = false;
	public bool IsSprinting = false;
	private CharacterController characterController;
	private CitizenAnimationHelper citizenAnimationHelper;

	protected override void OnAwake()
	{
		characterController = Components.Get<CharacterController>();
		citizenAnimationHelper = Components.Get<CitizenAnimationHelper>();
	}

	protected override void OnUpdate()
	{
		UpdateCrouch();
		IsSprinting = Input.Down("Run");
		if (Input.Pressed("Jump")) Jump();
		UpdateAnimations();
	}

	// Call on every physics update instead of scene update
	protected override void OnFixedUpdate()
	{
		BuildWishVelocity();
		Move();
		RotateBody();
	}

	void BuildWishVelocity()
	{
		WishVelocity = 0;

		Rotation PlayerRotation = Head.Transform.Rotation;

		// Define the dictionary mapping input directions to velocity vectors
		// Order of items defined based on the likelihood of the input being pressed
		// Source: https://www.reddit.com/r/dataisbeautiful/comments/6mqohs/i_logged_which_keys_i_used_most_often_while_pc/
		// W: 30.69%, D: 16.17%, A: 13.68%, S: 7.60%
		// This leads to less checks and iterations being performed
		Dictionary<string, Vector3> directionMap = new Dictionary<string, Vector3>
		{
			{ "Forward", PlayerRotation.Forward },
			{ "Right", PlayerRotation.Right },
			{ "Left", PlayerRotation.Left },
			{ "Backward", PlayerRotation.Backward }
		};

		// Check if any of the input directions are pressed and update WishVelocity
		foreach (string direction in directionMap.Keys)
		{
			if (Input.Down(direction))
			{
				WishVelocity += directionMap[direction];
			}
		}

		// Keep the player grounded
		// TODO: Need to verify its functionality
		WishVelocity = WishVelocity.WithZ(0);
		if (!WishVelocity.IsNearZeroLength) WishVelocity = WishVelocity.Normal;

		// Crouching takes priority, so we are performing check on it first
		if (IsCrouching) WishVelocity *= CrouchSpeed;
		else if (IsSprinting) WishVelocity *= RunSpeed;
		else WishVelocity *= Speed;
	}

	void Move()
	{
		// Get gravity from our scene
		Vector3 gravity = Scene.PhysicsWorld.Gravity;

		if(characterController.IsOnGround)
		{
			// Apply Friction/Acceleration
			characterController.Velocity = characterController.Velocity.WithZ(0);
			characterController.Accelerate(WishVelocity);
			characterController.ApplyFriction(GroundControl);
		}
		else
		{
			// Apply air control / gravity
			// We add a 0.5 constant be cause we perform the other 0.5 at the end of the movement for accuracy
			characterController.Velocity += gravity * Time.Delta * 0.5f;
			characterController.Accelerate(WishVelocity.ClampLength(MaxForce));
			characterController.ApplyFriction(AirControl);
		}

		// Move the character controller
		characterController.Move();

		// Apply the second half of gravity after movement
		if (!characterController.IsOnGround)
		{
			characterController.Velocity += gravity * Time.Delta * 0.5f;
		}
		else
		{
			characterController.Velocity = characterController.Velocity.WithZ(0);
		}
	}

	void Jump()
	{
		if(!characterController.IsOnGround) return;

		characterController.Punch(Vector3.Up * JumpForce);
		citizenAnimationHelper?.TriggerJump();
	}

	void UpdateCrouch()
    {
        if(characterController is null) return;

        if(Input.Pressed("Duck") && !IsCrouching)
        {
            IsCrouching = true;
            characterController.Height /= 2f; // Reduce the height of our character controller
        }

        if(Input.Released("Duck") && IsCrouching)
        {
            IsCrouching = false;
            characterController.Height *= 2f; // Return the height of our character controller to normal
        }
    }

	void RotateBody()
	{
		if(Body is null) return;

		var targetAngle = new Angles(0, Head.Transform.Rotation.Yaw(), 0).ToRotation();
		float rotateDifference = Body.Transform.Rotation.Distance(targetAngle);

		if (rotateDifference > 50f || characterController.Velocity.Length > 10f)
		{
			Body.Transform.Rotation = Rotation.Lerp(Body.Transform.Rotation, targetAngle, Time.Delta * 7f);
		}
	}

	void UpdateAnimations()
	{
		if(citizenAnimationHelper is null) return;

		citizenAnimationHelper.WithWishVelocity(WishVelocity);
		citizenAnimationHelper.WithVelocity(characterController.Velocity);
		citizenAnimationHelper.AimAngle = Head.Transform.Rotation;
		citizenAnimationHelper.IsGrounded = characterController.IsOnGround;
		citizenAnimationHelper.WithLook(Head.Transform.Rotation.Forward, 1f, 0.75f, 0.5f);
		citizenAnimationHelper.MoveStyle = CitizenAnimationHelper.MoveStyles.Run;
		citizenAnimationHelper.DuckLevel = IsCrouching ? 1f : 0f;
	}
}
