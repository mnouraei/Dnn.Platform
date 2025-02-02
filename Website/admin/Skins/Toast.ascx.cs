#region Copyright
// DotNetNuke® - https://www.dnnsoftware.com
// Copyright (c) 2002-2018
// by DotNetNuke Corporation
// All Rights Reserved
#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Microsoft.Extensions.DependencyInjection;
using DotNetNuke.Common;
using DotNetNuke.Abstractions;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Entities.Users;
using DotNetNuke.Framework.JavaScriptLibraries;
using DotNetNuke.Instrumentation;
using DotNetNuke.Services.Localization;
using DotNetNuke.Web.Client.ClientResourceManagement;

#endregion

namespace DotNetNuke.UI.Skins.Controls
{
    public partial class Toast : SkinObjectBase
    {
        private readonly INavigationManager _navigationManager;
        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(Toast));
        private static readonly string ToastCacheKey = "DNN_Toast_Config";

        private const string MyFileName = "Toast.ascx";

        protected string ServiceModuleName { get; private set; }

        protected string ServiceAction { get; private set; }

        public Toast()
        {
            _navigationManager = Globals.DependencyProvider.GetRequiredService<INavigationManager>();
        }

        public bool IsOnline()
        {
             var userInfo = UserController.Instance.GetCurrentUserInfo();
             return userInfo.UserID != -1;
        }

        public string GetNotificationLink()
        {
            return GetMessageLink() + "?view=notifications&action=notifications";
        }

        public string GetMessageLink()
        {
            return _navigationManager.NavigateURL(GetMessageTab(), "", string.Format("userId={0}", PortalSettings.UserId));
        }

        public string GetMessageLabel()
        {
            return Localization.GetString("SeeAllMessage", Localization.GetResourceFile(this, MyFileName));
        }

        public string GetNotificationLabel()
        {
            return Localization.GetString("SeeAllNotification", Localization.GetResourceFile(this, MyFileName));
        }

        //This method is copied from user skin object
        private int GetMessageTab()
        {
            var cacheKey = string.Format("MessageCenterTab:{0}:{1}", PortalSettings.PortalId, PortalSettings.CultureCode);
            var messageTabId = DataCache.GetCache<int>(cacheKey);
            if (messageTabId > 0)
                return messageTabId;

            //Find the Message Tab
            messageTabId = FindMessageTab();

            //save in cache
            //NOTE - This cache is not being cleared. There is no easy way to clear this, except Tools->Clear Cache
            DataCache.SetCache(cacheKey, messageTabId, TimeSpan.FromMinutes(20));

            return messageTabId;
        }

        //This method is copied from user skin object
        private int FindMessageTab()
        {
            //On brand new install the new Message Center Module is on the child page of User Profile Page
            //On Upgrade to 6.2.0, the Message Center module is on the User Profile Page
            var profileTab = TabController.Instance.GetTab(PortalSettings.UserTabId, PortalSettings.PortalId, false);
            if (profileTab != null)
            {
                var childTabs = TabController.Instance.GetTabsByPortal(profileTab.PortalID).DescendentsOf(profileTab.TabID);
                foreach (TabInfo tab in childTabs)
                {
                    foreach (KeyValuePair<int, ModuleInfo> kvp in ModuleController.Instance.GetTabModules(tab.TabID))
                    {
                        var module = kvp.Value;
                        if (module.DesktopModule.FriendlyName == "Message Center")
                        {
                            return tab.TabID;
                        }
                    }
                }
            }

            //default to User Profile Page
            return PortalSettings.UserTabId;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

			JavaScript.RequestRegistration(CommonJs.jQueryUI);

            ClientResourceManager.RegisterScript(Page, "~/Resources/Shared/components/Toast/jquery.toastmessage.js", DotNetNuke.Web.Client.FileOrder.Js.jQuery);
			ClientResourceManager.RegisterStyleSheet(Page, "~/Resources/Shared/components/Toast/jquery.toastmessage.css", DotNetNuke.Web.Client.FileOrder.Css.DefaultCss);

            InitializeConfig();
        }

        private void InitializeConfig()
        {
            ServiceModuleName = "InternalServices";
            ServiceAction = "NotificationsService/GetToasts";

            try
            {
                var toastConfig = DataCache.GetCache<IDictionary<string, string>>(ToastCacheKey);
                if (toastConfig == null)
                {
                    var configFile = Server.MapPath(Path.Combine(TemplateSourceDirectory, "Toast.config"));

                    if (File.Exists(configFile))
                    {
                        var xmlDocument = new XmlDocument { XmlResolver = null };
                        xmlDocument.Load(configFile);
                        var moduleNameNode = xmlDocument.DocumentElement?.SelectSingleNode("moduleName");
                        var actionNode = xmlDocument.DocumentElement?.SelectSingleNode("action");
                        var scriptsNode = xmlDocument.DocumentElement?.SelectSingleNode("scripts");

                        if (moduleNameNode != null && !string.IsNullOrEmpty(moduleNameNode.InnerText))
                        {
                            ServiceModuleName = moduleNameNode.InnerText;
                        }

                        if (actionNode != null && !string.IsNullOrEmpty(actionNode.InnerText))
                        {
                            ServiceAction = actionNode.InnerText;
                        }

                        if (scriptsNode != null && !string.IsNullOrEmpty(scriptsNode.InnerText))
                        {
                            addtionalScripts.Text = scriptsNode.InnerText;
                            addtionalScripts.Visible = true;
                        }
                    }

                    var config = new Dictionary<string, string>()
                    {
                        {"ServiceModuleName", ServiceModuleName },
                        {"ServiceAction", ServiceAction },
                        {"AddtionalScripts", addtionalScripts.Text },
                    };
                    DataCache.SetCache(ToastCacheKey, config);
                }
                else
                {
                    if (!string.IsNullOrEmpty(toastConfig["ServiceModuleName"]))
                    {
                        ServiceModuleName = toastConfig["ServiceModuleName"];
                    }

                    if (!string.IsNullOrEmpty(toastConfig["ServiceAction"]))
                    {
                        ServiceAction = toastConfig["ServiceAction"];
                    }

                    if (!string.IsNullOrEmpty(toastConfig["AddtionalScripts"]))
                    {
                        addtionalScripts.Text = toastConfig["AddtionalScripts"];
                        addtionalScripts.Visible = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }
}
