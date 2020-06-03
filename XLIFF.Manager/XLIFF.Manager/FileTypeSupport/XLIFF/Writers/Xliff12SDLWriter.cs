﻿using System;
using System.Collections.Generic;
using System.Xml;
using Sdl.Community.XLIFF.Manager.Common;
using Sdl.Community.XLIFF.Manager.FileTypeSupport.XLIFF.Model;
using Sdl.Community.XLIFF.Manager.Interfaces;
using Sdl.FileTypeSupport.Framework.NativeApi;

namespace Sdl.Community.XLIFF.Manager.FileTypeSupport.XLIFF.Writers
{
	public class Xliff12SDLWriter : IXliffWriter
	{
		private Dictionary<string, List<IComment>> Comments { get; set; }

		private bool IncludeTranslations { get; set; }

		public bool WriteFile(Xliff xliff, string outputFilePath, bool includeTranslations)
		{
			Comments = xliff.DocInfo.Comments;
			IncludeTranslations = includeTranslations;

			var settings = new XmlWriterSettings
			{
				OmitXmlDeclaration = true,
				Indent = true
			};

			var version = "1.2";
			var sdlSupport = Enumerators.XLIFFSupport.xliff12sdl.ToString();
			var sdlVersion = version + ".1";

			using (var writer = XmlWriter.Create(outputFilePath, settings))
			{
				writer.WriteStartElement("xliff");
				writer.WriteAttributeString("version", version);
				writer.WriteAttributeString("xmlns", "sdl", null, "http://schemas.sdl.com/xliff");
				writer.WriteAttributeString("sdl", "support", null, sdlSupport);
				writer.WriteAttributeString("sdl", "version", null, sdlVersion);

				WriteDocInfo(xliff, writer);

				foreach (var xliffFile in xliff.Files)
				{
					writer.WriteStartElement("file");
					writer.WriteAttributeString("original", xliffFile.Original);
					writer.WriteAttributeString("source-language", xliffFile.SourceLanguage);
					if (includeTranslations)
					{
						writer.WriteAttributeString("target-language", xliffFile.TargetLanguage);
					}

					writer.WriteAttributeString("datatype", xliffFile.DataType);

					WriterFileHeader(writer, xliffFile);
					WriteFileBody(writer, xliffFile);

					writer.WriteEndElement(); // file
				}

				writer.WriteEndElement(); //xliff
			}

			return true;
		}

		private void WriteDocInfo(Xliff xliff, XmlWriter writer)
		{
			writer.WriteStartElement("sdl", "doc-info", null);
			writer.WriteAttributeString("project-id", xliff.DocInfo.ProjectId);
			writer.WriteAttributeString("source", xliff.DocInfo.Source);
			writer.WriteAttributeString("source-language", xliff.DocInfo.SourceLanguage);
			if (IncludeTranslations)
			{
				writer.WriteAttributeString("target-language", xliff.DocInfo.TargetLanguage);
			}

			writer.WriteAttributeString("created", GetDateToString(xliff.DocInfo.Created));

			WriteCommentDefinitions(xliff, writer);

			writer.WriteEndElement(); //doc-info
		}

		private void WriteCommentDefinitions(Xliff xliff, XmlWriter writer)
		{
			writer.WriteStartElement("sdl", "cmt-defs", null);
			foreach (var comments in xliff.DocInfo.Comments)
			{
				WriteCommentDefinition(writer, comments);
			}

			writer.WriteEndElement(); //cmt-defs
		}

		private void WriteCommentDefinition(XmlWriter writer, KeyValuePair<string, List<IComment>> comments)
		{
			writer.WriteStartElement("sdl", "cmt-def", null);
			writer.WriteAttributeString("id", comments.Key);

			WriteComments(writer, comments);

			writer.WriteEndElement(); //cmt-def
		}

		private void WriteComments(XmlWriter writer, KeyValuePair<string, List<IComment>> comments)
		{
			writer.WriteStartElement("sdl", "comments", null);
			foreach (var comment in comments.Value)
			{
				WriteComment(writer, comment);
			}

			writer.WriteEndElement(); //comments
		}

		private void WriteComment(XmlWriter writer, IComment comment)
		{
			writer.WriteStartElement("sdl", "comment", null);
			writer.WriteAttributeString("user", comment.Author);
			writer.WriteAttributeString("date", GetDateToString(comment.Date));
			writer.WriteAttributeString("version", comment.Version);
			writer.WriteAttributeString("severity", comment.Severity.ToString());
			writer.WriteString(comment.Text);
			writer.WriteEndElement(); //comment
		}

		private void WriteFileBody(XmlWriter writer, File xliffFile)
		{
			writer.WriteStartElement("body");
			foreach (var transUnit in xliffFile.Body.TransUnits)
			{
				// SDL flavor
				WriteTransUnit(writer, transUnit);
			}
			writer.WriteEndElement(); // body
		}

		private void WriteTransUnit(XmlWriter writer, TransUnit transUnit)
		{
			writer.WriteStartElement("trans-unit");
			writer.WriteAttributeString("id", transUnit.Id);

			WriteSourceParagraph(writer, transUnit);
			WriteSegSource(writer, transUnit);
			WriteTargetParagraph(writer, transUnit);
			WriteSdlSegDefs(writer, transUnit);

			writer.WriteEndElement(); // trans-unit
		}

		private void WriteSdlSegDefs(XmlWriter writer, TransUnit transUnit)
		{
			writer.WriteStartElement("sdl", "seg-defs", null);

			foreach (var segmentPair in transUnit.SegmentPairs)
			{
				WriteSdlSeg(writer, segmentPair);
			}

			writer.WriteEndElement(); //sdl:seg-defs
		}

		private void WriteSdlSeg(XmlWriter writer, SegmentPair segmentPair)
		{
			writer.WriteStartElement("sdl", "seg", null);
			writer.WriteAttributeString("id", segmentPair.Id);
			writer.WriteAttributeString("conf", segmentPair.ConfirmationLevel.ToString());

			if (segmentPair.IsLocked)
			{
				writer.WriteAttributeString("locked", segmentPair.IsLocked.ToString());
			}

			if (segmentPair.TranslationOrigin != null)
			{
				WriteTranslationOrigin(writer, segmentPair.TranslationOrigin);

				if (segmentPair.TranslationOrigin?.OriginBeforeAdaptation != null)
				{
					writer.WriteStartElement("sdl", "prev-origin", null);
					WriteTranslationOrigin(writer, segmentPair.TranslationOrigin?.OriginBeforeAdaptation);
					writer.WriteEndElement(); //sdl:prev-origin
				}
			}

			writer.WriteEndElement(); //sdl:seg
		}

		private static void WriteTranslationOrigin(XmlWriter writer, ITranslationOrigin translationOrigin)
		{
			var originType = translationOrigin != null ? translationOrigin.OriginType : string.Empty;
			var originSystem = translationOrigin != null ? translationOrigin.OriginSystem : string.Empty;
			var matchPercentage = translationOrigin?.MatchPercent.ToString() ?? "0";
			var structMatch = translationOrigin?.IsStructureContextMatch.ToString() ?? string.Empty;
			var textMatch = translationOrigin?.TextContextMatchLevel != null
				? translationOrigin.TextContextMatchLevel.ToString()
				: string.Empty;

			if (!string.IsNullOrEmpty(originType))
			{
				writer.WriteAttributeString("origin", originType);
			}

			if (!string.IsNullOrEmpty(originSystem))
			{
				writer.WriteAttributeString("origin-system", originSystem);
			}

			if (!string.IsNullOrEmpty(matchPercentage) && matchPercentage != "0")
			{
				writer.WriteAttributeString("percent", matchPercentage);
			}

			if (!string.IsNullOrEmpty(structMatch) && structMatch != "False")
			{
				writer.WriteAttributeString("struct-match", structMatch);
			}

			if (!string.IsNullOrEmpty(textMatch) && textMatch != "None")
			{
				writer.WriteAttributeString("text-match", textMatch);
			}

			if (translationOrigin?.MetaData != null)
			{
				foreach (var keyValuePair in translationOrigin.MetaData)
				{
					writer.WriteStartElement("sdl", "value", null);

					writer.WriteAttributeString("key", keyValuePair.Key);
					writer.WriteString(keyValuePair.Value);

					writer.WriteEndElement(); //sdl:value
				}
			}
		}

		private void WriteSourceParagraph(XmlWriter writer, TransUnit transUnit)
		{
			writer.WriteStartElement("source");
			for (var index = 0; index < transUnit.SegmentPairs.Count; index++)
			{
				var segmentPair = transUnit.SegmentPairs[index];

				if (index > 0)
				{
					var addSpace = AddSpaceBetweenSegmentationPosition(transUnit, index);
					if (addSpace)
					{
						writer.WriteString(" ");
					}
				}

				foreach (var element in segmentPair.Source.Elements)
				{
					WriteSegment(writer, element);
				}
			}

			writer.WriteEndElement(); // source
		}

		private void WriteSegSource(XmlWriter writer, TransUnit transUnit)
		{
			writer.WriteStartElement("seg-source");
			foreach (var segmentPair in transUnit.SegmentPairs)
			{
				writer.WriteStartElement("mrk");
				writer.WriteAttributeString("mtype", "seg");
				writer.WriteAttributeString("mid", segmentPair.Id);

				if (segmentPair.IsLocked)
				{
					writer.WriteStartElement("mrk");
					writer.WriteAttributeString("mtype", "protected");
				}

				foreach (var element in segmentPair.Source.Elements)
				{
					WriteSegment(writer, element);
				}

				if (segmentPair.IsLocked)
				{
					writer.WriteEndElement(); // mrk
				}

				writer.WriteEndElement(); // mrk
			}

			writer.WriteEndElement(); // seg-source
		}

		private void WriteTargetParagraph(XmlWriter writer, TransUnit transUnit)
		{
			writer.WriteStartElement("target");

			foreach (var segmentPair in transUnit.SegmentPairs)
			{
				writer.WriteStartElement("mrk");
				writer.WriteAttributeString("mtype", "seg");
				writer.WriteAttributeString("mid", segmentPair.Id);

				if (segmentPair.IsLocked)
				{
					writer.WriteStartElement("mrk");
					writer.WriteAttributeString("mtype", "protected");
				}

				foreach (var element in segmentPair.Target.Elements)
				{
					WriteSegment(writer, element);
				}

				if (segmentPair.IsLocked)
				{
					writer.WriteEndElement(); // mrk
				}


				writer.WriteEndElement(); // mrk
			}

			writer.WriteEndElement(); // seg-source
		}

		private void WriteSegment(XmlWriter writer, Element element)
		{
			if (element is ElementText text)
			{
				writer.WriteString(text.Text);
			}

			if (element is ElementTagPair tag)
			{
				switch (tag.Type)
				{
					case Element.TagType.OpeningTag:
						writer.WriteStartElement("bpt");
						writer.WriteAttributeString("id", tag.TagId);
						writer.WriteString(tag.TagContent);
						writer.WriteEndElement();
						break;
					case Element.TagType.ClosingTag:
						writer.WriteStartElement("ept");
						writer.WriteAttributeString("id", tag.TagId);
						writer.WriteString(tag.TagContent);
						writer.WriteEndElement();
						break;
				}
			}

			if (element is ElementPlaceholder placeholder)
			{
				writer.WriteStartElement("ph");
				writer.WriteAttributeString("id", placeholder.TagId);
				writer.WriteString(placeholder.TagContent);
				writer.WriteEndElement();
			}

			if (element is ElementLocked locked)
			{
				switch (locked.Type)
				{
					case Element.TagType.OpeningTag:
						writer.WriteStartElement("mrk");
						writer.WriteAttributeString("mtype", "protected");
						break;
					case Element.TagType.ClosingTag:
						writer.WriteEndElement();
						break;
				}
			}

			if (element is ElementComment comment)
			{
				switch (comment.Type)
				{
					case Element.TagType.OpeningTag:
						writer.WriteStartElement("mrk");
						writer.WriteAttributeString("mtype", "x-sdl-comment");
						writer.WriteAttributeString("cid", comment.Id);
						break;
					case Element.TagType.ClosingTag:
						writer.WriteEndElement();
						break;
				}
			}
		}

		private void WriterFileHeader(XmlWriter writer, File xliffFile)
		{
			writer.WriteStartElement("header");
			if (!string.IsNullOrEmpty(xliffFile.Header?.Skl?.ExternalFile?.Uid))
			{
				writer.WriteStartElement("skl");

				writer.WriteStartElement("external-file");
				writer.WriteAttributeString("uid", xliffFile.Header.Skl.ExternalFile.Uid);
				writer.WriteAttributeString("href", xliffFile.Header.Skl.ExternalFile.Href);
				writer.WriteEndElement(); // external-file

				writer.WriteEndElement(); // skl
			}
			writer.WriteEndElement(); // header
		}

		private bool AddSpaceBetweenSegmentationPosition(TransUnit transUnit, int index)
		{
			var addSpace = true;

			var foundSpaceStart = false;
			var foundSpaceEnd = false;

			var currentFirstElement = transUnit.SegmentPairs[index].Source.Elements[0];
			if (currentFirstElement is ElementText text1)
			{
				foundSpaceStart = text1.Text.StartsWith(" ");
			}

			var previous = transUnit.SegmentPairs[index - 1].Source;
			var previousLastElement = previous.Elements[previous.Elements.Count - 1];
			if (previousLastElement is ElementText text2)
			{
				foundSpaceEnd = text2.Text.EndsWith(" ");
			}

			if (foundSpaceStart || foundSpaceEnd)
			{
				addSpace = false;
			}

			return addSpace;
		}

		private string GetDateToString(DateTime date)
		{
			var value = string.Empty;

			if (date != DateTime.MinValue || date != DateTime.MaxValue)
			{
				return date.ToUniversalTime()
					.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
			}

			return value;
		}		
	}
}
