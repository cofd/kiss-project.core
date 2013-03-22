﻿using Kiss.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kiss.Notice
{
    public class NoticeFactory
    {
        public static INotice Create(string channel)
        {
            if (string.IsNullOrEmpty(channel))
                throw new ArgumentException("channel name is empty.");

            return ServiceLocator.Instance.Resolve("kiss.notice." + channel.ToLowerInvariant()) as INotice;
        }

        public static INoticeConfig Config
        {
            get
            {
                return ServiceLocator.Instance.Resolve<INoticeConfig>();
            }
        }

        public static void Send(string templateId, Dictionary<string, object> param, IUser from, params IUser[] to)
        {
            if (to.Length == 0) return;

            List<IUser> list = new List<IUser>(to);

            foreach (var item in Config.GetsValidChannel(templateId, (from q in to
                                                                      select q.Id).ToArray()))
            {
                List<IUser> sendto = new List<IUser>();

                foreach (string userid in item.Value)
                {
                    IUser user = list.Find((u) => { return u.Id == userid; });
                    if (user == null) continue;

                    sendto.Add(user);
                }

                Create(item.Key).Send(templateId, param, from, sendto.ToArray());
            }
        }

        public static void Send(string templateId, Dictionary<string, object> param, string from, params string[] to)
        {
            if (to.Length == 0) return;

            foreach (var item in Config.GetsValidChannel(templateId, to))
            {
                Create(item.Key).Send(templateId, param, from, item.Value.ToArray());
            }
        }

        /// <summary>
        /// 根据模板Id 和 doc 获得标题和内容
        /// </summary>
        public static bool ResolverTemplate(string channel, string templateId, Dictionary<string, object> param, out string title, out string content)
        {
            title = content = string.Empty;

            DictSchema schema = (from q in DictSchema.CreateContext(true)
                                 where q.Type == "msg" && q.Name == "template" && q.Title == templateId
                                 select q).FirstOrDefault();

            if (schema == null)
            {
                LogManager.GetLogger<NoticeFactory>().ErrorFormat("指定的模板：{0} 不存在。", templateId);
                return false;
            }

            ITemplateEngine te = ServiceLocator.Instance.Resolve<ITemplateEngine>();

            using (StringWriter sw = new StringWriter())
            {
                string template = schema[channel + "_title"];
                if (string.IsNullOrEmpty(template))
                    template = schema["default_title"];

                te.Process(param, string.Empty, sw, template);

                title = sw.GetStringBuilder().ToString();
            }

            using (StringWriter sw = new StringWriter())
            {
                string template = schema[channel + "_content"];
                if (string.IsNullOrEmpty(template))
                    template = schema["default_content"];

                te.Process(param, string.Empty, sw, template);

                content = sw.GetStringBuilder().ToString();
            }

            return true;
        }
    }
}
