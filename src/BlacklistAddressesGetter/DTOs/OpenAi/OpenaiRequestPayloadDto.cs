using System.Text.Json.Serialization;

namespace BlacklistAddressesGetter.DTOs.OpenAi;

internal class OpenaiRequestPayloadDto
{
    
    public OpenaiRequestPayloadDto(string model, OpenAiChatMessageDto[] messages, int temperature)
    {
        Model = model;
        Messages = messages;
        Temperature = temperature;
    }
    
    public string Model { get; set; }
    public OpenAiChatMessageDto[] Messages { get; set; }
    public int Temperature { get; set; }
    
}