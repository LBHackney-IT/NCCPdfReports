using ceTe.DynamicPDF;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace NCCPdfReports
{
    public partial class SMSStatements : System.Web.UI.Page
    {
        string contactId = "";
        string tenAgreementRef = "";
        string startdate = "";
        string enddate = "";
        string emailId = "";
        string templateId = "";
        private string GovNotifyAPIURL = ConfigurationManager.AppSettings["GovNotifyAPIURL"];
        
        protected void Page_Load(object sender, System.EventArgs e)
        {
            contactId = Request.QueryString["contactid"];
            tenAgreementRef = Request.QueryString["tenagreementref"];
            startdate = Request.QueryString["startdate"];
            enddate = Request.QueryString["enddate"];
            emailId = Request.QueryString["emailid"];
            templateId = Request.QueryString["templateid"];
            if (!IsPostBack)
            {
                RegisterAsyncTask(new PageAsyncTask(GeneratePdfDocument));
            }
        }
        private async Task GeneratePdfDocument()
        {
            if (!string.IsNullOrEmpty(contactId) || !string.IsNullOrEmpty(startdate) )
            {
                BuildDoc bdoc = new BuildDoc();
                Document document = bdoc.GeneratePdfDocument(contactId, tenAgreementRef, startdate);
                if (document != null)
                {
                    byte[] docbytes = document.Draw();
                    string parameters = string.Format("EmailTo={0}&TemplateId={1}&TemplateData={'rent balance':'30', 'link_to_document':'{2}','Rent amount':'55.6'}",emailId,templateId, docbytes);
                    var jsonciresponse = bdoc.ExecuteAPI(GovNotifyAPIURL, parameters);
                    if (jsonciresponse != null)
                    {

                    }
                }
                    
            }
        }

    }
}