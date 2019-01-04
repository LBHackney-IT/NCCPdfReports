using System;
using ceTe.DynamicPDF;
using System.Web.UI;
using System.Threading.Tasks;

namespace NCCPdfReports
{
    public partial class CustomerTransactions : System.Web.UI.Page
    {
        string contactId = "";
        string tenAgreementRef = "";
        string startdate = "";
        string enddate = "";
        protected void Page_Load(object sender, System.EventArgs e)
        {
            contactId = Request.QueryString["contactid"];
            tenAgreementRef = Request.QueryString["tenagreementref"];
            startdate = Request.QueryString["startdate"];
            enddate = Request.QueryString["enddate"];
            if (!IsPostBack)
            {
                RegisterAsyncTask(new PageAsyncTask(GeneratePdfDocument));
            }
        }

        private async Task GeneratePdfDocument()
        {
            if (!string.IsNullOrEmpty(contactId) || !string.IsNullOrEmpty(tenAgreementRef) || !string.IsNullOrEmpty(startdate) || !string.IsNullOrEmpty(enddate))
            {
                BuildDoc bdoc = new BuildDoc();
                Document document = bdoc.GeneratePdfDocument(contactId, tenAgreementRef, startdate, enddate);
                if (document != null)
                    document.DrawToWeb();
            }
        }
        #region Web Form Designer generated code
        override protected void OnInit(EventArgs e)
        {
            //
            // CODEGEN: This call is required by the ASP.NET Web Form Designer.
            //
            InitializeComponent();
            base.OnInit(e);
        }

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
        }
        #endregion

    }
}