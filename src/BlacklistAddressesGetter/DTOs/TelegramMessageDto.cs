namespace BlacklistAddressesGetter.DTOs
{
    internal record TelegramMessageDto
    {
        public string ChannelUsername { get; set; }

        public string Message { get; set; }
    }
}
