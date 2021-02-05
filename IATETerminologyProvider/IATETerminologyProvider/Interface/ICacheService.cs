﻿using System;
using System.Collections.Generic;
using Sdl.Community.IATETerminologyProvider.Model;
using Sdl.Terminology.TerminologyProvider.Core;

namespace Sdl.Community.IATETerminologyProvider.Interface
{
	public interface ICacheService:IDisposable
	{
		IList<SearchResultModel> GetCachedResults(string sourceText, string targetLanguage, string bodyModelString);
		IEnumerable<SearchCache> GetAllCachedResults();
		void AddSearchResults(SearchCache searchCache);	
		void ClearCachedResults();
	}
}
