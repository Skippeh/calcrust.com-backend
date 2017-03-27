using System.Collections.Generic;
using System.Linq;
using Nancy;
using RustCalc.Common.Models;

namespace RustCalc.Api.V1.Modules
{
    public class RecipesModuleV1 : RustCalcModuleV1
    {
        public RecipesModuleV1() : base("/recipes")
        {
            Get["/"] = WrapMethod(_ => GetRecipes());
            Get["/{shortname}"] = WrapMethod((dynamic _) => GetRecipe(_.shortname));
        }

        private List<Recipe> GetRecipes()
        {
            return Data.Recipes.Values.ToList();
        }

        public Recipe GetRecipe(string shortname)
        {
            string lowerShortname = shortname.ToLower();
            var recipe = Data.Recipes.FirstOrDefault(kv => kv.Key.Shortname.ToLower() == lowerShortname).Value;

            if (recipe == null)
                throw new ApiResponseException(HttpStatusCode.NotFound, $"No recipe found with shortname '{shortname}'.");

            return recipe;
        }
    }
}