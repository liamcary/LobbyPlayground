using System;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;
using static Unity.Services.Lobbies.Models.DataObject;

public class LobbyDetailsMenuItem : MonoBehaviour
{
	public MultiplayerLobby Lobby { get; private set; }

	[Header("Lobby properties")]
	[SerializeField] TMP_Text _id;
	[SerializeField] TMP_Text _lobbyName;
	[SerializeField] TMP_Text _isMember;
	[SerializeField] TMP_Text _isHost;
	[SerializeField] TMP_Text _isCreator;
	[SerializeField] TMP_Text _hostId;
	[SerializeField] TMP_Text _creatorId;
	[SerializeField] TMP_Text _lobbyCode;
	[SerializeField] TMP_Text _eventConnectionState;
	[SerializeField] TMP_Text _upid;
	[SerializeField] TMP_Text _environmentId;
	[SerializeField] TMP_Text _maxPlayers;
	[SerializeField] TMP_Text _availableSlots;
	[SerializeField] TMP_Text _isPrivate;
	[SerializeField] TMP_Text _isLocked;
	[SerializeField] TMP_Text _hasPassword;
	[SerializeField] TMP_Text _password;
	[SerializeField] TMP_Text _dateCreated;
	[SerializeField] TMP_Text _lastUpdated;

	[SerializeField] Button _leaveButton;
	[SerializeField] Button _deleteButton;
	[SerializeField] Button _addLobbyDataButton;
	[SerializeField] Button _addPlayerDataButton;
	[SerializeField] Button _dumpDataButton;

	const string _dateTimeFormat = "yyyy/MM/dd HH:mm:ss";

	void Awake()
	{
		_leaveButton.onClick.AddListener(HandleLeaveButtonClicked);
		_deleteButton.onClick.AddListener(HandleDeleteButtonClicked);

		_addLobbyDataButton.onClick.AddListener(HandleAddLobbyDataButtonClicked);
		_addPlayerDataButton.onClick.AddListener(HandleAddPlayerDataButtonClicked);
		_dumpDataButton.onClick.AddListener(HandleDumpDataButtonClicked);
	}

	void OnDestroy()
	{
		_leaveButton.onClick.RemoveListener(HandleLeaveButtonClicked);
		_deleteButton.onClick.RemoveListener(HandleDeleteButtonClicked);

		_addLobbyDataButton.onClick.RemoveListener(HandleAddLobbyDataButtonClicked);
		_addPlayerDataButton.onClick.RemoveListener(HandleAddPlayerDataButtonClicked);
		_dumpDataButton.onClick.RemoveListener(HandleDumpDataButtonClicked);
	}

	public void SetLobby(MultiplayerLobby lobby)
	{
		if (Lobby != null) {
			Lobby.IsMember.OnValueChanged -= HandleIsMemberChanged;
			Lobby.IsHost.OnValueChanged -= HandleIsHostChanged;
			Lobby.IsCreator.OnValueChanged -= HandleIsCreatorChanged;
			Lobby.HostId.OnValueChanged -= HandleHostIdChanged;
			Lobby.CreatorId.OnValueChanged -= HandleCreatorIdChanged;
			Lobby.EventConnectionState.OnValueChanged -= HandleEventConnectionStateChanged;
			Lobby.Name.OnValueChanged -= HandleNameChanged;
			Lobby.AvailableSlots.OnValueChanged -= HandleAvailableSlotsChanged;
			Lobby.MaxPlayers.OnValueChanged -= HandleMaxPlayersChanged;
			Lobby.IsLocked.OnValueChanged -= HandleIsLockedChanged;
			Lobby.IsPrivate.OnValueChanged -= HandleIsPrivateChanged;
			Lobby.HasPassword.OnValueChanged -= HandleHasPasswordChanged;
			Lobby.Password.OnValueChanged -= HandlePasswordChanged;
			Lobby.DateCreated.OnValueChanged -= HandleDateCreatedChanged;
			Lobby.DateLastUpdated.OnValueChanged -= HandleDateLastUpdatedChanged;
		}

		Lobby = lobby;

		if (Lobby != null) {
			Lobby.IsMember.OnValueChanged += HandleIsMemberChanged;
			Lobby.IsHost.OnValueChanged += HandleIsHostChanged;
			Lobby.IsCreator.OnValueChanged += HandleIsCreatorChanged;
			Lobby.HostId.OnValueChanged += HandleHostIdChanged;
			Lobby.CreatorId.OnValueChanged += HandleCreatorIdChanged;
			Lobby.EventConnectionState.OnValueChanged += HandleEventConnectionStateChanged;
			Lobby.Name.OnValueChanged += HandleNameChanged;
			Lobby.AvailableSlots.OnValueChanged += HandleAvailableSlotsChanged;
			Lobby.MaxPlayers.OnValueChanged += HandleMaxPlayersChanged;
			Lobby.IsLocked.OnValueChanged += HandleIsLockedChanged;
			Lobby.IsPrivate.OnValueChanged += HandleIsPrivateChanged;
			Lobby.HasPassword.OnValueChanged += HandleHasPasswordChanged;
			Lobby.Password.OnValueChanged += HandlePasswordChanged;
			Lobby.DateCreated.OnValueChanged += HandleDateCreatedChanged;
			Lobby.DateLastUpdated.OnValueChanged += HandleDateLastUpdatedChanged;
		}

		_id.SetText(Lobby?.Id);

		HandleIsMemberChanged(Lobby?.IsMember.Value ?? false);
		HandleIsHostChanged(Lobby?.IsHost.Value ?? false);
		HandleIsCreatorChanged(Lobby?.IsCreator.Value ?? false);
		HandleHostIdChanged(Lobby?.HostId.Value);
		HandleCreatorIdChanged(Lobby?.CreatorId.Value);
		HandleEventConnectionStateChanged(Lobby?.EventConnectionState.Value ?? LobbyEventConnectionState.Unknown);
		HandleNameChanged(Lobby?.Name.Value);
		HandleAvailableSlotsChanged(Lobby?.AvailableSlots.Value ?? -1);
		HandleMaxPlayersChanged(Lobby?.MaxPlayers.Value ?? -1);
		HandleIsLockedChanged(Lobby?.IsLocked.Value ?? false);
		HandleIsPrivateChanged(Lobby?.IsPrivate.Value ?? false);
		HandleHasPasswordChanged(Lobby?.HasPassword.Value ?? false);
		HandlePasswordChanged(Lobby?.Password.Value);
		HandleDateCreatedChanged(Lobby?.DateCreated.Value ?? default);
		HandleDateLastUpdatedChanged(Lobby?.DateCreated.Value ?? default);
	}

	void Update()
	{
		_deleteButton.interactable = Lobbies.CanDeleteLobby(Lobby);
		_leaveButton.interactable = Lobbies.CanLeaveLobby(Lobby);
	}

	void HandleDeleteButtonClicked()
	{
		if (Lobby != null && Lobbies.CanDeleteLobby(Lobby)) {
			Debug.LogFormat("Deleting lobby {0} via button click", Lobby.Id);
			_ = Lobbies.DeleteLobbyAsync(Lobby);
		}
	}

	void HandleLeaveButtonClicked()
	{
		if(Lobbies.CanLeaveLobby(Lobby)) {
			Debug.LogFormat("Leaving lobby {0} via button click", Lobby.Id);
			_ = Lobbies.LeaveLobbyAsync(Lobby);
		}
	}

	void HandleAddLobbyDataButtonClicked()
	{
		string key = $"key-{Guid.NewGuid().ToString().Substring(0, 6)}";
		string value = $"value-{Guid.NewGuid().ToString().Substring(0, 6)}";
		var visibility = (DataObject.VisibilityOptions) UnityEngine.Random.Range(1, 4);

		Debug.LogFormat("Adding random lobby data: Key {0}, Value {1}, Visibility {2}", key, value, visibility);

		_ = Lobby.UpdateLobbyDataAsync(new() {
			{ key, new DataObject (visibility, value) }
		});
	}

	void HandleAddPlayerDataButtonClicked()
	{
		string key = $"key-{Guid.NewGuid().ToString().Substring(0, 6)}";
		string value = $"value-{Guid.NewGuid().ToString().Substring(0, 6)}";
		var visibility = (PlayerDataObject.VisibilityOptions) UnityEngine.Random.Range(1, 4);

		Debug.LogFormat("Adding random player data: Key {0}, Value {1}, Visibility {2}", key, value, visibility);

		_ = Lobby.UpdatePlayerDataAsync(new() {
			{ key, new PlayerDataObject(visibility, value) }
		});
	}

	void HandleDumpDataButtonClicked()
	{
		if (Lobby?.Lobby != null) {
			Debug.Log(Lobby.Lobby.DumpLobbyData());
			Debug.Log(Lobby.Lobby.DumpPlayerData());
		}
	}

	void HandleIsMemberChanged(bool value)
	{
		_isMember.text = value.ToString();
	}

	void HandleIsHostChanged(bool value)
	{
		_isHost.text = value.ToString();
	}

	void HandleIsCreatorChanged(bool value)
	{
		_isCreator.text = value.ToString();
	}

	void HandleHostIdChanged(string value)
	{
		_hostId.text = value;
	}

	void HandleCreatorIdChanged(string value)
	{
		_creatorId.text = value;
	}

	void HandleEventConnectionStateChanged(LobbyEventConnectionState value)
	{
		_eventConnectionState.text = value.ToString();
	}

	void HandleNameChanged (string value)
	{
		_lobbyName.text = value;
	}

	void HandleAvailableSlotsChanged(int value)
	{
		_availableSlots.text = value.ToString();
	}

	void HandleMaxPlayersChanged(int value)
	{
		_maxPlayers.text = value.ToString();
	}

	void HandleIsLockedChanged(bool value)
	{
		_isLocked.text = value.ToString();
	}

	void HandleIsPrivateChanged(bool value)
	{
		_isPrivate.text = value.ToString();
	}

	void HandleHasPasswordChanged(bool value)
	{
		_hasPassword.text = value.ToString();
	}

	void HandlePasswordChanged(string value)
	{
		_password.text = value;
	}

	void HandleDateCreatedChanged(DateTime value)
	{
		_dateCreated.text = value.ToString(_dateTimeFormat);
	}

	void HandleDateLastUpdatedChanged(DateTime value)
	{
		_lastUpdated.text = value.ToString(_dateTimeFormat);
	}
}
