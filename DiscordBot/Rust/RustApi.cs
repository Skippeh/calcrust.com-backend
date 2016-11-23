using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using DiscordBot.Rust.Models;
using Newtonsoft.Json;

namespace DiscordBot.Rust
{
    public class RustApi
    {
        public readonly string ApiUrl;
        
        public RustApi(string apiUrl)
        {
            ApiUrl = apiUrl;
        }

        public async Task<ApiResponse<Dictionary<string, Recipe>>> SearchRecipe(string term)
        {
            return await MakeRequest<Dictionary<string, Recipe>>($"recipes/search/{HttpUtility.UrlEncode(term)}/detailed");
        }

        private async Task<ApiResponse<T>> MakeRequest<T>(string apiMethod)
        {
            using (var webClient = new WebClient())
            {
                ApiResponse<T> response;

                try
                {
                    string json = await webClient.DownloadStringTaskAsync(ApiUrl + apiMethod);
                    response = JsonConvert.DeserializeObject<ApiResponse<T>>(json, Program.JsonSettings);
                }
                catch (WebException ex)
                {
                    string potentialJson;

                    using (var binaryReader = new StreamReader(ex.Response.GetResponseStream()))
                    {
                        potentialJson = await binaryReader.ReadToEndAsync();
                    }

                    try
                    {
                        response = JsonConvert.DeserializeObject<ApiResponse<T>>(potentialJson, Program.JsonSettings);
                        response.IsError = true;
                    }
                    catch (JsonException jsonEx)
                    {
                        return ApiResponse<T>.Error(((HttpWebResponse) ex.Response).StatusCode);
                    }
                }
                catch (JsonException ex)
                {
                    throw new NotImplementedException("Invalid json response", ex);
                }

                return response;
            }
        }

        public async Task<ApiResponse<RecipeRequirements>> GetRequirements(string shortname, int count)
        {
            return await MakeRequest<RecipeRequirements>($"recipes/{HttpUtility.UrlEncode(shortname)}/calculate/{count}/true/detailed");
        }
    }
}