using Fiddler;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Layout;
using System.Linq;
using System.Linq.Expressions;
namespace AutoRequest
{
    public class UIAutoRequest : UserControl
    {
        private IContainer components;

        internal CheckBox cbAutoRespond;

        private Panel pnlAutoResponders;

        internal DoubleBufferedListView lvRespondRules;

        private ColumnHeader colRespondMatch;

        private Panel pnlResponderActions;

        private Button btnRespondAdd;

        private GroupBox gbResponderEditor;

        internal ComboBox cbxRuleURI;

        private Button btnSaveRule;

        private Label lblAutoRespondHeader;

        internal ComboBox cbxRuleAction;

        private ContextMenuStrip mnuContextAutoResponder;

        private ToolStripMenuItem miRemoveRule;

        private ToolStripMenuItem miPromoteRule;

        private ToolStripMenuItem miDemoteRule;

        private ToolStripMenuItem miExportRules;

        private Label lblMultipleMatch;

        private ToolStripMenuItem miRespondCloneRule;

        private ToolStripSeparator toolStripMenuItem2;

        private LinkLabel lnkTestRule;

        private ToolStripMenuItem miFindRule;

        private CheckBox cbAutoRespondOnce;
        private LinkLabel lnkAutoRespondHelp;
        private ColumnHeader colAction;
        private ColumnHeader colHeader;
        private ColumnHeader colHeaderValue;
        internal ComboBox cbHeader;
        private TextBox tbHeaderValue;
        private Button RespondImport;
        public List<RequesterRule> _alRules = new List<RequesterRule>();
        public UIAutoRequest()
        {
            this.InitializeComponent();
            this.lvRespondRules.Font = new Font(this.lvRespondRules.Font.FontFamily, CONFIG.flFontSize);
            Utilities.SetCueText(this.cbxRuleURI, "Request URL Pattern");
            Utilities.SetCueText(this.cbxRuleAction, "Action to execute.");
            this.lvRespondRules.EmptyText = "Click [Add Rule] above to create a rule.";
        }

        private void _SelectMostRecentLVRespondRule()
        {
            if (this.lvRespondRules.Items.Count > -1)
            {
                this.lvRespondRules.SelectedItems.Clear();
                this.lvRespondRules.Items[this.lvRespondRules.Items.Count - 1].Selected = true;
            }
        }

        private void actCloneRule()
        {
            if (this.lvRespondRules.SelectedItems.Count != 1)
            {
                return;
            }
            RequesterRule tag = (RequesterRule)this.lvRespondRules.SelectedItems[0].Tag;
            AutoRequest._AutoRequester.AddRule(
                tag.sMatch, tag.Action, tag.HeaderValue, tag.HeaderValue, tag.IsEnabled);
        }

        private void actDemoteRule()
        {
            if (this.lvRespondRules.SelectedItems.Count != 1)
            {
                return;
            }
            ListViewItem item = this.lvRespondRules.SelectedItems[0];
            if (AutoRequest._AutoRequester.DemoteRule((RequesterRule)item.Tag))
            {
                int index = item.Index + 1;
                item.Remove();
                this.lvRespondRules.Items.Insert(index, item);
                item.Focused = true;
                item.EnsureVisible();
            }
        }

        private void actPromoteRule()
        {
            if (this.lvRespondRules.SelectedItems.Count != 1)
            {
                return;
            }
            ListViewItem item = this.lvRespondRules.SelectedItems[0];
            if (AutoRequest._AutoRequester.PromoteRule((RequesterRule)item.Tag))
            {
                int index = item.Index - 1;
                item.Remove();
                this.lvRespondRules.Items.Insert(index, item);
                item.Focused = true;
                item.EnsureVisible();
            }
        }

        private void actRemoveSelectedRules()
        {
            ListView.SelectedListViewItemCollection selectedItems = this.lvRespondRules.SelectedItems;
            if (selectedItems.Count < 1) { return; }
            this.lvRespondRules.BeginUpdate();
            foreach (ListViewItem selectedItem in selectedItems)
            {
                AutoRequest._AutoRequester.RemoveRule((RequesterRule)selectedItem.Tag);
            }
            this.lvRespondRules.EndUpdate();
            this.cbxRuleURI.Text = this.cbxRuleAction.Text = string.Empty;
            if (this.lvRespondRules.FocusedItem != null)
            {
                this.lvRespondRules.FocusedItem.Selected = true;
            }
        }

        private void btnRespondAdd_Click(object sender, EventArgs e)
        {
            string str;
            if (FiddlerApplication.UI.lvSessions.SelectedItems.Count != 1)
            {
                int count = this.lvRespondRules.Items.Count + 1;
                str = string.Concat("StringToMatch[", count.ToString(), "]");
            }
            else
            {
                str = string.Concat("EXACT:", FiddlerApplication.UI.GetFirstSelectedSession().fullUrl);
            }
            this.lvRespondRules.SelectedItems.Clear();
            RequesterRule RequesterRule = AutoRequest._AutoRequester.AddRule(str, 0, true);
            if (RequesterRule != null)
            {
                RequesterRule.ViewItem.EnsureVisible();
                RequesterRule.ViewItem.Selected = true;
            }
            this.cbxRuleURI.Focus();
        }

        private void btnRespondImport_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                DefaultExt = "saz",
                RestoreDirectory = true,
                InitialDirectory = CONFIG.GetPath("Captures"),
                Title = "Import file for replay",
                Filter = "Rules (*.farx)|*.farx"
            };
            if (DialogResult.OK != openFileDialog.ShowDialog(this))
            {
                openFileDialog.Dispose();
                return;
            }
            string fileName = openFileDialog.FileName;
            openFileDialog.Dispose();
            if (fileName.OICEndsWith(".farx"))
            {
                AutoRequest._AutoRequester.ImportFARX(fileName);
            }
        }

        private void btnSaveRule_Click(object sender, EventArgs e)
        {
            if (this.lvRespondRules.SelectedItems.Count < 1)
            {
                return;
            }
            foreach (ListViewItem selLvlItem in this.lvRespondRules.SelectedItems)
            {
                RequesterRule tag = (RequesterRule)selLvlItem.Tag;
                if (this.cbxRuleURI.Visible)
                {
                    tag.sMatch = this.cbxRuleURI.Text;
                    selLvlItem.Text = tag.sMatch;
                    tag.bDisableOnMatch = this.cbAutoRespondOnce.Checked;
                }
                else if (this.cbAutoRespondOnce.CheckState != CheckState.Indeterminate)
                {
                    tag.bDisableOnMatch = this.cbAutoRespondOnce.Checked;
                }

                tag.Action = cbxRuleAction.SelectedIndex;
                tag.Header = cbHeader.Text;
                tag.HeaderValue = cbxRuleAction.SelectedIndex == 2 ? string.Empty : tbHeaderValue.Text;
                selLvlItem.SubItems[2].Text = tag.Header;
                selLvlItem.SubItems[1].Text = cbxRuleAction.Text;
                selLvlItem.SubItems[3].Text = tag.HeaderValue;
            }
            AutoRequest._AutoRequester.IsRuleListDirty = true;
        }

        private void cbAutoRespond_CheckedChanged(object sender, EventArgs e)
        {
            bool @checked = this.cbAutoRespond.Checked;
            AutoRequest._AutoRequester.IsEnabled = @checked;
            this.lblAutoRespondHeader.BackColor = (@checked ? Color.Green : Color.Gray);
            this.lvRespondRules.BackColor = (@checked ? SystemColors.Window : SystemColors.Control);

            if (this.Parent != null) (Parent as TabPage).ImageIndex = (this.cbAutoRespond.Checked ? 24 : 23);
        }

        private void cbxRuleAction_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == (Keys.LButton | Keys.A | Keys.Control))
            {
                this.cbxRuleAction.SelectAll();
                e.Handled = e.SuppressKeyPress = true;
                return;
            }
            if (e.KeyCode == Keys.Return && !this.cbxRuleAction.DroppedDown)
            {
                base.ActiveControl = base.GetNextControl(base.ActiveControl, true);
                e.Handled = e.SuppressKeyPress = true;
            }
        }


        private void cbxRuleURI_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == (Keys.LButton | Keys.A | Keys.Control))
            {
                this.cbxRuleURI.SelectAll();
                e.Handled = e.SuppressKeyPress = true;
                return;
            }
            if (e.KeyCode == Keys.Return)
            {
                base.ActiveControl = base.GetNextControl(base.ActiveControl, true);
                e.SuppressKeyPress = e.Handled = true;

            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && this.components != null)
            {
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }

        private string GetSelectedHyperlink()
        {
            if (this.lvRespondRules.SelectedItems.Count != 1)
            {
                return string.Empty;
            }
            ListViewItem item = this.lvRespondRules.SelectedItems[0];
            string tag = (item.Tag as RequesterRule).sMatch;
            if (tag.OICContains("REGEX:"))
            {
                return string.Empty;
            }
            if (tag.OICStartsWith("METHOD:"))
            {
                tag = Utilities.TrimBefore(tag, ' ');
            }
            if (tag.OICStartsWith("URLWithBody:"))
            {
                tag = tag.Substring(12);
                tag = Utilities.TrimAfter(tag, ' ');
            }
            if (tag.OICStartsWith("EXACT:"))
            {
                tag = tag.Substring(6);
            }
            string[] strArrays = new string[] { "http://", "https://", "ftp://" };
            if (!tag.OICStartsWithAny(strArrays))
            {
                tag = string.Concat("http://", tag);
            }
            return tag;
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.cbAutoRespond = new System.Windows.Forms.CheckBox();
            this.pnlAutoResponders = new System.Windows.Forms.Panel();
            this.lvRespondRules = new Fiddler.DoubleBufferedListView();
            this.colRespondMatch = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colAction = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colHeaderValue = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.mnuContextAutoResponder = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.miRemoveRule = new System.Windows.Forms.ToolStripMenuItem();
            this.miPromoteRule = new System.Windows.Forms.ToolStripMenuItem();
            this.miDemoteRule = new System.Windows.Forms.ToolStripMenuItem();
            this.miRespondCloneRule = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.miFindRule = new System.Windows.Forms.ToolStripMenuItem();
            this.miExportRules = new System.Windows.Forms.ToolStripMenuItem();
            this.gbResponderEditor = new System.Windows.Forms.GroupBox();
            this.tbHeaderValue = new System.Windows.Forms.TextBox();
            this.cbAutoRespondOnce = new System.Windows.Forms.CheckBox();
            this.lnkTestRule = new System.Windows.Forms.LinkLabel();
            this.cbxRuleURI = new System.Windows.Forms.ComboBox();
            this.lblMultipleMatch = new System.Windows.Forms.Label();
            this.cbHeader = new System.Windows.Forms.ComboBox();
            this.cbxRuleAction = new System.Windows.Forms.ComboBox();
            this.btnSaveRule = new System.Windows.Forms.Button();
            this.pnlResponderActions = new System.Windows.Forms.Panel();
            this.RespondImport = new System.Windows.Forms.Button();
            this.btnRespondAdd = new System.Windows.Forms.Button();
            this.lblAutoRespondHeader = new System.Windows.Forms.Label();
            this.lnkAutoRespondHelp = new System.Windows.Forms.LinkLabel();
            this.pnlAutoResponders.SuspendLayout();
            this.mnuContextAutoResponder.SuspendLayout();
            this.gbResponderEditor.SuspendLayout();
            this.pnlResponderActions.SuspendLayout();
            this.SuspendLayout();
            // 
            // cbAutoRespond
            // 
            this.cbAutoRespond.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.cbAutoRespond.Location = new System.Drawing.Point(7, 30);
            this.cbAutoRespond.Name = "cbAutoRespond";
            this.cbAutoRespond.Size = new System.Drawing.Size(96, 18);
            this.cbAutoRespond.TabIndex = 0;
            this.cbAutoRespond.Text = "Enable rules";
            this.cbAutoRespond.UseVisualStyleBackColor = false;
            this.cbAutoRespond.CheckedChanged += new System.EventHandler(this.cbAutoRespond_CheckedChanged);
            // 
            // pnlAutoResponders
            // 
            this.pnlAutoResponders.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlAutoResponders.AutoScroll = true;
            this.pnlAutoResponders.Controls.Add(this.lvRespondRules);
            this.pnlAutoResponders.Controls.Add(this.gbResponderEditor);
            this.pnlAutoResponders.Controls.Add(this.pnlResponderActions);
            this.pnlAutoResponders.Location = new System.Drawing.Point(3, 54);
            this.pnlAutoResponders.Name = "pnlAutoResponders";
            this.pnlAutoResponders.Size = new System.Drawing.Size(576, 327);
            this.pnlAutoResponders.TabIndex = 7;
            // 
            // lvRespondRules
            // 
            this.lvRespondRules.AllowDrop = true;
            this.lvRespondRules.BackColor = System.Drawing.SystemColors.Control;
            this.lvRespondRules.CheckBoxes = true;
            this.lvRespondRules.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colRespondMatch,
            this.colHeader,
            this.colAction,
            this.colHeaderValue});
            this.lvRespondRules.ContextMenuStrip = this.mnuContextAutoResponder;
            this.lvRespondRules.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvRespondRules.EmptyText = null;
            this.lvRespondRules.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lvRespondRules.FullRowSelect = true;
            this.lvRespondRules.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lvRespondRules.HideSelection = false;
            this.lvRespondRules.Location = new System.Drawing.Point(0, 27);
            this.lvRespondRules.Name = "lvRespondRules";
            this.lvRespondRules.Size = new System.Drawing.Size(576, 221);
            this.lvRespondRules.TabIndex = 1;
            this.lvRespondRules.UseCompatibleStateImageBehavior = false;
            this.lvRespondRules.View = System.Windows.Forms.View.Details;
            this.lvRespondRules.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.lvRespondRules_ItemCheck);
            this.lvRespondRules.SelectedIndexChanged += new System.EventHandler(this.lvRespondRules_SelectedIndexChanged);
            this.lvRespondRules.DragDrop += new System.Windows.Forms.DragEventHandler(this.lvRespondRules_DragDrop);
            this.lvRespondRules.DragOver += new System.Windows.Forms.DragEventHandler(this.lvRespondRules_DragOver);
            this.lvRespondRules.KeyDown += new System.Windows.Forms.KeyEventHandler(this.lvRespondRules_KeyDown);
            // 
            // colRespondMatch
            // 
            this.colRespondMatch.Text = "If request matches...";
            this.colRespondMatch.Width = 250;
            // 
            // colHeader
            // 
            this.colHeader.DisplayIndex = 2;
            this.colHeader.Text = "header";
            this.colHeader.Width = 120;
            // 
            // colAction
            // 
            this.colAction.DisplayIndex = 1;
            this.colAction.Text = "action";
            // 
            // colHeaderValue
            // 
            this.colHeaderValue.Text = "header value";
            this.colHeaderValue.Width = 250;
            // 
            // mnuContextAutoResponder
            // 
            this.mnuContextAutoResponder.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miRemoveRule,
            this.miPromoteRule,
            this.miDemoteRule,
            this.miRespondCloneRule,
            this.toolStripMenuItem2,
            this.miFindRule,
            this.miExportRules});
            this.mnuContextAutoResponder.Name = "mnuContextAutoResponder";
            this.mnuContextAutoResponder.ShowImageMargin = false;
            this.mnuContextAutoResponder.ShowItemToolTips = false;
            this.mnuContextAutoResponder.Size = new System.Drawing.Size(117, 142);
            this.mnuContextAutoResponder.Opening += new System.ComponentModel.CancelEventHandler(this.mnuContextAutoResponder_Opening);
            // 
            // miRemoveRule
            // 
            this.miRemoveRule.Name = "miRemoveRule";
            this.miRemoveRule.ShortcutKeyDisplayString = "Del";
            this.miRemoveRule.Size = new System.Drawing.Size(116, 22);
            this.miRemoveRule.Text = "&Remove";
            this.miRemoveRule.Click += new System.EventHandler(this.miRemoveRule_Click);
            // 
            // miPromoteRule
            // 
            this.miPromoteRule.Name = "miPromoteRule";
            this.miPromoteRule.ShortcutKeyDisplayString = "+";
            this.miPromoteRule.Size = new System.Drawing.Size(116, 22);
            this.miPromoteRule.Text = "&Promote";
            this.miPromoteRule.Click += new System.EventHandler(this.miPromoteRule_Click);
            // 
            // miDemoteRule
            // 
            this.miDemoteRule.Name = "miDemoteRule";
            this.miDemoteRule.ShortcutKeyDisplayString = "-";
            this.miDemoteRule.Size = new System.Drawing.Size(116, 22);
            this.miDemoteRule.Text = "&Demote";
            this.miDemoteRule.Click += new System.EventHandler(this.miDemoteRule_Click);
            // 
            // miRespondCloneRule
            // 
            this.miRespondCloneRule.Name = "miRespondCloneRule";
            this.miRespondCloneRule.ShortcutKeyDisplayString = "D";
            this.miRespondCloneRule.Size = new System.Drawing.Size(116, 22);
            this.miRespondCloneRule.Text = "&Clone";
            this.miRespondCloneRule.Click += new System.EventHandler(this.miRespondCloneRule_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(113, 6);
            // 
            // miFindRule
            // 
            this.miFindRule.Name = "miFindRule";
            this.miFindRule.Size = new System.Drawing.Size(116, 22);
            this.miFindRule.Text = "F&ind...";
            this.miFindRule.Click += new System.EventHandler(this.miFindRule_Click);
            // 
            // miExportRules
            // 
            this.miExportRules.Name = "miExportRules";
            this.miExportRules.Size = new System.Drawing.Size(116, 22);
            this.miExportRules.Text = "E&xport All...";
            this.miExportRules.Click += new System.EventHandler(this.miExportRules_Click);
            // 
            // gbResponderEditor
            // 
            this.gbResponderEditor.BackColor = System.Drawing.SystemColors.Control;
            this.gbResponderEditor.Controls.Add(this.tbHeaderValue);
            this.gbResponderEditor.Controls.Add(this.cbAutoRespondOnce);
            this.gbResponderEditor.Controls.Add(this.lnkTestRule);
            this.gbResponderEditor.Controls.Add(this.cbxRuleURI);
            this.gbResponderEditor.Controls.Add(this.lblMultipleMatch);
            this.gbResponderEditor.Controls.Add(this.cbHeader);
            this.gbResponderEditor.Controls.Add(this.cbxRuleAction);
            this.gbResponderEditor.Controls.Add(this.btnSaveRule);
            this.gbResponderEditor.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.gbResponderEditor.Enabled = false;
            this.gbResponderEditor.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gbResponderEditor.Location = new System.Drawing.Point(0, 248);
            this.gbResponderEditor.Name = "gbResponderEditor";
            this.gbResponderEditor.Size = new System.Drawing.Size(576, 79);
            this.gbResponderEditor.TabIndex = 2;
            this.gbResponderEditor.TabStop = false;
            this.gbResponderEditor.Text = "Rule Editor";
            // 
            // tbHeaderValue
            // 
            this.tbHeaderValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbHeaderValue.Location = new System.Drawing.Point(228, 43);
            this.tbHeaderValue.Name = "tbHeaderValue";
            this.tbHeaderValue.Size = new System.Drawing.Size(232, 21);
            this.tbHeaderValue.TabIndex = 6;
            this.tbHeaderValue.Text = "custom cookie";
            // 
            // cbAutoRespondOnce
            // 
            this.cbAutoRespondOnce.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbAutoRespondOnce.AutoSize = true;
            this.cbAutoRespondOnce.Location = new System.Drawing.Point(466, 45);
            this.cbAutoRespondOnce.Name = "cbAutoRespondOnce";
            this.cbAutoRespondOnce.Size = new System.Drawing.Size(104, 17);
            this.cbAutoRespondOnce.TabIndex = 5;
            this.cbAutoRespondOnce.Text = "Match only once";
            this.cbAutoRespondOnce.UseVisualStyleBackColor = true;
            // 
            // lnkTestRule
            // 
            this.lnkTestRule.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lnkTestRule.AutoSize = true;
            this.lnkTestRule.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.lnkTestRule.Location = new System.Drawing.Point(467, 20);
            this.lnkTestRule.Name = "lnkTestRule";
            this.lnkTestRule.Size = new System.Drawing.Size(40, 13);
            this.lnkTestRule.TabIndex = 4;
            this.lnkTestRule.TabStop = true;
            this.lnkTestRule.Text = "Test...";
            this.lnkTestRule.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkTestRule_LinkClicked);
            // 
            // cbxRuleURI
            // 
            this.cbxRuleURI.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbxRuleURI.FormattingEnabled = true;
            this.cbxRuleURI.Items.AddRange(new object[] {
            "regex:(?inx).+\\.jpg$ #Match strings ending with JPG",
            "regex:(?inx).+\\.(gif|png|jpg)$ #Match strings ending with img types",
            "regex:(?inx)^https://.+\\.gif$ #Match HTTPS-delivered GIFs",
            "URLWithBody:Upload.php regex:^.*BodyText.*$",
            "method:OPTIONS XHRCors.php",
            "flag:x-ProcessInfo=iexplore",
            "Header:Accept=html",
            "65%PartialUrl"});
            this.cbxRuleURI.Location = new System.Drawing.Point(8, 16);
            this.cbxRuleURI.Name = "cbxRuleURI";
            this.cbxRuleURI.Size = new System.Drawing.Size(453, 21);
            this.cbxRuleURI.TabIndex = 0;
            this.cbxRuleURI.KeyDown += new System.Windows.Forms.KeyEventHandler(this.cbxRuleURI_KeyDown);
            // 
            // lblMultipleMatch
            // 
            this.lblMultipleMatch.AutoSize = true;
            this.lblMultipleMatch.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMultipleMatch.Location = new System.Drawing.Point(10, 20);
            this.lblMultipleMatch.Name = "lblMultipleMatch";
            this.lblMultipleMatch.Size = new System.Drawing.Size(223, 13);
            this.lblMultipleMatch.TabIndex = 3;
            this.lblMultipleMatch.Text = "Update all selected matches to respond with:";
            this.lblMultipleMatch.Visible = false;
            // 
            // cbHeader
            // 
            this.cbHeader.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cbHeader.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystem;
            this.cbHeader.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.cbHeader.ItemHeight = 13;
            this.cbHeader.Items.AddRange(new object[] {
            "Cookie",
            "User-Agent",
            "Content-Type",
            "Accept"});
            this.cbHeader.Location = new System.Drawing.Point(6, 43);
            this.cbHeader.MaxDropDownItems = 20;
            this.cbHeader.Name = "cbHeader";
            this.cbHeader.Size = new System.Drawing.Size(120, 21);
            this.cbHeader.TabIndex = 1;
            this.cbHeader.Text = "Cookie";
            this.cbHeader.KeyDown += new System.Windows.Forms.KeyEventHandler(this.cbxRuleAction_KeyDown);
            // 
            // cbxRuleAction
            // 
            this.cbxRuleAction.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystem;
            this.cbxRuleAction.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxRuleAction.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.cbxRuleAction.ItemHeight = 13;
            this.cbxRuleAction.Items.AddRange(new object[] {
            "add",
            "replace",
            "remove"});
            this.cbxRuleAction.Location = new System.Drawing.Point(132, 43);
            this.cbxRuleAction.MaxDropDownItems = 20;
            this.cbxRuleAction.Name = "cbxRuleAction";
            this.cbxRuleAction.Size = new System.Drawing.Size(90, 21);
            this.cbxRuleAction.TabIndex = 1;
            this.cbxRuleAction.SelectedIndexChanged += new System.EventHandler(this.cbxRuleAction_SelectedIndexChanged);
            // 
            // btnSaveRule
            // 
            this.btnSaveRule.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSaveRule.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSaveRule.Location = new System.Drawing.Point(512, 16);
            this.btnSaveRule.Name = "btnSaveRule";
            this.btnSaveRule.Size = new System.Drawing.Size(58, 21);
            this.btnSaveRule.TabIndex = 2;
            this.btnSaveRule.Text = "Save";
            this.btnSaveRule.Click += new System.EventHandler(this.btnSaveRule_Click);
            // 
            // pnlResponderActions
            // 
            this.pnlResponderActions.Controls.Add(this.RespondImport);
            this.pnlResponderActions.Controls.Add(this.btnRespondAdd);
            this.pnlResponderActions.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlResponderActions.Location = new System.Drawing.Point(0, 0);
            this.pnlResponderActions.Name = "pnlResponderActions";
            this.pnlResponderActions.Size = new System.Drawing.Size(576, 27);
            this.pnlResponderActions.TabIndex = 0;
            // 
            // RespondImport
            // 
            this.RespondImport.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RespondImport.Location = new System.Drawing.Point(81, 2);
            this.RespondImport.Name = "RespondImport";
            this.RespondImport.Size = new System.Drawing.Size(75, 22);
            this.RespondImport.TabIndex = 0;
            this.RespondImport.Text = "Import";
            this.RespondImport.Click += new System.EventHandler(this.btnRespondImport_Click);
            // 
            // btnRespondAdd
            // 
            this.btnRespondAdd.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRespondAdd.Location = new System.Drawing.Point(0, 2);
            this.btnRespondAdd.Name = "btnRespondAdd";
            this.btnRespondAdd.Size = new System.Drawing.Size(75, 22);
            this.btnRespondAdd.TabIndex = 0;
            this.btnRespondAdd.Text = "Add Rule";
            this.btnRespondAdd.Click += new System.EventHandler(this.btnRespondAdd_Click);
            // 
            // lblAutoRespondHeader
            // 
            this.lblAutoRespondHeader.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblAutoRespondHeader.BackColor = System.Drawing.Color.Gray;
            this.lblAutoRespondHeader.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblAutoRespondHeader.ForeColor = System.Drawing.Color.White;
            this.lblAutoRespondHeader.Location = new System.Drawing.Point(3, 0);
            this.lblAutoRespondHeader.Name = "lblAutoRespondHeader";
            this.lblAutoRespondHeader.Padding = new System.Windows.Forms.Padding(4);
            this.lblAutoRespondHeader.Size = new System.Drawing.Size(537, 24);
            this.lblAutoRespondHeader.TabIndex = 5;
            this.lblAutoRespondHeader.Text = "This Fiddler Extension can append,replace or remove headers before the request wa" +
    "s sent to server.";
            this.lblAutoRespondHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lnkAutoRespondHelp
            // 
            this.lnkAutoRespondHelp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lnkAutoRespondHelp.AutoSize = true;
            this.lnkAutoRespondHelp.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.lnkAutoRespondHelp.Location = new System.Drawing.Point(546, 6);
            this.lnkAutoRespondHelp.Name = "lnkAutoRespondHelp";
            this.lnkAutoRespondHelp.Size = new System.Drawing.Size(28, 13);
            this.lnkAutoRespondHelp.TabIndex = 9;
            this.lnkAutoRespondHelp.TabStop = true;
            this.lnkAutoRespondHelp.Text = "Help";
            this.lnkAutoRespondHelp.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkAutoRespondHelp_LinkClicked);
            // 
            // UIAutoRequest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.cbAutoRespond);
            this.Controls.Add(this.lnkAutoRespondHelp);
            this.Controls.Add(this.pnlAutoResponders);
            this.Controls.Add(this.lblAutoRespondHeader);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.Name = "UIAutoRequest";
            this.Size = new System.Drawing.Size(582, 384);
            this.pnlAutoResponders.ResumeLayout(false);
            this.mnuContextAutoResponder.ResumeLayout(false);
            this.gbResponderEditor.ResumeLayout(false);
            this.gbResponderEditor.PerformLayout();
            this.pnlResponderActions.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void lnkAutoRespondHelp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Utilities.LaunchHyperlink(CONFIG.GetRedirUrl("AutoResponder"));
        }

        private void lnkTestRule_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string text = this.cbxRuleURI.Text;
            string[] strArrays = new string[] { "header:", "method:", "flag:" };
            if (text.OICStartsWithAny(strArrays))
            {
                MessageBox.Show("Sorry, the TEST command can only be used with Rules that evaluate the URL.", "Untestable Expression");
                return;
            }
            UIARRuleTester uIARRuleTester = new UIARRuleTester(this.cbxRuleURI.Items, this.cbxRuleURI.Text);
            if (DialogResult.OK == uIARRuleTester.ShowDialog(FiddlerApplication.UI))
            {
                this.cbxRuleURI.Text = uIARRuleTester.cbxPattern.Text;
                this.btnSaveRule_Click(null, null);
            }
        }

        private void lvRespondRules_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                Session[] data = (Session[])e.Data.GetData("Fiddler.Session[]");
                if (data == null || (int)data.Length < 1)
                {
                    return;
                }
                AutoRequest._AutoRequester.ImportSessions(data);
                this._SelectMostRecentLVRespondRule();
                return;
            }
            this._SelectMostRecentLVRespondRule();
        }

        private void lvRespondRules_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("Fiddler.Session[]") && !e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.None;
                return;
            }
            e.Effect = DragDropEffects.Copy;
        }

        private void lvRespondRules_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            ListViewItem item = this.lvRespondRules.Items[e.Index];
            RequesterRule tag = (RequesterRule)item.Tag;
            if (tag != null)
            {
                bool currentValue = e.CurrentValue == CheckState.Checked;
                bool isEnabled = tag.IsEnabled;
                bool newValue = e.NewValue == CheckState.Checked;
                if (currentValue == isEnabled && newValue != isEnabled)
                {
                    tag.IsEnabled = e.NewValue == CheckState.Checked;
                    AutoRequest._AutoRequester.IsRuleListDirty = true;
                }
            }
            item.ForeColor = (e.NewValue == CheckState.Checked ? SystemColors.WindowText : SystemColors.ControlDark);
        }

        private void lvRespondRules_KeyDown(object sender, KeyEventArgs e)
        {
            Keys keyCode = e.KeyCode;
            if (keyCode > Keys.D)
            {
                if (keyCode > Keys.U)
                {
                    switch (keyCode)
                    {
                        case Keys.Add:
                            {
                                this.actPromoteRule();
                                e.SuppressKeyPress = true;
                                return;
                            }
                        case Keys.Separator:
                            {
                                break;
                            }
                        case Keys.Subtract:
                            {

                                this.actDemoteRule();
                                e.SuppressKeyPress = true;
                                break;
                            }
                        default:
                            {
                                switch (keyCode)
                                {
                                    case Keys.Oemplus:
                                        {
                                            this.actPromoteRule();
                                            e.SuppressKeyPress = true;
                                            return;
                                        }
                                    case Keys.Oemcomma:
                                        {
                                            break;
                                        }
                                    case Keys.OemMinus:
                                        {
                                            this.actDemoteRule();
                                            e.SuppressKeyPress = true;
                                            return;
                                        }
                                    default:
                                        {
                                            return;
                                        }
                                }
                                break;
                            }
                    }
                }
                else
                {
                    if (keyCode == Keys.M)
                    {
                        //,this.actSetRuleComment();
                        //,e.SuppressKeyPress = true;
                        return;
                    }
                    if (keyCode != Keys.U)
                    {
                        return;
                    }
                    if (e.Modifiers == Keys.Control)
                    {
                        if (this.lvRespondRules.SelectedItems.Count != 1)
                        {
                            return;
                        }
                        Utilities.CopyToClipboard(this.GetSelectedHyperlink());
                        return;
                    }
                }
            }
            else if (keyCode != Keys.Return)
            {
                if (keyCode == Keys.Delete)
                {
                    this.actRemoveSelectedRules();
                    e.SuppressKeyPress = true;
                    return;
                }
                switch (keyCode)
                {
                    case Keys.A:
                        {
                            if (e.Modifiers != Keys.Control)
                            {
                                break;
                            }
                            this.lvRespondRules.BeginUpdate();
                            foreach (ListViewItem item in this.lvRespondRules.Items)
                            {
                                item.Selected = true;
                            }
                            this.lvRespondRules.EndUpdate();
                            return;
                        }
                    case Keys.B:
                        {
                            break;
                        }
                    case Keys.C:
                        {
                            if (e.Modifiers != Keys.Control)
                            {
                                break;
                            }
                            if (this.lvRespondRules.SelectedItems.Count < 1)
                            {
                                return;
                            }
                            StringBuilder stringBuilder = new StringBuilder();
                            foreach (ListViewItem selectedItem in this.lvRespondRules.SelectedItems)
                            {
                                stringBuilder.AppendFormat("{0}\t{1}\r\n", selectedItem.Text, selectedItem.SubItems[1].Text);
                            }
                            Utilities.CopyToClipboard(stringBuilder.ToString());
                            e.SuppressKeyPress = true;
                            return;
                        }
                    case Keys.D:
                        {
                            this.actCloneRule();
                            e.SuppressKeyPress = true;
                            return;
                        }
                    default:
                        {
                            return;
                        }
                }
            }
        }

        private void lvRespondRules_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.lvRespondRules.SelectedItems.Count == 0)
            {
                this.gbResponderEditor.Enabled = false;
                return;
            }
            if (this.lvRespondRules.SelectedItems.Count > 1)
            {
                this.lnkTestRule.Visible = this.cbxRuleURI.Visible = false;
                this.cbAutoRespondOnce.CheckState = CheckState.Indeterminate;
                this.lblMultipleMatch.Visible = true;
                this.lblMultipleMatch.Text = string.Concat("Update ", this.lvRespondRules.SelectedItems.Count, " selected matches to respond with:");
                return;
            }
            RequesterRule tag = (RequesterRule)this.lvRespondRules.SelectedItems[0].Tag;
            this.lnkTestRule.Visible = this.cbxRuleURI.Visible = true;
            this.lblMultipleMatch.Visible = false;
            this.cbxRuleURI.Text = tag.sMatch;
            this.cbHeader.Text = tag.Header;
            this.tbHeaderValue.Text = tag.HeaderValue;
            this.cbxRuleAction.SelectedIndex = tag.Action;
            this.cbAutoRespondOnce.CheckState = (tag.bDisableOnMatch ? CheckState.Checked : CheckState.Unchecked);
            this.gbResponderEditor.Enabled = true;
        }


        private void miDemoteRule_Click(object sender, EventArgs e)
        {
            this.actDemoteRule();
        }


        private void miExportRules_Click(object sender, EventArgs e)
        {
            string str = Utilities.ObtainSaveFilename("Export rules...", "AutoResponder Ruleset|*.farx");
            if (!string.IsNullOrEmpty(str))
            {
                if (!str.OICEndsWith(".farx"))
                {
                    str = string.Concat(str, ".farx");
                }
                if (AutoRequest._AutoRequester.ExportFARX(str))
                {
                    FiddlerApplication.UI.sbpInfo.Text = string.Concat("Exported AutoRequest Rules to: ", str);
                    return;
                }
                FiddlerApplication.UI.sbpInfo.Text = "Failed to export AutoResponder Rules.";
            }
        }

        private void miFindRule_Click(object sender, EventArgs e)
        {
            string userString = frmPrompt.GetUserString("Find Rule", "Enter the string to find...", string.Empty, true);
            if (userString == null)
            {
                return;
            }
            this.lvRespondRules.BeginUpdate();
            int num = 0;
            foreach (ListViewItem item in this.lvRespondRules.Items)
            {
                if (item.Text.OICContains(userString) || item.SubItems[1].Text.OICContains(userString))
                {
                    num++;
                    if (num == 1)
                    {
                        item.EnsureVisible();
                    }
                    item.BackColor = Color.Yellow;
                }
                else
                {
                    item.BackColor = this.lvRespondRules.BackColor;
                }
            }
            this.lvRespondRules.EndUpdate();
            FiddlerApplication.UI.SetStatusText(string.Format("Found {0} matching rules.", num));
        }

        private void miPromoteRule_Click(object sender, EventArgs e)
        {
            this.actPromoteRule();
        }

        private void miRemoveRule_Click(object sender, EventArgs e)
        {
            this.actRemoveSelectedRules();
        }

        private void miRespondCloneRule_Click(object sender, EventArgs e)
        {
            this.actCloneRule();
        }
        private void mnuContextAutoResponder_Opening(object sender, CancelEventArgs e)
        {

            this.miRemoveRule.Enabled = this.lvRespondRules.SelectedItems.Count > 0;

            this.miPromoteRule.Enabled =
                this.miRespondCloneRule.Enabled =
                this.miDemoteRule.Enabled = this.lvRespondRules.SelectedItems.Count == 1;
            this.miExportRules.Enabled = this.lvRespondRules.Items.Count > 0;
        }

        private void cbxRuleAction_SelectedIndexChanged(object sender, EventArgs e)
        {

            this.tbHeaderValue.Enabled = this.cbxRuleAction.SelectedIndex != 2;

        }
    }
}

