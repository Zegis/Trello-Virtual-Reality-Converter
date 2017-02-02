using RestSharp;
using System;
using System.Text;
using System.Drawing;
using Newtonsoft.Json;
using System.Collections.Generic;
using QRCoder;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using System.IO;

namespace TVRC_console
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseUrl = "https://api.trello.com/1/";
            string key;
            string token;
            bool generateQr = false;

            using (StreamReader file = new StreamReader("Config.txt"))
            {
                key = file.ReadLine();
                token = file.ReadLine();
            }

            StringBuilder url = new StringBuilder("members/zegis/boards?");
            url.Append("fields=id,name");
            url.Append("&key=");
            url.Append(key);
            url.Append("&token=");
            url.Append(token);

            RestClient client = new RestClient(baseUrl);
            RestRequest request = new RestRequest(url.ToString());
            
            var response = client.Execute(request);

            List<BoardSimple> boards = JsonConvert.DeserializeObject<List<BoardSimple>>(response.Content);

            int i = 0;
            foreach(var board in boards)
            {
                Console.WriteLine(i++ + ". " + board.name);
            }
            Console.WriteLine("Type number of board you want to convert: ");
            int boardIndex = Convert.ToInt32(Console.ReadLine());

            Console.WriteLine("You chose: " + boards[boardIndex].name);

            url.Clear();
            url.Append("/boards/");
            url.Append(boards[boardIndex].id);
            url.Append("/lists?fields=name");
            url.Append("&key=");
            url.Append(key);
            url.Append("&token=");
            url.Append(token);
            
            request.Resource = url.ToString();
            response = client.Execute(request);

            List<ListSimple> lists = JsonConvert.DeserializeObject<List<ListSimple>>(response.Content);
            i = 0;
            Console.WriteLine("Choose list: ");
            foreach(var list in lists)
            {
                Console.WriteLine(i++ + "." + list.name + " " + list.id);
            }
            
            int listIndex = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("You chose: " + lists[listIndex].name);

            url.Clear();
            url.Append("/lists/");
            url.Append(lists[listIndex].id);
            url.Append("/cards?fields=name,url");
            url.Append("&key=");
            url.Append(key);
            url.Append("&token=");
            url.Append(token);

            request.Resource = url.ToString();
            response = client.Execute(request);

            List<CardSimple> cards = JsonConvert.DeserializeObject<List<CardSimple>>(response.Content);
            Console.WriteLine();
            foreach(var card in cards)
            {
                Console.WriteLine(card.name);
            }

            List<Bitmap> qrCodes = new List<Bitmap>();
            if (generateQr)
            {
                for(int j=0; j < cards.Count; ++j)
                {
                    QRCodeGenerator qrGenerator = new QRCodeGenerator();
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(cards[j].url, QRCodeGenerator.ECCLevel.Q);
                    QRCode qrCode = new QRCode(qrCodeData);
                    //Bitmap qrCodeImage = qrCode.GetGraphic(20);
                    qrCodes.Add(qrCode.GetGraphic(20));

                    //qrCodeImage.Save("output/" + cards[j].name + ".jpg");
                }
            }

            PdfDocument document = new PdfDocument();
            document.Info.Title = "Cards from" + boards[boardIndex].name;

            PdfPage page;

            
            XFont font = new XFont("Verdana", 18, XFontStyle.Regular);
            XStringFormat format = new XStringFormat();
            XPen pen = new XPen(XColors.Black, 2);
            XImage img;

            int y = 0;
            int height = 120;

            page = document.AddPage();
            XGraphics gfx = XGraphics.FromPdfPage(page);
            XTextFormatter tf = new XTextFormatter(gfx);

           // cards.RemoveAt(0);
            
            for (int j = 0; j < cards.Count-1; j+=2)
            {
                gfx.DrawRectangle(pen, 0, y * height, page.Width / 2, height);
                gfx.DrawRectangle(pen, page.Width / 2, y * height, page.Width /2, height);
                if(generateQr)
                {
                    img = XImage.FromGdiPlusImage(qrCodes[j]);
                    gfx.DrawImage(img, 2, 2 + y * height, 160, 160);
                }
                tf.DrawString(cards[j].name, font, XBrushes.Black, new XRect(10, 15 + y*height, page.Width /2 - 10, height), XStringFormats.TopLeft);
                if(j+1 < cards.Count)
                    tf.DrawString(cards[j+1].name, font, XBrushes.Black, new XRect(page.Width / 2 + 10 , 15 + y * height, page.Width / 2 - 10, height), XStringFormats.TopLeft);

                if (y < 5)
                {
                    ++y;
                }
                else
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    tf = new XTextFormatter(gfx);
                    y = 0;
                }

            }

            //    gfx.DrawRectangle(pen, 0, 0, page.Width, 165);

            //gfx.DrawRectangle(pen, 0, 165, (page.Width), 165);

            //gfx.DrawRectangle(pen, 0, 330, (page.Width), 165);

            //gfx.DrawRectangle(pen, 0, 495, (page.Width), 165);

            //gfx.DrawRectangle(pen, 0, 660, (page.Width), 165);

            //XImage img = XImage.FromFile("output/Duchy ognia.jpg");
            //gfx.DrawImage(img, 2, 2, 160, 160);            

            //tf.DrawString(cards[2].name, font, XBrushes.Black, new XRect(180, 15, page.Width - 180, 190), XStringFormats.TopLeft);

            document.Save("Cards.pdf");
            Console.WriteLine("All green");
            Console.ReadKey();
        }
    }
}
