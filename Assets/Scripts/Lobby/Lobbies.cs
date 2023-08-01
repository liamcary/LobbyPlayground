using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class Lobbies
{
	public static bool IsEnabled => Services.State == Services.InitializationState.Initialized
		&& Authentication.State == Authentication.AuthenticationState.Authenticated;
	public static bool IsInLobby => CurrentLobby != null;
	public static bool IsRefreshing { get; private set; }
	public static DateTime LastRefreshTime { get; private set; }

	public static IReadOnlyCollection<Lobby> QueriedLobbies => _queriedLobbies;
	public static MultiplayerLobby CurrentLobby { get; private set; }

	public static event Action OnQueriedLobbiesChanged;
	public static event Action OnCurrentLobbyChanged;
	public static event Action<MultiplayerLobby> OnCreatedLobby;
	public static event Action<MultiplayerLobby> OnDeletedLobby;
	public static event Action<MultiplayerLobby> OnJoinedLobby;
	public static event Action<MultiplayerLobby> OnLeftLobby;

	static readonly QueryLobbiesOptions _options = new QueryLobbiesOptions {
		Filters = new List<QueryFilter>()
	};

	static List<Lobby> _queriedLobbies = new();

	static readonly RateLimitedRequest _queryLobbiesRequest = new RateLimitedRequest(5000);
	static readonly RateLimitedRequest _joinLobbyRequest = new RateLimitedRequest(5000);
	static readonly RateLimitedRequest _createLobbyRequest = new RateLimitedRequest(5000);
	static readonly RateLimitedRequest _deleteLobbyRequest = new RateLimitedRequest(5000);

	public static bool CanQueryLobbies()
	{
		return IsEnabled && !_queryLobbiesRequest.IsRunning && !_queryLobbiesRequest.IsOnCooldown;
	}

	public static async Task<List<Lobby>> QueryLobbiesAsync()
	{
		if (!CanQueryLobbies()) {
			return null;
		}

		Debug.LogFormat("Getting available lobbies");

		var response = await _queryLobbiesRequest.RunTask(() => LobbyService.Instance.QueryLobbiesAsync(_options));

		_queriedLobbies = response?.Results;

		LastRefreshTime = DateTime.Now;

		OnQueriedLobbiesChanged?.Invoke();

		return _queriedLobbies;
	}

	public static bool CanJoinLobbyByCode(string lobbyCode)
	{
		return IsEnabled
			&& !IsInLobby
			&& !string.IsNullOrEmpty(lobbyCode);
	}

	public static async Task<MultiplayerLobby> JoinLobbyByCodeAsync(string lobbyCode, string password = null)
	{
		if (!CanJoinLobbyByCode(lobbyCode)) {
			return null;
		}

		var options = new JoinLobbyByCodeOptions {
			Player = LobbyHelpers.GetPlayer(),
			Password = password
		};

		var lobby = await _joinLobbyRequest.RunTask(() => LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, options));

		if (lobby == null) {
			Debug.LogErrorFormat("Failed to join lobby with Code {0} and Password {1}", lobbyCode, password);
			return null;
		}

		Debug.LogFormat("Successfully joined lobby with code {0}", lobbyCode);

		SetCurrentLobby(new MultiplayerLobby(lobby));

		OnJoinedLobby?.Invoke(CurrentLobby);

		return CurrentLobby;
	}

	public static bool CanJoinLobby(Lobby lobby, string password = null)
	{
		return IsEnabled
			&& !IsInLobby
			&& lobby != null
			&& lobby.AvailableSlots> 0
			&& (!lobby.HasPassword || !string.IsNullOrEmpty(password));
	}

	public static async Task<MultiplayerLobby> JoinLobbyAsync(Lobby lobby, string password = null)
	{
		if (!CanJoinLobby(lobby, password)) {
			return null;
		}

		var options = new JoinLobbyByIdOptions {
			Player = LobbyHelpers.GetPlayer(),
			Password = password
		};

		var newLobby = await _joinLobbyRequest.RunTask(() => LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, options));

		if (newLobby == null) {
			Debug.LogErrorFormat("Failed to join lobby with Id {0}", lobby.Id);
			return null;
		}

		SetCurrentLobby(new MultiplayerLobby(newLobby));

		OnJoinedLobby?.Invoke(CurrentLobby);

		return CurrentLobby;
	}

	public static bool CanLeaveLobby(MultiplayerLobby lobby)
	{
		return IsEnabled
			&& IsInLobby
			&& lobby != null
			&& lobby == CurrentLobby;
	}

	public static async Task LeaveLobbyAsync(MultiplayerLobby lobby)
	{
		if (!CanLeaveLobby(lobby)) {
			return;
		}

		Debug.LogFormat("Leaving lobby {0} by removing local player", lobby.Id);

		await LobbyService.Instance.RemovePlayerAsync(lobby.Id, Authentication.LocalPlayerId);

		SetCurrentLobby(null);

		OnLeftLobby?.Invoke(lobby);
	}

	public static bool CanCreateLobby()
	{
		return IsEnabled
			&& CurrentLobby == null
			&& !_createLobbyRequest.IsRunning
			&& !_createLobbyRequest.IsOnCooldown;
	}

	public static async Task<MultiplayerLobby> CreateLobbyAsync(string lobbyName, bool isPrivate, string password = null)
	{
		if (!CanCreateLobby()) {
			return null;
		}

		var createOptions = new CreateLobbyOptions {
			Player = LobbyHelpers.GetPlayer(),
			Password = password,
			IsPrivate = isPrivate,
			IsLocked = false
		};

		createOptions.SetCreatorId();

		Debug.LogFormat("Creating {0} lobby with name {1} and password {2}", isPrivate ? "private" : "public", lobbyName, password);

		var lobby = await _createLobbyRequest.RunTask(() => LobbyService.Instance.CreateLobbyAsync(lobbyName, 4, createOptions));

		if (lobby == null) {
			Debug.LogError("Failed to create lobby. Lobby is null");
			return null;
		}

		Debug.LogFormat("Created lobby: {0}", lobby.Id);

		SetCurrentLobby(new MultiplayerLobby(lobby));

		OnCreatedLobby?.Invoke(CurrentLobby);

		return CurrentLobby;
	}

	public static bool CanDeleteLobby(MultiplayerLobby lobby)
	{
		return IsEnabled
			&& IsInLobby
			&& lobby != null
			&& lobby == CurrentLobby
			&& CurrentLobby.IsHost.Value;
	}

	public static async Task DeleteLobbyAsync(MultiplayerLobby lobby)
	{
		if (!CanDeleteLobby(lobby)) {
			return;
		}

		Debug.LogFormat("Deleting lobby with {0}", CurrentLobby.Id);

		await _deleteLobbyRequest.RunTask(() => LobbyService.Instance.DeleteLobbyAsync(CurrentLobby.Id));

		SetCurrentLobby(null);

		OnDeletedLobby?.Invoke(lobby);
	}

	static void SetCurrentLobby(MultiplayerLobby lobby)
	{
		if (lobby == CurrentLobby) {
			Debug.LogFormat("Current lobby is already set to lobby {0}", lobby?.Id);
			return;
		}

		Debug.LogFormat("Changing current lobby from {0} to {1}", CurrentLobby?.Id, lobby?.Id);

		if (CurrentLobby != null) {
			CurrentLobby.OnKickedFromLobby -= HandleKickedFromLobby;
			CurrentLobby.OnLobbyDeleted -= HandleLobbyDeleted;
		}

		CurrentLobby = lobby;

		if (CurrentLobby != null) {
			CurrentLobby.OnKickedFromLobby += HandleKickedFromLobby;
			CurrentLobby.OnLobbyDeleted += HandleLobbyDeleted;
		}

		OnCurrentLobbyChanged?.Invoke();
	}

	static void HandleLobbyDeleted()
	{
		SetCurrentLobby(null);
	}

	static void HandleKickedFromLobby()
	{
		SetCurrentLobby(null);
	}
}
