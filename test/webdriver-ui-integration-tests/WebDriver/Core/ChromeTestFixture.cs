﻿using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using System;

namespace Fsw.Enterprise.AuthCentral.Webdriver.Core
{
    public class ChromeTestFixture : TestFixtureBase
    {
        public ChromeTestFixture()
        {
            this.Driver = new ChromeDriver(@"C:\git\auth-central\test\webdriver-ui-integration-tests\Drivers");
        }
    }
}
