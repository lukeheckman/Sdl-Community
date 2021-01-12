﻿using System.Collections.Generic;
using System.IO;
using Sdl.FileTypeSupport.Framework.BilingualApi;
using Sdl.FileTypeSupport.Framework.Core.Utilities.IntegrationApi;

namespace StudioViews.Services
{
	public class SdlxliffReader
	{
		public List<IParagraphUnit> GetParagraphUnits(string filePathInput)
		{
			var dummyFile = Path.GetTempFileName();
			
			var fileTypeManager = DefaultFileTypeManager.CreateInstance(true);
			var converter = fileTypeManager.GetConverterToDefaultBilingual(filePathInput, dummyFile, null);
			
			var contentReader = new ContentReader();

			converter.AddBilingualProcessor(contentReader);
			converter.SynchronizeDocumentProperties();

			converter.Parse();

			DeleteFile(dummyFile);

			return contentReader.ParagraphUnits;
		}

		private bool DeleteFile(string dummyFile)
		{
			try
			{
				if (File.Exists(dummyFile))
				{
					File.Delete(dummyFile);

					return true;
				}
			}
			catch
			{
				// ignore; catch all
			}

			return false;
		}
	}
}
