using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.Connectors.OpenAI; // For OpenAIChatCompletionService
using HelperMAS; // To access ExcelHelper

namespace HelperMAS.Agents
{
    public class RemoveSignerAgent
    {
        private readonly OpenAIChatCompletionService _chatService;
        private readonly string _excelFilePath;

        public RemoveSignerAgent(OpenAIChatCompletionService chatService, string excelFilePath)
        {
            _chatService = chatService;
            _excelFilePath = excelFilePath;
        }

        // Helper method to call the chat service.
        private async Task<string> GetResponseAsync(string prompt, int maxTokens, double temperature)
        {
            var responses = await _chatService.GetTextContentsAsync(prompt, executionSettings: null, kernel: null);
            return responses.Count > 0 ? responses[0].Text.Trim() : "";
        }

        public async Task RunAsync(string userRequest)
        {
            // Attempt to extract inline names from the userRequest.
            string inlineNames = "";
            int index = userRequest.IndexOf("remove signer");
            if (index >= 0)
            {
                inlineNames = userRequest.Substring(index + "remove signer".Length).Trim();
                // If the inline names start with "called", remove that word.
                if (inlineNames.StartsWith("called ", StringComparison.OrdinalIgnoreCase))
                {
                    inlineNames = inlineNames.Substring("called ".Length).Trim();
                }
            }

            var names = Enumerable.Empty<string>().ToList();
            if (!string.IsNullOrEmpty(inlineNames))
            {
                // Use inline names if provided.
                names = inlineNames.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                   .Select(n => n.Trim())
                                   .ToList();
            }
            else
            {
                // Otherwise, prompt the user for names.
                string askNamesPrompt = "Please provide the names of the signers to be removed, separated by commas.";
                string askNamesResponse = await GetResponseAsync(askNamesPrompt, 50, 0.7);
                Console.WriteLine("Remove Signer Agent: " + askNamesResponse);
                string namesInput = Console.ReadLine()?.Trim() ?? "";
                names = namesInput.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                  .Select(n => n.Trim())
                                  .ToList();
            }

            if (names.Count == 0)
            {
                Console.WriteLine("Remove Signer Agent: No valid names provided.");
                return;
            }

            // Update the Excel file for each signer.
            foreach (var name in names)
            {
                ExcelHelper.UpdateSignerStatus(_excelFilePath, name, "deactivated");
            }
        }
    }
}
