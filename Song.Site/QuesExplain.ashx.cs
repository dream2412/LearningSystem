﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WeiSha.Common;
using Song.ServiceInterfaces;
using VTemplate.Engine;

namespace Song.Site
{
    /// <summary>
    /// 试题解析
    /// </summary>
    public class QuesExplain : BasePage
    {
        //试题id
        protected int id = WeiSha.Common.Request.QueryString["id"].Int32 ?? 0;
        protected override void InitPageTemplate(HttpContext context)
        {
            //当前试题
            Song.Entities.Questions qus = Business.Do<IQuestions>().QuesSingle(id);
            if (qus != null)
            {
                if (!string.IsNullOrWhiteSpace(qus.Qus_Explain))
                {
                    qus.Qus_Explain = qus.Qus_Explain.Replace("\n","<br/>");
                    qus.Qus_Explain = Extend.Html.ClearHTML(qus.Qus_Explain, "font", "pre");
                }
                this.Document.SetValue("explain", qus.Qus_Explain);
            }    
        }
    }
}