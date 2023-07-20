// See https://aka.ms/new-console-template for more information
using iText.Html2pdf;
using iText.Html2pdf.Attach.Impl;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout.Font;
using System;
using System.IO;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Microsoft.VisualBasic.FileIO;
using iText.Kernel.Utils;
using iText.Layout;
using System.Data.Common;
using OpenQA.Selenium.DevTools.V105.DOM;
using OpenQA.Selenium.DevTools.V105.Runtime;
using iText.Commons.Actions;
using iText.Kernel.Pdf.Tagging;
using iText.Pdfa;
using iText.Html2pdf.Attach.Impl.Tags;
using iText.Html2pdf.Attach;
using iText.StyledXmlParser.Node;
using iText.Layout.Element;
using iText.Kernel.Pdf.Tagutils;
using OpenQA.Selenium.DevTools.V105.Animation;
using System.Reflection;
using System.Linq;

namespace pdf508Script
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string baseDirectory = "C:\\Users\\ekurtz\\Documents\\Projects\\fact_sheets\\pdf508\\";

            string html1 = File.ReadAllText(baseDirectory + "template_508-1.html");
            string html2 = File.ReadAllText(baseDirectory + "template_508-2.html");

            string csvPath = baseDirectory +"merged_20230720.csv";

            //SET THIS NUMBER TO INDICATE WHAT ROW TO STOP PROCESSING EXCEL SHEET. 
            int rowsToProcess = 9999;

            using (TextFieldParser parser = new TextFieldParser(csvPath))
            {
                //SET THIS NUMBER TO INDICATE WHAT ROW TO START PROCESSING EXCEL SHEET - 1 to start at begining. 
                int startCount = 1;

                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                bool first = true;

                string[] headers = null;

                int count = 1;
                

                while (!parser.EndOfData)
                {
                    //if (count > rowsToProcess) {

                    if (count == 1)
                    {
                        first = false;

                        headers = parser.ReadFields();

                    }

                    if (count < startCount)
                    {
                        parser.ReadFields();
                    }
                    else { 
                        if (count > rowsToProcess)
                        {
                            break;
                        }



                        string[] fields = parser.ReadFields();

                        string newHtml1 = replaceTagsInHtmlFromRow(html1, fields, headers, baseDirectory);
                        string newHtml2 = replaceTagsInHtmlFromRow(html2, fields, headers, baseDirectory);

                        string docName = fields[1].Replace(".pdf", "");

                        File.WriteAllText(baseDirectory + "filledin-" + docName + "-page1.html", newHtml1);
                        File.WriteAllText(baseDirectory + "filledin-" + docName + "-page2.html", newHtml2);

                        CreatePdfFromFilledInHTML(docName, 1, baseDirectory);
                        CreatePdfFromFilledInHTML(docName, 2, baseDirectory);

                        DocConverter.MergePdfs(baseDirectory + docName + "-page1.pdf", baseDirectory + docName + "-page2.pdf", baseDirectory + docName + ".pdf");

                        File.Delete(baseDirectory + docName + "-page1.pdf");
                        File.Delete(baseDirectory + docName + "-page2.pdf");

                        File.Delete(baseDirectory + "filledin-" + docName + "-page1.html");
                        File.Delete(baseDirectory + "filledin-" + docName + "-page2.html");

                    }
                    count++;
                }
            }
        }

        public static string replaceTagsInHtmlFromRow(string html, string[] fields, string[] headers, string baseDirectory) 
        {
            string toReturn = html;

            for (int i = 1; i < headers.Length; i++)
            {
                string tag = "//" + headers[i].Trim() + "//";
                string value = fields[i].Trim();

                if ("//ContactAddress//" == tag)
                {
                    
                }


                if ("//map_path//" == tag) {
                    //value = "C:\\Code\\pdf508\\template_508\\image\\hex-map.png"; //NEED TO REMOVE THIS LINE LATER!!

                    string firstPath = baseDirectory + "map_export_20230727\\";

                    value = value.Replace("folder/", "");
              


                    value = ImagePathToBase64String(firstPath+value);
                }

                if ("//County//" == tag || "//State//" == tag)
                {
                    value = value.Replace("'","<span class='europa-regular-sans-serif'>'</span>");
                }

                string tag2 = tag.Replace("//", "") + "_data";

                if (html.Contains("//"+tag2+"//"))
                {

                    toReturn = toReturn.Replace("//" + tag2 + "//", value);

                }

                

                if ("//ConditionalCounty//" == tag)
                {
                    if (value != "") {
                        value = " " + value;
                    }
                    
                }

                if ("//lu_pct_tc//" == tag
                || "//Annual Benefits//" == tag
                || "//Annual Benefits ($Custom)//" == tag
                || "//AB mil plus//" == tag
                || "//tc_net//" == tag
                || "//lu_pct_ag//" == tag
                || "//lu_pct_ot//" == tag
                || "//lu_pct_tg//" == tag
                || "//lu_pct_wt//" == tag
                || "//lu_pct_imp//" == tag
                || "//tc_pct_fore//" == tag
                || "//tc_pct_tcis//" == tag
                || "//tc_pct_tctg//" == tag
                || "//tc_pct_tcot//" == tag)
                {
                    value = value.Replace(".", "<span class='new-spirit-semiBold-serif'>.</span>");
                    value = value.Replace(",", "<span class='new-spirit-semiBold-serif'>,</span>");
                }

                if ("//tc_gain//" == tag || "//tc_loss//" == tag)
                {
                   
                    value = value.Replace("9", "<span class='europa-light-sans-serif'>9</span>");
                }



                //if ("//T1-2//" == tag || "//T2-2//" == tag)
                if ("//T1//" == tag || "//T2//" == tag)
                {

                    string tag3 = tag.Replace("//", "");

                    if (html.Contains("//" + tag3 + "-2//"))
                    {
                        toReturn = toReturn.Replace("//" + tag3 + "-2//", value.Replace("4", "<span class='new-spirit-semi-serif'>4</span>"));
                        toReturn = toReturn.Replace("//" + tag3 + "-2//", value.Replace("8", "<span class='new-spirit-semi-serif'>8</span>"));
                    }
                }


                if ("//LinkText//" == tag)
                {
                    string LinkTextValue = fields[i].Trim();
                    string ContactTextValue = fields[i-1].Trim();
                    string URLValue = fields[i-2].Trim();
                    string ContactAddressValue = fields[i-3].Trim();

                    tag = "//StateContantArea//";
                    //value = "<span class=\"page2-bottom-box-sub-text\">(<a href=\"mailto://ContactAddress//\">//ContactText//</a> and <a href=\"//URL//\">//LinkText//</a>)</span>";

                    if ((ContactTextValue != "" || ContactAddressValue != "") && (URLValue == "" || LinkTextValue == "")) 
                    {
                        value = "<span class=\"page2-bottom-box-sub-text\">(<a class=\"text-url\" href=\"mailto:" + ContactAddressValue + "\">"+ ContactTextValue + "</a>)</span>";
                    }
                    if ((ContactTextValue == "" || ContactAddressValue == "") && (URLValue != "" || LinkTextValue != ""))
                    {
                        value = "<span class=\"page2-bottom-box-sub-text\">(<a class=\"text-url\" href=\"" + URLValue +"\">"+ LinkTextValue + "</a>)</span>";
                    }
                    if ((ContactTextValue != "" || ContactAddressValue != "") && (URLValue != "" || LinkTextValue != ""))
                    {
                        value = "<span class=\"page2-bottom-box-sub-text\">(<a class=\"text-url\" href=\"mailto:" + ContactAddressValue + "\">" + ContactTextValue + "</a>, <a class=\"text-url\" href=\"" + URLValue +"\">"+ LinkTextValue + "</a>)</span>";
                    }
                    if ((ContactTextValue == "" || ContactAddressValue == "") && (URLValue == "" || LinkTextValue == ""))
                    {
                        value = "";
                    }
                }

                

                toReturn = toReturn.Replace(tag, value);

            }

            return toReturn;

        }

        public static string ImagePathToBase64String(string path)
        {
            byte[] imageBytes = File.ReadAllBytes(path);
            string base64String = Convert.ToBase64String(imageBytes);
            return "data:image/png;base64,"+base64String;
        }   


        public static void CreatePdfFromFilledInHTML(string docName , int pageNum, string baseDirectory) {

            Console.WriteLine("Create PDF: "+ docName);

            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArguments("headless");

            using (var browser = new ChromeDriver(chromeOptions))
            {
                Console.WriteLine("browser rendering start");
                browser.Navigate().GoToUrl(baseDirectory+"filledin-" + docName + "-page" + pageNum + ".html");

                System.Threading.Thread.Sleep(10000);

                string evaluatedHtml = browser.PageSource;
                Console.WriteLine("browser rendering done");

                Console.WriteLine("Create new PDF: " + baseDirectory + docName + "-page" + pageNum + ".pdf");

                File.WriteAllBytes(baseDirectory + docName + "-page"+ pageNum + ".pdf", DocConverter.ConvertToPdfWithTags(evaluatedHtml, "docName"));
            }

            Console.WriteLine("pdf "+ docName + " done");
        }


    }


    public static class DocConverter
    {


        [Flags]
        public enum DocOptions
        {
            None = 0,
            DisplayTitle = 1,
            AddHeaderPageOne = 2,
            AddHeaderAllPages = 4,
            AddLineBottomEachPage = 8
        }


        public static byte[] ConvertToPdfWithTags(string html, string title)
        {


            ConverterProperties props = new ConverterProperties();

            DefaultTagWorkerFactory tagWorkerFactory = new AccessibilityTagWorkerFactory();
            props.SetTagWorkerFactory(tagWorkerFactory);

            using (var workStream = new MemoryStream())
            {
                using (var pdfWriter = new PdfWriter(workStream, new WriterProperties().AddUAXmpMetadata().SetPdfVersion
                    (PdfVersion.PDF_2_0).SetFullCompressionMode(true)))
                {

                    PdfDocument pdfDoc = new PdfDocument(pdfWriter);


                    PageSize pageSize = new PageSize(PageSize.LETTER);

                    pdfDoc.SetDefaultPageSize(pageSize);
                    pdfDoc.GetCatalog().SetLang(new PdfString("en-us"));


                    pdfDoc.GetCatalog().SetViewerPreferences(new PdfViewerPreferences().SetDisplayDocTitle(true));

                    var pdfMetaData = pdfDoc.GetDocumentInfo();
                    pdfMetaData.AddCreationDate();
                    pdfMetaData.GetProducer();
                    pdfMetaData.SetCreator("iText Software");

                    pdfDoc.SetTagged();



                    using (var document = HtmlConverter.ConvertToDocument(html, pdfDoc, props))
                    {

                        document.SetMargins(0, 0, 0, 0);
                    }

                    return workStream.ToArray();

                }


            }
        }

        public static byte[] MergePdfs(string path1, string path2, string outPath3)
        {
            using (var workStream = new MemoryStream())
            {
                using (var pdfWriter = new PdfWriter(workStream, new WriterProperties().AddUAXmpMetadata().SetPdfVersion
                    (PdfVersion.PDF_2_0).SetFullCompressionMode(true)))
                {

                    PdfDocument pdf = new PdfDocument(new PdfWriter(outPath3));

                    pdf.GetCatalog().SetLang(new PdfString("en-us"));


                    pdf.GetCatalog().SetViewerPreferences(new PdfViewerPreferences().SetDisplayDocTitle(true));
                    //This event handler used for adding background images.  Also where I've tried setting the tab order on pdfPage
                    //if (documentOptions > 0)
                    //    pdfDoc.AddEventHandler(PdfDocumentEvent.END_PAGE, new PublicReportHeaderFooter(documentOptions, title));
                    //Set meta tags
                    var pdfMetaData = pdf.GetDocumentInfo();
                   // pdfMetaData.AddCreationDate();
                    pdfMetaData.GetProducer();

                    pdfMetaData.SetTitle("Tree Cover Status and Change");
                    pdfMetaData.SetAuthor("Chesapeake Bay Program, University of Vermont, Chesapeake Conservancy, USGS, United States Department of Agriculture, Forest Service, Eastern Region State and Private Forestry.");
                    pdfMetaData.SetSubject("Tree Cover status and change facts");
                    pdfMetaData.SetKeywords("tree cover, chesapeake bay, usgs, data change, tree cover change, developed lands, land use, land cover statistics.");
                    
                    //Set the document to be tagged
                    pdf.SetTagged();

                    PdfMerger merger = new PdfMerger(pdf);

                    //Add pages from the first document
                    PdfDocument firstSourcePdf = new PdfDocument(new PdfReader(path1));
                    merger.Merge(firstSourcePdf, 1, firstSourcePdf.GetNumberOfPages());

                    //Add pages from the second pdf document
                    PdfDocument secondSourcePdf = new PdfDocument(new PdfReader(path2));
                    merger.Merge(secondSourcePdf, 1, secondSourcePdf.GetNumberOfPages());

                    firstSourcePdf.Close();
                    secondSourcePdf.Close();


                    var rootPointer = pdf.GetTagStructureContext().GetAutoTaggingPointer();

                    rootPointer.MoveToRoot();

                    TagTreePointer pointer = new TagTreePointer(rootPointer);

                    // var test = pointer.GetKidsRoles();

                     GetChildrenMoveToRoot(pointer, rootPointer);

                    TagTreePointer pointer2 = new TagTreePointer(rootPointer);
                     RemoveEmptyTags(pointer2, rootPointer);


                    Document document = new Document(pdf, PageSize.LETTER);


                    var bottomMargin = document.GetBottomMargin();
                    var topMargin = document.GetTopMargin();
                    var rightMargin = document.GetRightMargin();
                    var leftMargin = document.GetLeftMargin();


                    TagTreePointer pointer3 = new TagTreePointer(rootPointer);
                   LoopTags(pointer3, rootPointer);


                    pdf.Close();

                    //Returns the written-to MemoryStream containing the PDF.   
                    return workStream.ToArray();

                }
            }
        }

        public static bool GetChildrenMoveToRoot(TagTreePointer pointer, TagTreePointer rootPointer) {

            var inRoot = rootPointer.GetKidsRoles();
            var kidsHere = pointer.GetKidsRoles();
            var currentRole = pointer.GetRole();

            if (pointer.GetRole() != "Div" && pointer.GetRole() != "Document")
            {

                pointer.Relocate(rootPointer);
                return true;
            }
            else {
                int kids = pointer.GetKidsRoles().Count;

                if (kids == 0) {
                    var currentRole3 = pointer.GetRole();
                }

                for (int i = 0; i < kids; i++)
                {


                    TagTreePointer pointer2 = new TagTreePointer(pointer);

                    var kidsHere2 = pointer2.GetKidsRoles();
                    var currentRole2 = pointer2.GetRole();


                    pointer2.MoveToKid(i);
                    bool relocated = GetChildrenMoveToRoot(pointer2, rootPointer);

                    if (relocated) {
                        kids--;
                        i--;
                    }

                    // pointer2.RemoveTag();
                }

                

            }
            return false;
        }

        public static void RemoveEmptyTags(TagTreePointer pointer, TagTreePointer rootPointer)
        {

            var inRoot = rootPointer.GetKidsRoles();
            var kidsHere = pointer.GetKidsRoles();

            TagTreePointer pointer2 = new TagTreePointer(pointer.MoveToKid(0));

            while (pointer2.GetRole() == "Div")
            {
                pointer2.RemoveTag();
                       
                var inRoot2 = rootPointer.GetKidsRoles();

                pointer2.MoveToKid(0);
    
            }
        }

        public static void LoopTags(TagTreePointer pointer, TagTreePointer rootPointer)
        {

            var inRoot = rootPointer.GetKidsRoles();
            var kidsHere = pointer.GetKidsRoles();

            //TagTreePointer pointer2 = new TagTreePointer(pointer.MoveToKid(0));

            int totalChildren = kidsHere.Count;
   

            for (int i = 0; i < totalChildren; i++)
            {
                TagTreePointer pointer2 = new TagTreePointer(rootPointer);
                pointer2.MoveToKid(i);

                if (i==0) {
                    pointer2.SetRole("H1");
                }
            }

        }

    }



    public class AccessibilityTagWorkerFactory: DefaultTagWorkerFactory
    {
        IElementNode root = null;
       
    public override ITagWorker GetCustomTagWorker(IElementNode tag, ProcessorContext context)
    {
            //This can probably replaced with a regex or string pattern'

            if (root == null) {
                root = tag;
                Console.WriteLine("root: " + root.Name());
            }

            
            //if (tag.Name().Equals("Div"))//Division


            if (tag.Name() =="test")//Division
            {

                return null;

                //return new SpanTagWorker(tag, context); ;
            }
            else { 
                return base.GetCustomTagWorker(tag, context); 
            }
      

        return base.GetCustomTagWorker(tag, context); 
    }
}
    
}