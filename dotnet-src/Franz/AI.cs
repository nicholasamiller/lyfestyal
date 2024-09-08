using OpenAI.Chat;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Franz
{


    public class AI
    {
        
        private static string BuildSystemPrompt()
        {
            return "You are an expert editor who's to write comments on whether particular rule applies to, or 'flags' a particular piece of text.  For each task, you will receive the text and the rule separately.  If the rule does flag the text, you make a suggestion about how to reword the text so that it does not get flagged.  The audience for your comments is professionals who have high degree of literacy and intelligence, but not much time.  So your comments should be concise at to the point, with clear simple suggestions.  Your comments will be linked to particular parts of text in the documents.  Hence, you do not need to reference to the document or the text explicitly.  Assume the readers will read your comment right next to the relevant text.";
        }

        private static string BuildPrompt(string rule, string text)
        {
            var prompt = $"Rule: {rule}\nText: {text}";

            return prompt;
        }

        public static async Task<(bool IsFlagged, string? Advice)> CheckText(string rule, string text)
        {
            ChatCompletionOptions options = new()
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
      name: "comment",
      jsonSchema: BinaryData.FromString("""
            {
                "type": "object",
                "properties": {
                    "isFlagged": {
                        "type": "boolean",
                        "description": "Whether or not the text is flagged according to the provided rule."
                        },
                    
                    "advice": { 
                        "type": "string",
                        "description": "Advice to the author of the text about how to amend the text to better comply with the rule."
                        }
                },
                "required": ["isFlagged","advice"],
                "additionalProperties": false
            }
            """),
  strictSchemaEnabled: true)
            };

            ChatClient client = new ChatClient(model: "gpt-4o-2024-08-06", Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

            ChatCompletion chatCompletion = await client.CompleteChatAsync(
                [new SystemChatMessage(BuildSystemPrompt()), new UserChatMessage(BuildPrompt(rule, text))], options
            );

            using JsonDocument structuredJson = JsonDocument.Parse(chatCompletion.ToString());
            
            var isFlagged = structuredJson.RootElement.GetProperty("isFlagged").GetBoolean();
            if (isFlagged)
            {
                if (structuredJson.RootElement.TryGetProperty("advice", out var advice))
                {
                    return (isFlagged, advice.GetString());
                }
                else
                {
                    Log.Logger.Warning("Flagged text did not return advice.");
                    return (isFlagged, null);
                }
            }
            else return (isFlagged, null);
        }
        
    }
}
