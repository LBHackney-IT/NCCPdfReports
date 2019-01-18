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
        private static float PAGE_MIDDLE = 300;
        private static float TABLE_TOP = 0;
        private float CURRENT_Y = 0;
        private float bodyTop = 0;
        int NormalFontSize = 14;
        int NormalFontSize2 = 12;
        int NormalFontSize3 = 10;
        int TitleFontSize = 30;
        int BoldFontSize = 14;
        int BoldFontSize2 = 12;
        int BoldFontSize3 = 10;
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
            int RightLabelStart = LeftLabelWidth;
            // Adds elements to the header template
            template.Elements.Add(new Image(HttpContext.Current.Server.MapPath("/Images/Hackney_Logo_Green_small20.jpg"), 300, 0));
            template.Elements.Add(new Label("Rent transactions", LEFTMARGIN, currentPos, RightLabelWidth, TitleFontSize, Font.HelveticaBold, TitleFontSize));
            template.Elements.Add(new Label("Name", LEFTMARGIN, currentPos += TitleFontSize+10, LeftLabelWidth, BoldFontSize, Font.Helvetica, NormalFontSize3));
            string customername = string.Format("{0} {1} {2}", jsonciresponse["title"], jsonciresponse["firstName"], jsonciresponse["lastName"]);
            template.Elements.Add(new Label(customername, RightLabelStart, currentPos, RightLabelWidth, BoldFontSize, Font.HelveticaBold, BoldFontSize3));
            string strBreakdownsDesc = "";
            string strBreakdownsValues = "";
            int breakdowncount = 1;
            foreach (var rentBreakdown in jsonrentbreakiresponse)
            {
                breakdowncount++;
                float fcurrentval = float.Parse(rentBreakdown["value"].ToString());
                strBreakdownsDesc += string.Format("{0}\n", rentBreakdown["description"].ToString().Trim());
                strBreakdownsValues += string.Format("{0,10:C2}\n", fcurrentval);  
            }
            float frent = float.Parse(jsontransdetresponse["rent"].ToString());
            strBreakdownsDesc += "Total Rent";
            strBreakdownsValues += string.Format("{0,10:C2}\n", frent);
            int RentBreakDownHeight = BoldFontSize * breakdowncount;
            template.Elements.Add(new Label(strBreakdownsDesc, PAGE_MIDDLE, currentPos, RightLabelWidth+500, RentBreakDownHeight, Font.Helvetica, 9));
            template.Elements.Add(new Label(strBreakdownsValues, PAGE_MIDDLE+100, currentPos, 50, RentBreakDownHeight, Font.HelveticaBold, 9, TextAlign.Right));

            template.Elements.Add(new Label("Address", LEFTMARGIN, currentPos += BoldFontSize, LeftLabelWidth, BoldFontSize, Font.Helvetica, NormalFontSize3));
            template.Elements.Add(new Label(jsonciresponse["addressLine1"].ToString(), RightLabelStart, currentPos, RightLabelWidth, BoldFontSize, Font.HelveticaBold, BoldFontSize3));
            if(!string.IsNullOrEmpty(jsonciresponse["addressLine2"].ToString()))
                template.Elements.Add(new Label(jsonciresponse["addressLine2"].ToString(), RightLabelStart, currentPos += BoldFontSize3, RightLabelWidth, BoldFontSize3, Font.HelveticaBold, BoldFontSize3));
            template.Elements.Add(new Label(jsonciresponse["addressLine3"].ToString(), RightLabelStart, currentPos += BoldFontSize3, RightLabelWidth, BoldFontSize3, Font.HelveticaBold, BoldFontSize3));
            template.Elements.Add(new Label(jsonciresponse["postCode"].ToString(), RightLabelStart, currentPos += BoldFontSize3, RightLabelWidth, BoldFontSize3, Font.HelveticaBold, BoldFontSize3));
            template.Elements.Add(new Label("Payment Ref", LEFTMARGIN, currentPos += BoldFontSize3, LeftLabelWidth, BoldFontSize3, Font.Helvetica, NormalFontSize3));
            template.Elements.Add(new Label(jsontransdetresponse["paymentReferenceNumber"].ToString(), RightLabelStart, currentPos, LeftLabelWidth, BoldFontSize3, Font.HelveticaBold, BoldFontSize3));
            currentPos += 30;//Adding some buffer space
            if(currentPos< RentBreakDownHeight)
                currentPos = RentBreakDownHeight + 20;
            template.Elements.Add(new Line(LEFTMARGIN, currentPos, PageWidth, currentPos));

            currentPos += 5;//Adding some buffer space
            template.Elements.Add(new Label(string.Format("As of {0} your balance is:", DateTime.Today.ToString("dd MMM yyyy")), PAGE_MIDDLE, currentPos, RightLabelWidth, BoldFontSize, Font.Helvetica, BoldFontSize2));

            string strTransactionDateText = string.Format("Transactions: {0} to {1}", startDate, DateTime.Now.ToString("dd/MM/yyyy"));
            template.Elements.Add(new Label(strTransactionDateText, LEFTMARGIN, currentPos += BoldFontSize, LeftLabelWidth + 300, BoldFontSize, Font.Helvetica, NormalFontSize));
            CurrentBalance = jsontransdetresponse["displayBalance"].ToString();
            string IsCreditOrArears = " in credit";
            if (CurrentBalance.Contains("-"))
            {
                IsCreditOrArears = " in arrears";
            }
            RecordBalance = float.Parse(CurrentBalance);
            string DisplayRecordBalance = RecordBalance.ToString("c2") + IsCreditOrArears; 
            template.Elements.Add(new Label(DisplayRecordBalance, 300, currentPos, LeftLabelWidth+100, BoldFontSize, Font.HelveticaBold, BoldFontSize));

            template.Elements.Add(new Label("You can pay online anytime by visiting ", LEFTMARGIN, currentPos += BoldFontSize +5, LeftLabelWidth+300, BoldFontSize, Font.Helvetica, BoldFontSize2));
            string strlinktest = "www.hackney.gov.uk/rentaccount";
            Label lbl = new Label(strlinktest, 210, currentPos, LeftLabelWidth + 100, BoldFontSize + 10, Font.Helvetica, BoldFontSize2, RgbColor.Blue);
            lbl.Underline = true;
            template.Elements.Add(lbl);
            template.Elements.Add(new Link(210, currentPos, LeftLabelWidth + 100, BoldFontSize+10, new UrlAction(strlinktest)));
            currentPos += BoldFontSize2 + 20;
            TABLE_TOP = currentPos;
            template.Elements.Add(new Label("Date", LEFTMARGIN, TABLE_TOP, 100, BoldFontSize2, Font.HelveticaBold, BoldFontSize2));
            template.Elements.Add(new Label("Type of transaction", POS_TRANSACTION, TABLE_TOP, 200, 11, Font.HelveticaBold, BoldFontSize2));
            template.Elements.Add(new Label("Credits", POS_INTO, TABLE_TOP, 100, BoldFontSize2, Font.HelveticaBold, BoldFontSize2));
            template.Elements.Add(new Label("Charges", POS_OUTOF, TABLE_TOP, 100, BoldFontSize2, Font.HelveticaBold, BoldFontSize2));
            template.Elements.Add(new Label("Balance", POS_BALANCE, TABLE_TOP, 100, BoldFontSize2, Font.HelveticaBold, BoldFontSize2));

            CURRENT_Y = currentPos + BoldFontSize; 
            bodyTop = CURRENT_Y;
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
            currentPage.Elements.Add(new Label(balance, POS_BALANCE, CURRENT_Y + 3, 50, BoldFontSize2, Font.Helvetica, BoldFontSize2, TextAlign.Right));
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