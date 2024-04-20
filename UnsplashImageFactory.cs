using JP.Utils.Data.Json;
using MyerSplashShared.API;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using Windows.Data.Json;

namespace MyerSplash.Data
{
    public class UnsplashImageFactory
    {
        private bool _isFeatured = false;

        public UnsplashImageFactory(bool isFeatured)
        {
            _isFeatured = isFeatured;
        }

        public ObservableCollection<UnsplashImage> GetImages(string json)
        {
            if (_isFeatured)
            {
                return GetFeaturedImageFromJson(json);
            }
            else
            {
                return GetImageFromJson(json);
            }
        }

        private static ObservableCollection<UnsplashImage> GetImageFromJson(string json)
        {
            var list = new ObservableCollection<UnsplashImage>();
            var array = JsonArray.Parse(json);
            foreach (var item in array)
            {
                var image = JsonConvert.DeserializeObject<UnsplashImage>(item.ToString());
                list.Add(image);
            }
            return list;
        }

        private static ObservableCollection<UnsplashImage> GetFeaturedImageFromJson(string json)
        {
            var list = new ObservableCollection<UnsplashImage>();
            var array = JsonArray.Parse(json);
            foreach (var item in array)
            {
                var image = JsonConvert.DeserializeObject<UnsplashImage>(item.ToString());
                list.Add(image);
            }
            return list;
        }

        public static UnsplashImage CreateTodayHighlightImage()
        {
            return CreateHighlightImage(DateTime.Now, false);
        }

        public static UnsplashImage CreateHighlightImage(DateTime dateTime, bool inHighlightList)
        {
            var date = dateTime.ToString("yyyyMMdd");
            var thumbUrl = $"{Request.GetTodayThumbWallpaper}/{date}.jpg";
            var largeUrl = $"{Request.GetTodayWallpaper}/{date}.jpg";

            var image = new UnsplashImage()
            {
                Likes = 100,
                Urls = new UnsplashUrl()
                {
                    Thumb = thumbUrl,
                    Regular = thumbUrl,
                    Small = thumbUrl,
                    Full = largeUrl,
                    Raw = largeUrl,
                },

                ColorValue = "#ffffff",
                ID = date,
                CreateTimeString = dateTime.ToString("yyyy-MM-dd"),
                IsUnsplash = false,
                IsInHighlightList = inHighlightList,
                Owner = new UnsplashUser()
                {
                    Name = "JuniperPhoton",
                    Id = "JuniperPhoton",
                }
            };

            return image;
        }
    }
}