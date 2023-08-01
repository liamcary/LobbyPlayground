using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class Services
{
	public enum InitializationState
	{
		Uninitialized,
		Initializing,
		Initialized,
		FailedToInitialize
	}

	public static InitializationState State 
	{
		get => _state;
		private set
		{
			if (_state == value) {
				return;
			}

			_state = value;
			OnStateChanged?.Invoke();
		}
	}

	public static event Action OnStateChanged;

	static InitializationState _state;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	static void Initialize()
	{
		_ = InitializeAsync();
	}

	static async Task InitializeAsync()
	{
		State = InitializationState.Initializing;

		string authId = DateTime.Now.ToString(Guid.NewGuid().ToString().Substring(0, 6));
		var options = new InitializationOptions().SetProfile(authId);

		await UnityServices.InitializeAsync(options);

		State = UnityServices.State == ServicesInitializationState.Initialized
			? InitializationState.Initialized
			: InitializationState.FailedToInitialize;
	}
}
