using ParrelSync;
using System.Text;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public static class LobbyExtensions
{
	const string _creatorIdKey = "creator-id";

	static readonly StringBuilder _dataBuilder = new StringBuilder(512);

	public static string GetCreatorId(this Lobby lobby)
	{
		return lobby.Data.TryGetValue(_creatorIdKey, out var dataObject) ? dataObject.Value : null;
	}

	public static string DumpLobbyData(this Lobby lobby)
	{
		if (lobby?.Data == null) {
			return null;
		}

		foreach (var data in lobby.Data) {
			_dataBuilder.AppendLine("Lobby Data:")
				.Append(data.Key)
				.Append(": ")
				.Append(data.Value.Value)
				.Append(" (Visibility: ")
				.Append(data.Value.Visibility)
				.Append(", Index: ")
				.Append(data.Value.Index)
				.AppendLine(")");
		}

		string result = _dataBuilder.ToString();
		_dataBuilder.Length = 0;

		return result;
	}

	public static string DumpPlayerData(this Lobby lobby)
	{
		if (lobby?.Players == null) {
			return "Player data is null";
		}

		int index = 0;

		foreach(var player in lobby.Players) {
			_dataBuilder.AppendLine("Player Data:")
				.Append(index)
				.Append(": Id = ")
				.Append(player.Id)
				.Append(", Name = ")
				.AppendLine(player.Profile?.Name)
				.Append("Joined = ")
				.Append(player.Joined.ToString("HH:mm:ss.sss"))
				.Append(", LastUpdated = ")
				.AppendLine(player.LastUpdated.ToString("HH:mm:ss.sss"))
				.AppendLine("Data:");

			if (player.Data != null) {
				foreach (var data in player.Data) {
					_dataBuilder.Append(data.Key)
						.Append(": ")
						.Append(data.Value.Value)
						.Append(" (Visibility: ")
						.Append(data.Value.Visibility)
						.AppendLine(")");
				}
			}

			index++;
		}

		string result = _dataBuilder.ToString();
		_dataBuilder.Length = 0;

		return result;
	}

	public static void SetCreatorId(this CreateLobbyOptions options)
	{
		var creatorData = new DataObject(DataObject.VisibilityOptions.Member, Authentication.LocalPlayerId);

		options.Data = new() {
			{ _creatorIdKey, creatorData }
		};
	}
}
