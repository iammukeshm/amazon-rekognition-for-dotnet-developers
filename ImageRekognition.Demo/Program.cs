using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using SixLabors.ImageSharp.Formats.Png;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonRekognition>();
var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/detect-labels", async (IFormFile file, IAmazonRekognition client) =>
{
    var memStream = new MemoryStream();
    file.CopyTo(memStream);
    var response = await client.DetectLabelsAsync(new DetectLabelsRequest()
    {
        Image = new Amazon.Rekognition.Model.Image()
        {
            Bytes = memStream
        },
        MinConfidence = 95
    });
    Console.WriteLine(JsonSerializer.Serialize(response));
    var labels = new List<string>();
    foreach (var label in response.Labels)
    {
        labels.Add(label.Name);
    }
    return Results.Ok(labels);
}).DisableAntiforgery();

app.MapPost("/moderate", async (IFormFile file, IAmazonRekognition client) =>
{
    var memStream = new MemoryStream();
    file.CopyTo(memStream);
    var response = await client.DetectModerationLabelsAsync(new DetectModerationLabelsRequest()
    {
        Image = new Amazon.Rekognition.Model.Image()
        {
            Bytes = memStream
        },
        MinConfidence = 90
    });
    Console.WriteLine(JsonSerializer.Serialize(response));
    var labels = new List<string>();
    if (response.ModerationLabels.Count > 0) return Results.Ok("unsafe");
    else return Results.Ok("safe");
}).DisableAntiforgery();

app.MapPost("/blur-faces", async (IFormFile file, IAmazonRekognition client) =>
{
    var memStream = new MemoryStream();
    file.CopyTo(memStream);
    var response = await client.DetectFacesAsync(new DetectFacesRequest()
    {
        Image = new Amazon.Rekognition.Model.Image()
        {
            Bytes = memStream
        }
    });
    if (response.FaceDetails.Count > 0)
    {
        var image = SixLabors.ImageSharp.Image.Load(file.OpenReadStream());
        foreach (var face in response.FaceDetails)
        {
            var rectangle = new Rectangle()
            {
                Width = (int)(image.Width * face.BoundingBox.Width),
                Height = (int)(image.Height * face.BoundingBox.Height),
                X = (int)(image.Width * face.BoundingBox.Left),
                Y = (int)(image.Height * face.BoundingBox.Top)
            };
            image.Mutate(ctx => ctx.BoxBlur(30, rectangle));
            memStream.Position = 0;
        }
        image.Save(file.FileName, new PngEncoder());
    }
}).DisableAntiforgery();

app.Run();