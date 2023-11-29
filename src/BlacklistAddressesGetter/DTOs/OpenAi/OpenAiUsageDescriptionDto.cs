namespace BlacklistAddressesGetter.DTOs.OpenAi;

public class OpenAiUsageDescriptionDto
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}