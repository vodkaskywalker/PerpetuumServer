using Perpetuum.Accounting;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Players;
using Perpetuum.Robots;
using Perpetuum.Services.Sparks.Teleports;
using System;
using System.Collections.Generic;
using System.Transactions;

namespace Perpetuum.Items
{
    public class SparkTeleportToken : Item
    {
        public int BaseId
        {
            get
            {
                return ED.Options.GetOption<int>("baseId");
            }
        }
    }
}
