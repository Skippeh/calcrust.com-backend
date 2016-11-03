using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using Nancy;
using Nancy.Extensions;
using Nancy.Routing;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using WebAPI.Models;

namespace WebAPI
{
    public class ApiModule : NancyModule
    {
        private RustData data = DataManager.Data;

        private readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public ApiModule()
        {
            Config.Load(); // Load config before every request.

            // Items
            Get["/items"] = WrapMethod(GetItems);
            Get["/items/{shortname}"] = WrapMethod(GetItem);
            Get["/items/search/{term}"] = WrapMethod(SearchItems);

            // Recipes
            Get["/recipes"] = WrapMethod(parameters => GetRecipes(false));
            Get["/recipes/detailed"] = WrapMethod(parameters => GetRecipes(true));

            Get["/recipes/{shortname}"] = WrapMethod((dynamic _) => GetRecipe(_.shortname, false));
            Get["/recipes/{shortname}/detailed"] = WrapMethod((dynamic _) => GetRecipe(_.shortname, true));

            Get["/recipes/search/{term}"] = WrapMethod((dynamic _) => SearchRecipes(_.term, false));
            Get["/recipes/search/{term}/detailed"] = WrapMethod((dynamic _) => SearchRecipes(_.term, true));

            Get["/recipes/{shortname}/calculate/{amount:decimal}/{total:bool}"] = WrapMethod((dynamic _) => CalculateRecipe(_.shortname, _.amount, _.total, false));
            Get["/recipes/{shortname}/calculate/{amount:decimal}/{total:bool}/detailed"] = WrapMethod((dynamic _) => CalculateRecipe(_.shortname, _.amount, _.total, true));

            // Cookables
            Get["/cookables"] = WrapMethod(_ => GetCookables(false));
            Get["/cookables/detailed"] = WrapMethod(_ => GetCookables(true));

            Get["/cookables/{shortname}"] = WrapMethod((dynamic _) => GetCookable(_.shortname, false));
            Get["/cookables/{shortname}/detailed"] = WrapMethod((dynamic _) => GetCookable(_.shortname, true));

            Get["/cookables/search/{term}"] = WrapMethod((dynamic _) => SearchCookables(_.term, false));
            Get["/cookables/search/{term}/detailed"] = WrapMethod((dynamic _) => SearchCookables(_.term, true));

            // Damage info
            Get["/damages"] = WrapMethod((dynamic _) => GetDamageInfos(false));
            Get["/damages/detailed"] = WrapMethod((dynamic _) => GetDamageInfos(true));
            Get["/damages/items"] = WrapMethod((dynamic _) => GetDamageableItems());
            Get["/damages/items/{shortname}"] = WrapMethod((dynamic _) => GetDamageableItemInfo(_.shortname));
            Get["/damages/{shortname}"] = WrapMethod((dynamic _) => GetDamageInfo(_.shortname));

            // Search all
            Get["/search/{term}"] = WrapMethod((dynamic _) => SearchAll(_.term, false));
            Get["/search/{term}/detailed"] = WrapMethod((dynamic _) => SearchAll(_.term, true));
            
            Get["/dump"] = WrapMethod(_ =>
            {
                var data = DataManager.Data;
                return new ApiResponse(new
                {
                    data.Meta,
                    data.Items,
                    recipes = data.Recipes.ToDictionary(keyval => keyval.Key, keyval => WrapRecipe(keyval.Value, false)),
                    cookables = data.Cookables.ToDictionary(keyval => keyval.Key, keyval => WrapCookable(keyval.Value, false))
                });
            });
            Get["/meta"] = WrapMethod(GetMeta);
            Post["/upload"] = WrapMethod(UploadData, true);
        }

        /// <param name="allowOffline">If true then this method will be called even if rust data is currently unavailable.</param>
        private Func<object, object> WrapMethod(Func<object, object> func, bool allowOffline = false)
        {
            data = DataManager.Data;
            return parameters =>
            {
                object apiResponse;

                if (data == null || !allowOffline && data.Disposed)
                {
                    apiResponse = Error(HttpStatusCode.ServiceUnavailable, "Api is currently unavailable, try again.");
                }
                else
                {
                    apiResponse = func(parameters);
                }
                
                Response response = new Response
                {
                    StatusCode = (apiResponse as ApiResponse)?.StatusCode ?? HttpStatusCode.OK,
                    Contents = stream =>
                    {
                        var bytes = Encoding.UTF8.GetBytes(apiResponse is string ? (string)apiResponse : JsonConvert.SerializeObject(apiResponse, serializerSettings));
                        stream.Write(bytes, 0, bytes.Length);
                    },
                    ContentType = "application/json"
                };
                return response;
            };
        }

        private ApiResponse SearchAll(string term, bool detailed)
        {
            var items = searchItems(term);
            var recipes = searchRecipes(term, detailed);
            var cookables = searchCookables(term, detailed);

            return new ApiResponse(new
            {
                items,
                recipes,
                cookables
            });
        }

        private ApiResponse GetDamageInfo(string shortnames)
        {
            Dictionary<string, DamageInfo> result = new Dictionary<string, DamageInfo>();
            string[] splitShortnames = shortnames.ToLower().Split('&');

            foreach (string shortname in splitShortnames)
            {
                if (data.DamageInfo.ContainsKey(shortname))
                {
                    result.Add(shortname, data.DamageInfo[shortname]);
                }
                else
                {
                    return Error(HttpStatusCode.NotFound, "Could not find an item with the shortname: '" + shortname + "'.");
                }
            }

            return new ApiResponse(result);
        }

        private ApiResponse GetDamageInfos(bool detailed)
        {
            return new ApiResponse(data.DamageInfo.Keys.Select(shortname =>
            {
                if (detailed)
                    return (object) data.Items[shortname];

                return shortname;
            }).ToList());
        }

        private ApiResponse GetDamageableItems()
        {
            List<string> allItemNames = data.DamageInfo.Values.First().Damages.Keys.ToList();

            // Only include the building block types and not all of its material upgrades.
            allItemNames = allItemNames.DistinctBy(name =>
            {
                if (name.Contains(':'))
                {
                    name = name.Substring(0, name.LastIndexOf(':'));
                }

                return name;
            }).ToList();

            List<DamageableItem> allItems = allItemNames.Select(name =>
            {
                var item = new DamageableItem();
                item.IsBuildingBlock = name.Contains(':');

                item.Id = item.IsBuildingBlock
                            ? name.Substring(0, name.LastIndexOf(':'))
                            : name;

                if (!item.IsBuildingBlock)
                {
                    item.Name = data.Items[item.Id].Name;
                }
                else
                {
                    item.Name = item.Id;
                }

                return item;
            }).ToList();

            return new ApiResponse(allItems);
        }

        private ApiResponse GetDamageableItemInfo(string shortname)
        {
            string lowerName = shortname.ToLower();

            bool exists = data.DamageInfo.Values.First().Damages.Keys.Any(name => lowerName == name.ToLower());

            if (!exists)
            {
                return Error(HttpStatusCode.NotFound, "Could not find the item/building block with the name '" + shortname + "'.");
            }

            Dictionary<string, DamageInfo.WeaponInfo> result = data.DamageInfo.ToDictionary(kv => kv.Key, kv =>
            {
                return kv.Value.Damages.First(kv2 => kv2.Key.ToLower() == lowerName).Value;
            });

            string displayName = shortname;

            if (data.Items.ContainsKey(shortname))
            {
                displayName = data.Items[shortname].Name;
            }
            else
            {
                // Todo: return building block name.
                displayName = displayName.Substring(0, displayName.LastIndexOf(':'));
            }

            return new ApiResponse(new
            {
                name = displayName,
                data = result
            });
        }

        private ApiResponse GetCookables(bool detailed)
        {
            var dictionary = data.Cookables.ToDictionary(keyval => keyval.Key, keyval => WrapCookable(keyval.Value, detailed));
            return new ApiResponse(dictionary);
        }

        private ApiResponse GetCookable(string shortname, bool detailed)
        {
            Cookable cookable = data.Cookables.FirstOrDefault(keyval => keyval.Key == shortname).Value;

            if (cookable == null)
                return Error(HttpStatusCode.NotFound, "Could not find a cookable with that name or shortname as the output.");

            return new ApiResponse(WrapCookable(cookable, detailed));
        }

        private ApiResponse SearchCookables(string term, bool detailed)
        {
            string searchTerm = term.Trim().ToLower();

            if (string.IsNullOrEmpty(searchTerm))
                return Error(HttpStatusCode.BadRequest, "Search term is empty.");

            return new ApiResponse(searchCookables(searchTerm, detailed));
        }

        private ApiResponse GetMeta(dynamic parameters)
        {
            return new ApiResponse(DataManager.Data.Meta);
        }

        private ApiResponse UploadData(dynamic parameters)
        {
            if (DataManager.IsBanned(Request.UserHostAddress))
                return Error(HttpStatusCode.Forbidden);

            string password = Request.Headers["pw"].FirstOrDefault();
            
            if (password != Config.UploadPassword)
            {
                Console.WriteLine("Wrong auth password passed to /upload, ignoring request from " + Request.UserHostAddress + ".");
                DataManager.IncrementTries(Request.UserHostAddress);

                if (DataManager.IsBanned(Request.UserHostAddress))
                    Console.WriteLine("Wrong auth password entered too many times, ignoring all /upload requests from now on from " + Request.UserHostAddress + ".");

                return Error(HttpStatusCode.Unauthorized);
            }

            DataManager.ResetTries(Request.UserHostAddress);

            try
            {
                string json = (string) Request.Form.data;
                DataManager.Save(json);
                Console.WriteLine("Updated data from remote: " + Request.UserHostAddress + "!");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Failed to save data! Most likely input error.\n" + ex);
                return Error(HttpStatusCode.InternalServerError, "Data invalid!");
            }

            return OK();
        }

        private ApiResponse GetRecipe(string shortname, bool detailed)
        {
            Recipe recipe = data.Recipes.FirstOrDefault(keyval =>
            {
                string name = shortname.ToLower();
                return keyval.Key.ToLower() == name || keyval.Value.Output.Result.Name.ToLower() == name;
            }).Value;

            if (recipe == null)
                return Error(HttpStatusCode.NotFound, "Could not find a recipe with that shortname or name as the output item.");

            return new ApiResponse(WrapRecipe(recipe, detailed));
        }

        private ApiResponse GetRecipes(bool detailed)
        {
            return new ApiResponse(data.Recipes.ToDictionary(keyval => keyval.Key, keyval => WrapRecipe(keyval.Value, detailed)));
        }

        private ApiResponse CalculateRecipe(string shortName, double amount, bool totalRequirements, bool detailed)
        {
            bool precise = false;
            {
                object preciseVal = Request.Query["precise"];

                if (preciseVal != null)
                {
                    bool.TryParse(preciseVal.ToString(), out precise);
                }
            }

            Item item = data.Items.FirstOrDefault(i => i.Key.ToLower() == shortName.ToLower()).Value;

            if (item == null)
                return Error(HttpStatusCode.NotFound, "No recipe found with that shortname.");

            var recipe = data.Recipes.FirstOrDefault(r => r.Key == item.Shortname).Value;

            if (recipe == null)
                return Error(HttpStatusCode.NotFound, "No recipe found with that shortname.");

            var requirements = recipe.CalculateRequirements(amount, totalRequirements, !precise);

            return new ApiResponse(new
            {
                requirements.TotalRequirements,
                requirements.Count,
                requirements.TTC,
                output = new
                {
                    count = requirements.Output.Count,
                    item = detailed ? requirements.Output.Result : (object) requirements.Output.Result.Shortname
                },
                input = requirements.Input.Select(input => new
                {
                    input.Count,
                    item = detailed ? input.Result : (object) input.Result.Shortname
                })
            });
        }

        private ApiResponse SearchRecipes(string term, bool detailed)
        {
            string searchTerm = term.Trim().ToLower();

            if (string.IsNullOrEmpty(searchTerm))
                return Error(HttpStatusCode.BadRequest, "Search term is empty.");

            return new ApiResponse(searchRecipes(searchTerm, detailed));
        }

        private ApiResponse GetItem(dynamic parameters)
        {
            string name = ((string) parameters.shortname).ToLower();
            Item item = data.Items.FirstOrDefault(keyval => keyval.Key.ToLower() == name || keyval.Value.Name.ToLower() == name).Value;

            if (item == null)
                return Error(HttpStatusCode.NotFound, "Could not find an item with that name or shortname.");

            return new ApiResponse(item);
        }

        private ApiResponse GetItems(dynamic parameters)
        {
            return new ApiResponse(data.Items.ToDictionary(keyval => keyval.Key, keyval =>
            {
                var item = keyval.Value;
                return item;
            }));
        }

        private ApiResponse SearchItems(dynamic parameters)
        {
            string searchTerm = ((string) parameters.term).ToLower();
            return new ApiResponse(searchItems(searchTerm));
        }

        private Dictionary<string, Item> searchItems(string searchTerm)
        {
            return data.Items.Where(keyval => keyval.Value.Name.ToLower().Contains(searchTerm.ToLower())).ToDictionary(keyval => keyval.Key, keyval => keyval.Value);
        }

        private Dictionary<string, object> searchRecipes(string searchTerm, bool detailed)
        {
            return data.Recipes.Where(keyval => keyval.Value.Output.Result.Name.ToLower().Contains(searchTerm)).ToDictionary(keyval => keyval.Key, keyval => WrapRecipe(keyval.Value, detailed));
        }

        private Dictionary<string, object> searchCookables(string searchTerm, bool detailed)
        {
            return data.Cookables.Where(keyval => keyval.Value.Output.Result.Name.ToLower().Contains(searchTerm)).ToDictionary(keyval => keyval.Key, keyval => WrapCookable(keyval.Value, detailed));
        }

        private ApiResponse Error(HttpStatusCode statusCode, string message = null)
        {
            return new ApiResponse(null, statusCode, message);
        }

        private ApiResponse OK()
        {
            return new ApiResponse(null);
        }

        private object WrapRecipe(Recipe recipe, bool detailed)
        {
            if (recipe == null)
                return null;

            return new
            {
                output = new
                {
                    recipe.Output.Count,
                    item = detailed ? (object)recipe.Output.Result : recipe.Output.Result.Shortname
                },
                input = recipe.Input.Select(input => new
                {
                    input.Count,
                    item = detailed ? (object)input.Result : input.Result.Shortname
                }),
                recipe.TTC,
                recipe.Level,
                recipe.Price,
                parent = detailed ? (object)recipe.Parent : recipe.Parent?.Shortname
            };
        }

        private object WrapCookable(Cookable cookable, bool detailed)
        {
            if (cookable == null)
                return null;
            
            return new
            {
                ovenList = detailed ? (object)cookable.UsableOvens : cookable.UsableOvens.Select(item => new
                {
                    fuelConsumed = item.FuelConsumed,
                    item = item.Item.Shortname
                }),
                cookable.TTC,
                output = new
                {
                    cookable.Output.Count,
                    item = detailed ? (object)cookable.Output.Result : cookable.Output.Result.Shortname
                }
            };
        }
    }
}