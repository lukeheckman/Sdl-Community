﻿using System;
using Sdl.FileTypeSupport.Framework.BilingualApi;
using Sdl.FileTypeSupport.Framework.Core.Utilities.BilingualApi;
using Sdl.FileTypeSupport.Framework.Core.Utilities.NativeApi;
using Sdl.FileTypeSupport.Framework.Formatting;
using Sdl.FileTypeSupport.Framework.NativeApi;

namespace Sdl.Community.XLIFF.Manager.FileTypeSupport.SDLXLIFF
{
	public class SegmentBuilder
	{
		private readonly IDocumentItemFactory _factory;
		private readonly IPropertiesFactory _propertiesFactory;
		private readonly IFormattingItemFactory _formattingFactory;

		public SegmentBuilder()
		{
			_factory = DefaultDocumentItemFactory.CreateInstance();
			_propertiesFactory = DefaultPropertiesFactory.CreateInstance();
			_formattingFactory = _propertiesFactory.FormattingItemFactory;
		}

		public ITranslationOrigin CreateTranslationOrigin()
		{
			return _factory.CreateTranslationOrigin();
		}

		public IAbstractMarkupData Text(string text)
		{
			var textProperties = _propertiesFactory.CreateTextProperties(text);
			return _factory.CreateText(textProperties);
		}

		public ISegmentPairProperties CreateSegmentPairProperties()
		{
			var properties = _factory.CreateSegmentPairProperties();
			properties.TranslationOrigin = CreateTranslationOrigin();
			return properties;
		}

		public IComment CreateComment(string text, string author, Severity severity, DateTime dateTime, string version)
		{
			var comment = _propertiesFactory.CreateComment(text, author, severity);
			comment.Date = dateTime;
			comment.Version = version;
			return comment;
		}

		private IAbstractMarkupData ItalicText(IAbstractMarkupData innerMarker)
		{
			var x = _propertiesFactory.CreateStartTagProperties("<cf italic=True>");
			var y = _propertiesFactory.CreateEndTagProperties("</cf>");

			var xItem = _formattingFactory.CreateFormattingItem("italic", "True");

			x.Formatting = _formattingFactory.CreateFormatting();
			x.Formatting.Add(xItem);

			var tagPair = _factory.CreateTagPair(x, y);
			tagPair.Add(innerMarker);

			return tagPair;
		}
	}
}
