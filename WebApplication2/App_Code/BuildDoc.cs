using System;
using ceTe.DynamicPDF;
using ceTe.DynamicPDF.PageElements;
using System.Collections.Generic;
using System.Web;
using System.Configuration;
using System.Net.Http;
using System.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace NCCPdfReports
{
    public class BuildDoc
    {
        string PdfLicenceKey = ConfigurationManager.AppSettings["PdfLicenceKey"];
        private string ContactDetailsAPIURL = ConfigurationManager.AppSettings["ContactDetailsAPIURL"];
        private string SandboxContactDetailsAPIURL = ConfigurationManager.AppSettings["SandboxContactDetailsAPIURL"];
        private string TransactionDetailsAPIURL = ConfigurationManager.AppSettings["TransactionDetailsAPIURL"];
        private string TransactionStatementsAPIURl = ConfigurationManager.AppSettings["TransactionStatementsAPIURl"];
        private string AccountsDetailAPIURl = ConfigurationManager.AppSettings["AccountsDetailAPIURl"];
        private string RentBreakdownAPIURl = ConfigurationManager.AppSettings["RentBreakdownAPIURl"];
        
        // Template for document elements
        private Template template = new Template();

        // Page Dimensions of pages
        private static PageDimensions pageDimensions = new PageDimensions(PageSize.Letter, PageOrientation.Portrait, 54.0f);
        // Current page that elements are being added to
        private ceTe.DynamicPDF.Page currentPage = null;
        // Top Y coordinate for the body of the report

        private int PageWidth = 504;
        private static float Footer = 60;
        // Bottom Y coordinate for the body of the report
        private float bodyBottom = pageDimensions.Body.Bottom - pageDimensions.Body.Top - Footer;
        // Current Y coordinate where elements are being added
        private static float LEFTMARGIN = 2;
        private static float POS_TRANSACTION = 100;
        private static float POS_INTO = 250;
        private static float POS_OUTOF = 350;
        private static float POS_BALANCE = 450;
        private static float TABLE_TOP = 200;
        private static float PAGE_MIDDLE = 300;
        private float CURRENT_Y = TABLE_TOP + 30;
        private float bodyTop = TABLE_TOP + 30;
        int NormalFontSize = 14;
        int TitleFontSize = 30;
        int BoldFontSize = 14;
        int BoldFontSize2 = 12;
        string CurrentBalance = "";
        float RecordBalance = 0;
        // Used to control the alternating background
        private bool alternateBG = false;
        //Constructor
        public BuildDoc()
        {

        }

        public Document GeneratePdfDocument(string contactId, string tenAgreementRef, string sStartDate)
        {
            // Create a document and set it's properties
            Document.AddLicense(PdfLicenceKey);

            Document document = new Document();
            document.Creator = "London Borough of Hackney";
            document.Author = "ceTe Software";
            document.Title = "NCC Customer Reports";

            if (string.IsNullOrEmpty(tenAgreementRef) || string.IsNullOrEmpty(sStartDate) )
                return null;

            DateTime startDate = DateTime.Parse(sStartDate);
            var jsonciresponse = ExecuteAPI(ContactDetailsAPIURL, contactId);
            if (jsonciresponse == null)
            {
                string SandboxContactDetailsAPIURL = ConfigurationManager.AppSettings["SandboxContactDetailsAPIURL"];

                jsonciresponse = ExecuteAPI(SandboxContactDetailsAPIURL, contactId);
            }
            var jsonrentbreakiresponse = ExecuteAPIRetJArray(RentBreakdownAPIURl, $@"{tenAgreementRef}");
            if (jsonrentbreakiresponse == null)
            {

            }
            var jsontransdetresponse = ExecuteAPI(TransactionDetailsAPIURL, tenAgreementRef);
            if (jsontransdetresponse != null && jsonciresponse != null)
            {
                // Adds elements to the header template
                document.Template = SetTemplate(tenAgreementRef, startDate.ToShortDateString(), jsonciresponse, jsontransdetresponse, jsonrentbreakiresponse);
                string parameters = $@"{tenAgreementRef}&startdate={startDate}";
                var jsontranshistresponse = ExecuteAPIRetJArray(TransactionStatementsAPIURl, parameters);
                if (jsontranshistresponse != null)
                {
                        // Builds the report
                        BuildDocument(startDate, document, jsontranshistresponse);
                }
            }

            return document;
        }


        public JObject ExecuteAPI(string url, string parameters)
        {
            JObject jresponse = null;
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var response = httpClient.GetStringAsync(new Uri(url + parameters)).Result;
                    jresponse = JsonConvert.DeserializeObject<JObject>(response);
                    return jresponse;
                }
            }
            catch(Exception ex)
            {
                return jresponse;
            }
        }

        public JArray ExecuteAPIRetJArray(string url, string parameters)
        {
            using (var httpClient = new HttpClient())
            {
                var response = httpClient.GetStringAsync(new Uri(url + parameters)).Result;
                var jresponse = JsonConvert.DeserializeObject<JArray>(response);
                return jresponse;
            }

        }

        public Template SetTemplate(string tenAgreementRef, string startDate, JObject jsonciresponse, JToken jsontransdetresponse, JArray jsonrentbreakiresponse)
        {
            int currentPos = 0;
            int LeftLabelWidth = 100;
            int RightLabelWidth = 300;
            int RightLabelStart = LeftLabelWidth + 20;
            // Adds elements to the header template
            template.Elements.Add(new Image(HttpContext.Current.Server.MapPath("/Images/Hackney_Logo_Green_small20.jpg"), 300, 0));
            template.Elements.Add(new Label("Rent transactions", LEFTMARGIN, currentPos, RightLabelWidth, TitleFontSize, Font.HelveticaBold, TitleFontSize));
            template.Elements.Add(new Label("Name", LEFTMARGIN, currentPos += TitleFontSize+10, LeftLabelWidth, BoldFontSize, Font.Helvetica, NormalFontSize));
            string customername = string.Format("{0} {1} {2}", jsonciresponse["title"], jsonciresponse["firstName"], jsonciresponse["lastName"]);
            template.Elements.Add(new Label(customername, RightLabelStart, currentPos, RightLabelWidth, BoldFontSize, Font.HelveticaBold, BoldFontSize));
            string strBreakdownsDesc = "";
            string strBreakdownsValues = "";
            foreach (var rentBreakdown in jsonrentbreakiresponse)
            {
                float fcurrentval = float.Parse(rentBreakdown["value"].ToString());
                strBreakdownsDesc += string.Format("{0}\n", rentBreakdown["description"].ToString().Trim());
                strBreakdownsValues += string.Format("{0}\n", fcurrentval.ToString("c2"));  
            }
            //strBreakdownsDesc += "________________________\n";
            //strBreakdownsValues += "_________\n";
            float frent = float.Parse(jsontransdetresponse["rent"].ToString());
            strBreakdownsDesc += "Total Rent";
            strBreakdownsValues += string.Format("{0}\n", frent.ToString("c2"));

            template.Elements.Add(new Label(strBreakdownsDesc, PAGE_MIDDLE, currentPos, RightLabelWidth+500, 600, Font.Helvetica, 9));
            template.Elements.Add(new Label(strBreakdownsValues, PAGE_MIDDLE+100, currentPos, RightLabelWidth + 500, 600, Font.HelveticaBold, 9));

            template.Elements.Add(new Label("Address", LEFTMARGIN, currentPos += BoldFontSize, LeftLabelWidth, BoldFontSize, Font.Helvetica, NormalFontSize));
            template.Elements.Add(new Label(jsonciresponse["addressLine1"].ToString(), RightLabelStart, currentPos, RightLabelWidth, BoldFontSize, Font.HelveticaBold, BoldFontSize));
            template.Elements.Add(new Label(jsonciresponse["addressLine2"].ToString(), RightLabelStart, currentPos += BoldFontSize, RightLabelWidth, BoldFontSize, Font.HelveticaBold, BoldFontSize));
            template.Elements.Add(new Label(jsonciresponse["addressLine3"].ToString(), RightLabelStart, currentPos += BoldFontSize, RightLabelWidth, BoldFontSize, Font.HelveticaBold, BoldFontSize));
            template.Elements.Add(new Label(jsonciresponse["postCode"].ToString(), RightLabelStart, currentPos += BoldFontSize, RightLabelWidth, BoldFontSize, Font.HelveticaBold, BoldFontSize));
            template.Elements.Add(new Label("Payment Ref", LEFTMARGIN, currentPos += BoldFontSize, LeftLabelWidth, BoldFontSize, Font.Helvetica, NormalFontSize));
            template.Elements.Add(new Label(jsontransdetresponse["paymentReferenceNumber"].ToString(), RightLabelStart, currentPos, LeftLabelWidth, BoldFontSize, Font.HelveticaBold, BoldFontSize));
            currentPos += 30;//Adding some buffer space

            template.Elements.Add(new Line(LEFTMARGIN, currentPos += BoldFontSize, PageWidth, currentPos));

             currentPos += 5;//Adding some buffer space
            template.Elements.Add(new Label(string.Format("As of {0} your balance is:", DateTime.Today.ToString("dd MMM yyyy")), PAGE_MIDDLE, currentPos, RightLabelWidth, BoldFontSize, Font.Helvetica, BoldFontSize2));

            string strTransactionDateText = string.Format("Transactions: {0} to {1}", startDate, DateTime.Now.ToString("dd/MM/yyyy"));
            template.Elements.Add(new Label(strTransactionDateText, LEFTMARGIN, currentPos += BoldFontSize2, LeftLabelWidth + 300, BoldFontSize, Font.Helvetica, NormalFontSize));
            CurrentBalance = jsontransdetresponse["displayBalance"].ToString();
            string IsCreditOrArears = " is in credit";
            if (CurrentBalance.Contains("-"))
            {
                IsCreditOrArears = " is in arrears";
            }
            RecordBalance = float.Parse(CurrentBalance);
            string DisplayRecordBalance = RecordBalance.ToString("c2") + IsCreditOrArears; 
            template.Elements.Add(new Label(DisplayRecordBalance, 300, currentPos, LeftLabelWidth+100, BoldFontSize, Font.HelveticaBold, BoldFontSize));

            template.Elements.Add(new Label("You can pay online anytime by visiting ", LEFTMARGIN, currentPos += BoldFontSize, LeftLabelWidth+300, BoldFontSize, Font.Helvetica, BoldFontSize2));
            string strlinktest = "www.hackney.gov.uk/rentaccount";
            Label lbl = new Label(strlinktest, 210, currentPos, LeftLabelWidth + 100, BoldFontSize + 10, Font.Helvetica, BoldFontSize2, RgbColor.Blue);
            lbl.Underline = true;
            template.Elements.Add(lbl);
            template.Elements.Add(new Link(210, currentPos, LeftLabelWidth + 100, BoldFontSize+10, new UrlAction(strlinktest)));

            template.Elements.Add(new Label("Date", LEFTMARGIN, TABLE_TOP + BoldFontSize2, 100, BoldFontSize2, Font.HelveticaBold, BoldFontSize2));
            template.Elements.Add(new Label("Type of transaction", POS_TRANSACTION, TABLE_TOP + BoldFontSize2, 200, 11, Font.HelveticaBold, BoldFontSize2));
            template.Elements.Add(new Label("Credit", POS_INTO, TABLE_TOP + BoldFontSize2, 100, BoldFontSize2, Font.HelveticaBold, BoldFontSize2));
            template.Elements.Add(new Label("Charges", POS_OUTOF, TABLE_TOP + BoldFontSize2, 100, BoldFontSize2, Font.HelveticaBold, BoldFontSize2));
            template.Elements.Add(new Label("Balance", POS_BALANCE, TABLE_TOP + BoldFontSize2, 100, BoldFontSize2, Font.HelveticaBold, BoldFontSize2));
            template.Elements.Add(new Line(LEFTMARGIN, CURRENT_Y, PageWidth, CURRENT_Y));

            return template;
        }

        private void AddRecord(DateTime transDate, Document document, JToken response)
        {
            // Adds a new page to the document if needed
            if (CURRENT_Y > bodyBottom)
            {
                CURRENT_Y += 25;
                PageNumberingLabel pageNumLabel = new PageNumberingLabel("Page %%CP%% of %%TP%%", LEFTMARGIN, CURRENT_Y, PageWidth, BoldFontSize2, Font.HelveticaBold, BoldFontSize2);
                template.Elements.Add(pageNumLabel);
                template.Elements.Add(new ceTe.DynamicPDF.PageElements.Label(string.Format("Created On {0}", DateTime.Now.ToString("dd MMM yyyy")), LEFTMARGIN, CURRENT_Y, PageWidth, BoldFontSize2, Font.HelveticaBold, BoldFontSize2, TextAlign.Right));
                AddNewPage(document);
            }

            // Adds Labels to the document with data from the current node
             currentPage.Elements.Add(new Label(string.Format("{0:d}", transDate.ToShortDateString()), LEFTMARGIN, CURRENT_Y + 3, 100, BoldFontSize2, Font.Helvetica, BoldFontSize2));
            currentPage.Elements.Add(new Label(response["description"].ToString(), POS_TRANSACTION, CURRENT_Y + 3, 200, BoldFontSize2, Font.Helvetica, BoldFontSize2));

            string moneyin = response["in"].ToString();
            string monyeout = response["out"].ToString();
            string balance = response["balance"].ToString();
            currentPage.Elements.Add(new Label(moneyin, POS_INTO, CURRENT_Y + 3, 100, BoldFontSize2, Font.Helvetica, BoldFontSize2));
            currentPage.Elements.Add(new Label(monyeout, POS_OUTOF, CURRENT_Y + 3, 100, BoldFontSize2, Font.Helvetica, BoldFontSize2));
            currentPage.Elements.Add(new Label(balance, POS_BALANCE, CURRENT_Y + 3, 100, BoldFontSize2, Font.Helvetica, BoldFontSize2));
            template.Elements.Add(new Line(LEFTMARGIN, CURRENT_Y, PageWidth, CURRENT_Y, 1));
            // Toggles alternating background
            alternateBG = !alternateBG;

            // Increments the current Y position on the page
            CURRENT_Y += 18;
        }

        public void BuildDocument(DateTime startDate, Document document, JArray transResponse)
        {
            bool hasRecords = false;
            // Builds the PDF document with data from the XML Data
            AddNewPage(document);
            foreach (var response in transResponse)
            {
                DateTime transDate = DateTime.Parse(response["date"].ToString());
                if(transDate > startDate)
                {
                    //Add current node to the document
                    AddRecord(transDate, document, response);
                    hasRecords = true;
                }
            }

            if (!hasRecords)
            {
                currentPage.Elements.Add(new Label("No records found for the given date range of the statement.", LEFTMARGIN, CURRENT_Y + 3, PageWidth, BoldFontSize2, Font.Helvetica, BoldFontSize2));
                template.Elements.Add(new Line(LEFTMARGIN, CURRENT_Y, PageWidth, CURRENT_Y, 1));
            }
        }

        private void AddNewPage(Document document)
        {
            // Adds a new page to the document
            currentPage = new Page(pageDimensions);
            CURRENT_Y = bodyTop;
            alternateBG = false;
            document.Pages.Add(currentPage);
        }
    }
}