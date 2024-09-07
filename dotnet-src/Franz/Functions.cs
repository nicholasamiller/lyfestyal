using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;

public static class Functions
{
    
    public static List<(string TextId, string Text)> ExtractTextIdsWithTexts(WordprocessingDocument wordDoc)
    {
        var textIdWithTexts = new List<(string TextId, string Text)>();

            var body = wordDoc.MainDocumentPart.Document.Body;
            var paragraphs = body.Descendants<Paragraph>();

            foreach (var paragraph in paragraphs)
            {
                var textId = paragraph.GetAttribute("textId", "http://schemas.microsoft.com/office/word/2010/wordml");
                if (textId != null)
                {
                    var paragraphText = paragraph.InnerText;
                    textIdWithTexts.Add((textId.Value, paragraphText));
                }
            }

        return textIdWithTexts;
    }

    public static void AddCommentToTextId(WordprocessingDocument wordDoc, string textId, string commentText, string author = "Author", string initial = "A")
    {
        // Ensure the document has a comments part
        var commentsPart = wordDoc.MainDocumentPart.GetPartsOfType<WordprocessingCommentsPart>().FirstOrDefault();
        if (commentsPart == null)
        {
            commentsPart = wordDoc.MainDocumentPart.AddNewPart<WordprocessingCommentsPart>();
            commentsPart.Comments = new Comments();
        }

        var comments = commentsPart.Comments;

        // Generate a new comment ID
        int maxId = comments.Elements<Comment>().Select(c => int.Parse(c.Id.Value)).DefaultIfEmpty(0).Max();
        int newCommentId = maxId + 1;

        // Create the new comment
        var newComment = new Comment()
        {
            Id = newCommentId.ToString(),
            Author = author,
            Initials = initial,
            Date = DateTime.Now
        };
        newComment.AppendChild(new Paragraph(new Run(new Text(commentText))));
        comments.AppendChild(newComment);

        // Find the paragraph with the specified textId
        var paragraph = wordDoc.MainDocumentPart.Document.Body.Descendants<Paragraph>()
            .FirstOrDefault(p => p.GetAttribute("textId", "http://schemas.microsoft.com/office/word/2010/wordml").Value == textId);

        if (paragraph != null)
        {
            // Add comment range start, comment reference, and comment range end
            var commentRangeStart = new CommentRangeStart() { Id = newCommentId.ToString() };
            var commentRangeEnd = new CommentRangeEnd() { Id = newCommentId.ToString() };
            var commentReference = new Run(new CommentReference() { Id = newCommentId.ToString() });

            // Insert the comment elements into the paragraph
            paragraph.InsertBefore(commentRangeStart, paragraph.FirstChild);
            paragraph.Append(commentRangeEnd);
            paragraph.Append(commentReference);
        }

        // Save changes to the comments part
        comments.Save();
    }

    public static void SaveDocumentToFile(WordprocessingDocument originalDoc, FileInfo newFile)
    {
        // Create a copy of the original document and save it to the new file path
        using (WordprocessingDocument newDoc = (WordprocessingDocument)originalDoc.Clone(newFile.FullName, true))
        {
            // Optionally, you can make additional changes to the new document here
        }
    }



}
