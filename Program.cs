var builder = WebApplication.CreateBuilder(args);
// Set maximum receive message size (default 32KB)
builder.Services.AddSignalR(options => {
    options.MaximumReceiveMessageSize = null; // Unlimited
}); 

var app = builder.Build();
app.UseFileServer();
app.MapHub<ChatHub>("/hub");
app.Run();
