using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LobbyListMenu : MonoBehaviour
{
	[SerializeField] LobbyPreviewMenuItem _lobbyMenuItemPrefab;
	[SerializeField] LobbyPreviewMenuItem _currentLobbyMenuItem;
	[SerializeField] LobbyPreviewMenuItem _selectionDetailsMenuItem;
	[SerializeField] LobbyDetailsMenuItem _currentLobbyDetailsMenuItem;
	[SerializeField] Transform _queriedLobbiesParent;

	[SerializeField] Button _refreshButton;
	[SerializeField] TMP_Text _lastRefreshText;
	[SerializeField] Toggle _autoRefresh;

	GameObject _currentSelection;

	readonly List<LobbyPreviewMenuItem> _availableLobbies = new();

	IEnumerator Start()
	{
		while (!Lobbies.IsEnabled) {
			yield return null;
		}

		_currentLobbyMenuItem.SetLobby(null);
		_currentLobbyDetailsMenuItem.SetLobby(null);

		_refreshButton.onClick.AddListener(HandleRefreshButtonClicked);

		Lobbies.OnCurrentLobbyChanged += HandleCurrentLobbyChanged;
	}

	void OnDestroy()
	{
		_refreshButton.onClick.RemoveListener(HandleRefreshButtonClicked);

		Lobbies.OnCurrentLobbyChanged -= HandleCurrentLobbyChanged;
	}

	void Update()
	{
		_refreshButton.interactable = Lobbies.IsEnabled && !_autoRefresh.isOn && Lobbies.CanQueryLobbies();

		if (Lobbies.IsEnabled && _autoRefresh.isOn && !Lobbies.IsRefreshing && Lobbies.CanQueryLobbies()) {
			_ = RefreshLobbiesAsync();
		}

		if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == _currentSelection) {
			return;
		}

		_currentSelection = EventSystem.current.currentSelectedGameObject;

		if (_currentSelection != null && _currentSelection.TryGetComponent<LobbyPreviewMenuItem>(out var lobbyMenuItem)) {
			_selectionDetailsMenuItem.SetLobby(lobbyMenuItem.Lobby);
		}
	}

	void HandleCurrentLobbyChanged()
	{
		if (Lobbies.CurrentLobby != _currentLobbyDetailsMenuItem.Lobby) {
			_currentLobbyDetailsMenuItem.SetLobby(Lobbies.CurrentLobby);
		}

		_currentLobbyMenuItem.SetLobby(Lobbies.CurrentLobby == null ? null : Lobbies.CurrentLobby.Lobby);
	}

	void HandleRefreshButtonClicked()
	{
		if (Lobbies.CanQueryLobbies()) {
			_ = RefreshLobbiesAsync();
		}
	}

	async Task RefreshLobbiesAsync()
	{
		var lobbies = await Lobbies.QueryLobbiesAsync();

		_lastRefreshText.SetText(Lobbies.LastRefreshTime.ToString("HH:mm:ss"));

		if (lobbies != null) {
			while (lobbies.Count > _availableLobbies.Count) {
				var menuItem = Instantiate(_lobbyMenuItemPrefab, _queriedLobbiesParent);
				_availableLobbies.Add(menuItem);
			}
		}

		for (int i = 0; i < _availableLobbies.Count; ++i) {
			var lobby = lobbies == null || lobbies.Count <= i ? null : lobbies[i];

			_availableLobbies[i].SetLobby(lobby);
			_availableLobbies[i].gameObject.SetActive(lobby != null);
		}
	}
}
