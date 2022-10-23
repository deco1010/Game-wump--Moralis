﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MoralisUnity.Samples.Shared.Interfaces;
using MoralisUnity.Samples.Shared.UnityWeb3Tools.Functions;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.CloudScriptModels;
using UnityEngine;
using MoralisUnity.Samples.Shared.UnityWeb3Tools.Models;
using MoralisUnity.Samples.SharedCustom.DesignPatterns.Creational.Singleton.CustomSingleton;
using MoralisUnity.Samples.SharedCustom.Exceptions;
using UnityEditor;

#pragma warning disable 1998, CS4014
namespace MoralisUnity.Samples.Shared
{
	/// <summary>
	/// Custom wrapper for the client-side of Moralis functionality
	/// </summary>
	public class MoralisTwoWeb3System : CustomSingleton<MoralisTwoWeb3System>, 
		IInitializableAsync, ICustomSingletonParent, ICustomWeb3System
	{
		// Properties -------------------------------------
		
		public bool IsInitialized { get; private set; }
		public bool IsAuthenticated { get; private set; }
		
		public ICustomWeb3WalletSystem CustomWeb3WalletSystem 
		{
			set
			{
				//To encourage vigilant use of this method,
				//it is required to be set early in lifecycle
				RequireIsNotInitialized();
				_customWeb3WalletSystem = value;
			}
			get
			{
				return _customWeb3WalletSystem;
			}
		}
		
		public ICustomBackendSystem CustomBackendSystem 
		{
			set
			{
				//To encourage vigilant use of this method,
				//it is required to be set early in lifecycle
				RequireIsNotInitialized();
				_customBackendSystem = value;
			}
			get
			{
				return _customBackendSystem;
			}
		}
		
		public int ChainId
		{
			get
			{
				return _customWeb3WalletSystem.ChainId;
			}
		}


		// Fields -----------------------------------------
		private ICustomWeb3WalletSystem _customWeb3WalletSystem;
		private ICustomBackendSystem _customBackendSystem;
		//
		private ExecuteContractFunctionSubSystem _executeContractFunctionSubsystem = new ExecuteContractFunctionSubSystem();
		private RunContractFunctionSubsystem _runContractFunctionSubsystem = new RunContractFunctionSubsystem();
		
		// Unity Methods ----------------------------------

		// Initialization Methods -------------------------
		void ICustomSingletonParent.OnInstantiatedChild()
		{
			//Do not InitializeAsync here
			//Wait for external scope to call, 	IsAuthenticatedAsync() which will do so
		}

		public async UniTask InitializeAsync()
		{
			if (!IsInitialized)
			{
				_runContractFunctionSubsystem = new RunContractFunctionSubsystem();
				_executeContractFunctionSubsystem = new ExecuteContractFunctionSubSystem();
				
				// Do initialize / do not auth
				_customWeb3WalletSystem.Initialize();
				if (!_customWeb3WalletSystem.IsInitialized)
				{
					Debug.Log($"{GetType().Name}.InitializeAsync() failed. Error 0001");
				}
				Debug.Log("HasWeb3UserAddressAsync: " + _customWeb3WalletSystem.HasWeb3UserAddressAsync());
				// Do initialize
				_customBackendSystem.Initialize();
				if (!_customBackendSystem.IsInitialized)
				{
					Debug.Log($"{GetType().Name}.InitializeAsync() failed. Error 0002");
				}
				
				// Do auth
				await _customBackendSystem.AuthenticateAsync();
				if (!_customBackendSystem.IsAuthenticated)
				{
					Debug.Log($"{GetType().Name}.InitializeAsync() failed. Error 0003");
				}

			}
			IsInitialized = true;
		}


		
		public void RequireIsInitialized()
		{
			if (!IsInitialized)
			{
				throw new NotInitializedException(this);
			}
		}
		
		public void RequireIsNotInitialized()
		{
			if (IsInitialized)
			{
				throw new InitializedException(this);
			}
		}
		
		public async void RequireIsAuthenticated()
		{
			if (!IsAuthenticated)
			{
				throw new NotAuthenticatedException(this);
			}
		}

		//Statics cannot be overriden, but this is the proper
		//solution to get parent static functionality in local scope. Keep.
		public new static void OnEnteredEditMode()
		{
			Debug.Log("OnEnteredEditMode() !!!!!! ");
			//Clear out statics when play stops
			Uninstantiate();
		}



		// General Methods --------------------------------

		public async UniTask ClearActiveSessionAsync()
		{
			Debug.Log("clear session");
			await _customWeb3WalletSystem.ClearActiveSessionAsync();
			await _customBackendSystem.ClearActiveSessionAsync();
		}
		
		public async UniTask CloseActiveSessionAsync()
		{
			Debug.Log("close session");
			await _customWeb3WalletSystem.CloseActiveSessionAsync();
		}
		
		public bool HasActiveSession
		{
			get
			{
				return _customWeb3WalletSystem.HasActiveSession;
			}
		}


		public async UniTask<bool> IsAuthenticatedAsync()
		{
			if (!IsInitialized)
			{
				await InitializeAsync();
			}
			RequireIsInitialized();
			
			//Recheck every time
			Debug.Log("mor 2 init, addy: " + await GetWeb3UserAddressAsync());
			IsAuthenticated = await HasWeb3UserAddressAsync();

			return IsAuthenticated;
		}
		
		public async UniTask AuthenticateAsync()
		{
			// Do initialize
			if (!_customWeb3WalletSystem.IsConnected)
			{
				
				// Call without await
				if (_customWeb3WalletSystem.IsConnected)
				_customWeb3WalletSystem.ConnectAsync();
				CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
				cancellationTokenSource.CancelAfterSlim(TimeSpan.FromSeconds(2)); // 2sec timeout is enough

				try
				{
					await UniTask.WaitWhile(
						() =>
						{
							return !_customWeb3WalletSystem.IsConnected;
						}, PlayerLoopTiming.Update, cancellationTokenSource.Token);
				}
				catch (OperationCanceledException common)
				{
					//Called for timeout. No problem.
				}

			}
			await IsAuthenticatedAsync();
		}
		

		
		private async UniTask<bool> HasWeb3UserAddressAsync()
		{
			RequireIsInitialized();
			return await _customWeb3WalletSystem.HasWeb3UserAddressAsync();
		}

		
		public async Task<string> GetWeb3UserAddressAsync()
		{
			RequireIsInitialized();
			return await _customWeb3WalletSystem.GetWeb3UserAddressAsync();
		}

		
		public string ConvertWeb3AddressToShortFormat(string web3Address)
		{
			RequireIsInitialized();
			const int n = 6;
			if (string.IsNullOrEmpty(web3Address))
			{
				return string.Empty;
			}
        
			if (web3Address.Length < n)
			{
				return web3Address;
			}

			return $"{web3Address.Substring(0, n)}...{web3Address.Substring(web3Address.Length - n)}";
		}

		
		public async UniTask<String> ExecuteContractFunctionAsync(string contractAddress, string abi,
			string functionName, object[] args, bool isLogging = false)
		{

			RequireIsInitialized();
			RequireIsAuthenticated();

			if (!_executeContractFunctionSubsystem.IsInitialized)
			{
				await _executeContractFunctionSubsystem.InitializeAsync();
			}
			
			//
			string web3UserAddress = await CustomWeb3System.Instance.GetWeb3UserAddressAsync();
			string result = await _executeContractFunctionSubsystem.RunAsync(
				web3UserAddress, 
				contractAddress, 
				abi, 
				functionName, 
				args,
				isLogging);
			
			return result;
		}


		// Event Handlers ---------------------------------
		public async UniTask<object> RunContractFunctionAsync(
			string contractAddress,
			string functionName,
			string abi, 
			object args,
			bool isLogging = false)
		{
			
			RequireIsInitialized();
			RequireIsAuthenticated();

			if (!_runContractFunctionSubsystem.IsInitialized)
			{
				_runContractFunctionSubsystem.Initialize();
			}
			
			//
			int chainId = CustomWeb3System.Instance.ChainId;
			object result = await _runContractFunctionSubsystem.RunAsync(
				contractAddress,
				chainId,
				functionName,
				abi,
				args,
				isLogging);

			return result;
		}


		public async UniTask<List<NftOwner>> GetNFTsForContractAsync(string contractAddress, bool isLogging = false)
		{
			RequireIsInitialized();
			RequireIsAuthenticated();
			//
			string web3UserAddress = await CustomWeb3System.Instance.GetWeb3UserAddressAsync();
			int chainid = this.ChainId;
			List<NftOwner> matchingNftOwners = new List<NftOwner>();
			bool hasCompletedExecuteFunction = false;
			
			if (isLogging)
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine($"GetNFTsForContractAsync() Starting ...\n\n");
				Debug.Log(stringBuilder);
			}

			PlayFabCloudScriptAPI.ExecuteFunction(new ExecuteFunctionRequest()
			{
				Entity = new PlayFab.CloudScriptModels.EntityKey()
				{
					Id = PlayFabSettings.staticPlayer.EntityId, //Get this from when you logged in,
					Type = PlayFabSettings.staticPlayer.EntityType, //Get this from when you logged in
				},
				FunctionName = "GetNftsForContract", //This should be the name of your Azure Function that you created.
				FunctionParameter =
					new Dictionary<string, object>() //This is the data that you would want to pass into your function.
					{
						{ "walletAddress", web3UserAddress },
						{ "chainid", chainid } 
					},
				GeneratePlayStreamEvent = true //Set this to true if you would like this call to show up in PlayStream
			}, (ExecuteFunctionResult result) =>
			{
				if (result.FunctionResultTooLarge ?? false)
				{
					Debug.LogError("error 1 for : " + result.FunctionResult);
					//"This can happen if you exceed the limit that can be returned from an Azure Function, See PlayFab Limits Page for details.");
					// If the is a error fire the OnFailed event
					hasCompletedExecuteFunction = true;
				}

				// If the authentication succeeded the user profile is update and we get the UpdateUserDataAsync return values a response
				// If it failed it returns empty
				if (String.IsNullOrEmpty(result.FunctionResult.ToString()))
				{
					Debug.LogError("error 2 for : " + result.FunctionResult);
					hasCompletedExecuteFunction = true;
				}
				else
				{
					
					List<NftOwner> allNftOwners =
						JsonConvert.DeserializeObject<List<NftOwner>>(result.FunctionResult.ToString());

					if (allNftOwners == null)
					{
						Debug.Log("No owners for this contractAddress");
						hasCompletedExecuteFunction = true;
					}

					foreach (NftOwner nftOwner in allNftOwners)
					{
						try
						{
							// Check if its minted in our contract
							if (string.Equals(nftOwner.TokenAddress, contractAddress, StringComparison.InvariantCultureIgnoreCase))
							{
								matchingNftOwners.Add(nftOwner);
							}
						}
						catch (Exception e)
						{
							Debug.LogError("Error with My NFT called: " + e.Message);
							throw;
						}
					}

					hasCompletedExecuteFunction = true;
				}
			}, (PlayFabError error) => { Debug.Log($"Oops Something went wrong: {error.GenerateErrorReport()}"); });

			
			if (isLogging)
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine($"GetNFTsForContractAsync() Pending");
				Debug.Log(stringBuilder);
			}
			
			await UniTask.WaitWhile(() => !hasCompletedExecuteFunction);

			if (isLogging)
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine($"GetNFTsForContractAsync() Completed ...\n\n");
				stringBuilder.AppendLine($"result.count = {matchingNftOwners.Count}");
				stringBuilder.AppendLine($"\n\n\n");
				Debug.Log(stringBuilder);
			}
			
			return matchingNftOwners;
		}
	}
}

