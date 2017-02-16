using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Rg.Plugins.Popup.Services;
using PrismUnityApp2.Views;
using Rg.Plugins.Popup.Pages;
using System.Threading.Tasks;
using Xamarin.Forms;
using Prism.Common;
using System.Reflection;

namespace PrismUnityApp2.ViewModels
{
    public class MainPageViewModel : BindableBase, INavigationAware
    {
        private string _title;
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private string _selectedUser;

        public string SelectedUser
        {
            get { return _selectedUser; }
            set { SetProperty(ref _selectedUser, value); }
        }

        private PopupPage2 page;

        private readonly INavigationService _navigationService;

        public MainPageViewModel(INavigationService navigationService)
        {
            page = new PopupPage2();
            _navigationService = navigationService;
            ShowPopupCommand = new DelegateCommand(ShowPopup);
            Title = "Main Page";
        }

        public void OnNavigatedFrom(NavigationParameters parameters)
        {
            
        }

        public void OnNavigatedTo(NavigationParameters parameters)
        {
            if (parameters.ContainsKey("SelectedUser"))
            {
                User selectedUser = (User)parameters["SelectedUser"];
                SelectedUser = selectedUser.FirstName + " " + selectedUser.LastName;
            }
            else
            {
                SelectedUser = "User not selected!";
            }
        }

        private async void ShowPopup()
        {
            var para = new NavigationParameters();
            para.Add("users", new List<User> { new User { FirstName = "vova", LastName = "123" }, new User { FirstName = "Vasya", LastName = "qqq" } });
            await _navigationService.PushPopupAsync(page,para);
        }

        public ICommand ShowPopupCommand { get; set; }
    }

    public static class NavigationExtension
    {
        public static async Task PushPopupAsync(this INavigationService sender, PopupPage page, NavigationParameters parameters, bool animated = false)
        {
            OnNavigatedFrom(page, parameters);
            await PopupNavigation.PushAsync(page, true);
            OnNavigatedTo(page, parameters);
        }

        public static void InvokeViewAndViewModelAction<T>(object view, Action<T> action) where T : class
        {
            T viewAsT = view as T;
            if (viewAsT != null)
                action(viewAsT);

            var element = view as BindableObject;
            if (element != null)
            {
                var viewModelAsT = element.BindingContext as T;
                if (viewModelAsT != null)
                {
                    action(viewModelAsT);
                }
            }
        }

        public static async Task PopPopupAsync(this INavigationService sender, NavigationParameters parameters)
        {
            var mainPage = App.Current.MainPage;
            
            var page = PopupNavigation.PopupStack.Last();
            OnNavigatedFrom(page, parameters);
            await PopupNavigation.PopAsync(false);
            var topage = PopupNavigation.PopupStack.LastOrDefault() ??mainPage;
            OnNavigatedTo(topage, parameters);
        }

        public static void OnNavigatedTo(object page, NavigationParameters parameters)
        {
            if (page != null)
                InvokeViewAndViewModelAction<INavigationAware>(page, v => v.OnNavigatedTo(parameters));
        }

        public static void OnNavigatedFrom(object page, NavigationParameters parameters)
        {
            if (page != null)
                InvokeViewAndViewModelAction<INavigationAware>(page, v => v.OnNavigatedFrom(parameters));
        }
    }

    public class BehaviorBase<T> : Behavior<T> where T : BindableObject
    {
        public T AssociatedObject { get; private set; }

        protected override void OnAttachedTo(T bindable)
        {
            base.OnAttachedTo(bindable);
            AssociatedObject = bindable;

            if (bindable.BindingContext != null)
            {
                BindingContext = bindable.BindingContext;
            }

            bindable.BindingContextChanged += OnBindingContextChanged;
        }

        protected override void OnDetachingFrom(T bindable)
        {
            base.OnDetachingFrom(bindable);
            bindable.BindingContextChanged -= OnBindingContextChanged;
            AssociatedObject = null;
        }

        void OnBindingContextChanged(object sender, EventArgs e)
        {
            OnBindingContextChanged();
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();
            BindingContext = AssociatedObject.BindingContext;
        }
    }

    public class EventToCommandBehavior : BehaviorBase<View>
    {
        Delegate eventHandler;

        public static readonly BindableProperty EventNameProperty = BindableProperty.Create("EventName", typeof(string), typeof(EventToCommandBehavior), null, propertyChanged: OnEventNameChanged);
        public static readonly BindableProperty CommandProperty = BindableProperty.Create("Command", typeof(ICommand), typeof(EventToCommandBehavior), null);
        public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create("CommandParameter", typeof(object), typeof(EventToCommandBehavior), null);
        public static readonly BindableProperty InputConverterProperty = BindableProperty.Create("Converter", typeof(IValueConverter), typeof(EventToCommandBehavior), null);

        public string EventName
        {
            get { return (string)GetValue(EventNameProperty); }
            set { SetValue(EventNameProperty, value); }
        }

        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        public IValueConverter Converter
        {
            get { return (IValueConverter)GetValue(InputConverterProperty); }
            set { SetValue(InputConverterProperty, value); }
        }

        protected override void OnAttachedTo(View bindable)
        {
            base.OnAttachedTo(bindable);
            RegisterEvent(EventName);
        }

        protected override void OnDetachingFrom(View bindable)
        {
            DeregisterEvent(EventName);
            base.OnDetachingFrom(bindable);
        }

        void RegisterEvent(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            EventInfo eventInfo = AssociatedObject.GetType().GetRuntimeEvent(name);
            if (eventInfo == null)
            {
                throw new ArgumentException(string.Format("EventToCommandBehavior: Can't register the '{0}' event.", EventName));
            }
            MethodInfo methodInfo = typeof(EventToCommandBehavior).GetTypeInfo().GetDeclaredMethod("OnEvent");
            eventHandler = methodInfo.CreateDelegate(eventInfo.EventHandlerType, this);
            eventInfo.AddEventHandler(AssociatedObject, eventHandler);
        }

        void DeregisterEvent(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            if (eventHandler == null)
            {
                return;
            }
            EventInfo eventInfo = AssociatedObject.GetType().GetRuntimeEvent(name);
            if (eventInfo == null)
            {
                throw new ArgumentException(string.Format("EventToCommandBehavior: Can't de-register the '{0}' event.", EventName));
            }
            eventInfo.RemoveEventHandler(AssociatedObject, eventHandler);
            eventHandler = null;
        }

        void OnEvent(object sender, object eventArgs)
        {
            if (Command == null)
            {
                return;
            }

            object resolvedParameter;
            if (CommandParameter != null)
            {
                resolvedParameter = CommandParameter;
            }
            else if (Converter != null)
            {
                resolvedParameter = Converter.Convert(eventArgs, typeof(object), null, null);
            }
            else
            {
                resolvedParameter = eventArgs;
            }

            if (Command.CanExecute(resolvedParameter))
            {
                Command.Execute(resolvedParameter);
            }
        }

        static void OnEventNameChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var behavior = (EventToCommandBehavior)bindable;
            if (behavior.AssociatedObject == null)
            {
                return;
            }

            string oldEventName = (string)oldValue;
            string newEventName = (string)newValue;

            behavior.DeregisterEvent(oldEventName);
            behavior.RegisterEvent(newEventName);
        }
    }


}
