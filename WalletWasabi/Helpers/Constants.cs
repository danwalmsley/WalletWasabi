using NBitcoin;
using NBitcoin.Protocol;
using System;

namespace WalletWasabi.Helpers
{
	public static class Constants
	{
		public const string ClientSupportBackendVersionMin = "4";
		public const string ClientSupportBackendVersionMax = "4";
		public const string BackendMajorVersion = "4";

		/// <summary>
		/// By changing this, we can force to start over the transactions file, so old incorrect transactions would be cleared.
		/// It is also important to force the KeyManagers to be reindexed when this is changed by renaming the BlockState Height related property.
		/// </summary>
		public const string ConfirmedTransactionsVersion = "2";

		public const uint ProtocolVersionWitnessVersion = 70012;

		public const int P2wpkhInputSizeInBytes = 41;
		public const int P2wpkhInputVirtualSize = 68;
		public const int P2pkhInputSizeInBytes = 145;
		public const int OutputSizeInBytes = 33;

		// https://en.bitcoin.it/wiki/Bitcoin
		// There are a maximum of 2,099,999,997,690,000 Bitcoin elements (called satoshis), which are currently most commonly measured in units of 100,000,000 known as BTC. Stated another way, no more than 21 million BTC can ever be created.
		public const long MaximumNumberOfSatoshis = 2099999997690000;

		public const decimal MaximumNumberOfBitcoins = 20999999.9769m;

		public const int TwentyMinutesConfirmationTarget = 2;
		public const int OneDayConfirmationTarget = 144;
		public const int SevenDaysConfirmationTarget = 1008;

		public const int BigFileReadWriteBufferSize = 1 * 1024 * 1024;

		public const int DefaultTorSocksPort = 9050;

		public const int DefaultMainNetBitcoinP2pPort = 8333;
		public const int DefaultTestNetBitcoinP2pPort = 18333;
		public const int DefaultRegTestBitcoinP2pPort = 18444;

		public const int DefaultMainNetBitcoinCoreRpcPort = 8332;
		public const int DefaultTestNetBitcoinCoreRpcPort = 18332;
		public const int DefaultRegTestBitcoinCoreRpcPort = 18443;

		public const double TransactionRBFSignalRate = 0.02; // 2% RBF transactions

		public const decimal DefaultDustThreshold = 0.00005m;

		public const string AlphaNumericCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
		public const string CapitalAlphaNumericCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

		public const string BuiltinBitcoinNodeName = "Bitcoin Knots";

		public static readonly Version ClientVersion = new Version(1, 1, 12, 3);
		public static readonly Version HwiVersion = new Version("1.2.1");
		public static readonly Version BitcoinCoreVersion = new Version("0.20.1");
		public static readonly Version LegalDocumentsVersion = new Version(2, 0);

		public static readonly NodeRequirement NodeRequirements = new NodeRequirement
		{
			RequiredServices = NodeServices.NODE_WITNESS,
			MinVersion = ProtocolVersionWitnessVersion,
			MinProtocolCapabilities = new ProtocolCapabilities { SupportGetBlock = true, SupportWitness = true, SupportMempoolQuery = true }
		};

		public static readonly NodeRequirement LocalNodeRequirements = new NodeRequirement
		{
			RequiredServices = NodeServices.NODE_WITNESS,
			MinVersion = ProtocolVersionWitnessVersion,
			MinProtocolCapabilities = new ProtocolCapabilities { SupportGetBlock = true, SupportWitness = true }
		};

		public static readonly ExtPubKey FallBackCoordinatorExtPubKey = NBitcoinHelpers.BetterParseExtPubKey("xpub6D2PqhWBAbF3xgfaAUW73KnaCXUroArcgMTzNkNzfVX7ykkSzQGbqaXZeaNyxKbZojAAqDwsne6B7NcVhiTrXbGYrQNq1yF76NkgdonGrEa");

		public static readonly string[] UserAgents = new[]
		{
			"/Satoshi:0.20.0/",
			"/Satoshi:0.19.1/",
			"/Satoshi:0.19.0.1/",
			"/Satoshi:0.19.0/",
			"/Satoshi:0.18.1/",
			"/Satoshi:0.18.0/",
			"/Satoshi:0.17.1/",
			"/Satoshi:0.17.0.1/",
			"/Satoshi:0.17.0/",
			"/Satoshi:0.16.3/",
			"/Satoshi:0.16.2/",
			"/Satoshi:0.16.1/",
			"/Satoshi:0.16.0/",
		};

		public static readonly int RangeProofWidth = (int)Math.Log2(MaximumNumberOfSatoshis);

		public static Money MinimumCredentailAmount = Money.Satoshis(10_000);

		public static Money MaximumCredentailAmount = Money.Satoshis(MaximumNumberOfSatoshis);

		public static string ClientSupportBackendVersionText => ClientSupportBackendVersionMin == ClientSupportBackendVersionMax
				? ClientSupportBackendVersionMin
				: $"{ClientSupportBackendVersionMin} - {ClientSupportBackendVersionMax}";
	}
}
