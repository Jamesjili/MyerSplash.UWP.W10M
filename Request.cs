using JP.Utils.Data;
using System.Collections.Generic;
using System.Text;

namespace MyerSplashShared.API
{
    public static class Request
    {
        public static string HOST => "api.unsplash.com";

        public static string AppKey => "403d9934ce4bb8dbef44765692144e8c6fac6d2698950cb40b07397d6c6635fe";

        public static string GetNewImages => $"https://{HOST}/photos?";

        public static string SearchImages => $"https://{HOST}/search/photos?";

        public static string GetRandomImages => $"https://{HOST}/photos/random?";

        public static string GetCategories => $"https://{HOST}/categories?";

        public static string GetFeaturedImages => $"https://{HOST}/users/juniperphoton/photos/";

        public static string GetImageDetail => $"https://{HOST}/photos/";

        public static string GetTodayWallpaper => "https://juniperphoton.dev/myersplash/wallpapers/";

        public static string GetTodayThumbWallpaper => "https://juniperphoton.dev/myersplash/wallpapers/thumbs/";

        public static string AppendParamsToUrl(string baseUrl, List<KeyValuePair<string, string>> paramList)
        {
            if (!baseUrl.EndsWith("?"))
            {
                baseUrl = baseUrl + "?";
            }
            StringBuilder sb = new StringBuilder(baseUrl);
            foreach (var item in paramList)
            {
                sb.Append(item.Key + "=" + item.Value + "&");
            }
            return sb.ToString();
        }

        public static string AppendParamsToUrl(string baseUrl, bool withAuth)
        {
            StringBuilder sb = new StringBuilder(baseUrl);
            if (withAuth)
            {
                sb.Append("&uid=" + LocalSettingHelper.GetValue("uid"));
                sb.Append("&access_token=" + LocalSettingHelper.GetValue("access_token"));
            }
            return sb.ToString();
        }
    }
}