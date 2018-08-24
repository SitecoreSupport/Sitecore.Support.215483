namespace Sitecore.Support.EmailCampaign.Controls.LanguageSwitcher
{
    using Microsoft.Extensions.DependencyInjection;
    using Sitecore.DependencyInjection;
    using Sitecore.Diagnostics;
    using Sitecore.EmailCampaign.Controls;
    using Sitecore.Modules.EmailCampaign;
    using Sitecore.Modules.EmailCampaign.Messages;
    using Sitecore.Modules.EmailCampaign.Messages.Interfaces;
    using Sitecore.Modules.EmailCampaign.Services;
    using Sitecore.Mvc.Presentation;
    using Sitecore.Web.UI.Controls.Common.UserControls;
    using System;
    using System.Linq;
    using System.Web;

    public class LanguageSwitcherViewModel : Sitecore.EmailCampaign.Controls.LanguageSwitcher.LanguageSwitcherViewModel
    {

        private readonly IExmCampaignService _exmCampaignService;

        private readonly ILanguageRepository languageRepository;

        private UserControl UserControl { get; set; }

        private readonly ISitecoreViewModelHelper _sitecoreViewModelHelper;
        public LanguageSwitcherViewModel()
            : this(ServiceProviderServiceExtensions.GetService<IExmCampaignService>(ServiceLocator.ServiceProvider), ServiceProviderServiceExtensions.GetService<ILanguageRepository>(ServiceLocator.ServiceProvider), new SitecoreViewModelHelper())
        {
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="LanguageSwitcherViewModel"/> class 
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="languageRepository">the language repository object.</param>
        /// <param name="sitecoreViewModelHelper">The helper for ViewModels.</param>
        public LanguageSwitcherViewModel(IExmCampaignService exmCampaignService, ILanguageRepository languageRepository, ISitecoreViewModelHelper sitecoreViewModelHelper)
        {
            Assert.ArgumentNotNull(exmCampaignService, "exmCampaignService");
            Assert.ArgumentNotNull(languageRepository, "languageRepository");
            Assert.ArgumentNotNull(sitecoreViewModelHelper, "sitecoreViewModelHelper");
            this._exmCampaignService = exmCampaignService;
            this.languageRepository = languageRepository;
            this._sitecoreViewModelHelper = sitecoreViewModelHelper;
        }

        public override void Initialize(Rendering rendering)
        {
            string messageId = this._sitecoreViewModelHelper.MessageId;
            if (this._exmCampaignService.GetMessageItem(Guid.Parse(messageId)) != null)
            {
                this.Rendering = rendering;
                this.Html = this._sitecoreViewModelHelper.HtmlHelper;
                this.UpdateCurrentUserControl();
                this.GetCurrentLanguage();
                this.RenderLanguageList();
                return;
            }
            this._sitecoreViewModelHelper.MessageDoesNotExistRedirect(messageId);
        }


        private void GetCurrentLanguage()
        {
            var messageId = _sitecoreViewModelHelper.MessageId;
            var contentLanguage = Sitecore.Web.WebUtil.GetQueryString("sc_speakcontentlang");

            MessageItem messageItem = this._exmCampaignService.GetMessageItem(Guid.Parse(messageId));
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

            this.CurrentLanguageToolTip = string.Empty;

            this.CurrentLanguage = this.MessageLanguages.SingleOrDefault(x => x.IsDefault);
            if (this.CurrentLanguage != null)
            {
                this.CurrentLanguageToolTip = this.CurrentLanguage.DisplayName;
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
        }
    }
}