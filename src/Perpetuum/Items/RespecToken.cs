using Perpetuum.Accounting;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Items
{
    /// <summary>
    /// Represents respec token item
    /// </summary>
    public class RespecToken : Item
    {
        public int OnceInMonths
        {
            get
            {
                return ED.Options.GetOption<int>("onceInMonths");
            }
        }

        public void Activate(
            IAccountManager accountManager,
            Account account,
            Character character)
        {
            var epData = accountManager.GetEPData(account, character);

            accountManager.FreeLockedEp(account, (int)epData[k.lockedEp]);
            
            var defaultExtensions = character.GetDefaultExtensions();

            character.ResetAllExtensions();
            character.SetExtensions(defaultExtensions);
        }
    }
}
