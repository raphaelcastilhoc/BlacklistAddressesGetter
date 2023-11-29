namespace BlacklistAddressesGetter.DTOs.OpenAi;

public class OpenAiResponseChoiceDto
{
    public int Index { get; set; }
    public OpenAiChatMessageDto Message { get; set; }
    public string FinishReason { get; set; }
}