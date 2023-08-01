using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using UnityEngine;

public class Authentication
{
	public enum AuthenticationState
	{
		NotAuthenticated,
		Authenticating,
		Authenticated,
		FailedToAuthenticate
	}

	public static AuthenticationState State
	{
		get => _state;
		set
		{
			if (_state == value) {
				return;
			}

			_state = value;

			OnStateChanged?.Invoke();
		}
	}

	public static event Action OnStateChanged;

	public static string LocalPlayerId => State == AuthenticationState.Authenticated ? AuthenticationService.Instance.PlayerId : null;

	static AuthenticationState _state;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	public static void Initialize()
	{
		_ = InitializeAsync();
	}

	static async Task InitializeAsync()
	{
		while(Services.State == Services.InitializationState.Uninitialized || Services.State == Services.InitializationState.Initializing) {
			await Task.Delay(100);
		}

		State = AuthenticationState.Authenticating;

		await AuthenticationService.Instance.SignInAnonymouslyAsync();

		State = AuthenticationService.Instance.IsSignedIn
			? AuthenticationState.Authenticated
			: AuthenticationState.FailedToAuthenticate;
	}
}
