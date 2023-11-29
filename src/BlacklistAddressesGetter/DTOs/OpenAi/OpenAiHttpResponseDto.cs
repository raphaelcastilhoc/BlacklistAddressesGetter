namespace BlacklistAddressesGetter.DTOs.OpenAi;

public class OpenAiHttpResponseDto
{
    public string Id { get; set; }
    public string Object { get; set; }
    public int Created { get; set; }
    public string Model { get; set; }
    public OpenAiResponseChoiceDto[] Choices { get; set; }
}