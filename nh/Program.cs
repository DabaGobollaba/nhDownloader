using System;
using System.Threading.Tasks;
using NHentaiSharp.Core;
using NHentaiSharp.Search;
using System.Linq;

using iText.IO.Image;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using System.IO;
using System.Collections.Generic;

namespace nh
{
    class Program
    {
        static async Task Main(string[] args)
        {
            while (true)
            {
                Console.Write("Enter an id: ");

                string strInput = Console.ReadLine();
                int id;

                if (!int.TryParse(strInput, out id))
                {
                    Console.WriteLine("Id was not parsable to an int! Try again... \n");
                    continue;
                }

                var doujin = await SearchClient.SearchByIdAsync(id);
                PrintDoujinInfo(doujin);
                Console.WriteLine();

                Console.Write("Enter a filename: ");
                string filename = Console.ReadLine();

                if (System.IO.Path.GetExtension(filename) != ".pdf")
                {
                    filename += ".pdf";
                }

                Console.WriteLine("Saving...");
                CreatePDF(doujin, filename);
                Console.WriteLine("Success!\n");
            }
        }

        static void PrintDoujinInfo(GalleryElement doujin)
        {
            Console.WriteLine("Id:     " + doujin.id);
            Console.WriteLine("Title:  " + doujin.englishTitle);
            Console.WriteLine("Tags:   " + string.Join(", ", doujin.tags.Select(x => x.name)));
            Console.WriteLine("Date:   " + doujin.uploadDate);
            Console.WriteLine("Pages:  " + doujin.numPages);
        }

        static void CreatePDF(GalleryElement doujin, string filename)
        {
            // set pdf path/name
            var file = new FileInfo(filename);

            // open file path
            var pdfWriter = new PdfWriter(file);
            var pdfDocument = new PdfDocument(pdfWriter);

            // create document
            var document = new Document(pdfDocument);


            // get all images from web
            var imgUrls = doujin.pages.Select(p => p.imageUrl.ToString()).ToList();
            var imgData = imgUrls.Select(url => ImageDataFactory.Create(url)).ToList();

            // loop through and add images to document
            Image pdfImg;
            for (int i = 0; i < imgData.Count; i++)
            {
                // convert current image
                pdfImg = new Image(imgData[i]);

                // add a new page to the pdf
                pdfDocument.AddNewPage(new PageSize(pdfImg.GetImageWidth(), pdfImg.GetImageHeight()));

                // set image position
                pdfImg.SetFixedPosition(i + 1, 0, 0);

                // add current image to document
                document.Add(pdfImg);
            }

            document.Close();
        }
    }
}
