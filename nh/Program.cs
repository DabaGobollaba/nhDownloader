using System;
using System.Threading.Tasks;
using NHentaiSharp.Core;
using NHentaiSharp.Search;
using System.Linq;
using System.Text.RegularExpressions;

using iText.IO.Image;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using System.IO;
using System.Collections.Generic;
using NHentaiSharp.Exception;

namespace nh
{
    class Program
    {
        private static readonly Random _rnd = new Random();

        static async Task Main(string[] args)
        {
            string[] lastTags = null;
            while (true)
            {
                Console.Write("Enter an id or command: ");

                string strInput = Console.ReadLine();

                Regex commaSeparatedNumbers = new Regex("^([0-9]{1,6},[0-9]{1,6}(,[0-9]{1,6})*)+$");

                // if list of ids
                if (commaSeparatedNumbers.IsMatch(strInput))
                {
                    var ids = strInput.Split(",");
                    var doujins = new List<GalleryElement>();
                    foreach (var id in ids)
                    {
                        try
                        {
                            doujins.Add(await SearchClient.SearchByIdAsync(int.Parse(id)));
                        }
                        catch (Exception)
                        {
                            Console.WriteLine(id + " not found.");
                        }
                    }
                    await Multiple(doujins);
                }
                else if ((strInput == string.Empty) && (lastTags != null))
                {
                    try
                    {
                        await GetRandomWithTags(lastTags);
                    }
                    catch (InvalidArgumentException)
                    {
                        continue;
                    }
                }
                else if (strInput == "random")
                {
                    try
                    {
                        await GetRandom();
                        lastTags = new string[] { "languages:english" };
                    }
                    catch (InvalidArgumentException)
                    {
                        continue;
                    }
                }
                else if (strInput == "taglist")
                {
                    Console.Write("Tags: ");
                    strInput = Console.ReadLine();
                    var tags = (strInput + ",languages:english").Split(",");
                    try
                    {
                        await GetRandomWithTags(tags);
                        lastTags = tags;
                    }
                    catch (InvalidArgumentException)
                    {
                        continue;
                    }
                }
                // if no list of ids
                else
                {
                    int id;
                    if (!int.TryParse(strInput, out id))
                    {
                        Console.WriteLine("Id was not parsable to an int! Try again... \n");
                        continue;
                    }

                    try
                    {
                        await GetDoujin(id);
                    }
                    catch (InvalidArgumentException)
                    {
                        continue;
                    }
                }

                Console.WriteLine("Done!\n");
            }
        }

        private static async Task GetRandom()
        {
            string language = "english";
            var languageTag = SearchClient.GetCategoryTag(language, TagType.Language);
            var tags = new string[] { languageTag };
            var result = await SearchClient.SearchWithTagsAsync(tags);

            //Console.WriteLine("Number of pages: " + result.numPages);
            //Console.WriteLine("Number per page: " + result.numPerPage);

            var lastPage = await SearchClient.SearchWithTagsAsync(tags, result.numPages);
            var lastPageLength = lastPage.elements.Length;
            //Console.WriteLine("LastPageLength: " + lastPageLength);

            int totalDoujins = ((result.numPages - 1) * result.numPerPage) + lastPageLength;
            Console.WriteLine("Total found: " + totalDoujins);
            Console.WriteLine();

            int i = _rnd.Next(1, totalDoujins + 1);

            var temp = await SearchClient.SearchWithTagsAsync(tags, (i / 25) + 1);

            int id = (int)temp.elements[i % 25].id;

            await GetDoujin(id);
        }

        private static async Task GetRandomWithTags(string[] tags)
        {
            var result = await SearchClient.SearchWithTagsAsync(tags);

            //Console.WriteLine("Number of pages: " + result.numPages);
            //Console.WriteLine("Number per page: " + result.numPerPage);

            var lastPage = await SearchClient.SearchWithTagsAsync(tags);
            var lastPageLength = lastPage.elements.Length;
            //Console.WriteLine("LastPageLength: " + lastPageLength);

            int totalDoujins = ((result.numPages - 1) * result.numPerPage) + lastPageLength;
            Console.WriteLine("Total found: " + totalDoujins);
            Console.WriteLine();

            int i = _rnd.Next(1, totalDoujins + 1);

            var temp = await SearchClient.SearchWithTagsAsync(tags);

            int id = (int)temp.elements[i % 25].id;

            await GetDoujin(id);
        }

        private static async Task GetDoujin(int id)
        {
            // Search for doujin and print out info
            GalleryElement doujin;
            try
            {
                doujin = await SearchClient.SearchByIdAsync(id);
            }
            catch (Exception)
            {
                Console.WriteLine("Error finding doujin...\n");
                throw new InvalidArgumentException();
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
        }

        static void PrintDoujinInfo(GalleryElement doujin)
        {
            Console.WriteLine("Id:     " + doujin.id);
            Console.WriteLine("Title:  " + doujin.englishTitle);
            Console.WriteLine("Tags:   " + string.Join(", ", doujin.tags.Select(x => x.name)));
            Console.WriteLine("Date:   " + doujin.uploadDate);
            Console.WriteLine("Pages:  " + doujin.numPages);
        }


        static async Task Multiple(IEnumerable<GalleryElement> doujins)
        {
            //var tasks = new List<Task>();
            //foreach (var doujin in doujins)
            //{
            //    tasks.Add(CreatePdfAsync(doujin, doujin.id.ToString() + ".pdf"));
            //}
            //Task.WaitAll(tasks.ToArray());

            Parallel.ForEach(doujins, (doujin) => 
            {
                CreatePdfSilent(doujin, doujin.id.ToString() + ".pdf");
            });
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
                Console.WriteLine("\nDownload finished!");
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

        static void CreatePdfSilent(GalleryElement doujin, string filename)
        {
            // set pdf path/name
            var file = new FileInfo(filename);

            // open file path
            var pdfWriter = new PdfWriter(file);
            var pdfDocument = new PdfDocument(pdfWriter);

            // create document
            var document = new Document(pdfDocument);


            // get all images from web
            Console.WriteLine("Downloading " + doujin.id + "... ");
            var imgUrls = doujin.pages.Select(p => p.imageUrl.ToString()).ToList();
            var imgData = imgUrls.Select(url => ImageDataFactory.Create(url)).ToList();
            Console.WriteLine("Finished downloading " + doujin.id + "!");

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
