﻿using System.Collections.Generic;
using Sdl.Community.MTCloud.Provider.Model.RateIt;
using Sdl.FileTypeSupport.Framework.NativeApi;

namespace Sdl.Community.MTCloud.Provider.Model
{
	public class MetadataTransferObject
	{
		public string FilePath { get; set; }
		public List<SegmentId> SegmentIds { get; set; }
		public TranslationOriginData TranslationOriginData{ get; set; }
	}
}