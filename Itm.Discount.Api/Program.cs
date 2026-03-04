using Itm.Discount.Api.Dtos;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

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