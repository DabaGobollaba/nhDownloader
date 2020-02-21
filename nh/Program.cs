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

                // Search for doujin and print out info
                GalleryElement doujin;
                try
                {
                    doujin = await SearchClient.SearchByIdAsync(id);
                }
                catch (Exception)
                {
                    Console.WriteLine("Error finding doujin...\n");
                    continue;
                }

                PrintDoujinInfo(doujin);
                Console.WriteLine();

                // Get filename
                Console.Write("Enter a filename (optional): ");
                string filename = Console.ReadLine();

                if (filename == string.Empty)
                {
                    filename = id.ToString();
                }
                if (System.IO.Path.GetExtension(filename) != ".pdf")
                {
                    filename += ".pdf";
                }

                // Download and save
                CreatePdf(doujin, filename);

                Console.WriteLine("Done!\n");
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

        static void CreatePdf(GalleryElement doujin, string filename)
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
            var imgData = new List<ImageData>();

            using (var progress = new ProgressBar())
            {
                Console.WriteLine("Downloading... ");
                for (int i = 0; i < imgUrls.Count; i++)
                {
                    imgData.Add(ImageDataFactory.Create(imgUrls[i]));
                    progress.Report((double)i / imgUrls.Count);
                }
                Console.WriteLine("Download finished!");
                progress.Dispose();
            }

            // loop through and add images to document
            Image pdfImg;
            Console.WriteLine("Saving... ");
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
            Console.WriteLine("Saved successfully!");
        }

        static void CreatePdfNoVerbose(GalleryElement doujin, string filename)
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
