using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ReactiveUI;
using Splat;
using WalletWasabi.Fluent.Behaviors;
using WalletWasabi.Fluent.ViewModels;
using WalletWasabi.Gui;
using WalletWasabi.Gui.CrashReport;
using WalletWasabi.Helpers;
using WalletWasabi.Logging;
using WalletWasabi.Services.Terminate;
using WalletWasabi.Wallets;
using Global = WalletWasabi.Gui.Global;
using MainWindow = WalletWasabi.Fluent.Views.MainWindow;

namespace WalletWasabi.Fluent
{
	public class App : Application
	{
		private Global? _global;
		private readonly TerminateService _terminateService;
		private readonly CrashReporter _crashReporter;

		public App()
		{
			Name = "Wasabi Wallet";
		}

		public App(TerminateService terminateService) : this()
		{
			_crashReporter = new CrashReporter();
			_terminateService = terminateService;
		}

		public override void Initialize()
		{
			AvaloniaXamlLoader.Load(this);
		}

		private static Global CreateGlobal()
		{
			string dataDir = EnvironmentHelpers.GetDataDir(Path.Combine("WalletWasabi", "Client"));
			Directory.CreateDirectory(dataDir);
			string torLogsFile = Path.Combine(dataDir, "TorLogs.txt");

			var uiConfig = new UiConfig(Path.Combine(dataDir, "UiConfig.json"));
			uiConfig.LoadOrCreateDefaultFile();
			var config = new Config(Path.Combine(dataDir, "Config.json"));
			config.LoadOrCreateDefaultFile();
			config.CorrectMixUntilAnonymitySet();
			var walletManager = new WalletManager(config.Network, new WalletDirectories(dataDir));

			return new Global(dataDir, torLogsFile, config, uiConfig, walletManager);
		}

		private async Task InitialiseAsync()
		{
			try
			{
				_global = await Task.Run(CreateGlobal);
				Locator.CurrentMutable.RegisterConstant(_global);
				Locator.CurrentMutable.RegisterConstant(_crashReporter);

				await _global.InitializeNoWalletAsync(_terminateService);

				MainViewModel.Instance!.Initialize(_global);

				Dispatcher.UIThread.Post(GC.Collect);
			}
			catch (Exception ex)
			{
				// There is no other way to stop the creation of the WasabiWindow, we have to exit the application here instead of return to Main.
				TerminateAppAndHandleException(ex, true);
			}
		}

		public override void OnFrameworkInitializationCompleted()
		{
			AutoBringIntoViewExtension.Initialise();

			if (!Design.IsDesignMode)
			{
				MainViewModel.Instance = new MainViewModel();

				if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
				{
					desktop.MainWindow = new MainWindow
					{
						DataContext = MainViewModel.Instance
					};
				}

				RxApp.MainThreadScheduler.Schedule(
					async () =>
					{
						await InitialiseAsync();
					});
			}

			base.OnFrameworkInitializationCompleted();
		}

		private void TerminateAppAndHandleException(Exception? ex, bool runGui)
		{
			if (ex is OperationCanceledException)
			{
				Logger.LogDebug(ex);
			}
			else if (ex is { })
			{
				Logger.LogCritical(ex);
				if (runGui)
				{
					_crashReporter.SetException(ex);
				}
			}

			_terminateService.Terminate(ex is { } ? 1 : 0);
		}
	}
}
