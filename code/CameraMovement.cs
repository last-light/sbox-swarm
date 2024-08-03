using Sandbox;

public sealed class CameraMovement : Component
{
	// Properties, mostly object references
	[Property] public PlayerMovement Player { get; set; }
	[Property] public GameObject Head { get; set; }
	[Property] public GameObject Body { get; set; }
	[Property] public float Distance { get; set; } = 150f;
	[Property] public float Sensitivity { get; set; } = 0.1f;

	// Variables
	public bool IsFirstPerson => Distance == 0f;
	private CameraComponent Camera;
	private ModelRenderer BodyRenderer;

	protected override void OnAwake()
	{
		Camera = Components.Get<CameraComponent>();
		Camera.FieldOfView = 110f;

		BodyRenderer = Body.Components.Get<ModelRenderer>();
	}

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
				//Enabling render of model if in third person
				BodyRenderer.RenderType = ModelRenderer.ShadowRenderType.On;
			}
			else {
				//Changing to render shadows only in first person
				BodyRenderer.RenderType = ModelRenderer.ShadowRenderType.ShadowsOnly;
			}
			// If crouching, half the height of the camera, smooth movement
			if(Player.IsCrouching)
			{
				camPos -= Vector3.Up * 25f;
			}

			// Set the position of the camera to our calculated position over a period of time
			Camera.Transform.Position = camPos;
			Camera.Transform.Rotation = eyeAngles.ToRotation();
		}
	}
}
