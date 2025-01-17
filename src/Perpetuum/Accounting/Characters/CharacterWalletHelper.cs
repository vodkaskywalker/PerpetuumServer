using Perpetuum.Common;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Wallets;

namespace Perpetuum.Accounting.Characters
{
    public class CharacterWalletHelper
    {
        private readonly CharacterWalletFactory walletFactory;
        private readonly ICentralBank centralBank;

        public CharacterWalletHelper(CharacterWalletFactory walletFactory, ICentralBank centralBank)
        {
            this.walletFactory = walletFactory;
            this.centralBank = centralBank;
        }

        public IWallet<double> GetWallet(Character character, TransactionType transactionType)
        {
            return walletFactory(character, transactionType);
        }

        public void TransferCredit(Character source, Character target, long amount)
        {
            source.IsInTraining().ThrowIfTrue(ErrorCodes.TrainingCharacterInvolved);
            target.IsInTraining().ThrowIfTrue(ErrorCodes.TrainingCharacterInvolved);
            target.IsActive.ThrowIfFalse(ErrorCodes.CharacterDeleted);

            //attempt to send money to a privileged character
            if (target.AccessLevel.IsAnyPrivilegeSet())
            {
                //source majer?
                if (!source.AccessLevel.IsAnyPrivilegeSet())
                {
                    //source nem majer
                    //target majer, de mennyire?
                    target.CheckPrivilegedTransactionsAndThrowIfFailed();
                }
            }

            //if gm is the source - transactions are controlled
            source.CheckPrivilegedTransactionsAndThrowIfFailed();

            ICharacterWallet sourceWallet = walletFactory(source, TransactionType.characterTransfer_to);
            sourceWallet.Balance -= amount;

            source.LogTransaction(TransactionLogEvent.Builder()
                .SetTransactionType(TransactionType.characterTransfer_to)
                .SetCreditBalance(sourceWallet.Balance)
                .SetCreditChange(-amount)
                .SetCharacter(source)
                .SetInvolvedCharacter(target));

            ICharacterWallet targetWallet = walletFactory(target, TransactionType.characterTransfer_from);
            targetWallet.Balance += amount;

            source.LogTransaction(TransactionLogEvent.Builder()
                .SetTransactionType(TransactionType.characterTransfer_from)
                .SetCreditBalance(targetWallet.Balance)
                .SetCreditChange(amount)
                .SetCharacter(target)
                .SetInvolvedCharacter(source));
        }

        public void AddToWallet(Character character, TransactionType transactionType, double amount)
        {
            ICharacterWallet wallet = walletFactory(character, transactionType);
            wallet.Balance += amount;

            TransactionLogEventBuilder b = TransactionLogEvent.Builder()
                .SetTransactionType(transactionType)
                .SetCreditBalance(wallet.Balance)
                .SetCreditChange(amount)
                .SetCharacter(character);

            character.LogTransaction(b);
            centralBank.SubAmount(amount, transactionType);
        }

        public void SubtractFromWallet(Character character, TransactionType transactionType, double amount)
        {
            ICharacterWallet wallet = walletFactory(character, transactionType);
            wallet.Balance -= amount;

            TransactionLogEventBuilder b = TransactionLogEvent.Builder()
                .SetTransactionType(transactionType)
                .SetCreditBalance(wallet.Balance)
                .SetCreditChange(-amount)
                .SetCharacter(character);

            character.LogTransaction(b);
        }
    }
}