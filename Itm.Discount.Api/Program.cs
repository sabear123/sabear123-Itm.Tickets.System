using Itm.Discount.Api.Dtos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var discountDB = new List<DiscountDto>
{
    new DiscountDto("ITM50", 0.5m)
};

app.MapGet("/api/discounts/{codigo}", (string codigo) =>
{
    var discount = discountDB.FirstOrDefault(d => d.Codigo == codigo);
    return discount is not null ? Results.Ok(discount) : Results.NotFound();
});

app.Run();

record DiscountDto(string Codigo, decimal Porcentaje);