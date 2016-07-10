using SteamKit2;
using SteamTrade;
using SteamTrade.TradeOffer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Xml.Linq;
using TradeAsset = SteamTrade.TradeOffer.TradeOffer.TradeStatusUser.TradeAsset;

namespace SteamBot
{
    public class MyTradeOfferUserHandler : UserHandler
    {
        private static List<ulong> opskinsBotIDs = new List<ulong>();

        private BackgroundWorker autoAcceptConfirmationsThread;
        private object mobileTradeConfirmLock = new object();

        private static void getOpskinsBotList()
        {
            opskinsBotIDs.Clear();
            XDocument doc = XDocument.Load("http://steamcommunity.com/gid/103582791439304221/memberslistxml/?xml=1");
            foreach (XElement e in doc.Descendants("steamID64"))
                opskinsBotIDs.Add(ulong.Parse(e.Value));
        }

        public MyTradeOfferUserHandler(Bot bot, SteamID sid) : base(bot, sid) { }

        private void AutoAcceptConfirmations(object sender, DoWorkEventArgs e)
        {
            while (!autoAcceptConfirmationsThread.CancellationPending)
            {
                if(Bot.IsLoggedIn)
                    lock(mobileTradeConfirmLock)
                        Bot.AcceptAllMobileTradeConfirmations();
                Thread.Sleep(6000);
            }
        }

        public override void OnNewTradeOffer(TradeOffer offer)
        {
            var myItems = offer.Items.GetMyItems();
            var theirItems = offer.Items.GetTheirItems();
            Log.Info("They want " + myItems.Count + " of my items.");
            Log.Info("And I will get " +  theirItems.Count + " of their items.");

            if (myItems.Count == 0 || IsAdmin || opskinsBotIDs.Contains(offer.PartnerSteamId.ConvertToUInt64()))
            {
                TradeOfferAcceptResponse acceptResp = offer.Accept();
                if (acceptResp.Accepted)
                {
                    if(myItems.Count > 0)
                        lock (mobileTradeConfirmLock)
                            Bot.AcceptAllMobileTradeConfirmations();
                    // new System.Media.SoundPlayer("assets\\nof.mp3").Play();
                    Log.Success("Accepted trade offer successfully : Trade ID: " + acceptResp.TradeId);
                }
            }
            else
            {
                if (offer.Decline())
                {
                    Log.Info("Declined trade offer : " + offer.TradeOfferId);
                }
            }
        }

        public override void OnMessage(string message, EChatEntryType type)
        {
            
        }

        public override void OnLoginCompleted()
        {
            getOpskinsBotList();

            if (autoAcceptConfirmationsThread == null)
            {
                autoAcceptConfirmationsThread = new BackgroundWorker();
                autoAcceptConfirmationsThread.WorkerSupportsCancellation = true;
                autoAcceptConfirmationsThread.DoWork += AutoAcceptConfirmations;
            }
            if (!autoAcceptConfirmationsThread.IsBusy)
                autoAcceptConfirmationsThread.RunWorkerAsync();
            Log.Info("Starting auto confirmation of sent trade offers");
        }

        public override void OnDisconnect()
        {
            autoAcceptConfirmationsThread.CancelAsync();
            while (autoAcceptConfirmationsThread.IsBusy)
                Thread.Yield();
            Log.Info("Stopping auto confirmation of sent trade offers");
        }

        public override bool OnGroupAdd() { return false; }

        public override bool OnFriendAdd() { return IsAdmin; }

        public override void OnFriendRemove() { }

        public override bool OnTradeRequest() { return false; }

        public override void OnTradeError(string error) { }

        public override void OnTradeTimeout() { }

        public override void OnTradeSuccess() { }

        public override void OnTradeAwaitingConfirmation(long tradeOfferID) { }

        public override void OnTradeInit() { }

        public override void OnTradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem) { }

        public override void OnTradeRemoveItem(Schema.Item schemaItem, Inventory.Item inventoryItem) { }

        public override void OnTradeMessage(string message) { }

        public override void OnTradeReady(bool ready) { }

        public override void OnTradeAccept() { }
    }
}
