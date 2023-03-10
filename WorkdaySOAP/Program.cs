/**
 * Retrieve WorkDay data via SOAP APIs and OAuth2 authentication 
 * Based on groovy code provided by Jeremy Heydman
 * 
 * Pulls 2 pages of HR Get-Workers data with 50 records each and saved as XML files
 * 
 * Documentation for Get-Workers API
 * https://community.workday.com/sites/default/files/file-hosting/productionapi/Human_Resources/v39.2/Get_Workers.html
 * 
 * Documentation for all Workday APIs
 * https://community.workday.com/sites/default/files/file-hosting/productionapi/operations/index.html
 * 
 * Nik Nik Hassan MNSU Mankato 2/7/2023
 * 
 * */
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;
using System.Text;
using System.Web;
using System.IO;
using System.Xml.Linq;
using Newtonsoft.Json;
using System.Net;
using System.Xml;

namespace WorkdaySOAP
{
    class Program
    {
        static string wdTenant = "minnstate4";
        static string wdVersion = "v39.1";
        static string wdUsername = "";

        static string clientId = "";
        static string clientSecret = "";
        static string refreshToken = "";

        static int resultsPerPage = 50;

        
       static void Main(string[] args)
        {
            // Location on local where files are saved
            string pathFile = @"C:\WorkdayData\";
            Console.WriteLine("WorkdayAPI");
            AccessToken token = GetToken().Result;
            Console.WriteLine(String.Format("Access Token: {0}", token.access_token));
            var p = new Program();
            bool getPages = true;
            //Loop until all pages are downloaded and saved
            int page = 1;
            while (getPages)
            {
                Guid guid = Guid.NewGuid();
                String results = p.SOAPManual(token.access_token, guid, page);
                //Print out files that are saved
                Console.WriteLine(String.Format("page: {0}", page));
                Console.WriteLine(String.Format("Request ID: {0}", guid));
                Console.WriteLine(String.Format("Saved file to: {0}", pathFile + $"GetWorkers_{guid}.xml\n"));
                
                //Save files
                FileStream outputFileStream = new FileStream(pathFile + $"GetWorkers_{guid}.xml", FileMode.Append, FileAccess.Write);
                StreamWriter writer = new StreamWriter(outputFileStream);
                writer.WriteLine(p.FormatXml(results));
                writer.Close();
                outputFileStream.Close();

                //Get total pages of results
                int totalPages = p.ReturnTotalPages(results);

                Console.WriteLine("***************");
                // Print the extracted data to the console
                Console.WriteLine(totalPages);
                Console.WriteLine("***************");

                //End loop if at end of results
                if (page == totalPages)
                {
                    getPages = false;
                }
                Console.WriteLine(getPages);
                page++;
            }
        }

        string FormatXml(string xml)
        {
            try
            {
                XDocument doc = XDocument.Parse(xml);
                return doc.ToString();
            }
            catch (Exception)
            {
                Console.WriteLine("Error in parsing xml");
                return xml;
            }
        }

        int ReturnTotalPages(string results)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(results);
            XmlNamespaceManager nsMgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsMgr.AddNamespace("env", "http://schemas.xmlsoap.org/soap/envelope/");
            nsMgr.AddNamespace("wd", "urn:com.workday/bsvc");

            // Find the node you want to extract data from using XPath with the namespace manager
            XmlNode node = xmlDoc.SelectSingleNode("//wd:Total_Pages", nsMgr);
            return Int32.Parse(node.InnerText);
        }

        static async Task<AccessToken> GetToken()
        {
            Console.WriteLine("Getting Token");
            string credentials = String.Format("{0}:{1}", clientId, clientSecret);

            using (var client = new HttpClient())
            {
                //Define Headers
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials)));

                //Prepare Request Body
                List<KeyValuePair<string, string>> requestData = new List<KeyValuePair<string, string>>();
                requestData.Add(new KeyValuePair<string, string>("client_id", clientId));
                requestData.Add(new KeyValuePair<string, string>("client_secret", clientSecret));
                requestData.Add(new KeyValuePair<string, string>("grant_type", "refresh_token"));
                requestData.Add(new KeyValuePair<string, string>("refresh_token", refreshToken));

                FormUrlEncodedContent requestBody = new FormUrlEncodedContent(requestData);

                //Request Token
                var request = await client.PostAsync($"https://wd2-impl-services1.workday.com/ccx/oauth2/{wdTenant}/token", requestBody);
                var response = await request.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<AccessToken>(response);
            }
        }

        public String SOAPManual(string token, Guid guid, int page)
        {
            const string action = "POST";

            XmlDocument soapEnvelopeXml = CreateSoapEnvelope(page);
            HttpWebRequest webRequest = CreateWebRequest($"https://wd2-impl-services1.workday.com/ccx/service/{wdTenant}/Human_Resources/{wdVersion}", action, token, guid);

            InsertSoapEnvelopeIntoWebRequest(soapEnvelopeXml, webRequest);

            string result;
            using (WebResponse response = webRequest.GetResponse())
            {
                using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                {
                    result = rd.ReadToEnd();
                }
            }
            return result;
        }

        private static HttpWebRequest CreateWebRequest(string url, string action, string token, Guid guid)
        {
            //Create web request header
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Headers.Add("wd-external-request-id", $"{guid}");
            webRequest.Headers.Add("wd-external-application-id", "campus-poc-example");
            webRequest.Headers.Add("wd-external-originator-id", $"{wdUsername}");
            webRequest.Headers.Add("Authorization", $"Bearer {token}");
            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            return webRequest;
        }

        private static XmlDocument CreateSoapEnvelope(int page)
        {
            //SOAP envelope for request
            XmlDocument soapEnvelopeXml = new XmlDocument();
            soapEnvelopeXml.LoadXml(@$"<?xml version=""1.0"" encoding=""utf-8"" ?>
                                        <env:Envelope
                                            xmlns:env=""http://schemas.xmlsoap.org/soap/envelope/""
                                            xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
                                            xmlns:wsse=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd"" >
                                            <env:Body>
                                                <bsvc:Get_Workers_Request bsvc:version =""{wdVersion}"" xmlns:bsvc=""urn:com.workday/bsvc"">
                                                      <bsvc:Request_Criteria>
                                                            <bsvc:Organization_Reference>
                                                                <bsvc:ID bsvc:type=""Company_Reference_ID"">CU0071</bsvc:ID>
                                                            </bsvc:Organization_Reference>
                                                            <bsvc:Include_Subordinate_Organizations>true</bsvc:Include_Subordinate_Organizations>
                                                       </bsvc:Request_Criteria>
                                                       <bsvc:Response_Filter>
                                                           <bsvc:Page>{page}</bsvc:Page>
                                                           <bsvc:Count>{resultsPerPage }</bsvc:Count>
                                                       </bsvc:Response_Filter>
                                                       <bsvc:Response_Group>
                                                            <bsvc:Include_User_Account>true</bsvc:Include_User_Account>
                                                      </bsvc:Response_Group>
                                                  </bsvc:Get_Workers_Request>
                                             </env:Body>
                                           </env:Envelope>");
            return soapEnvelopeXml;
        }

        private static void InsertSoapEnvelopeIntoWebRequest(XmlDocument soapEnvelopeXml, HttpWebRequest webRequest)
        {
            using (Stream stream = webRequest.GetRequestStream())
            {
                soapEnvelopeXml.Save(stream);
            }
        }
    }
}