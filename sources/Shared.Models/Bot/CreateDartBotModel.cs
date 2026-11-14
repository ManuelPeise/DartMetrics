using Shared.Enums.Bot;

namespace Shared.Models.Bot
{
    public class CreateDartBotModel
    {
        public string Name { get; set; } = null!;

        public string Alias { get; set; } = null!;

        public DartBotPlayingStyleEnum? PlayingStyle { get; set; }
    }
}
