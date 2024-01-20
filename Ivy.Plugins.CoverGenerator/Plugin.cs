using System.Diagnostics;
using Ivy.Common.Models;
using Ivy.Plugins.Abstract;
using Microsoft.Extensions.DependencyInjection;
using OpenAI_API;
using OpenAI_API.Images;
using OpenAI_API.Models;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;

namespace Ivy.Plugins.CoverGenerator;

public class Plugin : IPlugin
{
    private IPluginHost? _host;

    public void Initialize(IPluginHost host, IServiceCollection serviceCollection)
    {
        _host = host;

        host.AddMenuItem("Cover Generator", "Generate Cover...", GenerateCover);
    }

    private async Task<string> GenerateCoverDescription(Book book)
    {
        var api = new OpenAIAPI("XXX");
        
        var descriptionPrompt =
            $"Please describe the visual imagery of the book {book.Title} by {book.Author} in a format suitable for use in a DALL-E prompt.";

        var chat = api.Chat.CreateConversation();
        chat.Model = Model.GPT4;
        chat.RequestParameters.Temperature = 0;
        
        chat.AppendSystemMessage("You are a writer who is describing the visual imagery of a book in a format suitable for use in a DALL-E prompt. Please limit your response to 500 characters.");
        chat.AppendUserInput($"Please describe the visual imagery of the book {book.Title} by {book.Author} in a format suitable for use in a DALL-E prompt.");
        
        var response = await chat.GetResponseFromChatbotAsync();
        return response;
    }

    private Font GetFontFittingWidth(string text, int targetWidth, float initialFontSize)
    {
        var font = SystemFonts.CreateFont("Arial", initialFontSize);

        var size = TextMeasurer.MeasureSize(text, new TextOptions(font));
        while (Math.Abs(size.Width - targetWidth) > 0.1f)
        {
            var scaleFactor = targetWidth / size.Width;
            font = SystemFonts.CreateFont("Arial", font.Size * scaleFactor);
            size = TextMeasurer.MeasureSize(text, new TextOptions(font));

            if (Math.Abs(size.Width - targetWidth) < 1)
                break;
        }

        return font;
    }
    
    private async Task GenerateCover()
    {
        Debug.Assert(_host is not null);
        
        var api = new OpenAIAPI("XXX");
        
        var firstSelectedBook = _host.SelectedBooks.FirstOrDefault();
        
        if (firstSelectedBook is null)
            return;

        var coverImageDescription = await GenerateCoverDescription(firstSelectedBook);
        
        var coverImagePrompt =
            $"Create a painting based on the following description: \"{coverImageDescription}\". DO NOT include any text. Highly stylized and abstract. Draw inspiration from Chip Kidd, Peter Mendelsund, Coralie Bickford-Smith, Jost Hochuli, Paula Scher, Irma Boom, David Pearson, Alvin Lustig, Massimo Vignelli, and Saul Bass. There must be no text or writing in the image, and the artwork should extend all the way to the edges of the image. Please avoid the generic, obnoxious \"AI style\" and favor a style that looks more like a real, physical painting. ABSOLUTELY NO TEXT OR WRITING!";

        Debug.WriteLine(coverImagePrompt);
        
        try
        {
            var result = await api.ImageGenerations.CreateImageAsync(new ImageGenerationRequest(coverImagePrompt, Model.DALLE3, ImageSize._1024x1792, "hd"));
            
            var imageUrl = result.Data[0].Url;
            
            var httpClient = new HttpClient();
            var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
            
            var image = Image.Load(imageBytes);
            
            image.Mutate(x => x.Crop(new Rectangle(0, 128, 1024, 1536)));
            
            var titleFont = GetFontFittingWidth(firstSelectedBook.Title, image.Width - 40, 12);
            var authorFont = GetFontFittingWidth(firstSelectedBook.Author, image.Width - 40, 12);
            
            image.Mutate(x => x.DrawText(firstSelectedBook.Title, titleFont, Color.White, new PointF(10, 10)));
            image.Mutate(x => x.DrawText(firstSelectedBook.Author, authorFont, Color.White, new PointF(10, image.Height - authorFont.Size - 10)));
            
            // save image to C:\Users\Kyle\Desktop\test.jpg
            
            image.Save($@"C:\Users\Kyle\Desktop\{firstSelectedBook.Title}.jpg");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}