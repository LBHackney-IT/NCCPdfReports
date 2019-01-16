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
    public class BuildDocV1
    {
        string PdfLicenceKey = ConfigurationManager.AppSettings["PdfLicenceKey"];
        private string ManageTenancyAPIURL = ConfigurationManager.AppSettings["ManageTenancyAPIURL"];
        private string ContactDetailsAPIURL = ConfigurationManager.AppSettings["ContactDetailsAPIURL"];
        private string AccountsDetailAPIURl = ConfigurationManager.AppSettings["AccountsDetailAPIURl"];

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
        private static float TABLE_TOP = 220;
        private float CURRENT_Y = TABLE_TOP + 30;
        private float bodyTop = TABLE_TOP + 30;
        int NormalFontSize = 14;
        int BoldFontSize = 14;
        int BoldFontSize2 = 12;
        string CurrentBalance = "";
        float RecordBalance = 0;
        // Used to control the alternating background
        private bool alternateBG = false;
        //Constructor
        public BuildDocV1()
        {

        }

        public Document GeneratePdfDocument(string contactId, string sStartDate, string sEndDate)
        {
            // Create a document and set it's properties
            Document.AddLicense(PdfLicenceKey);

            Document document = new Document();
            document.Creator = "London Borough of Hackney";
            document.Author = "ceTe Software";
            document.Title = "NCC Customer Reports";

            if (string.IsNullOrEmpty(contactId) || string.IsNullOrEmpty(sStartDate) || string.IsNullOrEmpty(sEndDate))
                return null;

            DateTime startDate = DateTime.Parse(sStartDate);
            DateTime endDate = DateTime.Parse(sEndDate);
            var jsonciresponse = ExecuteAPI(ContactDetailsAPIURL, contactId);
            if (jsonciresponse != null)
            {

            }
            var housingtagref = "";
            var jsonaccresponse = ExecuteAPI(AccountsDetailAPIURl, contactId);
            if (jsonaccresponse?["results"] != null)
            {
                var accResponse = jsonaccresponse["results"];
                housingtagref = accResponse["tagReferenceNumber"].ToString();

                // Adds elements to the header template
                document.Template = SetTemplate(startDate.ToShortDateString(), endDate.ToShortDateString(), jsonciresponse, accResponse);
            }

            var jsontransresponse = ExecuteAPI(ManageTenancyAPIURL, housingtagref);
            if (jsontransresponse?["results"] != null)
            {
                var transResponse = jsontransresponse["results"].ToList();

                if (transResponse.Count > 0)
                {
                    // Builds the report
                    BuildDocument(startDate, endDate, document, transResponse);
                }
            }
            return document;
        }


        public JObject ExecuteAPI(string url, string parameters)
        {
            using (var httpClient = new HttpClient())
            {
                var response = httpClient.GetStringAsync(new Uri(url + parameters)).Result;
                var jresponse = JsonConvert.DeserializeObject<JObject>(response);
                return jresponse;
            }

        }

        public Template SetTemplate(string startDate, string endDate, JObject jsonciresponse, JToken jsonaccresponse)
        {
            // Adds elements to the header template
            template.Elements.Add(new Image(HttpContext.Current.Server.MapPath("/Images/Hackney_Logo_Green_small20.jpg"), 300, 0));
            int currentPos = 30;
            int LeftLabelWidth = 100;
            int RightLabelWidth = 300;
            int RightLabelStart = LeftLabelWidth + 20;
            template.Elements.Add(new Label("Name", LEFTMARGIN, currentPos += BoldFontSize, LeftLabelWidth, BoldFontSize, Font.Helvetica, NormalFontSize));
            string customername = string.Format("{0} {1} {2}", jsonciresponse["title"], jsonciresponse["firstName"], jsonciresponse["lastName"]);
            template.Elements.Add(new Label(customername, RightLabelStart, currentPos, RightLabelWidth, BoldFontSize, Font.HelveticaBold, BoldFontSize));
            template.Elements.Add(new Label("Address", LEFTMARGIN, currentPos += BoldFontSize, LeftLabelWidth, BoldFontSize, Font.Helvetica, NormalFontSize));
            template.Elements.Add(new Label(jsonciresponse["addressLine1"].ToString(), RightLabelStart, currentPos, RightLabelWidth, BoldFontSize, Font.HelveticaBold, BoldFontSize));
            template.Elements.Add(new Label(jsonciresponse["addressLine2"].ToString(), RightLabelStart, currentPos += BoldFontSize, RightLabelWidth, BoldFontSize, Font.HelveticaBold, BoldFontSize));
            template.Elements.Add(new Label(jsonciresponse["addressLine3"].ToString(), RightLabelStart, currentPos += BoldFontSize, RightLabelWidth, BoldFontSize, Font.HelveticaBold, BoldFontSize));
            template.Elements.Add(new Label(jsonciresponse["postCode"].ToString(), RightLabelStart, currentPos += BoldFontSize, RightLabelWidth, BoldFontSize, Font.HelveticaBold, BoldFontSize));
            template.Elements.Add(new Label("Account", LEFTMARGIN, currentPos += BoldFontSize, LeftLabelWidth, BoldFontSize, Font.Helvetica, NormalFontSize));
            template.Elements.Add(new Label(jsonaccresponse["tagReferenceNumber"].ToString(), RightLabelStart, currentPos, LeftLabelWidth, BoldFontSize, Font.HelveticaBold, BoldFontSize));

            currentPos += 20;//Adding some buffer space
            template.Elements.Add(new Label("Transactions since:", LEFTMARGIN, currentPos += BoldFontSize, LeftLabelWidth + 50, BoldFontSize, Font.Helvetica, BoldFontSize2));
            template.Elements.Add(new Label("Until:", 150, currentPos, LeftLabelWidth, BoldFontSize, Font.Helvetica, BoldFontSize2));
            template.Elements.Add(new Label(string.Format("As of {0} your balance is:", DateTime.Today.ToString("dd MMM yyyy")), 300, currentPos, RightLabelWidth, BoldFontSize, Font.Helvetica, BoldFontSize2));
            template.Elements.Add(new Label(startDate, LEFTMARGIN, currentPos += BoldFontSize, LeftLabelWidth, BoldFontSize, Font.HelveticaBold, BoldFontSize));
            template.Elements.Add(new Label(endDate, 150, currentPos, LeftLabelWidth, BoldFontSize, Font.HelveticaBold, BoldFontSize));
            CurrentBalance = jsonaccresponse["currentBalance"].ToString();
            var Rent = jsonaccresponse["rent"].ToString();
            var HousingBenefit = jsonaccresponse["benefit"].ToString();

            RecordBalance = float.Parse(CurrentBalance);
            float fRent = float.Parse(Rent);
            float fHousingBenefit = float.Parse(HousingBenefit);

            float fActualCurrentBalance = RecordBalance + (fRent - fHousingBenefit);
            string DisplayRecordBalance = (fActualCurrentBalance).ToString("c2");
            template.Elements.Add(new Label(DisplayRecordBalance, 300, currentPos, LeftLabelWidth, BoldFontSize, Font.HelveticaBold, BoldFontSize));

            template.Elements.Add(new Label("You can pay online anytime by visiting ", LEFTMARGIN, currentPos += BoldFontSize + 20, LeftLabelWidth+300, BoldFontSize, Font.Helvetica, BoldFontSize2));
            string strlinktest = "www.hackney.gov.uk/rentaccount";
            Label lbl = new Label(strlinktest, 210, currentPos, LeftLabelWidth + 100, BoldFontSize + 10, Font.Helvetica, BoldFontSize2, RgbColor.Blue);
            lbl.Underline = true;
            template.Elements.Add(lbl);
            template.Elements.Add(new Link(210, currentPos, LeftLabelWidth + 100, BoldFontSize+10, new UrlAction(strlinktest)));
          
            template.Elements.Add(new Label("Money into", POS_INTO, TABLE_TOP, 156, BoldFontSize2, Font.HelveticaBold, BoldFontSize2));
            template.Elements.Add(new Label("Money out of", POS_OUTOF, TABLE_TOP, 156, BoldFontSize2, Font.HelveticaBold, BoldFontSize2));

            template.Elements.Add(new Label("Date", LEFTMARGIN, TABLE_TOP + BoldFontSize2, 100, BoldFontSize2, Font.HelveticaBold, BoldFontSize2));
            template.Elements.Add(new Label("Type of transaction", POS_TRANSACTION, TABLE_TOP + BoldFontSize2, 200, 11, Font.HelveticaBold, BoldFontSize2));
            template.Elements.Add(new Label("your account", POS_INTO, TABLE_TOP + BoldFontSize2, 100, BoldFontSize2, Font.HelveticaBold, BoldFontSize2));
            template.Elements.Add(new Label("your account", POS_OUTOF, TABLE_TOP + BoldFontSize2, 100, BoldFontSize2, Font.HelveticaBold, BoldFontSize2));
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
            currentPage.Elements.Add(new Label(response["debDesc"].ToString(), POS_TRANSACTION, CURRENT_Y + 3, 200, BoldFontSize2, Font.Helvetica, BoldFontSize2));
            var realvalue = response["realValue"].ToString();
            var DebitValue = "";
            var CreditValue = "";
            float fDebitValue = 0F;
            float fCreditValue = 0F;
            string DisplayRecordBalance = (-RecordBalance).ToString("c2");
            if (realvalue.IndexOf("-") != -1)
            {
                DebitValue = realvalue;
                fDebitValue = float.Parse(DebitValue);
                RecordBalance = (RecordBalance - fDebitValue);
                DebitValue = (-fDebitValue).ToString();
            }
            else
            {
                CreditValue = realvalue;
                fCreditValue = float.Parse(CreditValue);
                RecordBalance = (RecordBalance - fCreditValue);
                CreditValue = (-fCreditValue).ToString();
            }

            currentPage.Elements.Add(new Label(DebitValue, POS_INTO, CURRENT_Y + 3, 100, BoldFontSize2, Font.Helvetica, BoldFontSize2));
            currentPage.Elements.Add(new Label(CreditValue, POS_OUTOF, CURRENT_Y + 3, 100, BoldFontSize2, Font.Helvetica, BoldFontSize2));
            currentPage.Elements.Add(new Label(DisplayRecordBalance, POS_BALANCE, CURRENT_Y + 3, 100, BoldFontSize2, Font.Helvetica, BoldFontSize2));
            template.Elements.Add(new Line(LEFTMARGIN, CURRENT_Y, PageWidth, CURRENT_Y, 1));
            // Toggles alternating background
            alternateBG = !alternateBG;

            // Increments the current Y position on the page
            CURRENT_Y += 18;
        }

        public void BuildDocument(DateTime startDate, DateTime endDate, Document document, List<JToken> transResponse)
        {
            bool hasRecords = false;
            // Builds the PDF document with data from the XML Data
            AddNewPage(document);
            foreach (var response in transResponse)
            {
                DateTime transDate = DateTime.Parse(response["postDate"].ToString());
                if(transDate > startDate && transDate < endDate)
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