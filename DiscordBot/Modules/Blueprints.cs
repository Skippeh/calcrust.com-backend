using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Modules;
using DiscordBot.Rust.Models;
using DiscordBot.Utility;

namespace DiscordBot.Modules
{
    public class Blueprints : CalcRustModule
    {
        public override void Install(ModuleManager manager)
        {
            base.Install(manager);

            manager.CreateCommands("", Config);
        }

        private void Config(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand("craft")
                .Description("Prints the requirements for the specified item.")
                .Parameter("count")
                .Parameter("item", ParameterType.Multiple)
                .Do(CraftCommand);
        }

        private async Task CraftCommand(CommandEventArgs args)
        {
            var searchTerm = String.Join(" ", args.Args.Skip(1));
            var response = await Api.SearchRecipe(searchTerm);

            if (response.IsError)
            {
                await args.Channel.SendMessage(response.Message);
                return;
            }

            var recipe = response.Data.Values.FirstOrDefault();

            if (recipe == null)
            {
                await args.Channel.SendMessage("Could not find any recipes.");
            }
            else
            {
                int count;

                if (!int.TryParse(args.GetArg("count"), out count))
                {
                    await args.Channel.SendMessage("Invalid count specified.");
                    return;
                }

                if (count <= 0)
                {
                    await args.Channel.SendMessage("Invalid count specified.");
                }

                var requirementsResponse = await Api.GetRequirements(recipe.Output.Item.Id, count);

                if (requirementsResponse.IsError)
                {
                    await args.Channel.SendMessage(requirementsResponse.Message);
                    return;
                }

                RecipeRequirements requirements = requirementsResponse.Data;

                var builder = new ResponseBuilder
                {
                    Title = $"Crafting {count}x {requirements.Output.Item.Name}"
                };

                float totalCount = requirements.Output.Count;
                int totalStacks = (int) Math.Ceiling(totalCount / requirements.Output.Item.MaxStack);
                string craftTime = FormatUtility.FriendlyTime((ulong) requirements.TTC);

                IEnumerable<Tuple<string, string>> tableData = requirements.Input.Select(itemCount => new Tuple<string, string>(itemCount.Count.ToString("0"), itemCount.Item.Name));

                builder.AddLine($"Output: {totalCount}x item{(totalCount != 1 ? "s" : "")} ({totalStacks} stack{(totalStacks != 1 ? "s" : "")})");
                builder.AddLine($"Time to craft: {craftTime}");
                builder.AddLine($"\n{Discord.Format.Bold("Requirements")}");
                builder.AddTable("Count", "Item name", tableData);

                await args.Channel.SendMessage(builder.Build());
            }
        }
    }
}