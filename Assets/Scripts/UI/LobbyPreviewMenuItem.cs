using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LobbyPreviewMenuItem : MonoBehaviour
{
	public Lobby Lobby{ get; private set; }

	[SerializeField] TMP_Text _name;
	[SerializeField] TMP_Text _id;
	[SerializeField] TMP_Text _availableSlots;
	[SerializeField] TMP_Text _maxPlayers;
	[SerializeField] TMP_Text _isPrivate;
	[SerializeField] TMP_Text _isLocked;
	[SerializeField] TMP_Text _hasPassword;
	[SerializeField] TMP_Text _created;
	[SerializeField] TMP_Text _lastUpdated;
	[SerializeField] Button _selectButton;
	[SerializeField] Button _joinButton;
	[SerializeField] Button _dumpDataButton;

	const string _dateFormat = "yyyy-MM-dd HH:mm:ss.sss";

	void Awake()
	{
		if (_selectButton != null) {
			_selectButton.onClick.AddListener(HandleSelectButtonClicked);
		}

		if (_joinButton != null) {
			_joinButton.onClick.AddListener(HandleJoinButtonClicked);
		}

		if (_dumpDataButton != null) {
			_dumpDataButton.onClick.AddListener(HandleDumpDataButtonClicked);
		}
	}

	void OnDestroy()
	{
		if (_selectButton != null) {
			_selectButton.onClick.RemoveListener(HandleSelectButtonClicked);
		}

		if (_joinButton != null) {
			_joinButton.onClick.RemoveListener(HandleJoinButtonClicked);
		}

		if (_dumpDataButton != null) {
			_dumpDataButton.onClick.RemoveListener(HandleDumpDataButtonClicked);
		}
	}

	public void SetLobby(Lobby lobby)
	{
		Lobby = lobby;

		_id?.SetText(Lobby?.Id);
		_name?.SetText(Lobby?.Name);
		_availableSlots?.SetText(Lobby?.AvailableSlots.ToString());
		_maxPlayers?.SetText(Lobby?.MaxPlayers.ToString());
		_isPrivate?.SetText(Lobby?.IsPrivate.ToString());
		_isLocked?.SetText(Lobby?.IsLocked.ToString());
		_hasPassword?.SetText(Lobby?.HasPassword.ToString());
		_created?.SetText(Lobby?.Created.ToString(_dateFormat));
		_lastUpdated?.SetText(Lobby?.LastUpdated.ToString(_dateFormat));

		if (_selectButton != null) {
			_selectButton.interactable = Lobby != null;
		}

		if(_joinButton != null && Lobby != null) {
			_joinButton.interactable = Lobbies.CanJoinLobby(Lobby);
		}
	}

	void HandleSelectButtonClicked()
	{
		EventSystem.current.SetSelectedGameObject(gameObject);
	}

	void HandleJoinButtonClicked()
	{
		if (Lobby != null) {
			_ = Lobbies.JoinLobbyAsync(Lobby);
		}
	}

	void HandleDumpDataButtonClicked()
	{
		if (Lobby != null) {
			Debug.Log(Lobby.DumpLobbyData());
			Debug.Log(Lobby.DumpPlayerData());
		}
	}
}
