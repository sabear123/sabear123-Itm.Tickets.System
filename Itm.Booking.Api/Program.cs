using Itm.Booking.Api.Dtos;
using System.Net.Http.Json;

var builder = WebApplication.CreateBuilder(args);

// Discount
builder.Services.AddHttpClient("DiscountClient", c =>
{
    c.BaseAddress = new Uri("http://localhost:5131/");
})
.AddStandardResilienceHandler();

// Event
builder.Services.AddHttpClient("EventClient", c =>
{
    c.BaseAddress = new Uri("http://localhost:5194/");
})
.AddStandardResilienceHandler();

var app = builder.Build();

app.MapPost("/api/bookings", async (BookingRequest request, IHttpClientFactory factory) =>
{

    if (request is null)
        return Results.BadRequest("La solicitud no puede estar vacía.");

    if (request.EventId <= 0)
        return Results.BadRequest("El EventId debe ser un número entero mayor que cero.");

    if (request.Tickets <= 0)
        return Results.BadRequest("La cantidad de tickets debe ser mayor que cero.");

    var eventClient = factory.CreateClient("EventClient");
    var discountClient = factory.CreateClient("DiscountClient");

    // Consultas en paralelo con manejo seguro
    var eventTask = SafeGetEventAsync(eventClient, request.EventId);
    var discountTask = SafeGetDiscountAsync(discountClient, request.DiscountCode);

    await Task.WhenAll(eventTask, discountTask);

    var evento = eventTask.Result;
    var descuento = discountTask.Result;

    if (evento is null)
        return Results.NotFound("Evento no encontrado.");

    // Reserva (SAGA Paso 1)
    var reserveResponse = await eventClient.PostAsJsonAsync("/api/events/reserve",
        new { EventId = request.EventId, Quantity = request.Tickets });

    if (!reserveResponse.IsSuccessStatusCode)
        return Results.BadRequest("No hay sillas suficientes.");

    try
    {
        // Simulación de pago
        bool paymentSuccess = new Random().Next(1, 10) > 5;
        if (!paymentSuccess)
        {
            Console.WriteLine("[BOOLEAN PAYMENT] ---> False");
        } else
        {
            Console.WriteLine("[BOOLEAN PAYMENT] ---> True");
        }
        if (!paymentSuccess)
            throw new Exception("Fondos insuficientes en la tarjeta.");

        // Cálculo total
        var total = evento.PrecioBase * request.Tickets;
        var descuentoAplicado = descuento is not null ? total * descuento.Porcentaje : 0;
        var totalPagado = total - descuentoAplicado;

        return Results.Ok(new
        {
            Status = "Éxito",
            Message = $"¡Disfruta el concierto {evento.Nombre}!",
            PrecioBase = evento.PrecioBase,
            CantidadTickets = request.Tickets,
            TotalSinDescuento = evento.PrecioBase * request.Tickets,
            PorcentajeDeDescuento = descuento is not null ? descuento.Porcentaje * 100 : 0,
            DescuentoAplicado = descuentoAplicado,
            TotalPagado = totalPagado
        });
    }
    catch (Exception ex)
    {
        // Compensación (SAGA Paso 2)
        Console.WriteLine($"[SAGA] Error en pago: {ex.Message}. Liberando sillas...");
        await eventClient.PostAsJsonAsync("/api/events/release",
            new { EventId = request.EventId, Quantity = request.Tickets });

        return Results.Problem("El pago falló, las sillas fueron liberadas.");
    }
});

app.Run();

// Método auxiliar que maneja 404 sin romper WhenAll
static async Task<DiscountDto?> SafeGetDiscountAsync(HttpClient client, string code)
{
    if (string.IsNullOrWhiteSpace(code))
    {
        Console.WriteLine("[INFO] No se proporcionó código de descuento.");
        return null;
    }
    try
    {
        var response = await client.GetAsync($"/api/discounts/{code}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Console.WriteLine($"[INFO] Código de descuento no existe: {code}.");
            return null;
        }

        response.EnsureSuccessStatusCode();
        Console.WriteLine($"[INFO] Descuento encontrado: {code}.");
        return await response.Content.ReadFromJsonAsync<DiscountDto>();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[WARN] Error al consultar descuento: {ex.Message}");
        return null;
    }
}
static async Task<EventDto?> SafeGetEventAsync(HttpClient client, int eventId)
{
    try
    {
        var response = await client.GetAsync($"/api/events/{eventId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Console.WriteLine($"[INFO] Evento con ID {eventId} no encontrado.");
            return null;
        }
        Console.WriteLine($"[INFO] Evento con ID {eventId} encontrado.");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EventDto>();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[WARN] Error al consultar evento: {ex.Message}");
        return null;
    }
}
