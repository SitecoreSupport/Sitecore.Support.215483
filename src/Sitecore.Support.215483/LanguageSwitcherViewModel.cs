using Sitecore.Diagnostics;
using Sitecore.EmailCampaign.Controls;
using Sitecore.Modules.EmailCampaign;
using Sitecore.Modules.EmailCampaign.Messages;
using Sitecore.Modules.EmailCampaign.Messages.Interfaces;
using Sitecore.Mvc.Presentation;
using Sitecore.Web.UI.Controls.Common.UserControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.Support.EmailCampaign.Controls.LanguageSwitcher
{
    public class LanguageSwitcherViewModel : Sitecore.EmailCampaign.Controls.LanguageSwitcher.LanguageSwitcherViewModel
    {

        private readonly ILanguageRepository languageRepository;

        private readonly Factory factory;

        private UserControl UserControl { get; set; }

        private readonly ISitecoreViewModelHelper _sitecoreViewModelHelper;
        public LanguageSwitcherViewModel()
      : this(Factory.Instance, new LanguageRepository(), new SitecoreViewModelHelper())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LanguageSwitcherViewModel"/> class 
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="languageRepository">the language repository object.</param>
        /// <param name="sitecoreViewModelHelper">The helper for ViewModels.</param>
        public LanguageSwitcherViewModel([NotNull] Factory factory, [NotNull] ILanguageRepository languageRepository, [NotNull] ISitecoreViewModelHelper sitecoreViewModelHelper)
            : base(factory, languageRepository, sitecoreViewModelHelper)
        {
            Assert.ArgumentNotNull(factory, "factory");
            Assert.ArgumentNotNull(languageRepository, "languageRepository");
            Assert.ArgumentNotNull(sitecoreViewModelHelper, "sitecoreViewModelHelper");

            this.factory = factory;
            this.languageRepository = languageRepository;
            _sitecoreViewModelHelper = sitecoreViewModelHelper;
        }
        public override void Initialize(Rendering rendering)
        {
            var messageId = _sitecoreViewModelHelper.MessageId;
            var messageItem = this.factory.GetMessageItem(messageId);
            if (messageItem != null)
            {
                this.Rendering = rendering;

                //Get current view context and initialize new HTMLHelper
                this.Html = _sitecoreViewModelHelper.HtmlHelper;

                // Update current language switch user control.
                UpdateCurrentUserControl();

                //Get the current language.
                GetCurrentLanguage();

                // Render language list with formatted languages list.
                RenderLanguageList();
            }
            else
            {
                _sitecoreViewModelHelper.MessageDoesNotExistRedirect(messageId);
            }
        }

        private void GetCurrentLanguage()
        {
            var messageId = _sitecoreViewModelHelper.MessageId;
            var contentLanguage = Sitecore.Web.WebUtil.GetQueryString("sc_speakcontentlang");

            var messageItem = this.factory.GetMessageItem(messageId);
            if (messageItem != null)
            {
                var mailMessageItem = messageItem as MailMessageItem;
                var targetLanguage = mailMessageItem == null ? messageItem.InnerItem.Language : mailMessageItem.TargetLanguage;
                if (targetLanguage != null && targetLanguage.Name != contentLanguage)
                {
                    contentLanguage = targetLanguage.Name;
                }
            }
            this.MessageLanguages = languageRepository.GetLanguages(messageId, contentLanguage);

            typeof(Sitecore.EmailCampaign.Controls.LanguageSwitcher.LanguageSwitcherViewModel)
                .GetMethod("set_CurrentLanguageToolTip", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(this, new object[] { string.Empty });

            typeof(Sitecore.EmailCampaign.Controls.LanguageSwitcher.LanguageSwitcherViewModel)
                .GetMethod("set_CurrentLanguage", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(this, new object[] { this.MessageLanguages.SingleOrDefault(x => x.IsDefault) });

            if (this.CurrentLanguage != null)
            {
                typeof(Sitecore.EmailCampaign.Controls.LanguageSwitcher.LanguageSwitcherViewModel)
                .GetMethod("set_CurrentLanguageToolTip", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(this, new object[] { this.CurrentLanguage.DisplayName });
                this.UserControl.Attributes.Add("data-sc-defaultLanguage", this.CurrentLanguage.IsoCode);
                this.UserControl.Attributes.Add("data-sc-defaultLanguageToolTip", this.CurrentLanguageToolTip);

                var myCookie = new HttpCookie("messageLanguage", this.CurrentLanguage.IsoCode);
                myCookie.Expires = DateTime.Now.AddDays(1);
                HttpContext.Current.Response.Cookies.Add(myCookie);
            }

            GetFormattedLanguages();
        }

        private void GetFormattedLanguages()
        {
            var allLanguages = new Util().GetDb().Languages;

            this.FormattedLanguages = allLanguages.Select(l =>
            {
                var messageLangauge = this.MessageLanguages.FirstOrDefault(messageLang => messageLang.IsoCode == l.Name);

                if (messageLangauge != null)
                {
                    return new LanguageInfo
                    {
                        HasVersion = messageLangauge.HasVersion,
                        IsDefault = messageLangauge.IsDefault,
                        IsoCode = messageLangauge.IsoCode,
                        DisplayName = messageLangauge.DisplayName
                    };
                }

                return new LanguageInfo
                {
                    HasVersion = false,
                    IsDefault = false,
                    IsoCode = l.Name,
                    DisplayName = l.CultureInfo.DisplayName
                };
            }).ToList();
        }

        private void RenderLanguageList()
        {
            this._sitecoreViewModelHelper.RenderControl(this.Html, this.ControlId,
              "/sitecore/shell/client/Applications/ECM/EmailCampaign.Controls/LanguageSwitcher/LanguageList.cshtml",
              "DropDownButton", this);
        }

        private void UpdateCurrentUserControl()
        {
            UserControl = this._sitecoreViewModelHelper.GetUserControl(Html);
            UserControl.Requires.Script("ecm", "LanguageSwitcher.js");
            UserControl.Requires.Css("ecm", "LanguageSwitcher.css");
            UserControl.Class = "sc-ecm-language sc-actionpanel";
            UserControl.Attributes["data-bind"] = "visible: isVisible, isOpen: false";

            typeof(Sitecore.EmailCampaign.Controls.LanguageSwitcher.LanguageSwitcherViewModel)
                .GetMethod("set_UserControl", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(this, new object[] { UserControl });
        }
    }
}