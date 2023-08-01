using System;
using System.Threading.Tasks;
using UnityEngine;

public class RateLimitedRequest
{
	public int Cooldown { get; }
	public bool IsRunning { get; private set; }
	public DateTime LastUsage { get; private set; }
	public bool IsOnCooldown => GetCooldownRemaining() > 0;

	const int _pingBuffer = 100;

	public RateLimitedRequest(int cooldownMs)
	{
		Cooldown = cooldownMs;
	}

	public async Task RunTask(Func<Task> task)
	{
		if (IsRunning) {
			Debug.LogError("Request is already running");
			return;
		}

		IsRunning = true;

		await CooldownAsync();
		await task();

		IsRunning = false;
		LastUsage = DateTime.Now;
	}

	public async Task<T> RunTask<T>(Func<Task<T>> task)
	{
		if (IsRunning) {
			Debug.LogError("Request is already running");
			return default;
		}

		IsRunning = true;

		await CooldownAsync();
		var result = await task();

		IsRunning = false;
		LastUsage = DateTime.Now;

		return result;
	}

	async Task CooldownAsync()
	{
		int cooldownRemaining = GetCooldownRemaining();

		if (cooldownRemaining > 0) {
			await Task.Delay(cooldownRemaining);
		}
	}

	int GetCooldownRemaining()
	{
		int timeSinceLastUsage = (int) (DateTime.Now - LastUsage).TotalMilliseconds;
		return Cooldown + _pingBuffer - timeSinceLastUsage;
	}
}
