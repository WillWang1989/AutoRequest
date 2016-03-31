using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;
using Fiddler;
using Microsoft.Win32;

namespace AutoRequest
{
    public class RequesterRule
    {
        private string _sMatch;

        private bool _bEnabled = true;

        private bool _bDisableOnMatch;

        private string _header;

        private string _headerValue;

        internal ListViewItem ViewItem;


        /// <summary>
        /// 0:add,1:replace,2:remove
        /// </summary>
        public int Action { set; get; }


        public string Header
        {
            set { _header = value; }
            get { return _header; }
        }

        public string HeaderValue
        {
            set { _headerValue = value; }
            get { return _headerValue; }
        }
        public bool bDisableOnMatch
        {
            get
            {
                return this._bDisableOnMatch;
            }
            set
            {
                this._bDisableOnMatch = value;
            }
        }


        internal bool IsEnabled
        {
            get
            {
                return this._bEnabled;
            }
            set
            {
                this._bEnabled = value;
            }
        }


        public string sMatch
        {
            get
            {
                return this._sMatch;
            }
            internal set
            {
                if (value == null || value.Trim().Length < 1)
                {
                    this._sMatch = "*";
                    return;
                }
                this._sMatch = value.Trim();
            }
        }

        internal RequesterRule(
            string strMatch,
            int action,
            string header,
            string headerValue,
            bool bEnabled)
        {
            this.sMatch = strMatch;
            this.Action = action;
            this.Header = header;
            this.HeaderValue = headerValue;
            this._bEnabled = bEnabled;
        }
    }
}
