using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using Prism.Navigation;
using System.Windows.Input;
using Rg.Plugins.Popup.Services;
using System.Threading.Tasks;

namespace PrismUnityApp2.ViewModels
{
    public class PopupPage2ViewModel : BindableBase, INavigationAware
    {


        private List<User> users;

        public List<User> Users
        {
            get { return users; }
            set { SetProperty(ref users, value); }
        }



        
        public User SelectedUser { get; set; }

        public ICommand SelectUserCommand { get; set; }
        private readonly INavigationService _navigationService;
        public PopupPage2ViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            CloseCommand = new DelegateCommand(async () => { await ClosePopup(); });
            SelectUserCommand = new DelegateCommand(() => { });
        }

        private async Task ClosePopup()
        {
            var navParameter = new NavigationParameters();
            navParameter.Add("selectedUser", SelectedUser);
            await _navigationService.PopPopupAsync(navParameter);
        }

        public void OnNavigatedFrom(NavigationParameters parameters)
        {
            parameters.Add("SelectedUser", SelectedUser);
        }

        public void OnNavigatedTo(NavigationParameters parameters)
        {
            if (parameters.ContainsKey("users"))
                Users = (List<User>)parameters["users"];
        }

        public ICommand CloseCommand { get; set; }
    }

    public class User
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int ID { get; set; }
    }


}
