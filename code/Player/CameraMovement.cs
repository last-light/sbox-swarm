using Sandbox;
using Sandbox.ModelEditor.Nodes;

public sealed class CameraMovement : Component
{
	// Properties for CameraMovement
	// Once created, can be modified from Sandbox inspector window
	[Property] public PlayerMovement Player {get; set;}
	[Property] public GameObject Body {get; set;}
	[Property] public GameObject Head {get; set;}
	[Property] public float Distance {get; set;} = 0f;
	[Property] public float Sensitivity {get; set;} = 0.1f;

	// Variables for first person
	public bool IsFirstPerson => Distance <= 0f;
	
	// Components
	private CameraComponent Camera;
	// Getting body component to call
	private ModelRenderer BodyRenderer;

	protected override void OnAwake()
	{
		Camera = Components.Get<CameraComponent>();
		BodyRenderer = Body.Components.Get<ModelRenderer>();
	}

	protected override void OnUpdate()
	{
		// Rotate head based on mouse movement
		var rotateAngle = Head.Transform.Rotation.Angles();
		// Value is placeholder (hardcoded sensitivity)
		rotateAngle.pitch += Input.MouseDelta.y * Sensitivity; 
		rotateAngle.yaw -= Input.MouseDelta.x * Sensitivity;
		// Zero because we are not rolling the camera
		rotateAngle.roll = 0f; 
		// Limit rotation to 90 degrees (so we cannot do 360)
		rotateAngle.pitch = rotateAngle.pitch.Clamp(-89.9f, 89.9f);
		Head.Transform.Rotation = rotateAngle.ToRotation();


		// Rotate Camera's position
		if(Camera is not null)
		{
			var camPos = Head.Transform.Position;
			if(!IsFirstPerson)
			{
				// Perform a trace backwards to see where we can place camera (to prevent clipping)
				var camForward = rotateAngle.ToRotation().Forward;
				var camTrace = Scene.Trace.Ray(camPos, camPos- (camForward* Distance))
				.WithoutTags("player", "trigger")
				.Run();
			if(camTrace.Hit)
			{
				camPos = camTrace.HitPosition + camTrace.Normal;
			}
			else
			{
				camPos = camTrace.EndPosition;
			}
			// Hide the body in first person, needs proper re-implementation
			BodyRenderer.Enabled = true;
			}
			else
			{
				BodyRenderer.Enabled = false;
			}
			// If crouching, half the height of the camera, smooth movement
			if(Player.IsCrouching)
			{
				camPos -= Vector3.Up * 25f;
			}

			// Set the position of the camera to our calculated position over a period of time
			Camera.Transform.Position=camPos;
			Camera.Transform.Rotation=rotateAngle.ToRotation();
		}
	}
}
