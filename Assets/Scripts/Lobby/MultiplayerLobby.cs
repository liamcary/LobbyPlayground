using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class MultiplayerLobby
{
	public Lobby Lobby { get; private set; }

	public string Id => Lobby.Id;
	public ChangableProperty<bool> IsMember { get; }
	public ChangableProperty<bool> IsHost { get; private set; }
	public ChangableProperty<bool> IsCreator { get; private set; }
	public ChangableProperty<string> HostId { get; }
	public ChangableProperty<string> CreatorId { get; }
	public ChangableProperty<LobbyEventConnectionState> EventConnectionState { get; }
	public ChangableProperty<string> Name { get; }
	public ChangableProperty<int> AvailableSlots { get; }
	public ChangableProperty<int> MaxPlayers { get; }
	public ChangableProperty<bool> IsLocked { get; }
	public ChangableProperty<bool> IsPrivate { get; }
	public ChangableProperty<bool> HasPassword { get; }
	public ChangableProperty<string> Password { get; }
	public ChangableProperty<DateTime> DateCreated { get; }
	public ChangableProperty<DateTime> DateLastUpdated { get; }

	public event Action OnPlayersJoined;
	public event Action OnPlayersLeft;
	public event Action OnLobbyDataChanged;
	public event Action OnPlayerDataChanged;
	public event Action OnLobbyDeleted;
	public event Action OnKickedFromLobby;

	readonly RateLimitedRequest _updateLobbyDataRequest = new RateLimitedRequest(5000);
	readonly RateLimitedRequest _updatePlayerDataRequest = new RateLimitedRequest(5000);

	bool _isSubscribedToEvents;
	ILobbyEvents _lobbyChannel;
	LobbyEventCallbacks _callbacks;

	readonly CancellationTokenSource _heartbeatCts = new();
	readonly RateLimitedRequest _heartbeatRequest = new RateLimitedRequest(5000);

	const int _heartbeatDelay = 15000; // milliseconds

	public MultiplayerLobby(Lobby lobby)
	{
		Lobby = lobby;

		IsMember = new();
		IsHost = new();
		IsCreator = new();
		HostId = new(lobby.HostId);
		CreatorId = new(lobby.GetCreatorId());
		EventConnectionState = new(LobbyEventConnectionState.Unsubscribed);
		Name = new(lobby.Name);
		AvailableSlots = new(lobby.AvailableSlots);
		MaxPlayers = new(lobby.MaxPlayers);
		IsLocked = new(lobby.IsLocked);
		IsPrivate = new(lobby.IsPrivate);
		HasPassword = new(lobby.HasPassword);
		Password = new();
		DateCreated = new(lobby.Created);
		DateLastUpdated = new(lobby.LastUpdated);

		HostId.OnValueChanged += HandleHostIdChanged;
		CreatorId.OnValueChanged += HandleCreatorIdChanged;

		UpdateMembership();
	}

	public async Task UpdateLobbyDataAsync(Dictionary<string, DataObject> newData = null)
	{
		var lobbyData = Lobby.Data ?? new();

		if (newData != null) {
			foreach (var data in newData) {
				if (lobbyData.ContainsKey(data.Key)) {
					lobbyData[data.Key] = data.Value;
				} else {
					lobbyData.Add(data.Key, data.Value);
				}
			}
		}

		var updateOptions = new UpdateLobbyOptions {
			Name = Name.Value,
			MaxPlayers = MaxPlayers.Value,
			IsPrivate = IsPrivate.Value,
			IsLocked = false,
			Password = Password.Value,
			HostId = HostId.Value,
			Data = lobbyData
		};

		Lobby = await _updateLobbyDataRequest.RunTask(() => LobbyService.Instance.UpdateLobbyAsync(Lobby.Id, updateOptions));

		// Manually update cache and invoke changed event because only remote clients receive normal change events since Lobby v1.1.0-pre5
		UpdateCachedData();

		if (newData != null) {
			OnLobbyDataChanged?.Invoke();
		}
	}

	// Players can only write to their own player data, not other player's.
	public async Task UpdatePlayerDataAsync(Dictionary<string, PlayerDataObject> newData)
	{
		var player = Lobby.Players.FirstOrDefault(p => p.Id == Authentication.LocalPlayerId);
		var playerData = player?.Data ?? new();

		foreach (var data in newData) {
			playerData[data.Key] = data.Value;
		}

		var options = new UpdatePlayerOptions {
			Data = playerData
		};

		Lobby = await _updatePlayerDataRequest.RunTask(() => LobbyService.Instance.UpdatePlayerAsync(Lobby.Id, Authentication.LocalPlayerId, options));

		OnPlayerDataChanged?.Invoke();
	}

	void UpdateMembership()
	{
		bool isMember = false;

		if (Lobby.Players != null) {
			foreach (var player in Lobby.Players) {
				if (player.Id == Authentication.LocalPlayerId) {
					isMember = true;
					break;
				}
			}
		}

		IsMember.Value = isMember;
		IsHost.Value = HostId.Value == Authentication.LocalPlayerId;
		IsCreator.Value = CreatorId.Value == Authentication.LocalPlayerId;

		if (IsMember.Value && !_isSubscribedToEvents) {
			_ = HeartbeatLoopAsync(Lobby.Id, _heartbeatCts.Token);
			_ = SubscribeLobbyEventsAsync();
		} else if (!IsMember.Value && _isSubscribedToEvents) {
			_heartbeatCts.Cancel();
			_ = UnsubscribeLobbyEventsAsync();
		}

		_isSubscribedToEvents = IsMember.Value;
	}

	async Task SubscribeLobbyEventsAsync()
	{
		_callbacks = new ();

		_callbacks.LobbyChanged += HandleLobbyChanged;
		_callbacks.LobbyEventConnectionStateChanged += HandleLobbyEventConnectionStateChanged;
		_callbacks.LobbyDeleted += HandleLobbyDeleted;
		_callbacks.KickedFromLobby += HandleKickedFromLobby;
		_callbacks.PlayerJoined += HandlePlayerJoined;
		_callbacks.PlayerLeft += HandlePlayerLeft;

		_lobbyChannel = await LobbyService.Instance.SubscribeToLobbyEventsAsync(Lobby.Id, _callbacks);

		Debug.LogFormat("Subscribed to lobby events");
	}

	async Task UnsubscribeLobbyEventsAsync()
	{
		if (_callbacks != null) {
			_callbacks.LobbyChanged -= HandleLobbyChanged;
			_callbacks.LobbyEventConnectionStateChanged -= HandleLobbyEventConnectionStateChanged;
			_callbacks.LobbyDeleted -= HandleLobbyDeleted;
			_callbacks.KickedFromLobby -= HandleKickedFromLobby;
			_callbacks.PlayerJoined -= HandlePlayerJoined;
			_callbacks.PlayerLeft -= HandlePlayerLeft;
		}

		if (_lobbyChannel != null) {
			await _lobbyChannel.UnsubscribeAsync();
		}
	}

	async Task HeartbeatLoopAsync(string lobbyId, CancellationToken cancellationToken)
	{
		await Task.Delay(_heartbeatDelay);

		while (!cancellationToken.IsCancellationRequested) {
			await _heartbeatRequest.RunTask(() => LobbyService.Instance.SendHeartbeatPingAsync(lobbyId));
			await Task.Delay(_heartbeatDelay);
		}
	}

	void HandleLobbyChanged(ILobbyChanges changes)
	{
		Debug.LogFormat("Lobby has changed");

		changes.ApplyToLobby(Lobby);

		UpdateCachedData();

		if (changes.Data.Added || changes.Data.Changed || changes.Data.Removed) {
			OnLobbyDataChanged?.Invoke();
		}

		if (changes.PlayerData.Added || changes.PlayerData.Changed) {
			OnPlayerDataChanged?.Invoke();
		}
	}

	void UpdateCachedData()
	{
		Name.Value = Lobby.Name;
		HostId.Value = Lobby.HostId;
		AvailableSlots.Value = Lobby.AvailableSlots;
		IsPrivate.Value = Lobby.IsPrivate;
		IsLocked.Value = Lobby.IsLocked;
		HasPassword.Value = Lobby.HasPassword;
		DateCreated.Value = Lobby.Created;
		DateLastUpdated.Value = Lobby.LastUpdated;
	}

	void HandleLobbyEventConnectionStateChanged(LobbyEventConnectionState state)
	{
		Debug.LogFormat("Lobby events connection state changed from {0} to {1}", EventConnectionState, state);

		EventConnectionState.Value = state;
	}

	void HandleLobbyDeleted()
	{
		Debug.LogFormat("Lobby {0} was deleted, requesting to leave", Lobby.Id);

		UpdateMembership();

		OnLobbyDeleted?.Invoke();
	}

	void HandleKickedFromLobby()
	{
		Debug.LogFormat("Kicked from lobby {0}", Lobby.Id);

		UpdateMembership();

		OnKickedFromLobby?.Invoke();
	}

	void HandleHostIdChanged(string value)
	{
		Debug.LogFormat("Host ID changed to {0}", value);

		UpdateMembership();
	}

	void HandleCreatorIdChanged(string value)
	{
		Debug.LogFormat("Creator ID changed to {0}", value);

		UpdateMembership();
	}

	void HandlePlayerJoined(List<LobbyPlayerJoined> players)
	{
		Debug.LogFormat("{0} players joined", players.Count);

		OnPlayersJoined?.Invoke();
	}

	void HandlePlayerLeft(List<int> playerIndices)
	{
		Debug.LogFormat("{0} players left", playerIndices.Count);

		OnPlayersLeft?.Invoke();
	}
}
