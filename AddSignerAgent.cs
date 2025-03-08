using System;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.Connectors.OpenAI; // For OpenAIChatCompletionService
using HelperMAS; // To access ExcelHelper

namespace HelperMAS.Agents
{
    public class AddSignerAgent
    {
        private readonly OpenAIChatCompletionService _chatService;
        private readonly string _excelFilePath;

        public AddSignerAgent(OpenAIChatCompletionService chatService, string excelFilePath)
        {
            _chatService = chatService;
            _excelFilePath = excelFilePath;
        }

        private async Task<string> GetResponseAsync(string prompt, int maxTokens, double temperature)
        {
            var responses = await _chatService.GetTextContentsAsync(prompt, executionSettings: null, kernel: null);
            return responses.Count > 0 ? responses[0].Text.Trim() : "";
        }

        public async Task RunAsync()
        {
            string askNamePrompt = "You are an add signer agent. Please ask politely for the name of the signer to be added.";
            string askNameResponse = await GetResponseAsync(askNamePrompt, 50, 0.7);
            Console.WriteLine("Add Signer Agent: " + askNameResponse);
            string signerName = Console.ReadLine()?.Trim() ?? "";
            if (string.IsNullOrEmpty(signerName))
            {
                Console.WriteLine("Add Signer Agent: No valid name provided.");
                return;
            }
            ExcelHelper.AddSigner(_excelFilePath, signerName);
        }
    }
}
