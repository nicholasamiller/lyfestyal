using DocumentFormat.OpenXml.Packaging;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;


class Program
{
    static async Task<int> Main(string[] args)
    {
        var fileOption = new Option<FileInfo?>(
            name: "--inDocx",
            description: "The DOCX file to comment.");

        var outputFileOption = new Option<FileInfo?>(
            name: "--outDocx",
            description: "The output file to save the commented DOCX file.");

        var rootCommand = new RootCommand("Franz");
        rootCommand.AddOption(fileOption);
        rootCommand.AddOption(outputFileOption);

        rootCommand.SetHandler((file,outputFile) =>
        {
            MutateFile(file!,outputFile!);
        },
            fileOption,outputFileOption);

        return await rootCommand.InvokeAsync(args);
    }

    static void MutateFile(FileInfo file, FileInfo outputFile)
    {
        using (var wordDoc = WordprocessingDocument.Open(file.FullName, true))
        {
            var textIdWithTexts = Functions.ExtractTextIdsWithTexts(wordDoc);
            foreach (var (textId, text) in textIdWithTexts)
            {
                var testComment = "TEST COMMENT HERE BRO";
                Functions.AddCommentToTextId(wordDoc, textId, testComment);
            }
            
            Functions.SaveDocumentToFile(wordDoc, outputFile);
        }
    }
}

