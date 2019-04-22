﻿namespace Sdl.Community.SignoffVerifySettings.Business.Helpers
{
	public static class Constants
	{
		// File phases
		public static readonly string PreparationPhase = "LanguageFileServerAssignmentsSettings_Preparation";
		public static readonly string TranslationPhase = "LanguageFileServerAssignmentsSettings_Translation";
		public static readonly string ReviewPhase = "LanguageFileServerAssignmentsSettings_Review";
		public static readonly string FinalisationPhase = "LanguageFileServerAssignmentsSettings_Finalisation";

		public static readonly string NumberVerifier2017CommunityPath = @"SDL Community\Number Verifier\Number Verifier 2017";
		public static readonly string NumberVerifierSettingsJson = "NumberVerifierSettings.json";
		
		// Report values
		public static readonly string ReportName = "SignoffVerifySettings";
		public static readonly string ReportDescription = "Verification statistics";
		public static readonly string ProjectInformation = "ProjectInformation";
		public static readonly string Project = "Project";
		public static readonly string Name = "Name";
		public static readonly string StudioVersion = "StudioVersion";
		public static readonly string Zero = "0";
		public static readonly string QASettingName = "QASettingName";
		public static readonly string SourceLanguage = "SourceLanguage";
		public static readonly string DisplayName = "DisplayName";
		public static readonly string TargetLanguages = "TargetLanguages";
		public static readonly string TargetLanguage = "TargetLanguage";
		public static readonly string RunAt = "RunAt";
		public static readonly string TranslationMemories = "TranslationMemories";
		public static readonly string TranslationMemory = "TranslationMemory";
		public static readonly string Termbases = "Termbases";
		public static readonly string Termbase = "Termbase";
		public static readonly string RegExRules = "RegExRules";
		public static readonly string CheckRegEx = "CheckRegEx";
		public static readonly string QAChecker = "QAChecker";
		public static readonly string LanguageFiles = "LanguageFiles";
		public static readonly string LanguageFile = "LanguageFile";
		public static readonly string LanguagePair = "LanguagePair";
		public static readonly string AssignedPhase = "AssignedPhase";
		public static readonly string IsCurrentAssignment = "IsCurrentAssignment";
		public static readonly string AssigneesNumber = "AssigneesNumber";
		public static readonly string Phases = "Phases";
		public static readonly string Phase = "Phase";
		public static readonly string NumberVerifier = "NumberVerifier";
		public static readonly string ExecutedDate = "ExecutedDate";
		public static readonly string VerificationSettings = "VerificationSettings";
		public static readonly string VerificationSetting = "VerificationSetting";
		public static readonly string FileName = "FileName";
		public static readonly string ApplicationVersion = "ApplicationVersion";
		public static readonly string ExecutedDateTime = "ExecutedDateTime";
		public static readonly string Count = "Count";
		public static readonly string False = "False";
		public static readonly string True = "True";

		// Report messages
		public static readonly string Enabled = @"The ""Search regular expression"" option is enabled.";
		public static readonly string Disabled = @"The ""Search regular expression"" option is disabled.";
		public static readonly string NoVerificationRun = "'Verify Files' batch task was not run.";
		public static readonly string NoTranslationMemory = "No translation memory set.";
		public static readonly string NoTermbase = "No termbase set.";
		public static readonly string RegExRulesApplied = "RegEx rules were applied.";
		public static readonly string NoRegExRules = "No RegEx rules were applied.";
		public static readonly string QAChekerExecuted = "Verification message reported.";
		public static readonly string NoQAChekerExecuted = "No verification message reported.";
		public static readonly string NoQAVerificationSettings = "No QA Verification Settings enabled.";
		public static readonly string NoPhaseAssigned = "No phase assigned.";
		public static readonly string NoUserAssigned = "No user(s) assigned.";
		public static readonly string NoNumberVerifierExecuted = "Number Verifier was not run.";

		// TellMe Actions values
		public static readonly string CategoryName = "SignoffVerifySettings results";
		public static readonly string ForumName = "SDL Community AppStore Forum";
		public static readonly string AppSupportLink = "https://community.sdl.com/appsupport";
		public static readonly string CommunityWikiName = "SDL Community Signoff Verify Settings plugin wiki";
		public static readonly string CommunityWikiLink = "https://community.sdl.com/product-groups/translationproductivity/w/customer-experience/4568.signoff-verify-settings";
		public static readonly string AppStoreName = "Download Signoff Verify Settings from AppStore";
		public static readonly string AppStoreLink = ""; // AppStoreLink will be added once it's generated on the store

		//TellMe Provider values
		public static readonly string SignoffVerifySettings = "signoff verify settings";
		public static readonly string SvsCommunity = "signoff verify settings community";
		public static readonly string SvsSupport = "signoff verify settings support";
		public static readonly string SvsStore = "signoff verify settings store";
		public static readonly string SvsAppStore = "signoff verify settings appstore";
		public static readonly string SvsWiki = "signoff verify settings wiki";
		public static readonly string SvsForum = "signoff verify settings forum";
		public static readonly string SvsDownload = "signoff verify settings download";
	}
}