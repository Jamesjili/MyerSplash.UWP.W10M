using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using MyerSplash.Data;
using MyerSplashShared.Data;

namespace MyerSplashShared.Service
{
    public class HighlightImageService : ImageServiceBase
    {
        private static DateTime END_TIME => DateTime.Parse("2021/12/31");
        private static DateTime START_TIME => DateTime.Parse("2017/03/20");
        private static int COUNT => 1747;

        public HighlightImageService(UnsplashImageFactory factory,
            CancellationTokenSourceFactory ctsFactory) : base(factory, ctsFactory)
        {
        }

        public async override Task<IEnumerable<UnsplashImage>> GetImagesAsync()
        {
            var list = new ObservableCollection<UnsplashImage>();

            var date = DateTime.Parse("2021/12/31");

            var start = date.AddDays(-(Page - 1) * COUNT);

            for (var i = 0; i < COUNT; i++)
            {
                var next = start.AddDays(-i);
                if (next < START_TIME)
                {
                    break;
                }
                    list.Add(UnsplashImageFactory.CreateHighlightImage(next, true));
            }

            return await Task.FromResult(list);
        }
    }
}
