using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using WalletWasabi.Fluent.ViewModels.Dialogs;
using WalletWasabi.Gui.ViewModels;

namespace WalletWasabi.Fluent.ViewModels.Navigation
{
	public abstract class RoutableViewModel : ViewModelBase, IRoutableViewModel
	{
		private bool _isBusy;
		private CompositeDisposable? _currentDisposable;
		public NavigationTarget CurrentTarget { get; private set; }

		public virtual NavigationTarget DefaultTarget => NavigationTarget.HomeScreen;

		protected RoutableViewModel(NavigationStateViewModel navigationState)
		{
			NavigationState = navigationState;

			BackCommand = ReactiveCommand.Create(GoBack);

			CancelCommand = ReactiveCommand.Create(ClearNavigation);
		}

		public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);

		public IScreen HostScreen { get; set; }

		public bool IsBusy
		{
			get => _isBusy;
			set => this.RaiseAndSetIfChanged(ref _isBusy, value);
		}

		public NavigationStateViewModel NavigationState { get; }

		public ICommand? NextCommand { get; protected set; }

		public ICommand BackCommand { get; protected set; }

		public ICommand CancelCommand { get; protected set; }

		private void DoNavigateTo(bool inStack)
		{
			if (_currentDisposable is { })
			{
				throw new Exception("Cant navigate to something that has already been navigated to.");
			}

			_currentDisposable = new CompositeDisposable();

			OnNavigatedTo(inStack, _currentDisposable);
		}

		protected virtual void OnNavigatedTo(bool inStack, CompositeDisposable disposable)
		{
		}

		private void DoNavigateFrom()
		{
			OnNavigatedFrom();

			if (_currentDisposable is null)
			{
				throw new Exception("Cant navigate from something that hasnt been navigated to.");
			}
			_currentDisposable?.Dispose();
			_currentDisposable = null;
		}

		protected virtual void OnNavigatedFrom()
		{
		}

		public async Task<TResult> NavigateDialog<TResult>(DialogViewModelBase<TResult> dialog)
			=> await NavigateDialog(dialog, CurrentTarget);

		public async Task<TResult> NavigateDialog<TResult>(DialogViewModelBase<TResult> dialog, NavigationTarget target)
		{
			TResult result;

			using (NavigateTo(dialog, target))
			{
				result = await dialog.GetDialogResultAsync();
			}

			return result;
		}

		public IDisposable NavigateTo(RoutableViewModel viewModel, bool resetNavigation = false) =>
			NavigateTo(viewModel, CurrentTarget, resetNavigation);

		public IDisposable NavigateTo(RoutableViewModel viewModel, NavigationTarget navigationTarget, bool resetNavigation = false)
		{
			if (navigationTarget == NavigationTarget.Default)
			{
				navigationTarget = DefaultTarget;
			}

			switch (navigationTarget)
			{
				case NavigationTarget.Default:
				case NavigationTarget.HomeScreen:
					{
						NavigateToHomeScreen(viewModel, resetNavigation);
					}
					break;

				case NavigationTarget.DialogScreen:
					{
						NavigateToDialogScreen(viewModel, resetNavigation);
					}
					break;

				case NavigationTarget.DialogHost:
					if (viewModel is DialogViewModelBase dialog)
					{
						NavigateToDialogHost(dialog);
					}
					break;

				default:
					break;
			}

			return Disposable.Create(()=>GoBack());
		}

		private void NavigateToHomeScreen(RoutableViewModel viewModel, bool resetNavigation)
		{
			viewModel.CurrentTarget = NavigationTarget.HomeScreen;

			if (NavigationState.HomeScreen().Router.GetCurrentViewModel() is RoutableViewModel rvm)
			{
				rvm.DoNavigateFrom();
			}

			var command = resetNavigation ?
				NavigationState.HomeScreen().Router.NavigateAndReset :
				NavigationState.HomeScreen().Router.Navigate;

			var inStack = NavigationState.HomeScreen().Router.NavigationStack.Contains(viewModel);

			command.Execute(viewModel);

			viewModel.DoNavigateTo(inStack);
		}

		private void NavigateToDialogScreen(RoutableViewModel viewModel, bool resetNavigation)
		{
			viewModel.CurrentTarget = NavigationTarget.DialogScreen;

			if (NavigationState.DialogScreen().Router.GetCurrentViewModel() is RoutableViewModel rvm)
			{
				rvm.DoNavigateFrom();
			}

			var command = resetNavigation ?
				NavigationState.DialogScreen().Router.NavigateAndReset :
				NavigationState.DialogScreen().Router.Navigate;

			var inStack = NavigationState.DialogScreen().Router.NavigationStack.Contains(viewModel);

			command.Execute(viewModel);

			viewModel.DoNavigateTo(inStack);
		}

		private void NavigateToDialogHost(DialogViewModelBase dialog)
		{
			dialog.CurrentTarget = NavigationTarget.DialogHost;

			if (NavigationState.DialogHost() is IDialogHost dialogHost)
			{
				if (dialogHost.CurrentDialog is RoutableViewModel rvm)
				{
					rvm.DoNavigateFrom();
				}

				dialogHost.CurrentDialog = dialog;

				dialog.DoNavigateTo(false);
			}
		}

		public void NavigateToSelf() => NavigateToSelf(NavigationTarget.Default);

		public void NavigateToSelf(NavigationTarget target) => NavigateTo(this, target, resetNavigation: false);

		public void NavigateToSelfAndReset(NavigationTarget target) => NavigateTo(this, target, resetNavigation: true);

		private RoutingState? GetRouter()
		{
			var router = default(RoutingState);

			switch (CurrentTarget)
			{
				case NavigationTarget.HomeScreen:
					router = NavigationState.HomeScreen.Invoke().Router;
					break;

				case NavigationTarget.DialogScreen:
					router = NavigationState.DialogScreen.Invoke().Router;
					break;
			}

			return router;
		}

		private void ClearStack(IEnumerable<IRoutableViewModel> navigationStack)
		{
			foreach (var routable in navigationStack)
			{
				// Close all dialogs so the awaited tasks can complete.
				// - DialogViewModelBase.ShowDialogAsync()
				// - DialogViewModelBase.GetDialogResultAsync()
				if (routable is DialogViewModelBase dialog)
				{
					dialog.IsDialogOpen = false;
				}

				if (routable is RoutableViewModel rvm)
				{
					rvm.DoNavigateFrom();
				}
			}
		}

		public void GoBack()
		{
			var router = GetRouter();
			if (router is not null && router.NavigationStack.Count >= 1)
			{
				// Close all dialogs so the awaited tasks can complete.
				// - DialogViewModelBase.ShowDialogAsync()
				// - DialogViewModelBase.GetDialogResultAsync()
				if (router.NavigationStack.LastOrDefault() is DialogViewModelBase dialog)
				{
					dialog.IsDialogOpen = false;
				}

				if (router.NavigationStack.LastOrDefault() is RoutableViewModel rvmFrom)
				{
					rvmFrom.DoNavigateFrom();
				}

				router.NavigateBack.Execute();

				if (router.NavigationStack.LastOrDefault() is RoutableViewModel rvmTo)
				{
					rvmTo.DoNavigateTo(true);
				}
			}
		}

		private void ClearNavigation(NavigationTarget navigationTarget)
		{
			var router = GetRouter();
			if (router is not null)
			{
				if (router.NavigationStack.Count >= 1)
				{
					var navigationStack = router.NavigationStack.ToList();

					router.NavigationStack.Clear();

					ClearStack(navigationStack);
				}
			}
		}

		public void ClearNavigation() => ClearNavigation(CurrentTarget);
	}
}