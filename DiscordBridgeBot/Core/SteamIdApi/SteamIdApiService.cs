using AzyWorks;

using Newtonsoft.Json;

using System.Text.RegularExpressions;

namespace DiscordBridgeBot.Core.SteamIdApi
{
    public static class SteamIdApiService
    {
        public const string BaseUrl = "https://steamid.xyz/query";

        public static string FormatUrl(string id)
        {
            return BaseUrl.Replace("query", id);
        }

        public static async Task<SteamIdApiResult> GetAsync(string id)
        {
            try
            {
                using (var httpClient = new HttpClient())
                using (var request = new HttpRequestMessage(HttpMethod.Get, FormatUrl(id)))
                {

                    request.Headers.Clear();
                    request.Headers.Accept.Clear();
                    request.Headers.AcceptEncoding.Clear();
                    request.Headers.AcceptLanguage.Clear();

                    request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/html"));
                    request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/xhtml+xml"));
                    request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/xml;q=0.9"));
                    request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("image/avif"));
                    request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("image/webp"));
                    request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("image/apng"));

                    request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*;q=0.8"));
                    request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/signed-exchange;v=b3;q=0.7"));

                    request.Headers.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
                    request.Headers.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("deflate"));
                    request.Headers.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("br"));

                    request.Headers.AcceptLanguage.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("cs-CZ"));
                    request.Headers.AcceptLanguage.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("cs;q=0.9"));
                    request.Headers.AcceptLanguage.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("en;q=0.8"));
                    request.Headers.AcceptLanguage.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("sk;q=0.7"));

                    request.Headers.CacheControl.MaxAge = TimeSpan.Zero;

                    request.Headers.Add("dnt", "1");
                    request.Headers.Add("referer", "https://steamid.xyz/");
                    request.Headers.Add("sec-ch-ua", "\"Google Chrome\";v=\"111\", \"Not(A:Brand\";v=\"8\", \"Chromium\";v=\"111\"");
                    request.Headers.Add("sec-ch-ua-mobile", "?0");
                    request.Headers.Add("sec-ch-ua-platform", "Windows");
                    request.Headers.Add("sec-fetch-dest", "document");
                    request.Headers.Add("sec-fetch-mode", "navigate");
                    request.Headers.Add("sec-fetch-site", "same-origin");
                    request.Headers.Add("sec-fetch-user", "?1");
                    request.Headers.Add("upgrade-insecure-requests", "1");
                    request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/111.0.0.0 Safari/537.36");

                    Log.SendInfo("SteamIdApiService", $"Sending request for {id} ({request.RequestUri})");

                    var response = await httpClient.SendAsync(request);

                    Log.SendInfo("SteamIdApiService", $"Status Code: {response.StatusCode}");

                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        return null;
                    }

                    var html = await response.Content.ReadAsStringAsync();
                    var lines = Regex.Split(html, "\r\n|\r|\n");
                    var result = new SteamIdApiResult();

                    Log.SendInfo("SteamIdApiService", $"Html");
                    Log.SendInfo("SteamIdApiService", html);
                    Log.SendInfo("SteamIdApiService", $"Lines: {lines}");

                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].StartsWith("<i>Real Name:"))
                        {
                            Log.SendInfo("SteamIdApiService", $"Real Name line!");
                            Log.SendInfo("SteamIdApiService", lines[i]);

                            result.RealName = lines[i]
                                .Replace("<i>RealName:", "")
                                .Replace("<br>", "")
                                .Replace("</i>", "")
                                .Trim();
                        }
                        else if (lines[i].StartsWith("<i>Country:"))
                        {
                            Log.SendInfo("SteamIdApiService", $"Country line!");
                            Log.SendInfo("SteamIdApiService", lines[i]);

                            result.Country = lines[i]
                                .Replace("<i>Country:", "")
                                .Replace("<br>", "")
                                .Replace("</i>", "")
                                .Trim();
                        }
                        else if (lines[i].StartsWith("<i>Account Created:"))
                        {
                            Log.SendInfo("SteamIdApiService", $"Account Created line!");
                            Log.SendInfo("SteamIdApiService", lines[i]);

                            result.CreatedAt = lines[i]
                                .Replace("<i>Account Created:", "")
                                .Replace("</i>", "")
                                .Replace("<br>", "")
                                .Trim();
                        }
                        else if (lines[i].StartsWith("<i>Last Logoff:"))
                        {
                            Log.SendInfo("SteamIdApiService", $"Last Logoff line!");
                            Log.SendInfo("SteamIdApiService", lines[i]);

                            result.LastLogOffAt = lines[i]
                                .Replace("<i>Last Logoff:", "")
                                .Replace("</i>", "")
                                .Replace("<br>", "")
                                .Trim();
                        }
                        else if (lines[i].StartsWith("<i>Status:"))
                        {
                            Log.SendInfo("SteamIdApiService", $"Status line!");
                            Log.SendInfo("SteamIdApiService", lines[i]);

                            result.Status = lines[i]
                                .Replace("<i>Status:", "")
                                .Replace("</i>", "")
                                .Replace("<br>", "")
                                .Trim();
                        }
                        else if (lines[i].StartsWith("<i>Visibility:"))
                        {
                            Log.SendInfo("SteamIdApiService", $"Visibility line!");
                            Log.SendInfo("SteamIdApiService", lines[i]);

                            result.Visibility = lines[i]
                                .Replace("<i>Visibility:", "")
                                .Replace("</i>", "")
                                .Replace("<br>", "")
                                .Trim();
                        }
                        else if (lines[i].Contains("User is VAC Clean"))
                        {
                            Log.SendInfo("SteamIdApiService", $"VAC Status line!");
                            Log.SendInfo("SteamIdApiService", lines[i]);

                            result.IsVacBanned = false;
                        }
                        else if (lines[i].Contains("User is Trade Clean"))
                        {
                            Log.SendInfo("SteamIdApiService", $"Trade Status line!");
                            Log.SendInfo("SteamIdApiService", lines[i]);

                            result.IsTradeClean = true;
                        }
                        else if (lines[i].Contains("User is Community Clean"))
                        {
                            Log.SendInfo("SteamIdApiService", $"Community Status line!");
                            Log.SendInfo("SteamIdApiService", lines[i]);

                            result.IsCommunityClean = true;
                        }
                        else if (lines[i].StartsWith("<img class=\"avatar\" src=\""))
                        {
                            Log.SendInfo("SteamIdApiService", $"Avatar line!");
                            Log.SendInfo("SteamIdApiService", lines[i]);

                            result.AvatarUrl = lines[i]
                                .Replace("<img class=\"avatar\" src=\"", "")
                                .Replace("\">", "")
                                .Trim();
                        }
                    }

                    Log.SendInfo("SteamIdApiService", $"{JsonConvert.SerializeObject(result, Formatting.Indented)}");

                    return result;
                }
            }
            catch (Exception ex)
            {
                Log.SendError("SteamIdApiService", ex);
            }

            return null;
        }
    }
}
