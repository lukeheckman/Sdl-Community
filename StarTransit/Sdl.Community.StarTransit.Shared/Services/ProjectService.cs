﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using Sdl.Community.StarTransit.Shared.Import;
using Sdl.Community.StarTransit.Shared.Models;
using Sdl.Community.StarTransit.Shared.Services.Interfaces;
using Sdl.Community.StarTransit.Shared.Utils;
using Sdl.Core.Globalization;
using Sdl.ProjectAutomation.Core;
using Sdl.ProjectAutomation.FileBased;
using Sdl.ProjectAutomation.Settings;
using Sdl.TranslationStudioAutomation.IntegrationApi;
using TaskStatus = Sdl.ProjectAutomation.Core.TaskStatus;

namespace Sdl.Community.StarTransit.Shared.Services
{
	public class ProjectService
	{
		private readonly List<StarTranslationMemoryMetadata> _penaltiesTmsList;
		private readonly List<StarTranslationMemoryMetadata> _machineTransList;
		private TranslationProviderConfiguration _tmConfig;
		private MessageModel _messageModel;
		private readonly ProjectsController _projectsController;
		private readonly List<ProjectFile> _targetProjectFiles;
		private readonly string _iconPath;
		private readonly IFileService _fileService;
		private readonly Logger _logger = LogManager.GetCurrentClassLogger();

		public ProjectService(Helpers helpers):this()
		{
			if (helpers == null) return;
			_projectsController = helpers.GetProjectsController();
			_iconPath = string.IsNullOrEmpty(_iconPath) ? helpers.GetIconPath() : _iconPath;
		}

		public ProjectService()
		{
			_fileService = new FileService();
			_messageModel = new MessageModel();
			_penaltiesTmsList = new List<StarTranslationMemoryMetadata>();
			_machineTransList = new List<StarTranslationMemoryMetadata>();
			_tmConfig = new TranslationProviderConfiguration();
			_targetProjectFiles = new List<ProjectFile>();
		}

		public virtual IProject CreateNewProject(ProjectInfo projectInfo, ProjectTemplateReference projectTemplateReference)
		{
			if(projectInfo is null)
			{
				return null;
			}
			var newProject = new FileBasedProject(projectInfo, projectTemplateReference);
			return newProject;
		}

		public virtual MessageModel UpdateProjectSettings(IProject project)
		{
			var fileBasedProject = (FileBasedProject)project;
			UpdateTmSettings(project);
			fileBasedProject.UpdateTranslationProviderConfiguration(_tmConfig);

			project.RunAutomaticTask(_targetProjectFiles?.GetIds(), AutomaticTaskTemplateIds.Scan);
			var taskSequence = fileBasedProject.RunAutomaticTasks(_targetProjectFiles?.GetIds(), new[]
			{
					AutomaticTaskTemplateIds.ConvertToTranslatableFormat,
					AutomaticTaskTemplateIds.CopyToTargetLanguages,
					AutomaticTaskTemplateIds.PreTranslateFiles,
			});

			if (taskSequence.Status.Equals(TaskStatus.Failed))
			{
				_messageModel.IsProjectCreated = false;
				foreach (var subTask in taskSequence.SubTasks)
				{
					_logger.Error($"Name:{subTask.Name}");

					foreach (var messages in subTask.Messages)
					{
						_logger.Error($"Exception: {messages?.Exception}");
						_logger.Error($"Message: {messages?.Message}");
					}
				}
				_messageModel.Message = "Project could not be created.Error occured while running automatic tasks!";
				_messageModel.Title = "Informative message";
				return _messageModel;
			}
			fileBasedProject.Save();
			return _messageModel;
		}

		public (MessageModel, IProject) CreateProject(PackageModel package)
		{
			var target = _fileService.GetStudioTargetLanguages(package.LanguagePairs);

			var projectInfo = new ProjectInfo
			{
				Name = package.Name,
				LocalProjectFolder = package.Location,
				SourceLanguage = new Language(package.LanguagePairs[0].SourceLanguage),
				TargetLanguages = target,
				DueDate = package.DueDate,
				ProjectOrigin = "Star Transit project",
				IconPath = _iconPath
			};

			var newProject = CreateNewProject(projectInfo, new ProjectTemplateReference(package.ProjectTemplate.Uri));

			if (package.Customer != null)
			{
				((FileBasedProject) newProject).SetCustomer(package.Customer);
			}

			//Add StarTransit package source files. The same on all language pairs
			newProject.AddFiles(package.LanguagePairs[0].SourceFile.ToArray());

			//set the file role(user to display project details in Studio view)
			var sourceFilesIds = newProject.GetSourceLanguageFiles().GetIds();
			newProject.SetFileRole(sourceFilesIds, FileRole.Translatable);
			_tmConfig = newProject.GetTranslationProviderConfiguration();

			_messageModel = SetLanguagePairInformation(newProject, package);

			if (_messageModel?.Message is null)
			{
				if (Directory.Exists(newProject?.GetProjectInfo()?.LocalProjectFolder))
				{
					CreateMetadataFolder(package.Location, package.PathToPrjFile);
					_projectsController?.RefreshProjects();
				}

				if (_messageModel != null)
				{
					_messageModel.IsProjectCreated = true;
					_messageModel.Message = "Project was successfully created!";
					_messageModel.Title = "Informative message";
				}
			}

			return (_messageModel, newProject);
		}

		private MessageModel SetLanguagePairInformation(
			IProject newProject,
			PackageModel package)
		{
			if (package?.LanguagePairs != null)
			{
				foreach (var pair in package.LanguagePairs)
				{
					if (!pair.TargetFile.Any() || pair.TargetFile.Count == 0)
					{
						_messageModel.IsProjectCreated = false;
						_messageModel.Message =
							"Project was not created correctly because no target files were found in the package!";
						_messageModel.Title = "Informative message";
						return _messageModel;
					}

					if (pair.CreateNewTm)
					{
						//TODO:Investigate and refactor
						foreach (var starTmMetadata in pair.StarTranslationMemoryMetadatas)
						{
							AddTmPenalties(package, starTmMetadata);
							AddMtMemories(package, starTmMetadata);
						}

						//TODO: Investigate if we really need this later we'll join the mt and tms becasue
						// Remove found items from pair.StarTranslationMemoryMetadatas (the remained ones are those which does not have penalties set on them)
						foreach (var item in _penaltiesTmsList)
						{
							pair.StarTranslationMemoryMetadatas.Remove(item);
						}

						// Remove Machine Translation memories from pair.StarTranslationMemoryMetadatas, if the user requests them, they will be imported separately, but never in the main TM
						pair.StarTranslationMemoryMetadatas.RemoveAll(item =>
							Path.GetFileName(item?.TargetFile ?? "").Contains("_AEXTR_MT_"));
					}

					_targetProjectFiles?.Clear();

					// Import language pair TM if any
					ImportLanguagePairTm(pair, newProject,package);

					_targetProjectFiles?.AddRange(newProject.AddFiles(pair.TargetFile.ToArray()));
					_messageModel = UpdateProjectSettings(newProject);
				}
				_projectsController?.RefreshProjects();
			}
			return _messageModel;
		}
		
		private void ImportLanguagePairTm(LanguagePair pair, IProject project, PackageModel package)
		{
			if (!pair.HasTm || string.IsNullOrEmpty(pair.TmPath)) return;
			if (pair.StarTranslationMemoryMetadatas !=null && pair.StarTranslationMemoryMetadatas.Any())
			{
				var localProjectFolder = project?.GetProjectInfo()?.LocalProjectFolder;
				if (localProjectFolder != null)
				{
					var newTmPath = Path.Combine(localProjectFolder, Path.GetFileName(pair.TmPath));
					var importer = new TransitTmImporter(pair, newTmPath,null);

					importer.ImportStarTransitTm(pair.StarTranslationMemoryMetadatas, package);
					var providerRef = importer.GetTranslationProviderReference(newTmPath,pair);
					_logger.Info($"-->Import lang pair Provider Reference:{providerRef?.Uri}");
					if(providerRef==null)return;
					//var test = new TranslationProviderCascadeEntry(providerRef, true, true, true);
					_tmConfig.Entries.Add(new TranslationProviderCascadeEntry(providerRef, true, true, true));
				}
			}
			
			CreateSeparateTms(pair, project, package);
		}

		// Create separate TM for each TM file on which user set penalty. The penalty is applied on top of any penalty that might be applied by the translation provider itself.
		// The name of the new TM will be the same with the one from StarTransit package
		// We'll create a single project which will contain all the tms and mt.
		private void CreateSeparateTms(LanguagePair pair, IProject project, PackageModel package)
		{
			var allTransitTms = _penaltiesTmsList?.Concat(_machineTransList).ToList();

			if (allTransitTms != null && !allTransitTms.Any()) return;
			var tpReference = CreateTpReference(allTransitTms, pair, project, package);
			foreach (var tpRef in tpReference)
			{
				_tmConfig.Entries.Add(new TranslationProviderCascadeEntry(tpRef.Key, true, true, true,tpRef.Value));
			}
		}

		private Dictionary<TranslationProviderReference, int> CreateTpReference(List<StarTranslationMemoryMetadata> tmsList, LanguagePair pair, IProject project, PackageModel package)
		{
			var localProjectPath = project?.GetProjectInfo()?.LocalProjectFolder;
			var importer = new TransitTmImporter(pair, localProjectPath, tmsList);
			importer.ImportStarTransitTm(tmsList, package);
			var translationProvRef = new Dictionary<TranslationProviderReference, int>();

			foreach (var tm in importer.StudioTranslationMemories)
			{
				var provider = new TranslationProviderReference(tm.Key.FilePath);
				if (!translationProvRef.ContainsKey(provider))
				{
					translationProvRef.Add(provider,tm.Value);
				}
			}
			return translationProvRef;
		}
		
		// Separate all items from package.TMPenalties(files that are having penalties set), that are found in pair.StarTranslationMemoryMetadatas
		private void AddTmPenalties(PackageModel package, StarTranslationMemoryMetadata starTmMetadata)
		{
			if (package?.TMPenalties == null) return;
			if (package.TMPenalties.Any(t => t.Key.Equals(starTmMetadata.TargetFile)))
			{
				starTmMetadata.TMPenalty = package.TMPenalties.FirstOrDefault(t => t.Key.Equals(starTmMetadata.TargetFile)).Value;
				_penaltiesTmsList.Add(starTmMetadata);
			}
		}

		//Separate all items from package.MachineTransMem (files that contain Machine Translation)
		private void AddMtMemories(PackageModel package, StarTranslationMemoryMetadata starTmMetadata)
		{
			if (package?.MTMemories == null) return;
			var hasMtMemories = package.MTMemories.Any(t => t.Equals(starTmMetadata.TargetFile));
			if (!hasMtMemories) return;
			starTmMetadata.TMPenalty = 1;
			_machineTransList.Add(starTmMetadata);
		}

		// Update the translation memory settings
		private void UpdateTmSettings(IProject project)
		{
			var settings = project.GetSettings();
			var updateTmSettings = settings.GetSettingsGroup<TranslationMemoryUpdateTaskSettings>();
			if (updateTmSettings != null)
			{
				updateTmSettings.TmImportOptions.Value = TmImportOption.AlwaysAddNewTranslation;
				updateTmSettings.UpdateWithApprovedSignOffSegments.Value = true;
				updateTmSettings.UpdateWithApprovedTranslationSegments.Value = true;
				updateTmSettings.UpdateWithTranslatedSegments.Value = true;
				project.UpdateSettings(settings);
			}
		}

		/// <summary>
		/// Creates a folder named "StarTransitMetadata"and save the PRJ file in it.
		/// </summary>
		private void CreateMetadataFolder(string studioProjectPath, string prjFilePath)
		{
			try
			{
				var starTransitMetadataFolderPath = Path.Combine(studioProjectPath, "StarTransitMetadata");

				Directory.CreateDirectory(starTransitMetadataFolderPath);
				
				var prjFileName = Path.GetFileName(prjFilePath);
				if (prjFileName != null)
				{
					File.Copy(prjFilePath, Path.Combine(starTransitMetadataFolderPath, prjFileName), true);
				}
			}
			catch (Exception ex)
			{
				_logger.Error($"{ex.Message}\n {ex.StackTrace}");
			}
		}
	}
}