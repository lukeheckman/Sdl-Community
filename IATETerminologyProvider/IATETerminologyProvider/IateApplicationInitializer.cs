﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using IATETerminologyProvider.Helpers;
using IATETerminologyProvider.Service;
using NLog;
using Sdl.Desktop.IntegrationApi;
using Sdl.Desktop.IntegrationApi.Extensions;

namespace IATETerminologyProvider
{
	[ApplicationInitializer]
	public class IateApplicationInitializer : IApplicationInitializer
	{
		public static HttpClient Clinet = new HttpClient();
		private static readonly AccessTokenService AccessTokenService = new AccessTokenService();

		public async void Execute()
		{
			Log.Setup();
			InitializeHttpClientSettings();

			var domanService = new DomainService();
			var termTypeService = new TermTypeService();
			await domanService.GetDomains();
			await termTypeService.GetTermTypes();
		}

		public static void SetAccessToken()
		{
			RefreshAccessToken();
			if (!string.IsNullOrEmpty(AccessTokenService.AccessToken))
			{
				Clinet.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessTokenService.AccessToken);
			}
		}

		private void InitializeHttpClientSettings()
		{
			Utils.AddDefaultParameters(Clinet);
			SetAccessToken();
		}

		private static void RefreshAccessToken()
		{
			var logger = LogManager.GetCurrentClassLogger();

			if (AccessTokenService.RefreshTokenExpired
			    || AccessTokenService.RequestedAccessToken == DateTime.MinValue
			    || string.IsNullOrEmpty(AccessTokenService.AccessToken))
			{
				var success = AccessTokenService.GetAccessToken("SDL_PLUGIN", "E9KWtWahXs4hvE9z");
				if (!success)
				{
					logger.Error(PluginResources.TermSearchService_Error_in_requesting_access_token);
				}
			}
			else if (AccessTokenService.AccessTokenExpired && !AccessTokenService.AccessTokenExtended)
			{
				var success = AccessTokenService.ExtendAccessToken();
				if (!success)
				{
					logger.Error(PluginResources.TermSearchService_Error_in_refreshing_access_token);
				}
			}
		}
	}
}
