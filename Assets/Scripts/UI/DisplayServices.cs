using TMPro;
using UnityEngine;

public class DisplayServices : MonoBehaviour
{
	[SerializeField] TMP_Text _unityServicesState;
	[SerializeField] TMP_Text _authenticationState;

	void Start()
	{
		Services.OnStateChanged += DisplayUnityServicesState;
		Authentication.OnStateChanged += DisplayAuthenticationState;

		DisplayUnityServicesState();
		DisplayAuthenticationState();
	}

	void OnDestroy()
	{
		Services.OnStateChanged -= DisplayUnityServicesState;
		Authentication.OnStateChanged -= DisplayAuthenticationState;
	}

	void DisplayUnityServicesState()
	{
		string color = Services.State switch {
			Services.InitializationState.Uninitialized => "grey",
			Services.InitializationState.Initializing => "yellow",
			Services.InitializationState.Initialized => "green",
			Services.InitializationState.FailedToInitialize => "red",
			_ => "white"
		};

		_unityServicesState.SetText($"Unity Services: <color={color}>{Services.State}</color>");
	}

	void DisplayAuthenticationState()
	{
		string color = Authentication.State switch {
			Authentication.AuthenticationState.NotAuthenticated => "grey",
			Authentication.AuthenticationState.Authenticating => "yellow",
			Authentication.AuthenticationState.Authenticated => "green",
			Authentication.AuthenticationState.FailedToAuthenticate => "red",
			_ => "white"
		};

		_authenticationState.SetText($"Authentication: <color={color}>{Authentication.State}</color>");
	}
}
