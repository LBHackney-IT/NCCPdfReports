using ceTe.DynamicPDF;
using System;
using System.Web.Http;

namespace NCCPdfReports
{
    public class GenerateStatements : ApiController
    {
        [HttpGet]
        public  byte[] GetDocumentBytes(string contactId, string startdate, string enddate)
        {
            try
            {
                BuildDoc bdoc = new BuildDoc();
                /*Document document = bdoc.GeneratePdfDocument(contactId, startdate, enddate);
                if (document != null)
                {
                    return document.Draw();
                }
                else*/
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}