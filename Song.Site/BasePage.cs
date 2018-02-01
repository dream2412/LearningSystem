using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using Song.Extend;
using System.IO;
using System.Text;
using VTemplate.Engine;
using WeiSha.Common;
using Song.ServiceInterfaces;
using Song.Entities;
using WeiSha.WebControl;
using System.Web.SessionState;

namespace Song.Site
{
    public abstract class BasePage : System.Web.UI.Page, IHttpHandler, IRequiresSessionState
    {
        /// <summary>
        /// ��ǰҳ���ģ���ĵ�����
        /// </summary>
        protected TemplateDocument Document { get; set; }
        /// <summary>
        /// ��ǰҳ���ģ���ĵ������ò���
        /// </summary>
        protected virtual TemplateDocumentConfig DocumentConfig
        {
            get { return TemplateDocumentConfig.Default; }
        }
        /// <summary>
        /// ��ʼ����ǰҳ��ģ������
        /// </summary>
        protected abstract void InitPageTemplate(HttpContext context);
        /// <summary>
        /// װ�ص�ǰҳ���ģ���ĵ�
        /// </summary>
        public virtual void LoadCurrentTemplate()
        {
            //�Ƿ����ֻ�����ҳ
            string filePath = this.Request.Url.AbsolutePath;
            bool isMobi = isMobilePage(out filePath);     //������filePathΪ�ļ�����������չ����  
            if (isMobi && LoginState.Accounts.IsLogin)
                LoginState.Accounts.Refresh(LoginState.Accounts.CurrentUser);            
            //ȡģ�����
            WeiSha.Common.Templates.TemplateBank curr = isMobi ?
                WeiSha.Common.Template.ForMobile.SetCurrent(this.Organ.Org_TemplateMobi)
                : WeiSha.Common.Template.ForWeb.SetCurrent(this.Organ.Org_Template);
            if (curr == null) throw new Exception("û���κ�ģ�����ã�");
            //�Ƿ��ǹ���ҳ��
            if (curr.Config.Public == null) throw new Exception("δ�ҵ�����ģ��⣡");
            bool isPublic = curr.Config.Public.PageExists(filePath);
            if (isPublic) curr = curr.Config.Public;
            //��ǰģ�������·��
            string tmFile = curr.Path.Physics + filePath + ".htm";
            //װ��ģ��
            this.Document = null;
            if (!System.IO.File.Exists(tmFile))
            {
                tmFile = curr.Config.Default.Path.Physics + filePath + ".htm";
                if (!System.IO.File.Exists(tmFile)) tmFile = curr.Config.Public.Path.Physics + "Notfound.htm";                             
            }
            this.Document = TemplateDocument.FromFileCache(tmFile, Encoding.UTF8, this.DocumentConfig);
            //this.Document = new TemplateDocument(tmFile, Encoding.UTF8, this.DocumentConfig);   //�����û��� 
        }
        /// <summary>
        /// �Ƿ����ֻ���
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        protected bool isMobilePage(out string path)
        {
            bool ismobi = false;
            string prefix = "/mobile";
            path = this.Request.Url.AbsolutePath;
            if (path.Length >= prefix.Length)
            {
                string pre = path.Substring(0, prefix.Length);
                if (pre.ToLower() == prefix.ToLower()) ismobi = true;
            }
            //������ֻ���ҳ�棬��ȥ��/mobile/·��
            if (ismobi) path = path.Substring(prefix.Length);
            if (path.IndexOf(".") > -1)
                path = path.Substring(path.IndexOf("/") + 1, path.LastIndexOf(".") - 1);
            else
                path = path.Substring(path.IndexOf("/") + 1);
            path = path.Replace("/", "\\");
            return ismobi;
        }
        #region ��ʼ���Ĳ���
        protected new HttpContext Context { get; private set; }
        protected new HttpApplicationState Application { get; private set; }
        protected new HttpRequest Request { get; private set; }
        protected new HttpResponse Response { get; private set; }
        protected new HttpServerUtility Server { get; private set; }
        protected new HttpSessionState Session { get; private set; }
        //��ǰ���ڻ���
        protected Song.Entities.Organization Organ { get; private set; }
        //ѧ������ʦ������Ա
        protected Song.Entities.Accounts Account { get; private set; }
        protected Song.Entities.Teacher Teacher { get; private set; }
        protected Song.Entities.EmpAccount Admin { get; private set; }
        /// <summary>
        /// ��ʼ������������
        /// </summary>
        /// <param name="context"></param>
        private void InitContext(HttpContext context)
        {
            this.Context = context;
            this.Application = context.Application;
            this.Request = context.Request;
            this.Response = context.Response;
            this.Server = context.Server;
            this.Session = context.Session;
            try
            {
                //������Ϣ
                this.Organ = Business.Do<IOrganization>().OrganCurrent();
                //��¼����Ϣ
                if (Extend.LoginState.Accounts.IsLogin)
                {
                    this.Account = Song.Extend.LoginState.Accounts.CurrentUser;
                    this.Teacher = Song.Extend.LoginState.Accounts.Teacher;
                }
                if (Extend.LoginState.Admin.IsLogin)
                    this.Admin = Song.Extend.LoginState.Admin.CurrentUser;
            }
            catch
            {
                throw;
            }
        }
        public new bool IsReusable
        {
            get { return false; }
        }

        public new void ProcessRequest(HttpContext context)
        {
            this.InitContext(context);
            //�������
            this.LoadCurrentTemplate();
            try
            {
                //һЩ��������
                this.Document.SetValue("org", this.Organ);
                this.Document.SetValue("orgpath", Upload.Get["Org"].Virtual);
                //ѧ������ʦ������Ա�Ƿ��¼
                if (Extend.LoginState.Accounts.IsLogin)
                {
                    this.Document.SetValue("Account", this.Account);
                    this.Document.SetValue("stuid", Extend.LoginState.Accounts.UID);
                }
                if (Extend.LoginState.Accounts.IsLogin) this.Document.SetValue("Teacher", this.Teacher);
                if (Extend.LoginState.Admin.IsLogin) this.Document.SetValue("Admin", this.Admin);
                //����·��
                this.Document.SetValue("stpath", Upload.Get["Accounts"].Virtual);
                this.Document.SetValue("thpath", Upload.Get["Teacher"].Virtual);
                this.Document.SetValue("adminpath", Upload.Get["Employee"].Virtual);
                //��ǰģ���·��
                this.Document.SetValue("TempPath", WeiSha.Common.Template.ForWeb.Current.Path.Virtual);

            }
            catch { }
            //ʱ��
            string WeekStr = DateTime.Now.ToString("dddd", new System.Globalization.CultureInfo("zh-cn"));
            this.Document.SetValue("week", WeekStr);
            this.Document.SetValue("tick", DateTime.Now.Ticks);
            //�����˵�
            this.Document.RegisterGlobalFunction(this.Navi);
            this.Document.RegisterGlobalFunction(this.NaviDrop);

            //�ñ���ģ�����洦����ǩ
            Song.Template.Handler.Start(this.Document);
            //
            //��ʼ���
            this.InitPageTemplate(context);
            this.Document.Render(this.Response.Output);
        }
        #endregion

        #region �����˵�
        /// <summary>
        /// ��ȡ�����˵�
        /// </summary>
        /// <param name="p">ֻ��һ��������Ϊ�˵�����</param>
        /// <returns></returns>
        protected Song.Entities.Navigation[] Navi(object[] p)
        {
            string type = null;
            if (p.Length > 0) type = p[0].ToString();
            int pid = 0;
            if (p.Length > 1) int.TryParse(p[1].ToString(), out pid);
            //�Ƿ����ֻ�����ҳ
            string filePath = this.Request.Url.AbsolutePath;
            bool isMobi = isMobilePage(out filePath);
            string device = isMobi ? "mobi" : "web"; //�豸
            Song.Entities.Navigation[] navi = Business.Do<IStyle>().NaviAll(true, device, type, Organ.Org_ID, pid);
            if (navi.Length < 1)
            {
                Song.Entities.Organization o = Business.Do<IOrganization>().OrganDefault();
                navi = Business.Do<IStyle>().NaviAll(true, device, type, o.Org_ID, pid);
            }
            return navi;
        }
        /// <summary>
        /// ��ȡ�����˵�����������
        /// </summary>
        /// <param name="p">ֻ��һ��������Ϊ�˵�����</param>
        /// <returns></returns>
        protected string NaviDrop(object[] p)
        {
            string type = null;
            if (p.Length > 0) type = p[0].ToString();
            //�Ƿ����ֻ�����ҳ
            string filePath = this.Request.Url.AbsolutePath;
            bool isMobi = isMobilePage(out filePath);
            string device = isMobi ? "mobi" : "web"; //�豸
            Song.Entities.Navigation[] navi = Business.Do<IStyle>().NaviAll(true, device, type, Organ.Org_ID, 0);
            if (navi.Length < 1)
            {
                Song.Entities.Organization o = Business.Do<IOrganization>().OrganDefault();
                navi = Business.Do<IStyle>().NaviAll(true, device, type, o.Org_ID, -1);
            }
            string html = "";
            if (navi.Length > 0)
            {
                foreach (Song.Entities.Navigation n in navi)
                {
                    html += _NaviDropHtml(n.Nav_ID, type);
                }
            }
            return html;
        }
        private string _NaviDropHtml(int nid, string type)
        {
            string html = "";
            Song.Entities.Navigation[] navi = Business.Do<IStyle>().NaviChildren(nid, true);
            if (navi.Length > 0)
            {
                html += string.Format("<div pid=\"{0}\" class=\"naviBox\" style=\"display:none;\">", nid);
                foreach (Song.Entities.Navigation n in navi)
                {
                    html += string.Format("<div nid=\"{1}\" class=\"naviItem\"><a href=\"{3}\" target=\"{4}\" title=\"{5}\" style=\"{6}{7}{8}\">{2}</a></div>",
                        n.Nav_PID, n.Nav_ID, n.Nav_Name, n.Nav_Url, n.Nav_Target, n.Nav_Title,
                        string.IsNullOrEmpty(n.Nav_Color) ? "" : "color: " + n.Nav_Color + ";",
                        string.IsNullOrEmpty(n.Nav_Font) ? "" : "font-family: " + n.Nav_Font + ";",
                        !n.Nav_IsBold ? "" : "font-weight:bold;");
                }
                html += "</div>";
                foreach (Song.Entities.Navigation n in navi)
                    html += _NaviDropHtml(n.Nav_ID, type);
            }
            return html;
        }
        #endregion

    }
}