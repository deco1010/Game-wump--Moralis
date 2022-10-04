using System.Collections.Generic;
using MoralisUnity.Samples.Shared.Data.Types;
using MoralisUnity.Samples.TheGame.MVCS.Model.Data.Types;
using MoralisUnity.Samples.TheGame.MVCS.Model.Data.Types.Configuration;

namespace MoralisUnity.Samples.TheGame.MVCS.Model
{
	/// <summary>
	/// Stores data for the game
	///		* See <see cref="TheGameSingleton"/>
	/// </summary>
	public class TheGameModel 
	{
		// Properties -------------------------------------
		public TheGameConfiguration TheGameConfiguration { get { return TheGameConfiguration.Instance; }  }
		public Observable<int> Gold { get { return _gold; } }
		public Observable<bool> IsRegistered { get { return _isRegistered; } }
		public Observable<List<Prize>> Prizes { get { return _prizes; } }

		// Fields -----------------------------------------
		private Observable<int> _gold = new Observable<int>();
		private ObservablePrizes _prizes = new ObservablePrizes();
		private Observable<bool> _isRegistered = new Observable<bool>();

		// Initialization Methods -------------------------
		public TheGameModel()
		{
			ResetAllData();
		}

		
		// General Methods --------------------------------
		public bool HasAnyData()
		{
			return false;
		}
		
		
		public void ResetAllData()
		{
			_gold.Value = 0;
			_prizes.Value = new List<Prize>();
			_isRegistered.Value = false;
		}
		
		
		
		// Event Handlers ---------------------------------
		public void InvokeOnChangeEvent()
		{
			//Do this with any or all. The controller will act like 'all' changes anyway
			_gold.OnValueChanged.Invoke(_gold.Value);
		}
	}
}
