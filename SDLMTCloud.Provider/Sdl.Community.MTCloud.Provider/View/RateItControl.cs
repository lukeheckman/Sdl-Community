﻿using System.Windows.Forms;
using Sdl.Community.MTCloud.Provider.Interfaces;
using Sdl.Community.MTCloud.Provider.Service;
using Sdl.Community.MTCloud.Provider.ViewModel;
using Sdl.Desktop.IntegrationApi.Interfaces;
using Sdl.TranslationStudioAutomation.IntegrationApi;
using Sdl.TranslationStudioAutomation.IntegrationApi.Internal;

namespace Sdl.Community.MTCloud.Provider.View
{
	public partial class RateItControl : UserControl, IUIControl
	{
		private RateItView _rateItWindow;
		public IRatingService RatingService { get; private set; }

		public RateItControl()
		{
			InitializeComponent();
			LoadDataContext();
		}

		public void FocusFeedbackTextBox()
		{
			_rateItWindow.FocusFeedbackTextBox();
		}

		private void LoadDataContext()
		{
			var versionService = new VersionService();
			var shortcutService = new ShortcutService(versionService);
			var actionProvider = new ActionProvider();
			var messageBoxService = new MessageBoxService();

			var editorController = MtCloudApplicationInitializer.EditorController;
			var segmentSupervisor = new SegmentSupervisor(editorController);

			var rateItViewModel = new RateItViewModel(shortcutService, actionProvider, segmentSupervisor, messageBoxService,
				editorController);
			_rateItWindow = new RateItView
			{
				DataContext = rateItViewModel
			};

			RatingService = rateItViewModel;

			rateItElementHost.Child = _rateItWindow;
		}
	}
}
