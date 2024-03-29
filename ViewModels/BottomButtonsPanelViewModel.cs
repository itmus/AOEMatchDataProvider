using AOEMatchDataProvider.Extensions.ExceptionHandling;
using AOEMatchDataProvider.Helpers.Navigation;
using AOEMatchDataProvider.Helpers.Windows;
using AOEMatchDataProvider.Services;
using AOEMatchDataProvider.Views;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOEMatchDataProvider.ViewModels
{
    public class BottomButtonsPanelViewModel : BindableBase
    {
        IStorageService StorageService { get; }

        public DelegateCommand GithubClickCommand { get; }
        public DelegateCommand SettingsClickCommand { get; }
        public DelegateCommand CoffeClickCommand { get; }

        public BottomButtonsPanelViewModel(IStorageService storageService)
        {
            StorageService = storageService;

            GithubClickCommand = new DelegateCommand(() => ExternalRedirectionHelper.RedirectTo("https://github.com/FIFOFridge/AOEMatchDataProvider"));
            SettingsClickCommand = new DelegateCommand(() =>
            {
                if(StorageService.TryGet<AppSettings>("appSettingsViewInstance", out AppSettings appConfig)) //appConfig window already exists
                {
                    appConfig.Focus(); //focus
                } 
                else //appConfig don't exist yet
                {
                    try
                    {
                        var newAppConfigInstance = new AppSettings();

                        StorageService.Create("appSettingsViewInstance", newAppConfigInstance, StorageEntryExpirePolicy.AfterSession);

                        newAppConfigInstance.Show();
                        newAppConfigInstance.Focus();

                        newAppConfigInstance.Closed += NewAppConfigInstance_Closed;
                    } 
                    catch(Exception e)
                    {
                        e.RethrowIfExceptionCantBeHandled();

                        if(StorageService.Has("appSettingsViewInstance"))
                            StorageService.Remove("appSettingsViewInstance");
                    }
                }

            });
            CoffeClickCommand = new DelegateCommand(() => ExternalRedirectionHelper.RedirectTo("coffe-redirection")); //patch before release
        }

        private void NewAppConfigInstance_Closed(object sender, EventArgs e)
        {
            StorageService.Remove("appSettingsViewInstance");
        }
    }
}
