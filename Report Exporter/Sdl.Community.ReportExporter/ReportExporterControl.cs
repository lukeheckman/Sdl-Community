﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using Sdl.Community.ReportExporter.Helpers;
using Sdl.Community.ReportExporter.Interfaces;
using Sdl.Community.ReportExporter.Model;
using Sdl.Community.ReportExporter.Service;
using static System.String;
using Help = Sdl.Community.ReportExporter.Helpers.Help;

namespace Sdl.Community.ReportExporter
{
	public partial class ReportExporterControl : Form
	{
		public static readonly Log Log = Log.Instance;

		private string _projectXmlPath;
		private OptionalInformation _optionalInformation;
		private List<ProjectDetails> _allStudioProjectsDetails;
		private readonly BindingList<LanguageDetails> _languages = new BindingList<LanguageDetails>();
		private BindingList<ProjectDetails> _projectsDataSource = new BindingList<ProjectDetails>();
		private readonly IMessageBoxService _messageBoxService;
		private bool _areExternalStudioProjects;

		public ReportExporterControl()
		{
			InitializeComponent();
			InitializeSettings();
			_messageBoxService = new MessageBoxService();
		}

		public ReportExporterControl(List<string> studioProjectsPath)
		{
			InitializeComponent();
			InitializeSettings();
			_messageBoxService = new MessageBoxService();

			foreach (var path in studioProjectsPath)
			{
				var selectedProject = _projectsDataSource.FirstOrDefault(p => p.ProjectPath.Equals(path));
				if (selectedProject != null)
				{
					PrepareProjectToExport(selectedProject);
				}
			}
			RefreshProjectsListBox();
		}

		private void InitializeSettings()
		{
			_areExternalStudioProjects = false;
			copyBtn.Enabled = false;
			csvBtn.Enabled = false;
			targetBtn.Enabled = false;
			includeHeaderCheck.Checked = true;
			_projectXmlPath = Help.GetStudioProjectsPath();
			_allStudioProjectsDetails = new List<ProjectDetails>();
			LoadProjectsList(_projectXmlPath);

			_optionalInformation = new OptionalInformation
			{
				IncludeAdaptiveBaseline = adaptiveMT.Checked,
				IncludeAdaptiveLearnings = adaptiveLearnings.Checked,
				IncludeInternalFuzzies = internalFuzzies.Checked,
				IncludeContextMatch = contextMatch.Checked,
				IncludeCrossRep = crossRep.Checked,
				IncludeLocked = locked.Checked,
				IncludePerfectMatch = perfectMatch.Checked
			};
		}

		private void FillLanguagesList()
		{
			try
			{
				var selectedProjectsToExport = _projectsDataSource.Where(e => e.ShouldBeExported).ToList();

				foreach (var selectedProject in selectedProjectsToExport)
				{
					foreach (var language in selectedProject.LanguagesForPoject.ToList())
					{
						var languageDetails = _languages.FirstOrDefault(n => n.LanguageName.Equals(language.Key));
						if (languageDetails == null)
						{
							var newLanguage = new LanguageDetails
							{
								LanguageName = language.Key,
								IsChecked = false
							};
							_languages.Add(newLanguage);
						}

					}
				}
				languagesListBox.DataSource = _languages;
				languagesListBox.DisplayMember = "LanguageName";
				languagesListBox.ValueMember = "IsChecked";
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"FillLanguagesList method: {ex.Message}\n {ex.StackTrace}");
			}
		}

		/// <summary>
		/// Reads studio projects from project.xml
		/// Adds projects to listbox
		/// </summary>
		private void LoadProjectsList(string projectXmlPath)
		{
			try
			{
				var projectXmlDocument = new XmlDocument();
				if (!string.IsNullOrEmpty(projectXmlPath))
				{
					projectXmlDocument.Load(projectXmlPath);

					var projectsNodeList = projectXmlDocument.SelectNodes("//ProjectListItem");
					if (projectsNodeList == null) return;
					foreach (var item in projectsNodeList)
					{
						var projectInfo = ((XmlNode)item).SelectSingleNode("./ProjectInfo");
						if (projectInfo?.Attributes != null && projectInfo.Attributes["IsInPlace"].Value != "true")
						{
							var reportExist = ReportFolderExist((XmlNode)item);
							if (reportExist)
							{
								var projectDetails = CreateProjectDetails((XmlNode)item);
								_projectsDataSource.Add(projectDetails);
								_allStudioProjectsDetails.Add(projectDetails);
							}
						}
					}
					projListbox.DataSource = _projectsDataSource;
					projListbox.ValueMember = "ShouldBeExported";
					projListbox.DisplayMember = "ProjectName";
				}
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"LoadProjectsList method: {ex.Message}\n {ex.StackTrace}");
			}
		}

		private bool ReportFolderExist(XmlNode projectInfoNode)
		{
			try
			{
				if (projectInfoNode?.Attributes != null)
				{
					var filePath = Empty;

					if (projectInfoNode.Attributes["ProjectFilePath"] != null)
					{
						filePath = projectInfoNode.Attributes["ProjectFilePath"].Value;
						if (!Path.IsPathRooted(filePath))
						{
							//project is located inside "Projects" folder in Studio
							var projectsFolderPath = _projectXmlPath.Substring
								(0, _projectXmlPath.LastIndexOf(@"\", StringComparison.Ordinal) + 1);
							var projectName = filePath.Substring(0, filePath.LastIndexOf(@"\", StringComparison.Ordinal));
							filePath = Path.Combine(projectsFolderPath, projectName, "Reports");
						}
						else
						{
							//is external project
							var reportsPath = filePath.Substring(0, filePath.LastIndexOf(@"\", StringComparison.Ordinal) + 1);
							filePath = Path.Combine(reportsPath, "Reports");
						}
					}
					return Help.ReportFileExist(filePath);
				}
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"ReportFolderExist method: {ex.Message}\n {ex.StackTrace}");
			}
			return false;
		}

		/// <summary>
		/// Creates project details for given project from xml file
		/// </summary>
		/// <param name="projNode"></param>
		/// <returns></returns>
		private ProjectDetails CreateProjectDetails(XmlNode projNode)
		{			
			var projectDetails = new ProjectDetails
			{
				LanguagesForPoject = new Dictionary<string, bool>(),
				ShouldBeExported = false
			};
			var projectFolderPath = Empty;
			var doc = new XmlDocument();

			try
			{
				var selectSingleNode = projNode.SelectSingleNode("ProjectInfo");
				if (selectSingleNode?.Attributes != null)
				{
					projectDetails.ProjectName = selectSingleNode.Attributes["Name"].Value;
				}
				if (projNode.Attributes != null)
				{
					projectFolderPath = projNode.Attributes["ProjectFilePath"].Value;
				}
				if (Path.IsPathRooted(projectFolderPath))
				{
					projectDetails.ProjectPath = projectFolderPath; //location outside standard project place
				}
				else
				{
					var projectsFolderPath = _projectXmlPath.Substring
						(0, _projectXmlPath.LastIndexOf(@"\", StringComparison.Ordinal) + 1);
					projectDetails.ProjectPath = projectsFolderPath + projectFolderPath;
				}
				var projectStatus = ProjectInformation.GetProjectStatus(projectDetails.ProjectPath);

				doc.Load(projectDetails.ProjectPath);

				var projectLanguages = Help.LoadLanguageDirections(doc);

				SetLanguagesForProject(projectDetails, projectLanguages);

				projectDetails.Status = projectStatus;
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"CreateProjectDetails method: {ex.Message}\n {ex.StackTrace}");
			}
			return projectDetails;
		}

		private void SetLanguagesForProject(ProjectDetails project, Dictionary<string, LanguageDirection> languages)
		{
			try
			{
				foreach (var language in languages)
				{
					project.LanguagesForPoject.Add(language.Value.TargetLang.EnglishName, false);
				}
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"SetLanguagesForProject method: {ex.Message}\n {ex.StackTrace}");
			}
		}

		private void projListbox_SelectedIndexChanged(object sender, EventArgs e)
		{
			try
			{
				if (projListbox.SelectedItem == null) return;
				var projectName = ((CheckedListBox)sender).Text;
				var selectedProject = _projectsDataSource.FirstOrDefault(n => n.ProjectName.Equals(projectName));

				var selectedProjectIndex = _projectsDataSource.IndexOf(selectedProject);
				if (selectedProjectIndex > -1)
				{
					var shouldExportProject = ((CheckedListBox)sender).GetItemChecked(selectedProjectIndex);

					if (shouldExportProject)
					{
						PrepareProjectToExport(selectedProject);
					}//that means user deselected a project
					else
					{
						if (selectedProject != null)
						{
							selectedProject.ShouldBeExported = false;
							ShouldUnselectLanguages(selectedProject);
						}
					}
				}
				IsClipboardEnabled();
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"projListbox_SelectedIndexChanged method: {ex.Message}\n {ex.StackTrace}");
			}
		}

		private void ShouldUnselectLanguages(ProjectDetails selectedProject)
		{
			try
			{
				var selectedLanguagesFromProject = selectedProject.LanguagesForPoject.Where(n => n.Value).Select(n => n.Key).ToList();
				var count = 0;
				foreach (var languageName in selectedLanguagesFromProject)
				{
					//unselect language for project in data source list
					selectedProject.LanguagesForPoject[languageName] = false;

					var projectsToBeExported = _projectsDataSource.Where(n => n.LanguagesForPoject.ContainsKey(languageName)
																			  && n.ShouldBeExported).ToList();
					foreach (var project in projectsToBeExported)
					{
						var languageShouldBeExported = project.LanguagesForPoject[languageName];
						if (languageShouldBeExported)
						{
							count++;
						}
					}

					//that means no other project has this language selected so we can uncheck the language ox
					if (count.Equals(0))
					{
						var languageToBeDeleted = _languages.FirstOrDefault(l => l.LanguageName.Equals(languageName));
						if (languageToBeDeleted != null)
						{
							_languages.Remove(languageToBeDeleted);
						}
					}
				}

				// if the are any projects selected clear language list
				if (_projectsDataSource.Count(p => p.ShouldBeExported).Equals(0))
				{
					_languages.Clear();
				}
				RefreshLanguageListbox();
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"ShouldUnselectLanguages method: {ex.Message}\n {ex.StackTrace}");
			}
		}

		private void PrepareProjectToExport(ProjectDetails selectedProject)
		{
			try
			{
				if (selectedProject != null)
				{
					var doc = new XmlDocument();
					var selectedProjectIndex = _projectsDataSource.IndexOf(selectedProject);

					if (selectedProjectIndex > -1)
					{
						//Read sdlproj
						doc.Load(selectedProject.ProjectPath);
						Help.LoadReports(doc, selectedProject.ProjectFolderPath, selectedProject);

						selectedProject.ShouldBeExported = true;
						//if an project has only one language select that language
						if (selectedProject.LanguagesForPoject != null)
						{
							if (selectedProject.LanguagesForPoject.Count.Equals(1))
							{
								var languageName = selectedProject.LanguagesForPoject.First().Key;
								var languageToBeSelected = _languages.FirstOrDefault(n => n.LanguageName.Equals(languageName));
								if (languageToBeSelected != null)
								{
									languageToBeSelected.IsChecked = true;

								}
								else
								{
									var newLanguage = new LanguageDetails
									{
										LanguageName = languageName,
										IsChecked = true
									};
									_languages.Add(newLanguage);
								}
								selectedProject.LanguagesForPoject[languageName] = true;
							}
						}

						var languagesAlreadySelectedForExport = _languages.Where(l => l.IsChecked).ToList();

						foreach (var language in languagesAlreadySelectedForExport)
						{
							if (selectedProject.LanguagesForPoject != null && selectedProject.LanguagesForPoject.ContainsKey(language.LanguageName))
							{
								selectedProject.LanguagesForPoject[language.LanguageName] = true;
							}
						}
						//show languages in language list box
						FillLanguagesList();

						reportOutputPath.Text = selectedProject.ReportPath ?? Empty;

						copyBtn.Enabled = projListbox.SelectedItems.Count == 1;
						if (projListbox.SelectedItems.Count > 0)
						{
							csvBtn.Enabled = true;
						}
						RefreshLanguageListbox();
					}
				}
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"PrepareProjectToExport method: {ex.Message}\n {ex.StackTrace}");
			}
		}

		private void languagesListBox_SelectedIndexChanged_1(object sender, EventArgs e)
		{
			try
			{
				var selectedLanguage = (LanguageDetails)languagesListBox.SelectedItem;
				if (selectedLanguage != null)
				{
					var index = languagesListBox.SelectedIndex;
					var shouldExportLanguage = languagesListBox.GetItemChecked(index);

					var projectsWithSelectedLaguage = _projectsDataSource
						.Where(p => p.ShouldBeExported && p.LanguagesForPoject.ContainsKey(selectedLanguage.LanguageName)).ToList();
					foreach (var project in projectsWithSelectedLaguage)
					{
						var language = project.LanguagesForPoject.FirstOrDefault(l => l.Key.Equals(selectedLanguage.LanguageName));
						project.LanguagesForPoject[language.Key] = shouldExportLanguage;
					}

					var languageToUpdate = _languages.FirstOrDefault(n => n.LanguageName.Equals(selectedLanguage.LanguageName));
					if (languageToUpdate != null)
					{
						languageToUpdate.IsChecked = shouldExportLanguage;
					}
				}

				RefreshLanguageListbox();
				IsClipboardEnabled();
				IsCsvBtnEnabled();
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"languagesListBox_SelectedIndexChanged_1 method: {ex.Message}\n {ex.StackTrace}");
			}
		}

		private void RefreshLanguageListbox()
		{
			try
			{
				for (var i = 0; i < languagesListBox.Items.Count; i++)
				{
					var language = (LanguageDetails)languagesListBox.Items[i];
					languagesListBox.SetItemChecked(i, language.IsChecked);
				}
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"RefreshLanguageListbox method: {ex.Message}\n {ex.StackTrace}");
			}
		}

		private void copyBtn_Click(object sender, EventArgs e)
		{
			try
			{
				var selectedProject = _projectsDataSource.FirstOrDefault(p => p.ShouldBeExported);

				if (selectedProject?.LanguagesForPoject.Count(c => c.Value) > 0)
				{
					var selectedLanguages = selectedProject.LanguagesForPoject.Where(l => l.Value == true);
					foreach (var selectedLanguage in selectedLanguages)
					{
						var languageAnalysisReportPath = selectedProject.LanguageAnalysisReportPaths.FirstOrDefault(l => l.Key.Equals(selectedLanguage.Key));
						var copyReport = new StudioAnalysisReport(languageAnalysisReportPath.Value);

						Clipboard.SetText(copyReport.ToCsv(includeHeaderCheck.Checked, _optionalInformation));
					}
					_messageBoxService.ShowOwnerInformationMessage(this, "Copy to clipboard successful.", "Copy result");
				}
				else
				{
					_messageBoxService.ShowOwnerInformationMessage(this, "Please select at least one language for export", "Copy result");
				}
			}
			catch (Exception exception)
			{
				Console.WriteLine(exception);
				Log.Logger.Error($"copyBtn_Click method: {exception.Message}\n {exception.StackTrace}");
				throw;
			}
		}

		private void IsClipboardEnabled()
		{
			if (_projectsDataSource.Count(p => p.ShouldBeExported) > 1)
			{
				copyBtn.Enabled = false;
			}
			else
			{
				copyBtn.Enabled = true;
			}
		}

		private void IsCsvBtnEnabled()
		{
			csvBtn.Enabled = _projectsDataSource.Count(p => p.ShouldBeExported) >= 1;
		}

		private void includeHeaderCheck_CheckedChanged(object sender, EventArgs e)
		{
		}

		private void exitBtn_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void csvBtn_Click(object sender, EventArgs e)
		{
			GenerateReport();
		}

		private void GenerateReport()
		{
			try
			{
				if (!IsNullOrEmpty(reportOutputPath.Text))
				{
					var projectsToBeExported = _projectsDataSource.Where(p => p.ShouldBeExported).ToList();
					foreach (var project in projectsToBeExported)
					{
						// check which languages to export
						var checkedLanguages = project.LanguagesForPoject.Where(l => l.Value).ToList();
						foreach (var languageReport in checkedLanguages)
						{

							if (project.ReportPath == null)
							{
								project.ReportPath = reportOutputPath.Text;
							}

							//write report to Reports folder
							using (var sw = new StreamWriter(project.ReportPath + Path.DirectorySeparatorChar + project.ProjectName + "_" +
															 languageReport.Key + ".csv"))
							{
								var analyseReportPath = project.LanguageAnalysisReportPaths.FirstOrDefault(l => l.Key.Equals(languageReport.Key));
								var report = new StudioAnalysisReport(analyseReportPath.Value);
								sw.Write(report.ToCsv(includeHeaderCheck.Checked, _optionalInformation));
							}
						}
					}

					//Clear all lists
					UncheckAllProjects();
					_languages.Clear();
					selectAll.Checked = false;
					_messageBoxService.ShowOwnerInformationMessage(this, "Export successful.", "Export result");
				}
				else
				{
					_messageBoxService.ShowOwnerInformationMessage(this, "Please select output path to export reports", string.Empty);
				}

			}
			catch (Exception exception)
			{
				Console.WriteLine(exception);
				Log.Logger.Error($"GenerateReport method: {exception.Message}\n {exception.StackTrace}");
				throw;
			}
		}

		private void UncheckAllProjects()
		{
			var projectsToUncheck = _projectsDataSource.Where(p => p.ShouldBeExported).ToList();
			foreach (var project in projectsToUncheck)
			{
				project.ShouldBeExported = false;
				foreach (var language in project.LanguagesForPoject.ToList())
				{
					project.LanguagesForPoject[language.Key] = false;
				}
			}
			RefreshProjectsListBox();
		}

		private void RefreshProjectsListBox()
		{
			for (var i = 0; i < projListbox.Items.Count; i++)
			{
				var project = (ProjectDetails)projListbox.Items[i];
				projListbox.SetItemChecked(i, project.ShouldBeExported);
				if (project.ShouldBeExported)
				{
					projListbox.SetSelected(i, true);
				}
			}
		}

		private void targetBtn_Click(object sender, EventArgs e)
		{
			if (!IsNullOrEmpty(reportOutputPath.Text))
			{
				Process.Start("explorer.exe", "\"" + reportOutputPath.Text + "\"");
			}
		}

		private void adaptiveMT_CheckedChanged(object sender, EventArgs e)
		{
			_optionalInformation.IncludeAdaptiveBaseline = adaptiveMT.Checked;
		}

		private void fragmentMatches_CheckedChanged(object sender, EventArgs e)
		{
			_optionalInformation.IncludeAdaptiveLearnings = adaptiveLearnings.Checked;
		}

		private void internalFuzzies_CheckedChanged(object sender, EventArgs e)
		{
			_optionalInformation.IncludeInternalFuzzies = internalFuzzies.Checked;
		}

		private void locked_CheckedChanged(object sender, EventArgs e)
		{
			_optionalInformation.IncludeLocked = locked.Checked;
		}

		private void perfectMatch_CheckedChanged(object sender, EventArgs e)
		{
			_optionalInformation.IncludePerfectMatch = perfectMatch.Checked;
		}

		private void contextMatch_CheckedChanged(object sender, EventArgs e)
		{
			_optionalInformation.IncludeContextMatch = contextMatch.Checked;
		}

		private void crossRep_CheckedChanged(object sender, EventArgs e)
		{
			_optionalInformation.IncludeCrossRep = crossRep.Checked;
		}

		private void browseBtn_Click(object sender, EventArgs e)
		{
			var folderPath = new FolderSelectDialog();
			if (folderPath.ShowDialog())
			{
				reportOutputPath.Text = folderPath.FileName;
			}
		}

		private void reportOutputPath_KeyUp(object sender, KeyEventArgs e)
		{
			var reportPath = ((TextBox)sender).Text;
			if (!IsNullOrWhiteSpace(reportPath))
			{
				targetBtn.Enabled = true;
			}
			if (e.KeyCode == Keys.Enter)
			{
				GenerateReport();
			}
		}

		private void projectStatusComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			try
			{
				var selectedStatus = ((ComboBox)sender).SelectedItem;
				var projectsBindingList = new BindingList<ProjectDetails>();
				_languages.Clear();

				var projects = _allStudioProjectsDetails;
				if (selectedStatus.Equals("InProgress"))
				{
					var inProgressProjects = projects.Where(s => s.Status.Equals("InProgress")).ToList();

					foreach (var project in inProgressProjects)
					{
						projectsBindingList.Add(project);
					}

				}
				if (selectedStatus.Equals("Completed"))
				{
					var completedProjects = projects.Where(s => s.Status.Equals("Completed")).ToList();
					foreach (var project in completedProjects)
					{
						projectsBindingList.Add(project);
					}
				}
				if (selectedStatus.Equals("All"))
				{
					foreach (var project in projects)
					{
						projectsBindingList.Add(project);
					}
				}
				_projectsDataSource = projectsBindingList;
				projListbox.DataSource = _projectsDataSource;
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"projectStatusComboBox_SelectedIndexChanged method: {ex.Message}\n {ex.StackTrace}");
			}
		}

		private void loadBtn_Click(object sender, EventArgs e)
		{
			try
			{
				var loadFolderPath = new FolderSelectDialog();
				var doc = new XmlDocument();
				if (loadFolderPath.ShowDialog())
				{
					var externalProjectsBindingList = new BindingList<ProjectDetails>();
					_areExternalStudioProjects = true;
					_languages.Clear();
					_projectsDataSource.Clear();
					var projectsPathList = Directory.GetFiles(loadFolderPath.FileName, "*.sdlproj", SearchOption.AllDirectories);
					foreach (var projectPath in projectsPathList)
					{
						var reportFolderPath = Path.Combine(projectPath.Substring(0, projectPath.LastIndexOf(@"\", StringComparison.Ordinal)), "Reports");
						if (Help.ReportFileExist(reportFolderPath))
						{
							var projectDetails = ProjectInformation.GetExternalProjectDetails(projectPath);

							doc.Load(projectDetails.ProjectPath);
							Help.LoadReports(doc, projectDetails.ProjectFolderPath, projectDetails);
							externalProjectsBindingList.Add(projectDetails);
						}
					}
					foreach (var item in externalProjectsBindingList)
					{
						_projectsDataSource.Add(item);
					}

					projListbox.DataSource = _projectsDataSource;
					RefreshProjectsListBox();
					RefreshLanguageListbox();
				}
			}
			catch (Exception ex)
			{
				Log.Logger.Error($"loadBtn_Click method: {ex.Message}\n {ex.StackTrace}");
			}
		}

		private void clearBtn_Click(object sender, EventArgs e)
		{
			_areExternalStudioProjects = false;

			_projectsDataSource.Clear();
			_languages.Clear();
			foreach (var project in _allStudioProjectsDetails)
			{
				_projectsDataSource.Add(project);
			}

			projListbox.DataSource = _projectsDataSource;
			copyBtn.Enabled = false;
			csvBtn.Enabled = false;
		}

		private void selectAll_CheckedChanged(object sender, EventArgs e)
		{
			var selectAll = ((CheckBox)sender).Checked;

			foreach (var project in _projectsDataSource)
			{
				project.ShouldBeExported = selectAll;
				foreach (var language in project.LanguagesForPoject.ToList())
				{
					project.LanguagesForPoject[language.Key] = selectAll;
				}
			}
			RefreshProjectsListBox();
			if (selectAll)
			{
				foreach (var language in _languages)
				{
					language.IsChecked = true;
				}
			}
			else
			{
				_languages.Clear();
			}
			RefreshLanguageListbox();
		}
	}
}