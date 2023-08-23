using Perpetuum.Accounting;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Items
{
    /// <summary>
    /// Represents respec token item
    /// </summary>
    public  class RespecToken : Item
    {
        public int EpLimit
        {
            get
            {
                return ED.Options.GetOption<int>("epLimit");
            }
        }

        public string Tier
        {
            get
            {
                return ED.Options.GetOption<string>("tier");
            }
        }

        public void Activate(
            IAccountManager accountManager,
            Account account,
            Character character)
        {
            var epData = accountManager.GetEPData(account, character);
            var avaliableEp = (int)epData[k.points];
            var spentEpPerCharacter = (int)epData["spentEpPerCharacter"];

            if (this.EpLimit >= avaliableEp + spentEpPerCharacter)
            {
                accountManager.FreeLockedEp(account, (int)epData[k.lockedEp]);
                character.ResetAllExtensions();
                var defaultExtensions = character.GetDefaultExtensions();
                character.SetExtensions(defaultExtensions);
            }
            else
            {
                throw PerpetuumException.Create(ErrorCodes.TooMuchEp);
            }
        }
    }
}
