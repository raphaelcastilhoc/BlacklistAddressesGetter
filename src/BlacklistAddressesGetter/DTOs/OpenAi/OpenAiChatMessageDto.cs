namespace BlacklistAddressesGetter.DTOs.OpenAi;

public class OpenAiChatMessageDto
{
    public string Role { get; set; }
    public string Content { get; set; }
    
    public OpenAiChatMessageDto(string role, string content)
    {
        Role = role;
        Content = content;
    }
    
}