﻿using OpenQA.Selenium;
using OpenQA.Selenium.Support.PageObjects;

namespace Fsw.Enterprise.AuthCentral.WebDriver.Tests.AuthCentral.Pages
{
    abstract class UserUIElementMap
    {
        [FindsBy(How = How.LinkText, Using = "Claims")]
        public IWebElement ClaimsMenuLink;
        [FindsBy(How = How.LinkText, Using = "Profile")]
        public IWebElement ProfileMenuLink;
    }
}
