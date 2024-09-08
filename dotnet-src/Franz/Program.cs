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

        var outputFileOption = new Option<FileInfo?>(
            name: "--outDocx",
            description: "The output file to save the commented DOCX file."
            )
        { IsRequired = true};

        var ruleOption = new Option<List<FileInfo?>>(
            name: "--ruleFiles",
            description: "JSON files with the rule to apply and a source reference.")
        { IsRequired = true};


        var rootCommand = new RootCommand("Franz");
        rootCommand.AddOption(fileOption);
        rootCommand.AddOption(outputFileOption);
        rootCommand.AddOption(ruleOption);

        rootCommand.SetHandler((file,outputFile,ruleOption) =>
        {
            MutateFile(file!,outputFile!,ruleOption!);
        },
            fileOption,outputFileOption,ruleOption);

        return await rootCommand.InvokeAsync(args);
    }

    static void MutateFile(FileInfo file, FileInfo outputFile, List<FileInfo?> ruleFiles)
    {
        var ruleData = ruleFiles.Select(f =>
        {
            var basePath = Path.GetDirectoryName(f.FullName);
            var deserialized = JsonSerializer.Deserialize<Rule>(File.ReadAllText(f.FullName));
            var ruleText = File.ReadAllText(Path.Combine(basePath, deserialized.RuleTextFile));
            var source = deserialized.RuleSourceReference;
            return (ruleText, source);
        });

        using (var wordDoc = WordprocessingDocument.Open(file.FullName, true))
        {
            var textIdWithTexts = Functions.ExtractTextIdsWithTexts(wordDoc);
            foreach (var (textId, text) in textIdWithTexts)
            {
                foreach (var r in ruleData)
                {
                    var (isFlagged, advice) = AI.CheckText(r.ruleText, text).Result;
                    if (isFlagged && !String.IsNullOrEmpty(advice))
                    {
                        Functions.AddCommentToTextId(wordDoc, textId, advice, r.source);
                    }
                }
            }
            
            Functions.SaveDocumentToFile(wordDoc, outputFile);
            Log.Logger.Information("Comments added to {outputFile}", outputFile.FullName);
        }
    }
}

