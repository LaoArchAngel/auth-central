﻿using Fsw.Enterprise.AuthCentral.Webdriver.Core;
using Fsw.Enterprise.AuthCentral.WebDriver.Tests.AuthCentral.Pages.Public;
using Fsw.Enterprise.AuthCentral.WebDriver.Tests.AuthCentral.UIMaps.Admin;
using Fsw.Enterprise.AuthCentral.WebDriver.Tests.AuthCentral.UIMaps.LoggedIn;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.PageObjects;


namespace Fsw.Enterprise.AuthCentral.WebDriver.Tests.AuthCentral.Pages.Admin
{
    public class UserListPage : LoggedInAdminPageObjectBase
    {
        private UserListUIElementMap _userListUI;

        public UserListPage(IWebDriver driver) : base(driver)
        {
            _userListUI = new UserListUIElementMap();
            PageFactory.InitElements(driver, _userListUI);
        }

        internal UserListUIElementMap Map { get { return _userListUI; } }

        internal NewUserPage ClickCreateUserButton()
        {
            _userListUI.CreateUserButton.Click();
            return new NewUserPage(Driver);
        }

        //Change these methods to what you need
        //internal ProfilePage UpdateName(string firstName, string lastName)
        //{
        //    _userListUI.FirstNameBox.Clear();
        //    _userListUI.FirstNameBox.SendKeys(firstName);
        //    _userListUI.LastNameBox.Clear();
        //    _userListUI.LastNameBox.SendKeys(lastName);
        //    _userListUI.NameSaveButton.Click();
        //    return this;
        //}
        

        public bool UserExists(string email, string otherEmail = null)
        {
            _userListUI.EmailSearchBox.SendKeys(email);
            _userListUI.EmailSearchButton.Click();
            UserProfilePage userProfilePage = new UserProfilePage(Driver);
            UserClaimsPage userClaimsPage = new UserClaimsPage(Driver);
            if (!userProfilePage.IsOnUserProfilePage() && !userClaimsPage.IsOnUserClaimsPage() && otherEmail != null)
            {
                _userListUI.EmailSearchBox.SendKeys(otherEmail);
                _userListUI.EmailSearchButton.Click();
            }
            if (!userProfilePage.IsOnUserProfilePage() && !userClaimsPage.IsOnUserClaimsPage())
            { return false; }
            else { return true; }
        }
    }
}
