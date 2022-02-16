var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.MapGet("/", (HttpRequest request) => {

    return request; //.HttpContext; //.ToString();

    // var data = new EchoData {
    //     IPv4Address = request.HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString()
    // };

    // return data;
})
.WithName("Echo");

app.Run();

public class EchoData {
    public string? IPv4Address { get; set; }
}