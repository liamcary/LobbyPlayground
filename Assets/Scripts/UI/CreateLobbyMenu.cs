using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreateLobbyMenu : MonoBehaviour
{
	[SerializeField] TMP_InputField _nameField;
	[SerializeField] Toggle _isPrivateToggle;
	[SerializeField] Button _createButton;

	void Start()
	{
		_createButton.onClick.AddListener(HandleCreateButtonClicked);
	}

	void OnDestroy()
	{
		_createButton.onClick.RemoveListener(HandleCreateButtonClicked);
	}

	void Update()
	{
		bool isInteractable = Lobbies.CanCreateLobby();

		_nameField.interactable = isInteractable;
		_isPrivateToggle.interactable = isInteractable;
		_createButton.interactable = isInteractable && !string.IsNullOrEmpty(_nameField.text);
	}

	void HandleCreateButtonClicked()
	{
		Debug.LogFormat("Creating lobby via button click");

		_ = Lobbies.CreateLobbyAsync(_nameField.text, _isPrivateToggle.isOn);
	}
}
