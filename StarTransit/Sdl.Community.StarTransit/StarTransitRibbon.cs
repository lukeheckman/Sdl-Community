﻿using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NLog;
using Sdl.Community.StarTransit.Shared.Interfaces;
using Sdl.Community.StarTransit.Shared.Models;
using Sdl.Community.StarTransit.Shared.Services;
using Sdl.Community.StarTransit.UI;
using Sdl.Community.StarTransit.UI.Controls;
using Sdl.Community.StarTransit.UI.Helpers;
using Sdl.Desktop.IntegrationApi;
using Sdl.Desktop.IntegrationApi.Extensions;
using Sdl.TranslationStudioAutomation.IntegrationApi;
using Sdl.TranslationStudioAutomation.IntegrationApi.Presentation.DefaultLocations;

namespace Sdl.Community.StarTransit
{
	[RibbonGroup("Sdl.Community.StarTransit", Name = "StarTransit", ContextByType = typeof(ProjectsController))]
	[RibbonGroupLayout(LocationByType = typeof(TranslationStudioDefaultRibbonTabs.HomeRibbonTabLocation))]
	public class StarTransitRibbon : AbstractRibbonGroup
	{
	}

	[Action("Sdl.Community.StarTransit", Name = "Open StarTransit Package", Icon = "open_package", Description = "Open a StarTransit package")]
	[ActionLayout(typeof(StarTransitRibbon), 20, DisplayType.Large)]
	public class StarTransitOpenPackageAction : AbstractAction
	{
		private IMessageBoxService _messageBoxService;
		private readonly Logger _logger = LogManager.GetCurrentClassLogger();

		protected override async void Execute()
		{
			_messageBoxService = new MessageBoxService();
			Utils.EnsureApplicationResources();

			var pathToTempFolder = CreateTempPackageFolder();
			try
			{
				var fileDialog = new OpenFileDialog
				{
					Filter = "Transit Project Package Files (*.ppf)|*.ppf"
				};
				var dialogResult = fileDialog.ShowDialog();
				if (dialogResult == DialogResult.OK)
				{
					var path = fileDialog.FileName;	
					var packageService = new PackageService();
					var package = await packageService.OpenPackage(path, pathToTempFolder);

					var templateService = new TemplateService();
					var templateList = templateService.LoadProjectTemplates();

					var packageModel = new PackageModel
					{
						Name = package.Name,
						Description = package.Description,
						StudioTemplates = templateList,
						LanguagePairs = package.LanguagePairs,
						PathToPrjFile = package.PathToPrjFile
					};

					// Start BackgroundWorker in InitializeMain method to have app working separately than Trados Studio process
					Program.InitializeMain(packageModel);
				}
			}
			catch (Exception ex)
			{
				_messageBoxService.ShowMessage(ex.Message, string.Empty);
				_logger.Error($"{ex.Message}\n {ex.StackTrace}");
			}
		}  
		
		/// <summary>
		/// We need to delete all the files from subfolders before deleteing the main directory. Otherwise sometimes it throws an error
		/// </summary>
		private string CreateTempPackageFolder()
		{
			var tempFolder = $@"C:\Users\{Environment.UserName}\StarTransit";
			var pathToTempPackageFolder = Path.Combine(tempFolder, Guid.NewGuid().ToString());

			//Delete existing folder where we extract transit packages
			try
			{
				if (Directory.Exists(tempFolder))
				{
					DeleteDirectory(tempFolder);
				}
				Directory.CreateDirectory(pathToTempPackageFolder);
			}
			catch (IOException)
			{
				Directory.Delete(tempFolder, true);
			}
			catch (UnauthorizedAccessException)
			{
				Directory.Delete(tempFolder, true);
			}
			catch (Exception e)
			{
				_logger.Error(e);
			}

			return pathToTempPackageFolder;
		}

		private void DeleteDirectory(string directoryPath)
		{
			//Delete all files from folder before deleting the folder
			var files = Directory.GetFiles(directoryPath);
			var dirs = Directory.GetDirectories(directoryPath);

			foreach (var file in files)
			{
				File.SetAttributes(file, FileAttributes.Normal);
				File.Delete(file);
			}

			foreach (var dir in dirs)
			{
				DeleteDirectory(dir);
			}
			Directory.Delete(directoryPath, false);
		}
	}

	[Action("Sdl.Community.StarTransit.Return", Name = "StarTransit return package", Icon = "return_package", Description = "StarTransit return package")]
	[ActionLayout(typeof(StarTransitRibbon), 20, DisplayType.Large)]

	public class ReturnPackageAction : AbstractAction
	{
		private IMessageBoxService _messageBoxService;

		protected override void Execute()
		{
			_messageBoxService = new MessageBoxService();
			Utils.EnsureApplicationResources();

			var returnService = new ReturnPackageService();
			var returnPackage = returnService.GetReturnPackage();

			if (!string.IsNullOrEmpty(returnPackage?.Item2))
			{
				_messageBoxService.ShowWarningMessage(returnPackage.Item2, "Warning");
			}
			else if (returnPackage?.Item1?.FileBasedProject != null && returnPackage?.Item1?.TargetFiles.Count > 0)
			{
				var xliffFiles = returnPackage?.Item1?.TargetFiles?.Any(file => file.Name.EndsWith(".sdlxliff"));
				if (xliffFiles.Value)
				{
					var window = new ReturnPackageMainWindow(returnPackage?.Item1);
					window.ShowDialog();
				}
				else
				{
					_messageBoxService.ShowWarningMessage("The target file(s) has already been returned. In order to repeat the process, you need to revert to .sdlxliff(s) from the Files view.", "Warning");
				}
			}
		}
	}

	[Action("Sdl.Community.StarTransit.Contribute", Name = "Contribute to project", Icon = "opensourceimage", Description = "Contribute to project")]
	[ActionLayout(typeof(StarTransitRibbon), 20, DisplayType.Large)]
	public class ContributeToProjectAction : AbstractAction
	{
		protected override void Execute()
		{
			System.Diagnostics.Process.Start("https://github.com/sdl/Sdl-Community/tree/master/StarTransit");
		}
	}

	[Action("Sdl.Community.StarTransit.Help", Name = "Help", Icon = "help_icon", Description = "Help")]
	[ActionLayout(typeof(StarTransitRibbon), 20, DisplayType.Large)]
	public class HelpLinkAction : AbstractAction
	{
		protected override void Execute()
		{
			System.Diagnostics.Process.Start("https://community.sdl.com/product-groups/translationproductivity/w/customer-experience/3270.star-transit");
		}
	}
}