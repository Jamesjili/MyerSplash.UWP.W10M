﻿using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using JP.Utils.Framework;
using Microsoft.QueryStringDotNET;
using MyerSplash.Common;
using MyerSplash.Data;
using MyerSplash.Model;
using MyerSplash.ViewModel.DataViewModel;
using MyerSplashShared.API;
using MyerSplashShared.Service;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using System.Collections.ObjectModel;
using System.Diagnostics;
using MyerSplashShared.Utils;
using MyerSplashShared.Data;
using Windows.ApplicationModel.Resources;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter;
using MyerSplash.View.Uc;
using MyerSplashCustomControl;
using JP.Utils.Data;
using Microsoft.AppCenter.Push;
using Microsoft.AppCenter.Crashes;
using System.IO;
using Windows.Data.Json;
using Newtonsoft.Json;

namespace MyerSplash.ViewModel
{
    public class MainViewModel : ViewModelBase, INavigable
    {
        private const int NEW_INDEX = 0;
        private const int HIGHLIGHTS_INDEX = 1;
        private const int RANDOM_INDEX = 2;
        private const int DEVELOPER_INDEX = 3;

        public static readonly string NewName = ResourceLoader.GetForCurrentView().GetString("New");
        public static readonly string RandomName = ResourceLoader.GetForCurrentView().GetString("Random");
        public static readonly string HighlightsName = ResourceLoader.GetForCurrentView().GetString("Highlights");
        public static readonly string DeveloperName = ResourceLoader.GetForCurrentView().GetString("Developer");

        public static readonly Dictionary<int, string> indexToName = new Dictionary<int, string>()
        {
            { NEW_INDEX,NewName },
            { HIGHLIGHTS_INDEX,HighlightsName },
            { RANDOM_INDEX,RandomName },
            { DEVELOPER_INDEX,DeveloperName },
        };

        private readonly Task _initTask;

        public event EventHandler<int> AboutToUpdateSelectedIndex;
        public event EventHandler DataUpdated;

        private Dictionary<int, ImageDataViewModel> _vms = new Dictionary<int, ImageDataViewModel>();

        private ImageDataViewModel _dataVM;
        public ImageDataViewModel DataVM
        {
            get
            {
                return _dataVM;
            }
            set
            {
                if (_dataVM != value)
                {
                    _dataVM = value;
                    RaisePropertyChanged(() => DataVM);
                }
            }
        }

        public bool IsInView { get; set; }

        private ObservableCollection<string> _tabs;
        public ObservableCollection<string> Tabs
        {
            get
            {
                return _tabs;
            }
            set
            {
                if (_tabs != value)
                {
                    _tabs = value;
                    RaisePropertyChanged(() => Tabs);
                }
            }
        }


        private ObservableCollection<PresetSearchWord> _presetSearchKeywords;
        public ObservableCollection<PresetSearchWord> PresetSearchKeywords
        {
            get
            {
                return _presetSearchKeywords;
            }
            set
            {
                if (_presetSearchKeywords != value)
                {
                    _presetSearchKeywords = value;
                    RaisePropertyChanged(() => _presetSearchKeywords);
                }
            }
        }

        #region Search

        private bool _showSearchBar;
        public bool ShowSearchBar
        {
            get
            {
                return _showSearchBar;
            }
            set
            {
                if (_showSearchBar != value)
                {
                    _showSearchBar = value;
                    RaisePropertyChanged(() => ShowSearchBar);
                }
            }
        }

        private string _searchKeyword;
        public string SearchKeyword
        {
            get
            {
                return _searchKeyword;
            }
            set
            {
                if (_searchKeyword != value)
                {
                    _searchKeyword = value;
                    RaisePropertyChanged(() => SearchKeyword);
                }
            }
        }

        private RelayCommand _searchCommand;
        public RelayCommand SearchCommand
        {
            get
            {
                if (_searchCommand != null) return _searchCommand;
                return _searchCommand = new RelayCommand(() =>
                  {
                      ShowSearchBar = true;

                      Events.LogEnterSearch();

                      NavigationService.AddOperation(() =>
                          {
                              if (ShowSearchBar)
                              {
                                  ShowSearchBar = false;
                                  return true;
                              }
                              else return false;
                          });
                  });
            }
        }

        private RelayCommand _hideSearchCommand;
        public RelayCommand HideSearchCommand
        {
            get
            {
                if (_hideSearchCommand != null) return _hideSearchCommand;
                return _hideSearchCommand = new RelayCommand(() =>
                  {
                      ShowSearchBar = false;
                  });
            }
        }

        private RelayCommand _beginSearchCommand;
        public RelayCommand BeginSearchCommand
        {
            get
            {
                if (_beginSearchCommand != null) return _beginSearchCommand;
                return _beginSearchCommand = new RelayCommand(async () =>
                  {
                      if (SearchKeyword == null)
                      {
                          return;
                      }

                      if (ShowSearchBar)
                      {
                          ShowSearchBar = false;
                          await SearchByKeywordAsync();
                          SearchKeyword = "";
                      }
                  });
            }
        }

        #endregion Search

        private RelayCommand _refreshCommand;
        public RelayCommand RefreshCommand
        {
            get
            {
                if (_refreshCommand != null) return _refreshCommand;
                return _refreshCommand = new RelayCommand(async () =>
                  {
                      await RefreshListAsync();
                  });
            }
        }

        private RelayCommand _retryCommand;
        public RelayCommand RetryCommand
        {
            get
            {
                if (_retryCommand != null) return _retryCommand;
                return _retryCommand = new RelayCommand(async () =>
                  {
                      FooterLoadingVisibility = Visibility.Visible;
                      FooterReloadVisibility = Visibility.Collapsed;
                      await DataVM.RetryAsync();
                  });
            }
        }

        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get
            {
                return _isRefreshing;
            }
            set
            {
                if (_isRefreshing != value)
                {
                    _isRefreshing = value;
                    RaisePropertyChanged(() => IsRefreshing);
                }
            }
        }

        private Visibility _footerLoadingVisibility;
        public Visibility FooterLoadingVisibility
        {
            get
            {
                return _footerLoadingVisibility;
            }
            set
            {
                if (_footerLoadingVisibility != value)
                {
                    _footerLoadingVisibility = value;
                    RaisePropertyChanged(() => FooterLoadingVisibility);
                }
            }
        }

        private Visibility _endVisiblity;
        public Visibility EndVisibility
        {
            get
            {
                return _endVisiblity;
            }
            set
            {
                if (_endVisiblity != value)
                {
                    _endVisiblity = value;
                    RaisePropertyChanged(() => EndVisibility);
                }
            }
        }

        private Visibility _noItemHintVisibility;
        public Visibility NoItemHintVisibility
        {
            get
            {
                return _noItemHintVisibility;
            }
            set
            {
                if (_noItemHintVisibility != value)
                {
                    _noItemHintVisibility = value;
                    RaisePropertyChanged(() => NoItemHintVisibility);
                }
            }
        }

        private Visibility _noNetworkHintVisibility;
        public Visibility NoNetworkHintVisibility
        {
            get
            {
                return _noNetworkHintVisibility;
            }
            set
            {
                if (_noNetworkHintVisibility != value)
                {
                    _noNetworkHintVisibility = value;
                    RaisePropertyChanged(() => NoNetworkHintVisibility);
                }
            }
        }

        private Visibility _footerReloadVisibility;
        public Visibility FooterReloadVisibility
        {
            get
            {
                return _footerReloadVisibility;
            }
            set
            {
                if (_footerReloadVisibility != value)
                {
                    _footerReloadVisibility = value;
                    RaisePropertyChanged(() => FooterReloadVisibility);
                }
            }
        }

        private RelayCommand _presentSettingsCommand;
        public RelayCommand PresentSettingsCommand
        {
            get
            {
                if (_presentSettingsCommand != null) return _presentSettingsCommand;
                return _presentSettingsCommand = new RelayCommand(() =>
                  {
                      SettingsPagePresented = true;

                      Events.LogEnterSettings();

                      NavigationService.AddOperation(() =>
                          {
                              if (SettingsPagePresented)
                              {
                                  SettingsPagePresented = false;
                                  return true;
                              }
                              return false;
                          });
                  });
            }
        }

        private bool _aboutPagePresented;
        public bool AboutPagePresented
        {
            get
            {
                return _aboutPagePresented;
            }
            set
            {
                if (_aboutPagePresented != value)
                {
                    _aboutPagePresented = value;
                    RaisePropertyChanged(() => AboutPagePresented);
                }
            }
        }

        private bool _downloadsPagePresented;
        public bool DownloadsPagePresented
        {
            get
            {
                return _downloadsPagePresented;
            }
            set
            {
                if (_downloadsPagePresented != value)
                {
                    _downloadsPagePresented = value;
                    RaisePropertyChanged(() => DownloadsPagePresented);
                }
            }
        }

        private bool _settingsPagePresented;
        public bool SettingsPagePresented
        {
            get
            {
                return _settingsPagePresented;
            }
            set
            {
                if (_settingsPagePresented != value)
                {
                    _settingsPagePresented = value;
                    RaisePropertyChanged(() => SettingsPagePresented);
                }
            }
        }

        private RelayCommand _presentDownloadsCommand;
        public RelayCommand PresentDownloadsCommand
        {
            get
            {
                if (_presentDownloadsCommand != null) return _presentDownloadsCommand;
                return _presentDownloadsCommand = new RelayCommand(() =>
                  {
                      DownloadsPagePresented = !DownloadsPagePresented;

                      Events.LogEnterDownloads();

                      if (DownloadsPagePresented)
                      {
                          NavigationService.AddOperation(() =>
                          {
                              if (DownloadsPagePresented)
                              {
                                  DownloadsPagePresented = false;
                                  return true;
                              }
                              return false;
                          });
                      }
                  });
            }
        }

        private RelayCommand _presentAboutCommand;
        public RelayCommand PresentAboutCommand
        {
            get
            {
                if (_presentAboutCommand != null) return _presentAboutCommand;
                return _presentAboutCommand = new RelayCommand(() =>
                  {
                      AboutPagePresented = true;

                      Events.LogEnterAbout();

                      NavigationService.AddOperation(() =>
                          {
                              if (AboutPagePresented)
                              {
                                  AboutPagePresented = false;
                                  return true;
                              }
                              return false;
                          });
                  });
            }
        }

        private int _selectedIndex;
        public int SelectedIndex
        {
            get
            {
                return _selectedIndex;
            }
            set
            {
                if (_selectedIndex != value)
                {
                    var lastValue = _selectedIndex;

                    _selectedIndex = value;

                    AboutToUpdateSelectedIndex?.Invoke(this, lastValue);

                    RaisePropertyChanged(() => SelectedIndex);

                    if (indexToName.ContainsKey(SelectedIndex))
                    {
                        Events.LogSelected(indexToName[SelectedIndex]);
                    }

                    if (value >= 0)
                    {
                        DataVM = CreateOrCacheDataVm(value);
                        if (DataVM != null && DataVM.DataList.Count == 0)
                        {
                            var task = RefreshListAsync();
                        }
                    }
                }
            }
        }

        private CancellationTokenSourceFactory _ctsFactory;
        public CancellationTokenSourceFactory CtsFactory
        {
            get
            {
                return _ctsFactory ?? (_ctsFactory = CancellationTokenSourceFactory.CreateDefault());
            }
        }

        private UnsplashImageFactory _normalFactory;
        public UnsplashImageFactory NormalFactory
        {
            get
            {
                return _normalFactory ?? (_normalFactory = new UnsplashImageFactory(false));
            }
        }

        private UnsplashImageFactory _featuredFactory;
        public UnsplashImageFactory FeaturedFactory
        {
            get
            {
                return _featuredFactory ?? (_featuredFactory = new UnsplashImageFactory(true));
            }
        }

#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
        public bool IsFirstActived { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations

        public MainViewModel()
        {
            FooterLoadingVisibility = Visibility.Collapsed;
            NoItemHintVisibility = Visibility.Collapsed;
            NoNetworkHintVisibility = Visibility.Collapsed;
            FooterReloadVisibility = Visibility.Collapsed;
            EndVisibility = Visibility.Collapsed;
            IsRefreshing = true;
            DownloadsPagePresented = false;

            SelectedIndex = -1;
            Tabs = new ObservableCollection<string>();
            PresetSearchKeywords = new ObservableCollection<PresetSearchWord>();

            DataVM = new ImageDataViewModel(this,
                new ImageService(Request.GetNewImages, NormalFactory, CtsFactory));

            _initTask = InitAsync();
        }

        private ImageDataViewModel CreateOrCacheDataVm(int index)
        {
            ImageDataViewModel vm = null;
            if (_vms.ContainsKey(index))
            {
                vm = _vms[index];
            }

            if (vm != null)
            {
                vm.Cancel();
            }
            else if (vm == null)
            {
                switch (index)
                {
                    case NEW_INDEX:
                        vm = new ImageDataViewModel(this, new ImageService(Request.GetNewImages, NormalFactory, CtsFactory));
                        break;
                    case DEVELOPER_INDEX:
                        vm = new ImageDataViewModel(this, new ImageService(Request.GetDeveloperPhotos, NormalFactory, CtsFactory));
                        break;
                    case RANDOM_INDEX:
                        vm = new RandomImagesDataViewModel(this, new RandomImageService(NormalFactory, CtsFactory));
                        break;
                    case HIGHLIGHTS_INDEX:
                        vm = new ImageDataViewModel(this, new HighlightImageService(NormalFactory, CtsFactory));
                        break;
                }

                if (vm != null)
                {
                    _vms[index] = vm;
                }
            }

            return vm;
        }

        private async Task SearchByKeywordAsync()
        {
            if (SearchKeyword == null)
            {
                return;
            }

            var searchService = new SearchImageService(NormalFactory, CtsFactory, SearchKeyword);

            if (Tabs.Count != indexToName.Count && Tabs.Count > 0)
            {
                Tabs.RemoveAt(Tabs.Count - 1);
            }
#pragma warning disable CA1304 // Specify CultureInfo
            Tabs.Add(SearchKeyword.ToUpper());
#pragma warning restore CA1304 // Specify CultureInfo

            SelectedIndex = Tabs.Count - 1;
            DataVM = new SearchResultViewModel(this, searchService);

            _vms[SelectedIndex] = DataVM;

            await RefreshListAsync();
        }

        private async Task RefreshListAsync()
        {
            IsRefreshing = true;
            await DataVM.RefreshAsync();

            Events.LogRefreshList(SelectedIndex);

            if (SelectedIndex == NEW_INDEX && AppSettings.Instance.EnableTodayRecommendation)
            {
                InsertTodayHighlight();
            }

            IsRefreshing = false;

            DataUpdated?.Invoke(this, null);
        }

        private void RemoveTodayHighlight()
        {
            var vm = _vms[NEW_INDEX];
            if (vm != null)
            {
                var first = vm.DataList.FirstOrDefault();
                if (first != null && !first.Image.IsUnsplash)
                {
                    vm.DataList.Remove(first);
                }
            }
        }

        private void InsertTodayHighlight()
        {
            var vm = _vms[NEW_INDEX];
            if (vm != null)
            {
#pragma warning disable CA1305 // Specify IFormatProvider
                var date = DateTime.Now.ToString("yyyyMMdd");
#pragma warning restore CA1305 // Specify IFormatProvider
                var first = vm.DataList.FirstOrDefault();
                if (first != null && first.Image.ID != date)
                {
                    RemoveTodayHighlight();

                    var imageItem = new ImageItem(UnsplashImageFactory.CreateTodayHighlightImage());
                    imageItem.Init();

                    vm.DataList.Insert(0, imageItem);
                }
            }
        }

        public void Activate(object param)
        {
            UpdateLiveTile();

            _ = HandleLaunchArg(param as string);
            _ = ShowFeatureDialogAsync();
        }

        private async Task ShowFeatureDialogAsync()
        {
            var key = "prompt_platforms";

            if (!LocalSettingHelper.HasValue(key))
            {
#pragma warning disable CS0162 // Unreachable code detected
                LocalSettingHelper.AddValue(key, true);
#pragma warning restore CS0162 // Unreachable code detected
                await Task.Delay(1000);
                var uc = new TipsControl();
                await PopupService.Instance.ShowAsync(uc);
            }
        }

        private void UpdateLiveTile()
        {
            if (App.AppSettings.EnableTile)
            {
                Debug.WriteLine("About to update tile.");
                LiveTileUpdater.UpdateLiveTile();
            }
        }

        private async Task HandleLaunchArg(string arg)
        {
            if (arg == Value.SEARCH)
            {
                ShowSearchBar = true;
            }
            else if (arg == Value.DOWNLOADS)
            {
                DownloadsPagePresented = true;
            }
            else
            {
                var queryStr = QueryString.Parse(arg);
                var action = queryStr[Key.ACTION_KEY];
                if (!queryStr.Contains(Key.FILE_PATH_KEY))
                {
                    return;
                }
                var filePath = queryStr[Key.FILE_PATH_KEY];
                if (filePath != null)
                {
                    switch (action)
                    {
                        case Value.SET_AS:
                            await WallpaperSettingHelper.SetAsBackgroundAsync(await StorageFile.GetFileFromPathAsync(filePath));
                            break;
                        case Value.VIEW:
                            await Launcher.LaunchFileAsync(await StorageFile.GetFileFromPathAsync(filePath));
                            break;
                    }
                }
            }
        }

        public void Deactivate(object param)
        {
        }

        public void OnLoaded()
        {
            var initTask = InitOnLoadedAsync();
        }

        private async Task InitAsync()
        {
            await Keys.Instance.InitializeAsync();
            AppCenter.Start(Keys.Instance.AppCenterKey, typeof(Analytics), typeof(Push), typeof(Crashes));
        }

        private async Task InitOnLoadedAsync()
        {
            await _initTask;

            SelectedIndex = NEW_INDEX;
            indexToName.Select(s => s.Value).ToList().ForEach(s =>
            {
                Tabs.Add(s);
            });

            await InitializeKeywordsAsync();
        }

        public async Task InitializeKeywordsAsync()
        {
            var uri = new Uri("ms-appx:///Assets/Json/preset_keywords.json");

            StorageFile file = null;
            try
            {
                file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            }
            catch (FileNotFoundException)
            {
                throw new ArgumentNullException("Please create a file named keys.json in assets folder");
            }

            var jsonString = await FileIO.ReadTextAsync(file);

            var list = JsonConvert.DeserializeObject<List<PresetSearchWord>>(jsonString);
            list.ForEach(s => PresetSearchKeywords.Add(s));
        }
    }
}