using ParrelSync;
using Unity.Services.Lobbies.Models;

public static class LobbyHelpers
{
	public static Player GetPlayer()
	{
		string displayName = ClonesManager.IsClone() ? ClonesManager.GetArgument() : "Original";
		var playerProfile = new PlayerProfile(displayName);

		return new Player(id: Authentication.LocalPlayerId, profile: playerProfile);
	}
}
