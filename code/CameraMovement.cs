using Sandbox;

public sealed class CameraMovement : Component
{
	// Properties, mostly object references
	[Property] public PlayerMovement Player { get; set; }
	[Property] public GameObject Head { get; set; }
	[Property] public GameObject Body { get; set; }
	[Property] public float Distance { get; set; } = 0f;
	[Property] public float Sensitivity { get; set; } = 0.1f;

	// Variables
	public bool IsFirstPerson => Distance == 0f;
	private CameraComponent Camera;
	private ModelRenderer BodyRenderer;

	protected override void OnAwake()
	{
		Camera = Components.Get<CameraComponent>();
		BodyRenderer = Body.Components.Get<ModelRenderer>();
	}
	// public bool IsThirdPerson => Distance > 0f;
	protected override void OnUpdate()
	{
		// Rotate the head based on mouse movement
		var eyeAngles = Head.Transform.Rotation.Angles(); // Pitch, raw, yaw
		eyeAngles.pitch += Input.MouseDelta.y * Sensitivity;
		eyeAngles.yaw -= Input.MouseDelta.x * Sensitivity;
		eyeAngles.roll = 0f;
		eyeAngles.pitch = eyeAngles.pitch.Clamp(-89.9f, 89.9f);
		Head.Transform.Rotation = eyeAngles.ToRotation();

		// Set the position of the camera
		if(Camera is not null)
		{
			var camPos = Head.Transform.Position;
			if(!IsFirstPerson)
			{
				// Perform a trace backwards to see where we can safely place the camera
				var camForward = eyeAngles.ToRotation().Forward;
				var camTrace = Scene.Trace.Ray(camPos, camPos - (camForward * Distance))
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
				// Enable BodyRenderer
				BodyRenderer.Enabled = true;
			}
			else {
				BodyRenderer.Enabled = false;
			}

			// Set the position of the camera to our calculated position
			Camera.Transform.Position = camPos;
			Camera.Transform.Rotation = eyeAngles.ToRotation();
		}
	}
}
