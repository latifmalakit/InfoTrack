using InfoTrack.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfoTrackApi(builder.Configuration);

var app = builder.Build();
app.UseInfoTrackApi();

app.Run();

public partial class Program;
