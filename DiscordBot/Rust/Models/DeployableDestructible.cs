namespace DiscordBot.Rust.Models
{
    public class DeployableDestructible : Destructible
    {
        public DestructibleValues Values;

        public DeployableDestructible(Destructible values)
        {
            Id = values.Id;
            Type = values.Type;
            Name = values.Name;
            HasProtection = values.HasProtection;
        }
    }
}