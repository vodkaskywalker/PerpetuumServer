using Perpetuum.Accounting;
using System;

namespace Perpetuum.Items
{
	public class EPBoost : Item
	{
		public int Boost
		{
			get
			{
				return ED.Options.GetOption<int>("addBoost");
			}
		}

		public int TimePeriodHours
		{
			get
			{
				return ED.Options.GetOption<int>("timePeriodHours");
			}
		}

		public string Tier
		{
			get
			{
				return ED.Options.GetOption<string>("tier");
			}
		}
		
		public void Activate(IAccountManager accountManager, Account account)
		{
			accountManager.ExtensionSubscriptionStart(account, DateTime.Now, DateTime.Now.AddHours(TimePeriodHours), Boost);
		}
	}
}
