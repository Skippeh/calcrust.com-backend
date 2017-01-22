using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Modules;
using DiscordBot.Rust.Models;
using Newtonsoft.Json;

namespace DiscordBot.Modules
{
    public class DamageInfo : CalcRustModule
    {
        public override void Install(ModuleManager manager)
        {
            base.Install(manager);

            manager.CreateCommands("", Config);
        }

        private void Config(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand("damage")
                .Description("Prints the damage info for the specified weapon, building part, and optional building grade.\n\nSurround the weapon and building part name with quotes if they are multiple words long.")
                .Parameter("weapon")
                .Parameter("building")
                .Parameter("buildingGrade", ParameterType.Optional)
                .Do(DamageCommand);
        }

        private async Task DamageCommand(CommandEventArgs args)
        {
            string weapon = args.GetArg(0);
            string buildingPart = args.GetArg(1);
            string buildingGrade = null;

            if (args.Args.Length >= 3)
            {
                buildingGrade = args.GetArg(2);

                // Alias "armored" and "armoured" with "topTier".
                if (buildingGrade.ToLower() == "armored" || buildingGrade.ToLower() == "armoured")
                {
                    buildingGrade = "topTier";
                }
            }

            var itemSearchResponse = await Api.SearchItem(weapon);
            string weaponShortname;
            string destructibleShortname;

            if (itemSearchResponse.StatusCode == HttpStatusCode.OK)
            {
                weaponShortname = itemSearchResponse.Data.FirstOrDefault().Key;

                if (weaponShortname == null)
                {
                    await args.Channel.SendMessage($"Could not find a weapon called \"{weapon}\".");
                    return;
                }
            }
            else
            {
                await args.Channel.SendMessage(itemSearchResponse.Message);
                return;
            }

            var destructibleSearchResponse = await Api.SearchDestructible(buildingPart);

            if (destructibleSearchResponse.StatusCode == HttpStatusCode.OK)
            {
                destructibleShortname = destructibleSearchResponse.Data.FirstOrDefault().Key;

                if (destructibleShortname == null)
                {
                    await args.Channel.SendMessage($"Could not find building part or deployable called \"{buildingPart}\".");
                    return;
                }

                var searchResult = destructibleSearchResponse.Data.First().Value;

                // Todo: Verify building grade.
                if (searchResult.Type == Destructible.DestructibleType.BuildingBlock && string.IsNullOrEmpty(buildingGrade))
                {
                    await args.Channel.SendMessage("The specified building part requires a building grade.");
                    return;
                }

                // Request full damage data. Search result doesn't include damage values.
                var searchResponse = await Api.GetDestructible(searchResult.Id, new[] {buildingGrade});

                if (searchResponse.IsError)
                {
                    await args.Channel.SendMessage(searchResponse.Message);
                    return;
                }

                searchResult = searchResponse.Data;
                
            }
            else
            {
                await args.Channel.SendMessage(destructibleSearchResponse.Message);
                return;
            }

            await args.Channel.SendMessage(JsonConvert.SerializeObject(args.Args, Formatting.Indented));
        }
    }
}