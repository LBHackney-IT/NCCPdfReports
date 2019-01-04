using ceTe.DynamicPDF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace NCCPdfReports
{
    public class GeneratePDFDoc : ApiController
    {
        // GET api/<controller>
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<controller>/5
        public byte[] Get(string id, string startdate, string enddate)
        {
            try
            {
                BuildDoc bdoc = new BuildDoc();
                /*Document document = bdoc.GeneratePdfDocument(id, startdate, enddate);
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

        // POST api/<controller>
        public void Post([FromBody]string value)
        {
        }

        // PUT api/<controller>/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/<controller>/5
        public void Delete(int id)
        {
        }
    }
}