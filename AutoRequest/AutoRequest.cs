using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Fiddler;


namespace AutoRequest
{
    public class AutoRequest : IAutoTamper3
    {
        internal static AutoRequester _AutoRequester { set; get; }

        public void AutoTamperRequestAfter(Session oSession)
        {
        }

        public void AutoTamperRequestBefore(Session oSession)
        {
            if (AutoRequest._AutoRequester != null && AutoRequest._AutoRequester.IsEnabled)
                AutoRequest._AutoRequester.DoMatchBeforeRequestTampering(oSession);
        }

        public void AutoTamperResponseAfter(Session oSession)
        {

        }

        public void AutoTamperResponseBefore(Session oSession)
        {

        }

        public void OnBeforeReturningError(Session oSession)
        {

        }

        public void OnBeforeUnload()
        {
            AutoRequest._AutoRequester.SaveDefaultRules();
        }

        public void OnLoad()
        {
            AutoRequest._AutoRequester = new AutoRequester();
            AutoRequest._AutoRequester.LoadRules();
            AutoRequest._AutoRequester.AddToUI();
        }

        public void OnPeekAtRequestHeaders(Session oSession)
        {

        }

        public void OnPeekAtResponseHeaders(Session oSession)
        {

        }
    }
}
