using System;
using Sandbox;

public sealed class MapLoader : Component
{
	[Property] public MapInstance MapInstance {get; set;}
	[Property] public GameObject Player {get; set;}
	protected override void OnAwake()
	{
		base.OnAwake();
	}

	protected override void OnEnabled()
	{
		if (MapInstance is not null)
		{
			MapInstance.OnMapLoaded = MapLoaded;

			if (MapInstance.IsLoaded)
			{
				MapLoaded();
			}

		}
	}

	protected override void OnDisabled()
	{
		if (MapInstance is not null)
		{
			MapInstance.OnMapLoaded -= MapLoaded;

		}
	}

	void MapLoaded()
	{
		var spawnPoints = Scene.Directory.FindByName("info_player_start").ToArray();
		var randomSpawn = Random.Shared.FromArray (spawnPoints);
		if (randomSpawn is not null)
		{
			Player.Transform.Position = randomSpawn.Transform.Position + Vector3.Up * 64;
			Player.Transform.Rotation = randomSpawn.Transform.Rotation;
		}
	}
}
