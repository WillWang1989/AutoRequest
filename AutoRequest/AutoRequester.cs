using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using Fiddler;

namespace AutoRequest
{
    public class AutoRequester
    {
        private static readonly string AutoRequestRulesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Fiddler2\\AutoRequester.xml";

        internal readonly static string STR_FIND_FILE;

        internal readonly static string STR_CREATE_NEW;

        private bool _bEnabled;

        private static string sColorAutoResponded;

        private ReaderWriterLockSlim _RWLockRules = new ReaderWriterLockSlim();

        private List<RequesterRule> Rules = new List<RequesterRule>();

        private bool _bRuleListIsDirty;

        private UIAutoRequest oAutoRequesterUI;

        private static Random myRand;

        public bool IsEnabled
        {
            get
            {
                return this._bEnabled;
            }
            set
            {
                if (value == this._bEnabled)
                {
                    return;
                }
                UIInvoke(() =>
                {
                    this._bEnabled = this.oAutoRequesterUI.cbAutoRespond.Checked = value;
                });
                this._bRuleListIsDirty = true;
            }
        }

        public bool IsRuleListDirty
        {
            get
            {
                return this._bRuleListIsDirty;
            }
            set
            {
                this._bRuleListIsDirty = value;
            }
        }

        static AutoRequester()
        {
            STR_FIND_FILE = "Find a file...";
            STR_CREATE_NEW = "Create New Request...";
            sColorAutoResponded = "yellow";
            myRand = new Random();
        }

        internal AutoRequester()
        {
            this.oAutoRequesterUI = new UIAutoRequest();
        }

        private static int _GetRandValue()
        {
            int num;
            lock (myRand)
            {
                num = myRand.Next(1, 101);
            }
            return num;
        }

        private static void _MarkMatch(Session oS, RequesterRule oMatch)
        {
            oS["ui-backcolor"] = sColorAutoResponded;
        }


        public RequesterRule AddRule(string sRule, int sAction, bool bIsEnabled)
        {
            return this.AddRule(sRule, sAction, "Custom", string.Empty, bIsEnabled);
        }

        public RequesterRule AddRule(string sRule, int action, string header, string headerValue, bool bEnabled)
        {
            RequesterRule responderRule;
            try
            {
                RequesterRule responderRule1 = new RequesterRule(sRule, action, header, headerValue, bEnabled);
                try
                {
                    this.GetWriterLock();
                    this.Rules.Add(responderRule1);
                }
                finally
                {
                    this.FreeWriterLock();
                }
                this._bRuleListIsDirty = true;
                this.CreateViewItem(responderRule1);
                responderRule = responderRule1;
            }
            catch (Exception)
            {
                responderRule = null;
            }
            return responderRule;
        }
        internal void AddToUI()
        {
            var tab = new TabPage("AutoRequest");
            tab.Controls.Add(this.oAutoRequesterUI);
            FiddlerApplication.UI.tabsViews.Controls.Add(tab);
            tab.ImageIndex = this.IsEnabled ? 24 : 23;
            this.oAutoRequesterUI.Parent = tab;
            this.oAutoRequesterUI.Dock = DockStyle.Fill;
        }

        private static bool CheckMatch(Session oS, RequesterRule oCandidate)
        {
            if (!oCandidate.IsEnabled)
            {
                return false;
            }
            return CheckMatch(oS.fullUrl, oS, oCandidate.sMatch);
        }

        internal static bool CheckMatch(string sURI, Session oSession, string sLookFor)
        {
            bool flag;
            string str = null;
            int num = sLookFor.IndexOf('%');
            if (num > 0 && num < 4)
            {
                int num1 = 0;
                if (int.TryParse(sLookFor.Substring(0, num), out num1) && _GetRandValue() > num1)
                {
                    return false;
                }
                sLookFor = sLookFor.Substring(num + 1);
            }
            if (sLookFor.OICStartsWith("METHOD:"))
            {
                if (oSession == null || !HasHeaders(oSession.oRequest))
                {
                    return false;
                }
                sLookFor = sLookFor.Substring(7);
                bool flag1 = false;
                bool flag2 = false;
                if (sLookFor.OICStartsWith("NOT:"))
                {
                    flag2 = true;
                    sLookFor = sLookFor.Substring(4);
                }
                flag1 = oSession.HTTPMethodIs(Utilities.TrimAfter(sLookFor, ' '));
                if (flag2)
                {
                    flag1 = !flag1;
                }
                if (!flag1)
                {
                    return false;
                }
                sLookFor = (sLookFor.Contains(" ") ? Utilities.TrimBefore(sLookFor, ' ') : "*");
            }
            if (sLookFor.OICStartsWith("URLWithBody:"))
            {
                sLookFor = sLookFor.Substring(12);
                str = Utilities.TrimBefore(sLookFor, ' ');
                sLookFor = Utilities.TrimAfter(sLookFor, ' ');
            }
            if (sLookFor.OICStartsWith("HEADER:"))
            {
                if (oSession == null || !HasHeaders(oSession.oRequest))
                {
                    return false;
                }
                sLookFor = sLookFor.Substring(7);
                bool flag3 = false;
                bool flag4 = false;
                if (sLookFor.OICStartsWith("NOT:"))
                {
                    flag4 = true;
                    sLookFor = sLookFor.Substring(4);
                }
                if (!sLookFor.Contains("="))
                {
                    flag3 = oSession.oRequest.headers.Exists(sLookFor);
                }
                else
                {
                    string str1 = Utilities.TrimAfter(sLookFor, "=");
                    string str2 = Utilities.TrimBefore(sLookFor, "=");
                    flag3 = oSession.oRequest.headers.ExistsAndContains(str1, str2);
                }
                if (flag4)
                {
                    flag3 = !flag3;
                }
                return flag3;
            }
            if (sLookFor.OICStartsWith("FLAG:"))
            {
                if (oSession == null)
                {
                    return false;
                }
                bool flag5 = false;
                bool flag6 = false;
                sLookFor = sLookFor.Substring(5);
                if (sLookFor.OICStartsWith("NOT:"))
                {
                    flag6 = true;
                    sLookFor = sLookFor.Substring(4);
                }
                if (!sLookFor.Contains("="))
                {
                    flag5 = oSession.oFlags.ContainsKey(sLookFor);
                }
                else
                {
                    string str3 = Utilities.TrimAfter(sLookFor, "=");
                    string str4 = Utilities.TrimBefore(sLookFor, "=");
                    string item = oSession.oFlags[str3];
                    if (item == null)
                    {
                        return false;
                    }
                    flag5 = item.OICContains(str4);
                }
                if (flag6)
                {
                    flag5 = !flag5;
                }
                return flag5;
            }
            if (sLookFor.Length > 6 && sLookFor.OICStartsWith("REGEX:"))
            {
                string str5 = sLookFor.Substring(6);
                try
                {
                    if (!(new Regex(str5)).Match(sURI).Success)
                    {
                        return false;
                    }
                    else
                    {
                        flag = (IsBodyMatch(oSession, str) ? true : false);
                    }
                }
                catch
                {
                    return false;
                }
                return flag;
            }
            if (sLookFor.Length > 6 && sLookFor.OICStartsWith("EXACT:"))
            {
                if (!sLookFor.Substring(6).Equals(sURI, StringComparison.Ordinal))
                {
                    return false;
                }
                if (!IsBodyMatch(oSession, str))
                {
                    return false;
                }
                return true;
            }
            if (sLookFor.Length <= 4 || !sLookFor.OICStartsWith("NOT:"))
            {
                if (!("*" == sLookFor) && !sURI.OICContains(sLookFor))
                {
                    return false;
                }
                return IsBodyMatch(oSession, str);
            }
            if (sURI.OICContains(sLookFor.Substring(4)))
            {
                return false;
            }
            if (!IsBodyMatch(oSession, str))
            {
                return false;
            }
            return true;
        }

        internal void ClearActionsFromUI()
        {
            this.oAutoRequesterUI.cbxRuleAction.Items.Clear();
        }

        public void ClearRules()
        {
            try
            {
                this.GetWriterLock();
                this.Rules.Clear();
            }
            finally
            {
                this.FreeWriterLock();
            }
            UIInvoke(() => this.oAutoRequesterUI.lvRespondRules.Items.Clear());
            this._bRuleListIsDirty = true;
        }

        private void CreateViewItem(RequesterRule oRule)
        {
            if (oRule == null)
            {
                return;
            }
            ListViewItem lvItem = this.oAutoRequesterUI.lvRespondRules.Items.Add(oRule.sMatch);
            oRule.ViewItem = lvItem;
            lvItem.SubItems.Add(oRule.Action == 0 ? "add" : oRule.Action == 1 ? "replace" : "remove");
            lvItem.SubItems.Add(oRule.Header);
            lvItem.SubItems.Add(oRule.HeaderValue);
            lvItem.Tag = oRule;
            lvItem.Checked = oRule.IsEnabled;
            if (!lvItem.Checked)
            {
                lvItem.ForeColor = Color.FromKnownColor(KnownColor.ControlDark);
            }
        }

        internal bool DemoteRule(RequesterRule oRule)
        {
            bool flag;
            try
            {
                this.GetWriterLock();
                int num = this.Rules.IndexOf(oRule);
                if (num <= -1 || num >= this.Rules.Count - 1)
                {
                    flag = false;
                }
                else
                {
                    this.Rules.Reverse(num, 2);
                    this._bRuleListIsDirty = true;
                    flag = true;
                }
            }
            finally
            {
                this.FreeWriterLock();
            }
            return flag;
        }

        internal void DoMatchBeforeRequestTampering(Session oSession)
        {
            if (oSession.isFlagSet(SessionFlags.Ignored)) return;

            try
            {
                this.GetReaderLock();
                foreach (RequesterRule rule in this.Rules)
                {
                    if (!CheckMatch(oSession, rule)) continue;
                    if (rule.bDisableOnMatch)
                    {
                        rule.IsEnabled = false;
                        UIInvokeAsync(new MethodInvoker(() => rule.ViewItem.Checked = false), null);
                    }
                    HandleMatch(oSession, rule);
                }
            }
            finally
            {
                this.FreeReaderLock();
            }
        }

        private void HandleMatch(Session oSession, RequesterRule rule)
        {
            if (rule.Action == 0)
            {
                oSession.RequestHeaders[rule.Header] = oSession.RequestHeaders[rule.Header] + rule.HeaderValue;
            }
            else if (rule.Action == 1)
            {
                oSession.RequestHeaders[rule.Header] = rule.HeaderValue;
            }
            else if (rule.Action == 2)
            {
                oSession.RequestHeaders.Remove(rule.Header);
            }
            _MarkMatch(oSession, rule);

        }

        internal bool ExportFARX(string sFilename)
        {
            return this.SaveRules(sFilename);
        }

        private void FreeReaderLock()
        {
            this._RWLockRules.ExitReadLock();
        }

        private void FreeWriterLock()
        {
            this._RWLockRules.ExitWriteLock();
        }

        private void GetReaderLock()
        {
            this._RWLockRules.EnterReadLock();
        }

        private void GetWriterLock()
        {
            this._RWLockRules.EnterWriteLock();
        }

        internal delegate void setStringDelegate(string sNew);

        internal bool ImportFARX(string sFilename)
        {
            return this.LoadRules(sFilename, false);
        }

        public bool ImportSessions(Session[] oSessions)
        {
            return this.ImportSessions(oSessions, null, false);
        }

        private bool ImportSessions(Session[] oSessions, string sAnnotation, bool bUsePlaybackHeuristics)
        {
            string str;
            List<HTTPHeaderItem> hTTPHeaderItems;
            object obj;
            if (oSessions == null || (int)oSessions.Length < 1)
            {
                return false;
            }
            Dictionary<string, List<HTTPHeaderItem>> strs = null;
            Session[] sessionArray = oSessions;
            for (int i = 0; i < (int)sessionArray.Length; i++)
            {
                Session session = sessionArray[i];
                if (session.bHasResponse && session.oResponse != null)
                {
                    if (!bUsePlaybackHeuristics || 401 != session.responseCode || !session.oResponse.headers.Exists("WWW-Authenticate"))
                    {
                        if (!bUsePlaybackHeuristics || !session.HTTPMethodIs("POST") || !HasHeaders(session.oRequest) || !session.oRequest.headers.Exists("SOAPAction"))
                        {
                            str = (session.HTTPMethodIs("GET") ? string.Concat("EXACT:", session.fullUrl) : string.Format("METHOD:{0} EXACT:{1}", session.RequestMethod, session.fullUrl));
                        }
                        else
                        {
                            str = string.Concat("Header:SOAPAction=", session.oRequest.headers["SOAPAction"]);
                        }
                        if (bUsePlaybackHeuristics)
                        {
                            bool flag = !session.HTTPMethodIs("GET");
                            foreach (RequesterRule rule in this.Rules)
                            {
                                if (rule.sMatch != str)
                                {
                                    if (!flag || !(rule.sMatch == string.Concat("EXACT:", session.fullUrl)))
                                    {
                                        continue;
                                    }
                                    rule.sMatch = string.Concat("METHOD:GET ", rule.sMatch);
                                    UIInvokeAsync(new MethodInvoker(() =>
                                    {
                                        if (rule.ViewItem != null)
                                        {
                                            rule.ViewItem.SubItems[0].Text = rule.sMatch;
                                        }
                                    }), null);
                                }
                                else
                                {
                                    rule.bDisableOnMatch = true;
                                }
                            }
                        }
                        this.AddRule(str, 0, true);
                    }
                }
            }
            this._bRuleListIsDirty = true;
            return true;
        }

        private static bool IsBodyMatch(Session oSession, string sBodyToMatch)
        {
            bool flag;
            if (oSession == null || string.IsNullOrEmpty(sBodyToMatch))
            {
                return true;
            }
            try
            {
                string requestBodyAsString = oSession.GetRequestBodyAsString();
                if (string.IsNullOrEmpty(requestBodyAsString))
                {
                    flag = false;
                }
                else if (sBodyToMatch.Length > 6 && sBodyToMatch.OICStartsWith("REGEX:"))
                {
                    string str = sBodyToMatch.Substring(6);
                    try
                    {
                        if ((new Regex(str)).Match(requestBodyAsString).Success)
                        {
                            flag = true;
                            return flag;
                        }
                    }
                    catch
                    {
                    }
                    flag = false;
                }
                else if (sBodyToMatch.Length > 6 && sBodyToMatch.OICStartsWith("EXACT:"))
                {
                    flag = (!sBodyToMatch.Substring(6).Equals(requestBodyAsString, StringComparison.Ordinal) ? false : true);
                }
                else if (sBodyToMatch.Length > 4 && sBodyToMatch.OICStartsWith("NOT:"))
                {
                    flag = (requestBodyAsString.OICContains(sBodyToMatch.Substring(4)) ? false : true);
                }
                else if (!requestBodyAsString.OICContains(sBodyToMatch))
                {
                    return false;
                }
                else
                {
                    flag = true;
                }
            }
            catch
            {
                flag = false;
            }
            return flag;
        }

        internal void LoadRules()
        {
            this.LoadRules(AutoRequestRulesPath, true);
        }

        public bool LoadRules(string sFilename)
        {
            return this.LoadRules(sFilename, true);
        }

        public bool LoadRules(string sFilename, bool bIsDefaultRuleFile)
        {
            FileStream fileStream;
            byte[] numArray;
            bool flag;
            if (bIsDefaultRuleFile)
            {
                this.ClearRules();
            }
            try
            {
                bool flag1 = false;
                if (!File.Exists(sFilename) || (new FileInfo(sFilename)).Length < (long)143)
                {
                    flag = false;
                }
                else
                {
                    try
                    {
                        fileStream = new FileStream(sFilename, FileMode.Open, FileAccess.Read, FileShare.Read);
                    }
                    catch (Exception exception1)
                    {
                        Exception exception = exception1;
                        FiddlerApplication.ReportException(exception, "AutoResponder Rules Unreadable", string.Format("Your AutoResponder settings file exists, but it is unreadable. Filename: {0}\nError Code: 0x{1:x}", sFilename, Marshal.GetHRForException(exception)));
                        if (bIsDefaultRuleFile)
                        {
                            this.IsEnabled = false;
                        }
                        flag = false;
                        return flag;
                    }
                    using (fileStream)
                    {
                        XmlTextReader xmlTextReader = new XmlTextReader(fileStream)
                        {
                            WhitespaceHandling = WhitespaceHandling.None
                        };
                        while (xmlTextReader.Read())
                        {
                            if (xmlTextReader.NodeType != XmlNodeType.Element)
                            {
                                continue;
                            }
                            string name = xmlTextReader.Name;
                            string str = name;
                            if (name == null)
                            {
                                continue;
                            }
                            if (str == "State")
                            {
                                if (!bIsDefaultRuleFile)
                                {
                                    continue;
                                }
                                this.IsEnabled = "true" == xmlTextReader.GetAttribute("Enabled");
                            }
                            else if (str == "ResponseRule")
                            {
                                try
                                {
                                    string match = xmlTextReader.GetAttribute("Match");
                                    string action = xmlTextReader.GetAttribute("Action");
                                    string header = xmlTextReader.GetAttribute("Header");
                                    string headerValue = xmlTextReader.GetAttribute("HeaderValue");

                                    bool flag2 = false;
                                    var str1 = xmlTextReader.GetAttribute("DisableAfterMatch");
                                    if ("true" == str1)
                                    {
                                        flag2 = true;
                                    }
                                    bool enabled = "false" != xmlTextReader.GetAttribute("Enabled");

                                    RequesterRule responderRule =
                                        this.AddRule(match, Convert.ToInt32(action ?? "0"),
                                        header, headerValue, bEnabled: enabled);
                                    responderRule.bDisableOnMatch = flag2;
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                    if (bIsDefaultRuleFile && this.Rules.Count < 1)
                    {
                        this.IsEnabled = false;
                    }
                    if (bIsDefaultRuleFile)
                    {
                        this._bRuleListIsDirty = false;
                    }
                    if (flag1)
                    {
                        this.oAutoRequesterUI.lvRespondRules.Columns[3].Width = Math.Max(this.oAutoRequesterUI.lvRespondRules.Columns[3].Width, 70);
                    }
                    flag = true;
                }
            }
            catch (Exception exception2)
            {
                FiddlerApplication.ReportException(exception2, "AutoResponder Rules Unreadable", string.Concat("Failed to load AutoResponder settings from ", sFilename));
                if (bIsDefaultRuleFile)
                {
                    this.IsEnabled = false;
                }
                flag = false;
            }
            return flag;
        }

        internal bool PromoteRule(RequesterRule oRule)
        {
            bool flag;
            try
            {
                this.GetWriterLock();
                int num = this.Rules.IndexOf(oRule);
                if (num <= 0)
                {
                    flag = false;
                }
                else
                {
                    this.Rules.Reverse(num - 1, 2);
                    this._bRuleListIsDirty = true;
                    flag = true;
                }
            }
            finally
            {
                this.FreeWriterLock();
            }
            return flag;
        }

        public bool RemoveRule(RequesterRule oRule)
        {
            bool flag;
            try
            {
                try
                {
                    this.GetWriterLock();
                    this.Rules.Remove(oRule);
                }
                finally
                {
                    this.FreeWriterLock();
                }
                this._bRuleListIsDirty = true;
                if (oRule.ViewItem != null)
                {
                    oRule.ViewItem.Remove();
                    oRule.ViewItem = null;
                }
                //if (oRule._oEditor != null)
                //{
                //    oRule._oEditor.Dispose();
                //    oRule._oEditor = null;
                //}
                flag = true;
            }
            catch (Exception)
            {
                flag = false;
            }
            return flag;
        }

        internal void SaveDefaultRules()
        {
            if (this._bRuleListIsDirty)
            {
                this.SaveRules(AutoRequestRulesPath);
                this._bRuleListIsDirty = false;
            }
        }

        public bool SaveRules(string sFilename)
        {
            bool flag;
            try
            {
                Utilities.EnsureOverwritable(sFilename);
                using (XmlTextWriter xmlTextWriter = new XmlTextWriter(sFilename, Encoding.UTF8))
                {
                    xmlTextWriter.Formatting = Formatting.Indented;
                    xmlTextWriter.WriteStartDocument();
                    xmlTextWriter.WriteStartElement("AutoRequester");
                    xmlTextWriter.WriteAttributeString("LastSave", XmlConvert.ToString(DateTime.Now, XmlDateTimeSerializationMode.RoundtripKind));
                    xmlTextWriter.WriteAttributeString("FiddlerVersion", Application.ProductVersion);
                    xmlTextWriter.WriteStartElement("State");
                    xmlTextWriter.WriteAttributeString("Enabled", XmlConvert.ToString(this._bEnabled));
                    try
                    {
                        this.GetReaderLock();
                        foreach (RequesterRule rule in this.Rules)
                        {
                            xmlTextWriter.WriteStartElement("ResponseRule");
                            xmlTextWriter.WriteAttributeString("Match", rule.sMatch);
                            xmlTextWriter.WriteAttributeString("Action", rule.Action + "");
                            xmlTextWriter.WriteAttributeString("Header", rule.Header + "");
                            xmlTextWriter.WriteAttributeString("HeaderValue", rule.HeaderValue + "");
                            if (rule.bDisableOnMatch)
                            {
                                xmlTextWriter.WriteAttributeString("DisableAfterMatch", XmlConvert.ToString(rule.bDisableOnMatch));
                            }
                            xmlTextWriter.WriteAttributeString("Enabled", XmlConvert.ToString(rule.IsEnabled));
                            xmlTextWriter.WriteEndElement();
                        }
                    }
                    finally
                    {
                        this.FreeReaderLock();
                    }
                    xmlTextWriter.WriteEndElement();
                    xmlTextWriter.WriteEndElement();
                    xmlTextWriter.WriteEndDocument();
                }
                flag = true;
            }
            catch (Exception exception)
            {
                FiddlerApplication.ReportException(exception, "Failed to save AutoResponder Rules", "Fiddler failed to save AutoResponder rules. This is often caused by redirected and misconfigured User Profile folders on corporate networks; you may need to contact your network administrator.");
                flag = false;
            }
            return flag;
        }
        internal static string RegExEscape(string sString, bool bAddPrefixCaret, bool bAddSuffixDollarSign)
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (bAddPrefixCaret)
            {
                stringBuilder.Append("^");
            }
            string str = sString;
            for (int i = 0; i < str.Length; i++)
            {
                char chr = str[i];
                char chr1 = chr;
                if (chr1 > '?')
                {
                    switch (chr1)
                    {
                        case '[':
                        case '\\':
                        case '\u005E':
                            {
                                break;
                            }
                        case ']':
                            {
                                goto Label0;
                            }
                        default:
                            {
                                switch (chr1)
                                {
                                    case '{':
                                    case '|':
                                        {
                                            break;
                                        }
                                    default:
                                        {
                                            goto Label0;
                                        }
                                }
                                break;
                            }
                    }
                }
                else
                {
                    switch (chr1)
                    {
                        case '#':
                        case '$':
                        case '(':
                        case ')':
                        case '+':
                        case '.':
                            {
                                break;
                            }
                        case '%':
                        case '&':
                        case '\'':
                        case ',':
                        case '-':
                            {
                                goto Label0;
                            }
                        case '*':
                            {
                                stringBuilder.Append('.');
                                goto Label0;
                            }
                        default:
                            {
                                if (chr1 == '?')
                                {
                                    break;
                                }
                                goto Label0;
                            }
                    }
                }
                stringBuilder.Append('\\');
                Label0:
                stringBuilder.Append(chr);
            }
            if (bAddSuffixDollarSign)
            {
                stringBuilder.Append('$');
            }
            return stringBuilder.ToString();
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            try
            {
                this.GetReaderLock();
                stringBuilder.AppendFormat("The AutoResponder list contains {0} rules.\r\n", this.Rules.Count);
                foreach (var rule in this.Rules)
                {
                    var msg = rule.Action == 0
                        ? "Append " + rule.HeaderValue + " to " + rule.Header
                        : rule.Action == 1 ? "replace " + rule.Header + " as " + rule.HeaderValue
                        : "remove " + rule.Header;
                    stringBuilder.AppendFormat("\t{0}\t->\t{1}\r\n", rule.sMatch, msg);
                }
            }
            finally
            {
                this.FreeReaderLock();
            }
            return stringBuilder.ToString();
        }
        internal static bool HasHeaders(ClientChatter oCC)
        {
            if (oCC == null)
            {
                return false;
            }
            return null != oCC.headers;
        }

        internal static void UIInvoke(MethodInvoker target)
        {
            if (FiddlerApplication.isClosing)
            {
                return;
            }
            if (!FiddlerApplication.UI.InvokeRequired)
            {
                target();
                return;
            }
            FiddlerApplication.UI.Invoke(target);
        }

        internal static void UIInvokeAsync(Delegate oDel, object[] args)
        {
            if (FiddlerApplication.isClosing)
            {
                return;
            }
            FiddlerApplication.UI.BeginInvoke(oDel, args);
        }
    }

}
