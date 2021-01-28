﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using IATETerminologyProvider.Helpers;
using IATETerminologyProvider.Model;
using IATETerminologyProvider.Model.ResponseModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using Sdl.Core.Globalization;
using Sdl.Terminology.TerminologyProvider.Core;

namespace IATETerminologyProvider.Service
{
	public class TermSearchService
	{
		private  readonly Logger _logger = LogManager.GetCurrentClassLogger();
		private readonly SettingsModel _providerSettings;
		private readonly ObservableCollection<ItemsResponseModel> _domains;
		private readonly TermTypeService _termTypeService;
		private readonly List<string> _subdomains = new List<string>();
		private int _termIndexId;
		public NotifyTaskCompletion<ObservableCollection<ItemsResponseModel>> IateTermTypes { get; set; }
		public ObservableCollection<TermTypeModel> TermTypes { get; set; }

		public TermSearchService(SettingsModel providerSettings)
		{
			TermTypes = new ObservableCollection<TermTypeModel>();
			_domains = DomainService.Domains;
			_termTypeService = new TermTypeService();

			LoadTermTypes();
			_providerSettings = providerSettings;
		}

		/// <summary>
		/// Get terms from IATE database.
		/// </summary>
		/// <param name="text">text used for searching</param>
		/// <param name="source">source language</param>
		/// <param name="target">target language</param>
		/// <param name="maxResultsCount">number of maximum results returned(set up in Studio Termbase search settings)</param>
		/// <returns>terms</returns>
		public List<ISearchResult> GetTerms(string text, ILanguage source, ILanguage target, int maxResultsCount)
		{
			var results = new List<ISearchResult>();

			var timer1 = new Stopwatch();
			timer1.Start();
			var bodyModel = SetApiRequestBodyValues(source, target, text);

			var httpRequest = new HttpRequestMessage
			{
				Method = HttpMethod.Post,
				RequestUri = new Uri(ApiUrls.BaseUri("true", "0", "500")),
				Content = new StringContent(JsonConvert.SerializeObject(bodyModel), Encoding.UTF8, "application/json")
			};

			//Refresh the Access token on Http client in case it expired
			IateApplicationInitializer.SetAccessToken();

			var httpResponse = IateApplicationInitializer.Clinet.SendAsync(httpRequest)?.Result;

			httpResponse?.EnsureSuccessStatusCode();
			try
			{
				timer1.Stop();

				var httpResponseString = httpResponse?.Content?.ReadAsStringAsync().Result;
				var domainsJsonResponse = JsonConvert.DeserializeObject<JsonDomainResponseModel>(httpResponseString);

				results = MapResponseValues(httpResponseString, domainsJsonResponse);

				return results;
			}
			finally
			{
				httpResponse?.Dispose();
			}
		}

		// Set the needed fields for the API search request
		private object SetApiRequestBodyValues(ILanguage source, ILanguage destination, string text)
		{
			var targetLanguges = new List<string>();
			var filteredDomains = new List<string>();
			var filteredTermTypes = new List<int>();

			targetLanguges.Add(destination.Locale.TwoLetterISOLanguageName);
			if (_providerSettings != null)
			{
				var domains = _providerSettings.Domains.Where(d => d.IsSelected).Select(d => d.Code).ToList();
				filteredDomains.AddRange(domains);

				if (_providerSettings.SearchInSubdomains)
				{
					IncludeSubdomainsId(filteredDomains);
				}

				var termTypes = _providerSettings.TermTypes.Where(t => t.IsSelected).Select(t => t.Code).ToList();
				filteredTermTypes.AddRange(termTypes);
			}

			var bodyModel = new
			{
				query = text,
				source = source.Locale.TwoLetterISOLanguageName,
				targets = targetLanguges,
				include_subdomains = "true",
				query_operator = 0,
				filter_by_domains = filteredDomains,
				search_in_term_types = filteredTermTypes
			};

			return bodyModel;
		}

		/// <summary>
		/// Add subdomains ids for selected domains
		/// </summary>
		/// <param name="filteredDomainsIds">Subdomains ids which needs to be sent to IATE</param>
		private void IncludeSubdomainsId(List<string> filteredDomainsIds)
		{
			var subdomainsIds = new List<string>();
			var correspondingSubdomains =
				_providerSettings.Domains.Where(d => d.IsSelected).Select(d => d.SubdomainsIds).ToList();
			foreach (var subdomainIds in correspondingSubdomains)
			{
				subdomainsIds.AddRange(subdomainIds);
			}
			filteredDomainsIds.AddRange(subdomainsIds);
		}

		/// <summary>
		/// Map the terms values returned from the IATE API response with the SearchResultModel
		/// </summary>
		/// <param name="response">IATE API response</param>
		/// <param name="domainResponseModel">domains response model</param>
		/// <returns>list of terms</returns>
		private List<ISearchResult> MapResponseValues(string response, JsonDomainResponseModel domainResponseModel)
		{
			var termsList = new List<ISearchResult>();
			if (!string.IsNullOrEmpty(response))
			{
				var jObject = JObject.Parse(response);
				var itemTokens = (JArray)jObject.SelectToken("items");
				if (itemTokens != null)
				{
					foreach (var item in itemTokens)
					{
						_termIndexId++;

						var itemId = item.SelectToken("id").ToString();
						var domainModel = domainResponseModel.Items.FirstOrDefault(i => i.Id == itemId);
						var domain = SetTermDomain(domainModel);
						SetTermSubdomains(domainModel);

						var searchResultItems = new List<SearchResultModel>();

						// get language childrens (source + target languages)
						var languageTokens = item.SelectToken("language").Children().ToList();
						if (languageTokens.Any())
						{
							// foreach language token get the terms
							foreach (var jToken in languageTokens)
							{
								var languageToken = (JProperty)jToken;

								// Multilingual and Latin translations are automatically returned by IATE API response->"la" code
								// Ignore the "mul" and "la" translations
								if (languageToken.Name.Equals("la") || languageToken.Name.Equals("mul"))
								{
									continue;
								}

								var termEntries = languageToken.FirstOrDefault()?.SelectToken("term_entries");
								if (termEntries == null)
								{
									continue;
								}

								foreach (var termEntry in termEntries)
								{
									var termValue = termEntry.SelectToken("term_value").ToString();
									var termType = GetTermTypeByCode(termEntry.SelectToken("type").ToString());
									var langTwoLetters = languageToken.Name;
									var definition = languageToken.Children().FirstOrDefault() != null
										? languageToken.Children().FirstOrDefault()?.SelectToken("definition")
										: null;

									var displayOrder = -1;
									var evaluation = -1;
									var metaData = termEntry.SelectToken("metadata");

									if (metaData != null)
									{
										var displayOrderObject = metaData.SelectToken("display_order");
										if (displayOrderObject != null)
										{
											displayOrder = displayOrderObject.Value<int>();
										}

										var evaluationObject = metaData.SelectToken("evaluation");
										if (evaluationObject != null)
										{
											evaluation = evaluationObject.Value<int>();
										}
									}

									try
									{
										var languageModel = new LanguageModel
										{
											Name = new Language(langTwoLetters).DisplayName,
											Locale = new Language(langTwoLetters).CultureInfo
										};

										var termResult = new SearchResultModel
										{
											Text = termValue,
											Id = _termIndexId,
											ItemId = itemId,
											Score = 100,
											Language = languageModel,
											Definition = definition?.ToString() ?? string.Empty,
											Domain = domain,
											Subdomain = FormatSubdomain(),
											TermType = termType,
											DisplayOrder = displayOrder,
											Evaluation = evaluation
										};

										searchResultItems.Add(termResult);
									}
									catch (Exception e)
									{
										_logger.Error($"{e.Message}\n{e.StackTrace}");
									}
								}
							}
						}

						termsList.AddRange(searchResultItems);
					}
				}
			}
			return termsList;
		}

		// Set term main domain
		private string SetTermDomain(ItemsResponseModel itemDomains)
		{
			var domain = string.Empty;
			foreach (var itemDomain in itemDomains.Domains)
			{
				var result = _domains?.FirstOrDefault(d => d.Code.Equals(itemDomain.Code));
				if (result != null)
				{
					domain = $"{result.Name}, ";
				}
			}
			return domain.TrimEnd(' ').TrimEnd(',');
		}

		// Set term subdomain
		private void SetTermSubdomains(ItemsResponseModel mainDomains)
		{
			// clear _subdomains list for each term
			_subdomains.Clear();
			if (_domains?.Count > 0)
			{
				foreach (var mainDomain in mainDomains.Domains)
				{
					foreach (var domain in _domains)
					{
						// if result returns null, means that code belongs to a subdomain
						var result = domain.Code.Equals(mainDomain.Code) ? domain : null;
						if (result == null && domain.Subdomains != null)
						{
							GetSubdomainsRecursively(domain.Subdomains, mainDomain.Code, mainDomain.Note);
						}
					}
				}
			}
		}

		// Get subdomains recursively
		private void GetSubdomainsRecursively(IEnumerable<SubdomainsResponseModel> subdomains, string code, string note)
		{
			foreach (var subdomain in subdomains)
			{
				if (subdomain.Code.Equals(code))
				{
					if (!string.IsNullOrEmpty(note))
					{
						var subdomainName = $"{subdomain.Name}. {note}";
						_subdomains.Add(subdomainName);
					}
					else
					{
						_subdomains.Add(subdomain.Name);
					}
				}
				else
				{
					if (subdomain.Subdomains != null)
					{
						GetSubdomainsRecursively(subdomain.Subdomains, code, note);
					}
				}
			}
		}

		// Format the subdomain in a user friendly mode.
		private string FormatSubdomain()
		{
			var result = string.Empty;
			var subdomainNo = 0;
			foreach (var subdomain in _subdomains.ToList())
			{
				subdomainNo++;
				result += $"{subdomainNo}.{subdomain}  ";
			}
			return result.TrimEnd(' ');
		}

		// Return the term type name based on the term type code.
		private string GetTermTypeByCode(string termTypeCode)
		{
			var typeCode = int.TryParse(termTypeCode, out _) ? int.Parse(termTypeCode) : 0;
			return TermTypes?.Count > 0 ? TermTypes?.FirstOrDefault(t => t.Code == typeCode)?.Name : string.Empty;
		}

		private void LoadTermTypes()
		{
			if (TermTypeService.IateTermType?.Count > 0)
			{
				SetTermTypes(TermTypeService.IateTermType);
			}
			else
			{
				IateTermTypes = new NotifyTaskCompletion<ObservableCollection<ItemsResponseModel>>(_termTypeService.GetTermTypes());
				IateTermTypes.PropertyChanged += IateTermTypes_PropertyChanged;
			}
		}

		private void SetTermTypes(ObservableCollection<ItemsResponseModel> termTypesResponse)
		{
			foreach (var item in termTypesResponse)
			{
				var selectedTermTypeName = Utils.UppercaseFirstLetter(item.Name.ToLower());

				var termType = new TermTypeModel
				{
					Code = int.TryParse(item.Code, out _) ? int.Parse(item.Code) : 0,
					Name = selectedTermTypeName
				};
				TermTypes.Add(termType);
			}
		}

		private void IateTermTypes_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (!e.PropertyName.Equals("Result")) return;
			if (!(IateTermTypes.Result?.Count > 0)) return;
			SetTermTypes(IateTermTypes.Result);
		}
	}
}