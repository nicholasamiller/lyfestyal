using DocumentFormat.OpenXml.Packaging;
using Franz;
using Serilog;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text.Json;


class Program
{
    static async Task<int> Main(string[] args)
    {

        Log.Logger = new LoggerConfiguration()
           .WriteTo.Console()
           .CreateLogger();

        var openApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(openApiKey))
        {
            Console.WriteLine("Set OPENAI_API_KEY environment variable.");
            return 1;
        }

        var fileOption = new Option<FileInfo?>(
            name: "--inDocx",
            description: "The DOCX file to comment.")
        { IsRequired = true};

        var ruleOption = new Option<List<FileInfo?>>(
            name: "--ruleFiles",
            description: "JSON files with the rule to apply and a source reference.")
        { IsRequired = true};


        var rootCommand = new RootCommand("Franz");
        rootCommand.AddOption(fileOption);
        rootCommand.AddOption(ruleOption);

        rootCommand.SetHandler((file,ruleOption) =>
        {
            MutateFile(file!,ruleOption!);
        },
            fileOption,ruleOption);

        return await rootCommand.InvokeAsync(args);
    }

    static void MutateFile(FileInfo file,  List<FileInfo?> ruleFiles)
    {
        var ruleData = ruleFiles.Select(f =>
        {
            var basePath = Path.GetDirectoryName(f.FullName);
            var deserialized = JsonSerializer.Deserialize<Rule>(File.ReadAllText(f.FullName));
            var ruleText = File.ReadAllText(Path.Combine(basePath, deserialized.RuleTextFile));
            var source = deserialized.RuleSourceReference;
            return (ruleText, source);
        }).ToList();

        using (var wordDoc = WordprocessingDocument.Open(file.FullName, true))
        {
            var textIdWithTexts = Functions.ExtractTextIdsWithTexts(wordDoc);
            Log.Information("Extracted {count} text chunks from {file}", textIdWithTexts.Count, file.FullName);

            foreach (var (textId, text) in textIdWithTexts)
            { 
                foreach (var r in ruleData)
                {   
                    Log.Logger.Information("Checking text chunk {textId} against rule {rule}...", textId, ruleData.IndexOf(r));
                    var (isFlagged, advice) = AI.CheckText(r.ruleText, text).Result;
                    Log.Logger.Information("Text chunk {textId} against rule {rule} is flagged: {isFlagged}", textId, ruleData.IndexOf(r), isFlagged);
                    if (isFlagged && !String.IsNullOrEmpty(advice))
                    {
                        Functions.AddCommentToTextId(wordDoc, textId, advice, r.source);
                        Log.Logger.Information("Comment added to text chunk {textId} for {ruleId}", textId, ruleData.IndexOf(r));
                    }
                }
            }
          
            Log.Logger.Information("Comments added to file, all done!");
        }
    }
}

